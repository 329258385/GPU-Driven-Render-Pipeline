using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using Unity.Mathematics;
using Unity.Jobs;
using static Unity.Mathematics.math;




[CreateAssetMenu(menuName ="GPURP Events/Geometry")]
public unsafe sealed class GeometryEvent : PipelineEvent
{
    public const bool       useHiZ = true;
    HizDepth                hizDepth;
    Material                linearDepth;
    Material                cluster;
    Material                linearDrawerMat;

    private PropertySetEvent    property;
    private NativeList_Int      gbufferCullResults;
    private JobHandle           cullHandle;

    private CommandBuffer       m_afterGeometryBuffer = null;
    private bool                needUpdateGeometryBuffer = false;

    public CommandBuffer afterGeometryBuffer
    {
        get
        {
            if (m_afterGeometryBuffer == null) m_afterGeometryBuffer = new CommandBuffer();
            
            needUpdateGeometryBuffer = true;
            return m_afterGeometryBuffer;
        }
    }

    protected override void Init(GPUDrivenRenderPipelineAsset resources)
    {
        linearDepth     = new Material( resources.shaders.linearDepthShader );
        linearDrawerMat = new Material( resources.shaders.linearDrawerShader);
        if (useHiZ)
        {
            hizDepth.InitHiz(resources);
            cluster     = new Material( resources.shaders.clusterRenderShader );
        }

        property        = GPUDrivenRenderPipeline.GetEvent<PropertySetEvent>();
    }

    public override bool CheckProperty()
    {
        if (useHiZ && Application.isPlaying)
        {
            return linearDepth && linearDrawerMat && cluster;
        }
        else
            return linearDepth && linearDrawerMat;
    }

    protected override void Dispose()
    {
        DestroyImmediate(linearDepth);
        DestroyImmediate(linearDrawerMat);
        if (useHiZ)
        {
            DestroyImmediate(cluster);
        }
        if (m_afterGeometryBuffer != null)
        {
            m_afterGeometryBuffer.Dispose();
            m_afterGeometryBuffer = null;
        }
        linearDepth = null;
    }

    public override void PreRenderFrame(PipelineCamera cam, ref PipelineCommandData data)
    {
        gbufferCullResults = new NativeList_Int( CustomDrawRequest.drawGBufferList.Length, Allocator.Temp );
        cullHandle  = new CustomRendererCullJob
        {
            cullResult      = gbufferCullResults,
            frustumPlanes   = (float4*)property.frustumPlanes.Ptr(),
            indexBuffer     = CustomDrawRequest.drawGBufferList
        }.Schedule( CustomDrawRequest.drawGBufferList.Length, max( 1, CustomDrawRequest.drawGBufferList.Length / 4 ));
    }


