using UnityEngine;
using System;
using UnityEngine.Rendering;
using Unity.Mathematics;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine.Profiling;

public unsafe sealed class GPUDrivenRenderPipeline : RenderPipeline
{
    #region STATIC_AREA
    public static GPUDrivenRenderPipeline   renderpipeline { get; private set; }
    public static PipelineCommandData       data;
    #endregion

    RenderTexture                           gdepth;
    RenderTexture[]                         gbuffers =  new RenderTexture[4];
    RenderTargetIdentifier[]                gbufferID = new RenderTargetIdentifier[4];
    RenderTexture                           lightPassTex;

    /// <summary>
    /// 噪声图
    /// </summary>
    public Texture                          blueNoiseTex;

    /// <summary>
    // IBL 贴图
    /// </summary>
    public Cubemap                          diffuseIBL;
    public Cubemap                          specularIBL;
    public Texture                          brdfLut;

    /// <summary>
    /// 阴影管理
    /// </summary>
    public int                              shadowMapResolution = 1024;
    public float                            orthoDistance = 500.0f;
    public float                            lightSize = 2.0f;
    private CSM                             csm;
    public CSMSetting                       csmsetting;

    /// <summary>
    ///  阴影贴图
    /// </summary>
    RenderTexture[]                         shadowTextures = new RenderTexture[4];
    RenderTexture                           shadowMask;
    RenderTexture                           shadowStrength;

    /// <summary>
    /// 
    /// </summary>
    ClusterBasedLighting                    clusterlighting;

    public GPUDrivenRenderPipelineAsset     pipelineAsset;
    private struct PtrEqual : IFunction<UIntPtr, UIntPtr, bool>
    {
        public bool Run(ref UIntPtr a, ref UIntPtr b)
        {
            return a == b;
        }
    }

