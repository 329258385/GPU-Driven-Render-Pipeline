using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using System;
using static Unity.Mathematics.math;
using System.Runtime.CompilerServices;




public struct RenderClusterOptions
{
    public Vector4[]                    frustumPlanes;
    public CommandBuffer                command;
    public ComputeShader                cullingShader;
    public ComputeShader                terrainCompute;
}


public unsafe static class SceneController
{
    public const int                    overrideShadowmapPass = 0;
    public const int                    overrideDepthPrePass = 1;

    private static GPUDrivenRenderPipelineAsset resources;

    public static PipelineBaseBuffer    baseBuffer
    {
        get;
        private set;
    }

    public static bool                  gpurpEnabled
    {
        get;
        private set;
    }

    private static bool                 singletonReady = false;


    public static void RenderScene( ref PipelineCommandData data, ref FilteringSettings filterSettings, ref DrawingSettings drawSettings, ref CullingResults cullResults, ref RenderStateBlock stateBlock )
    {
        data.ExecuteCommandBuffer();
        data.context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings, ref stateBlock);
    }

    public static void RenderScene(ref PipelineCommandData data, ref FilteringSettings filterSettings, ref DrawingSettings drawSettings, ref CullingResults cullResults)
    {
        data.ExecuteCommandBuffer();
        data.context.DrawRenderers(cullResults, ref drawSettings, ref filterSettings);
    }

    public static void DrawClusterRecheckHiz( ref RenderClusterOptions options, ref HizDepth hizDepth, HizOcclusionData hizOpts, Material targetMat, Material linearLODMat, PipelineCamera pipeCam )
    {
        ref RenderTargets target    = ref pipeCam.targets;
        Camera cam                  = pipeCam.cam;


        CommandBuffer buffer        = options.command;
        ComputeShader gpuFrustum    = options.cullingShader;

        buffer.BlitSRT(hizOpts.historyDepth, linearLODMat, 0);
        hizDepth.GenHizMipmap(hizOpts.historyDepth, buffer, hizOpts.mip);

        GpuFrustumCullingPipeline.ClearOcclusionData(baseBuffer, buffer, gpuFrustum);
        GpuFrustumCullingPipeline.OcclusionRecheck(baseBuffer, buffer, gpuFrustum, hizOpts);
    }
}
