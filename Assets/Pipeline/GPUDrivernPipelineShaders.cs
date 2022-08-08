using System;
using UnityEngine;




[Serializable]
public struct PipelineShaders
{
    public ComputeShader    cbdrShader;
    public ComputeShader    gpuFrustumCulling;
    public ComputeShader    gpuSkin;
    public ComputeShader    streamingShader;
    public ComputeShader    pointLightFrustumCulling;
    public ComputeShader    terrainCompute;
    public ComputeShader    volumetricScattering;
    public ComputeShader    texCopyShader;
    public ComputeShader    reflectionCullingShader;
    public ComputeShader    voxelNoise;
    public ComputeShader    occlusionProbeCalculate;
    public ComputeShader    minMaxDepthCompute;
    public ComputeShader    HizLodShader;
    public Shader           minMaxDepthBounding;
    public Shader           taaShader;
    public Shader           ssrShader;
    public Shader           indirectDepthShader;
    public Shader           reflectionShader;
    public Shader           linearDepthShader;
    public Shader           linearDrawerShader;
    public Shader           cubeDepthShader;
    public Shader           clusterRenderShader;
    public Shader           volumetricShader;
    public Shader           terrainShader;
    public Shader           spotLightDepthShader;
    public Shader           gtaoShader;
    public Shader           overrideOpaqueShader;
    public Shader           sssShader;
    public Shader           bakePreIntShader;
    public Shader           rapidBlurShader;
    public Shader           cyberGlitchShader;
}


public unsafe static class AllEvents
{
    [RenderingPath(GPUDrivenRenderPipelineAsset.CameraRenderingPath.Bake)]
    public static readonly Type[] bakeType =
    {

    };

    [RenderingPath(GPUDrivenRenderPipelineAsset.CameraRenderingPath.GPUDeferred)]
    public static readonly Type[] gpuDeferredType =
    {

    };

    [RenderingPath(GPUDrivenRenderPipelineAsset.CameraRenderingPath.Unlit)]
    public static readonly Type[] unlitType =
    {

    };
}