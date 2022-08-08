using UnityEngine;
using UnityEngine.Rendering;
using Unity.Mathematics;
using static Unity.Mathematics.math;


public unsafe struct HizDepth
{
    ComputeShader       hizShader;
    public void InitHiz (GPUDrivenRenderPipelineAsset resources )
    {
        hizShader       = resources.shaders.HizLodShader;
    }

    public void GenHizMipmap( RenderTexture depthMip, CommandBuffer buffer, int mip )
    {
        buffer.SetGlobalTexture("_MainTex", depthMip);
        int2 size = int2(depthMip.width, depthMip.height);
        for (int i = 1; i < mip; ++i)
        {
            size    = max(1, size / 2);
            buffer.SetComputeTextureParam(hizShader, 0, "_SrcTex", depthMip, i - 1);
            buffer.SetComputeTextureParam(hizShader, 0, "_DstTex", depthMip, i);
            buffer.SetComputeVectorParam(hizShader,     "_Size",   float4(size - float2(0.5f), 0, 0));
            int x, y;
            x       = Mathf.CeilToInt(size.x / 8f);
            y       = Mathf.CeilToInt(size.y / 8f);
            buffer.DispatchCompute(hizShader, 0, x, y, 1);
        }
    }
}