    private struct IntEqual : IFunction<int, int, bool>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Run(ref int a, ref int b)
        {
            return a == b;
        }
    }

    public static float3            sceneOffset { get; private set; }

    private static NativeDictionary<UIntPtr, int, PtrEqual> eventsGuideBook;
    public static T GetEvent<T>() where T : PipelineEvent
    {
        Type type = typeof(T);
        int value;
        if (eventsGuideBook.Get(new UIntPtr(MUnsafeUtility.GetManagedPtr(type)), out value))
        {
            return renderpipeline.pipelineAsset.availiableEvents[value] as T;
        }
        return null;
    }

    public static PipelineEvent GetEvent(Type type)
    {
        int value;
        if (eventsGuideBook.Get(new UIntPtr(MUnsafeUtility.GetManagedPtr(type)), out value))
        {
            return renderpipeline.pipelineAsset.availiableEvents[value];
        }
        return null;
    }


    private static NativeList<int>              waitReleaseRT;

    public static void ReleaseRT(int targetRT)
    {
        waitReleaseRT.Add(targetRT);
    }

    public GPUDrivenRenderPipeline( GPUDrivenRenderPipelineAsset resources )
    {
        ///---------------------------------------------------------------------------------
        QualitySettings.vSyncCount      = 0;
        Application.targetFrameRate     = 60;

        // 创建纹理
        gdepth                  = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0]             = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1]             = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2]             = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gbuffers[3]             = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        lightPassTex            = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        // 给纹理 ID 赋值
        for (int i = 0; i < 4; i++)
            gbufferID[i]        = gbuffers[i];

        shadowMask              = new RenderTexture(Screen.width / 4, Screen.height / 4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        shadowStrength          = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        for (int i = 0; i < 4; i++)
            shadowTextures[i]   = new RenderTexture(shadowMapResolution, shadowMapResolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

        csm                     = new CSM();
        csmsetting              = new CSMSetting();
        clusterlighting         = new ClusterBasedLighting();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        /// all events !!!!
        for (int i = 0; i < pipelineAsset.availiableEvents.Length; ++i)
        {
            pipelineAsset.availiableEvents[i].DisposeEvent();
        }
        for (int i = 0; i < pipelineAsset.availiableEvents.Length; ++i)
        {
            pipelineAsset.availiableEvents[i].DisposeDependEventsList();
        }

        foreach (var camPtr in PipelineCamera.CameraSearchDict)
        {
            PipelineCamera cam = MUnsafeUtility.GetObject<PipelineCamera>((void*)camPtr.value);
            if (cam.allDatas.isCreated)
            {
                foreach (var i in cam.allDatas)
                {
                    IPerCameraData data = ((IPerCameraData)MUnsafeUtility.GetHookedObject(i.value));
                    data.DisposeProperty();
                    MUnsafeUtility.RemoveHookedObject(i.value);
                }
                cam.allDatas.Dispose();
            }
        }
    }

    protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
    {
        // 主相机
        Camera camera           = cameras[0];

        // 全局变量设置
        Shader.SetGlobalFloat("_screenWidth",           Screen.width);
        Shader.SetGlobalFloat("_screenHeight",          Screen.height);
        Shader.SetGlobalTexture("_noiseTex",            blueNoiseTex);
        Shader.SetGlobalFloat("_noiseTexResolution",    blueNoiseTex.width);

        //  gbuffer 
        Shader.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
            Shader.SetGlobalTexture("_GT" + i, gbuffers[i]);

        // 设置相机矩阵
        Matrix4x4 viewMatrix    = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix    = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix      = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv   = vpMatrix.inverse;
        Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
        Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        // 设置 IBL 贴图
        Shader.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        Shader.SetGlobalTexture("_specularIBL", specularIBL);
        Shader.SetGlobalTexture("_brdfLut", brdfLut);

        // 设置 CSM 相关参数
        Shader.SetGlobalFloat("_orthoDistance",         orthoDistance);
        Shader.SetGlobalFloat("_shadowMapResolution",   shadowMapResolution);
        Shader.SetGlobalFloat("_lightSize",             lightSize);
        Shader.SetGlobalTexture("_shadowStrength",      shadowStrength);
        Shader.SetGlobalTexture("_shadoMask",           shadowMask);
        for (int i = 0; i < 4; i++)
        {
            Shader.SetGlobalTexture("_shadowtex" + i, shadowTextures[i]);
            Shader.SetGlobalFloat("_split" + i, csm.splts[i]);
        }

        // ------------------------ 管线各个 Pass ------------------------------------------------------------------
        ClusterLightingPass( renderContext, camera );

        ShadowCastingPass( renderContext, camera );

        GBufferPass( renderContext, camera );

        ShadowMapingPass( renderContext, camera );

        ShadingLightPass( renderContext, camera );

        // skybox and Gizmos
        renderContext.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos())
        {
            renderContext.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            renderContext.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // 提交绘制命令
        renderContext.Submit();
        // ------------------------- Pass end -----------------------------------------------------------------------
    }

    /// -------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// 灯光CBDR
    /// </summary>
    /// -------------------------------------------------------------------------------------------------------------
    void ClusterLightingPass( ScriptableRenderContext context, Camera camera)
    {
        camera.TryGetCullingParameters(out var cullingParameters);
        var Results         = context.Cull( ref cullingParameters );

        /// 更新光源
        clusterlighting.ClusterUpdateLightBuffer(Results.visibleLights.ToArray());

        // 划分 cluster
        clusterlighting.ClusterGenerate(camera);

        // 分配光源
        clusterlighting.ClusterAssignLight();

        // 传递参数
        clusterlighting.SetShaderParameters();
    }


    private void ShadowCastingPass( ScriptableRenderContext context, Camera camera )
    {
        Profiler.BeginSample("ShadowCastingPass");
        Light mainLight                         = RenderSettings.sun;
        Vector3  lightDir                       = mainLight.transform.rotation * Vector3.forward;

        // 分割阴影视锥体
        csm.Update(camera, lightDir);
        csmsetting.Set();
        csm.SaveMainCameraSettings(ref camera );

        for( int level = 0; level < 4; level++ )
        {
            // 相机移动到光源方向
            csm.ConfigCameraToShadowSpace( ref camera, lightDir, level, orthoDistance, shadowMapResolution);

            Matrix4x4 v                         = camera.worldToCameraMatrix;
            Matrix4x4 p                         = GL.GetGPUProjectionMatrix( camera.projectionMatrix, false );
            Shader.SetGlobalMatrix("_shadowVPMatrix", p * v);
            Shader.SetGlobalFloat("_orthoWidth" + level, csm.orthoWidths[level]);

            CommandBuffer cmd                   = new CommandBuffer();
            cmd.name                            = "ShadowPass" + level;

            // 绘制前准备
            context.SetupCameraProperties(camera);
            cmd.SetRenderTarget(shadowTextures[level]);
            cmd.ClearRenderTarget( true, true, Color.clear );
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // 剔除
            camera.TryGetCullingParameters( out var cullingParameters );
            var cullingResult                   = context.Cull(ref cullingParameters);
            ShaderTagId shaderTag               = new ShaderTagId("depthonly");
            SortingSettings sortingSetting      = new SortingSettings(camera);
            DrawingSettings drawingSetting      = new DrawingSettings(shaderTag, sortingSetting);
            FilteringSettings filteringSetting  = FilteringSettings.defaultValue;

            context.DrawRenderers(cullingResult, ref drawingSetting, ref filteringSetting);
            context.Submit();
        }
        csm.RevertMainCameraSettings(ref camera);
        Profiler.EndSample();
    }


    /// -------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// GBUFFER
    /// </summary>
    /// -------------------------------------------------------------------------------------------------------------
    void GBufferPass( ScriptableRenderContext context, Camera camera )
    {
        Profiler.BeginSample("GBuffer Pass");
        
        context.SetupCameraProperties(camera);
        CommandBuffer cmd                   = new CommandBuffer();
        cmd.name                            = "Gbuffer Pass";

        cmd.SetRenderTarget(gbufferID,      gdepth);
        cmd.ClearRenderTarget(true, true,   Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var result                          = context.Cull( ref cullingParameters );

        ShaderTagId shaderTag               = new ShaderTagId("gbuffer");
        SortingSettings sorting             = new SortingSettings( camera );
        DrawingSettings drawing             = new DrawingSettings( shaderTag, sorting );
        FilteringSettings filtering         = FilteringSettings.defaultValue;

        context.DrawRenderers(result, ref drawing, ref filtering);
        context.Submit();
        Profiler.EndSample();
    }

    // 阴影计算 pass : 输出阴影强度 texture
    void ShadowMapingPass( ScriptableRenderContext context, Camera camera )
    {
        CommandBuffer cmd                   = new CommandBuffer();
        cmd.name                            = "ShadowMapingPass";


        RenderTexture tempTex1              = RenderTexture.GetTemporary( Screen.width / 4, Screen.height / 4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear );
        RenderTexture tempTex2              = RenderTexture.GetTemporary( Screen.width / 4, Screen.height / 4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear );
        RenderTexture tempTex3              = RenderTexture.GetTemporary( Screen.width,     Screen.height,     0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear );

        if( csmsetting.usingShadowMask )
        {
            cmd.Blit(gbufferID[0], tempTex1, new Material(Shader.Find("preshadowmappingpass")));
            cmd.Blit( tempTex1,    tempTex2, new Material(Shader.Find("blurNx1")));
            cmd.Blit( tempTex2,    tempTex3, new Material(Shader.Find("blur1xN")));
        }

        // 生成阴影， 模糊阴影
        cmd.Blit(gbufferID[0],  tempTex3,       new Material(Shader.Find("shadowmappingpass")));
        cmd.Blit(tempTex3,      shadowStrength, new Material(Shader.Find("blurNxN")));

        RenderTexture.ReleaseTemporary(tempTex1);
        RenderTexture.ReleaseTemporary(tempTex2);
        RenderTexture.ReleaseTemporary(tempTex3);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
    void ShadingLightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd               = new CommandBuffer();
        cmd.name                        = "lightpass";

        Material mat                    = new Material(Shader.Find("lightpass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }


    void FinalPass( ScriptableRenderContext context, Camera camera )
    {
        CommandBuffer cmd               = new CommandBuffer();
        cmd.name                        = "FinalPass";

        Material material               = new Material( Shader.Find("finalpass") );
        cmd.Blit(lightPassTex, BuiltinRenderTextureType.CameraTarget, material);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }
}