    public override void FrameUpdate(PipelineCamera cam, ref PipelineCommandData data)
    {
        CommandBuffer buffer = data.buffer;
        RenderClusterOptions options = new RenderClusterOptions
        {
            command         = buffer,
            frustumPlanes   = property.frustumPlanes,
            cullingShader   = data.resources.shaders.gpuFrustumCulling,
            terrainCompute  = data.resources.shaders.terrainCompute
        };

        FilteringSettings alphaTestFilter = new FilteringSettings
        {
            layerMask       = cam.cam.cullingMask,
            renderingLayerMask = 1,
            renderQueueRange = new RenderQueueRange(2450, 2499 )
        };

        FilteringSettings opaqueFilter = new FilteringSettings
        {
            layerMask       = cam.cam.cullingMask,
            renderingLayerMask = 1,
            renderQueueRange = new RenderQueueRange(2000, 2499)
        };

        DrawingSettings depthAlphaTestDrawSetting = new DrawingSettings(new ShaderTagId("Depth"),
                        new SortingSettings(cam.cam) { criteria = SortingCriteria.OptimizeStateChanges })
        {
            perObjectData = UnityEngine.Rendering.PerObjectData.None,
            enableDynamicBatching = true,
            enableInstancing = false
        };

        DrawingSettings depthOpaqueDrawSettings = new DrawingSettings(new ShaderTagId("Depth"),
                        new SortingSettings(cam.cam) { criteria = SortingCriteria.None })
        {
            perObjectData = UnityEngine.Rendering.PerObjectData.None,
            enableDynamicBatching = true,
            enableInstancing = false,
            overrideMaterial = property.overrideOpaqueMaterial,
            overrideMaterialPassIndex = 1
        };

        DrawingSettings drawSettings = new DrawingSettings(new ShaderTagId("GBuffer"), new SortingSettings(cam.cam) { criteria = SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges })
        {
            perObjectData = UnityEngine.Rendering.PerObjectData.Lightmaps,
            enableDynamicBatching = true,
            enableInstancing = false
        };

        /// draw depth prepass
        data.buffer.SetRenderTarget(ShaderIDs._DepthBufferTexture);
        data.buffer.ClearRenderTarget(true, false, Color.black);
        cullHandle.Complete();  // wait cull job completed

        var lst = CustomDrawRequest.allEvents;
        foreach (var i in gbufferCullResults)
        {
            lst[i].DrawDepthPrepass(buffer);
        }


        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// HIZ 
        HizOcclusionData hizOccData = null;
        GpuFrustumCullingPipeline.UpdateFrustumMinMaxPoint(buffer, cam.frustumMinPoint, cam.frustumMaxPoint);
        if( useHiZ )
        {
            HizOcclusionData.GetHizOcclusionData getter = new HizOcclusionData.GetHizOcclusionData
            {
                screenWidth = cam.cam.pixelWidth
            };
            hizOccData = IPerCameraData.GetProperty<HizOcclusionData, HizOcclusionData.GetHizOcclusionData>(cam, getter);
            hizOccData.UpdateWidth(cam.cam.pixelWidth);

            //SceneController.CullCluster_LastFrameDepthHiZ(ref options, hizOccData, cam);
            //buffer.DrawProceduralIndirect(Matrix4x4.identity, cluster, 2, MeshTopology.Triangles, 0);
        }

        RenderStateBlock depthBlock = new RenderStateBlock
        {
            depthState  = new DepthState(true, CompareFunction.Less ),
            mask        = RenderStateMask.Depth
        };

        /// render scene
        SceneController.RenderScene(ref data, ref opaqueFilter, ref depthOpaqueDrawSettings, ref property.cullResults, ref depthBlock);
        data.context.DrawRenderers(property.cullResults, ref depthAlphaTestDrawSetting, ref alphaTestFilter);

        /// draw gbuffer
        data.buffer.SetRenderTarget(colors: cam.targets.gbufferIdentifier, depth: ShaderIDs._DepthBufferTexture);
        data.buffer.ClearRenderTarget(false, true, Color.black);
        foreach (var i in gbufferCullResults)
        {
            lst[i].DrawGBuffer(buffer);
        }

        if (useHiZ)
        {
            buffer.SetGlobalBuffer(ShaderIDs._MaterialBuffer, data.resources.clustermeshs.vmManager.materialBuffer);
            buffer.SetGlobalBuffer(ShaderIDs._TriangleMaterialBuffer, SceneController.baseBuffer.triangleMaterialBuffer);
            buffer.SetGlobalTexture(ShaderIDs._GPURPMainTex, data.resources.clustermeshs.rgbaPool.rt);
            buffer.SetGlobalTexture(ShaderIDs._GPURPEmissionMap, data.resources.clustermeshs.emissionPool.rt);
            buffer.SetGlobalTexture(ShaderIDs._GPURPHeightMap, data.resources.clustermeshs.heightPool.rt);
            buffer.DrawProceduralIndirect(Matrix4x4.identity, cluster, 0, MeshTopology.Triangles, SceneController.baseBuffer.instanceCountBuffer, 0);
        }
        SceneController.RenderScene(ref data, ref opaqueFilter, ref drawSettings, ref property.cullResults);

        //Draw Recheck HIZ Occlusion
        //if (useHiZ && SceneController.gpurpEnabled)
        //{
        //    SceneController.DrawCluster_RecheckHiz(ref options, ref hizDepth, hizOccData, clusterMat, linearMat, cam);
        //}

        // draw depth
        data.buffer.Blit(ShaderIDs._DepthBufferTexture, ShaderIDs._CameraDepthTexture);
        if( needUpdateGeometryBuffer )
        {
            needUpdateGeometryBuffer = false;
            data.ExecuteCommandBuffer();
            data.context.ExecuteCommandBuffer(m_afterGeometryBuffer);
            m_afterGeometryBuffer.Clear();
        }
    }
}
