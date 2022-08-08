using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using UnityEngine.Rendering;
using Unity.Collections.LowLevel.Unsafe;
using System;
using UnityEngine.Experimental.Rendering;
using System.Text;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;




public unsafe static class GpuFrustumCullingPipeline
{
    /// <summary>
    /// 正交相机六个裁剪面
    /// </summary>
    public static void GetOrthoCullingPlanes( ref OrthoCamera orghoCam, float4* planes )
    {
        planes[0] = MathLib.GetPlane( orghoCam.forward, orghoCam.position + orghoCam.forward * orghoCam.farClipPlane);  // 前
        planes[1] = MathLib.GetPlane(-orghoCam.forward, orghoCam.position + orghoCam.forward * orghoCam.nearClipPlane); // 后
        planes[2] = MathLib.GetPlane(-orghoCam.up,      orghoCam.position - orghoCam.up * orghoCam.size );
        planes[3] = MathLib.GetPlane( orghoCam.up,      orghoCam.position + orghoCam.up * orghoCam.size );
        planes[4] = MathLib.GetPlane( orghoCam.right,   orghoCam.position + orghoCam.right * orghoCam.size );
        planes[5] = MathLib.GetPlane(-orghoCam.right,   orghoCam.position - orghoCam.right * orghoCam.size );
    }

    /// <summary>
    /// 摄像机的是个角
    /// </summary>
    public static void GetFrustumCorner( ref PerspCam perspCam, float distance, float3* corners )
    {
        float fov       = Mathf.Deg2Rad * perspCam.fov * 0.5f;
        float upLenght  = distance * tan(fov);
        float rightLen  = upLenght * perspCam.aspect;
        float3 farPoint = perspCam.position + distance * perspCam.forward;
        float3 upVec    = upLenght * perspCam.up;
        float3 rightVec = rightLen * perspCam.right;

        corners[0]      = farPoint - upVec - rightVec;
        corners[1]      = farPoint - upVec + rightVec;
        corners[2]      = farPoint + upVec - rightVec;
        corners[3]      = farPoint + upVec + rightVec;
    }

    public static void GetFrustumCorner(ref OrthoCamera orthoCam, float distance, float3* corners)
    {
        float3 farPoint = orthoCam.position + distance * orthoCam.forward;
        float3 upVec    = orthoCam.size * orthoCam.up;
        float3 rightVec = orthoCam.size * orthoCam.right;
        corners[0]      = farPoint - upVec - rightVec;
        corners[1]      = farPoint - upVec + rightVec;
        corners[2]      = farPoint + upVec - rightVec;
        corners[3]      = farPoint + upVec + rightVec;
    }

    public static void GetFrustumCorner(ref PerspCam perspCam, float3* corners)
    {
        float fov = tan(Mathf.Deg2Rad * perspCam.fov * 0.5f);
        void GetCorner(float dist, ref PerspCam persp)
        {
            float upLength      = dist * (fov);
            float rightLength   = upLength * persp.aspect;
            float3 farPoint     = persp.position + dist * persp.forward;
            float3 upVec        = upLength * persp.up;
            float3 rightVec     = rightLength * persp.right;
            corners[0] = farPoint - upVec - rightVec;
            corners[1] = farPoint - upVec + rightVec;
            corners[2] = farPoint + upVec - rightVec;
            corners[3] = farPoint + upVec + rightVec;
            corners += 4;
        }
        GetCorner(perspCam.nearClipPlane, ref perspCam);
        GetCorner(perspCam.farClipPlane, ref perspCam);
    }

    public static void GetFrustumCorner(ref OrthoCamera orthoCam, float3* corners)
    {
        float3 upVec    = orthoCam.size * orthoCam.up;
        float3 rightVec = orthoCam.size * orthoCam.right;
        void GetCorner(ref OrthoCamera ortho, float dist)
        {

            float3 farPoint = ortho.position + dist * ortho.forward;
            corners[0] = farPoint - upVec - rightVec;
            corners[1] = farPoint - upVec + rightVec;
            corners[2] = farPoint + upVec - rightVec;
            corners[3] = farPoint + upVec + rightVec;
            corners += 4;
        }
        GetCorner(ref orthoCam, orthoCam.nearClipPlane);
        GetCorner(ref orthoCam, orthoCam.farClipPlane);
    }

    public static void GetPerspFrustumPlanesWithCorner(ref PerspCam perspCam, float4* planes, float3* corners)
    {
        planes[0] = MathLib.GetPlane(corners[1], corners[0], perspCam.position);
        planes[1] = MathLib.GetPlane(corners[2], corners[3], perspCam.position);
        planes[2] = MathLib.GetPlane(corners[0], corners[2], perspCam.position);
        planes[3] = MathLib.GetPlane(corners[3], corners[1], perspCam.position);
        planes[4] = MathLib.GetPlane(perspCam.forward, perspCam.position + perspCam.forward * perspCam.farClipPlane);
        planes[5] = MathLib.GetPlane(-perspCam.forward, perspCam.position + perspCam.forward * perspCam.nearClipPlane);
    }

    public static void GetFrustumPlanes(ref PerspCam perspCam, float4* planes)
    {
        float3* corners = stackalloc float3[4];
        GetFrustumCorner(ref perspCam, perspCam.farClipPlane, corners);
        planes[0] = MathLib.GetPlane(corners[1], corners[0], perspCam.position);
        planes[1] = MathLib.GetPlane(corners[2], corners[3], perspCam.position);
        planes[2] = MathLib.GetPlane(corners[0], corners[2], perspCam.position);
        planes[3] = MathLib.GetPlane(corners[3], corners[1], perspCam.position);
        planes[4] = MathLib.GetPlane(perspCam.forward, perspCam.position + perspCam.forward * perspCam.farClipPlane);
        planes[5] = MathLib.GetPlane(-perspCam.forward, perspCam.position + perspCam.forward * perspCam.nearClipPlane);
    }

    public static void GetFrustumPlanes(ref OrthoCamera ortho, float4* planes)
    {
        planes[0] = MathLib.GetPlane(ortho.up, ortho.position + ortho.up * ortho.size);
        planes[1] = MathLib.GetPlane(-ortho.up, ortho.position - ortho.up * ortho.size);
        planes[2] = MathLib.GetPlane(ortho.right, ortho.position + ortho.right * ortho.size);
        planes[3] = MathLib.GetPlane(-ortho.right, ortho.position - ortho.right * ortho.size);
        planes[4] = MathLib.GetPlane(ortho.forward, ortho.position + ortho.forward * ortho.farClipPlane);
        planes[5] = MathLib.GetPlane(-ortho.forward, ortho.position + ortho.forward * ortho.nearClipPlane);
    }

    public static void InitPipeline( PipelineBaseBuffer baseBuffer, int maximumLenght )
    {
        if( maximumLenght <= 0 )
        {
            baseBuffer.clusterCount         = 0;
            baseBuffer.prepareClusterCount  = 0;
            return;
        }

        baseBuffer.clusterBuffer            = new ComputeBuffer(maximumLenght, sizeof(Cluster));
        baseBuffer.resultBuffer             = new ComputeBuffer(maximumLenght, sizeof(uint));
        baseBuffer.instanceCountBuffer      = new ComputeBuffer(5,4,ComputeBufferType.IndirectArguments);

        NativeArray<uint> instanceCountBufferValue = new NativeArray<uint>(5, Allocator.Temp);
        instanceCountBufferValue[0]         = PipelineBaseBuffer.CLUSTERCLIPCOUNT;

        baseBuffer.moveCountBuffers         = new NativeList<int>(5, Allocator.Persistent );
        baseBuffer.instanceCountBuffer.SetData(instanceCountBufferValue);

        baseBuffer.verticesBuffer           = new ComputeBuffer(maximumLenght * PipelineBaseBuffer.CLUSTERCLIPCOUNT, sizeof(Point));
        baseBuffer.triangleMaterialBuffer   = new ComputeBuffer(maximumLenght * PipelineBaseBuffer.CLUSTERCLIPCOUNT, sizeof(int));
       
        baseBuffer.clusterCount             = 0;
        baseBuffer.prepareClusterCount      = 0;

        /// useHiZ
        {
            baseBuffer.reCheckCount         = new ComputeBuffer( 5, sizeof(int), ComputeBufferType.IndirectArguments );
            baseBuffer.dispatchBuffer       = new ComputeBuffer( 5, sizeof(int), ComputeBufferType.IndirectArguments );
            baseBuffer.reCheckResult        = new ComputeBuffer( baseBuffer.resultBuffer.count, sizeof(uint));
            baseBuffer.reCheckCount.SetData(instanceCountBufferValue);

            UnsafeUtility.MemClear(instanceCountBufferValue.GetUnsafePtr(), 5 * sizeof(uint));
            instanceCountBufferValue[1]     = 1;
            instanceCountBufferValue[2]     = 1;
            baseBuffer.dispatchBuffer.SetData(instanceCountBufferValue);
        }
    }

    public static void GetfrustumCorners( float* planes, int planesCount, Camera cam, float3* frustumCorners )
    {
        for( int i = 0; i < planesCount; ++i )
        {
            int index   = i * 4;
            float p     = planes[i];
            frustumCorners[0 + index]   = cam.ViewportToWorldPoint(new Vector3(0, 0, p));
            frustumCorners[1 + index]   = cam.ViewportToWorldPoint(new Vector3(0, 1, p));
            frustumCorners[2 + index]   = cam.ViewportToWorldPoint(new Vector3(1, 1, p));
            frustumCorners[3 + index]   = cam.ViewportToWorldPoint(new Vector3(1, 0, p));
        }
    }

    public static bool FrustumCulling( ref Matrix4x4 ObjectToWorld, Vector3 extent, Vector4* frustumPlanes )
    {
        /// 矩阵的三个基坐标
        Vector3 right       = new Vector3( ObjectToWorld.m00, ObjectToWorld.m10, ObjectToWorld.m20 );
        Vector3 up          = new Vector3( ObjectToWorld.m01, ObjectToWorld.m11, ObjectToWorld.m21 );
        Vector3 forward     = new Vector3( ObjectToWorld.m02, ObjectToWorld.m12, ObjectToWorld.m22 );
        Vector3 position    = new Vector3( ObjectToWorld.m03, ObjectToWorld.m13, ObjectToWorld.m23 );

        for( int i = 0; i < 6; ++i )
        {
            ref Vector4 plane = ref frustumPlanes[i];
            Vector3 normal  = new Vector3( plane.x, plane.y, plane.z );
            float distance  = plane.w;
            float r         = Vector3.Dot(position, normal );
            Vector3 anoraml = new Vector3(Mathf.Abs(Vector3.Dot(normal, right)), Mathf.Abs(Vector3.Dot(normal, up)), Mathf.Abs(Vector3.Dot(normal, forward)));
            float f         = Vector3.Dot( anoraml, extent );
            if ((r - f) >= -distance)
                return false;
        }
        return true;
    }

    public static bool FrustumCulling( Vector3 position, float range, Vector4* frustumPlanes )
    {
        for( int i = 0; i < 5; ++i )
        {
            ref Vector4 plane   = ref frustumPlanes[i];
            Vector3 normal      = new Vector3( plane.x, plane.y, plane.z );
            float rayDist       = Vector3.Dot( normal, position );  // 投影长度
            rayDist             += plane.w;
            if (rayDist > range)
                return false;
        }

        return true;
    }

    public static void UpdateFrustumMinMaxPoint(CommandBuffer buffer, float3 frustumMinPoint, float3 frustumMaxPoint)
    {
        buffer.SetGlobalVector(ShaderIDs._FrustumMaxPoint, float4(frustumMaxPoint, 1));
        buffer.SetGlobalVector(ShaderIDs._FrustumMinPoint, float4(frustumMinPoint, 1));
    }

    public static void SetBaseBuffer( PipelineBaseBuffer basebuffer, ComputeShader gpuFrustumShader, Vector4[] frustumCullingPlanes, CommandBuffer buffer )
    {
        var computer = gpuFrustumShader;
        buffer.SetComputeVectorArrayParam(computer, ShaderIDs.planes, frustumCullingPlanes);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel,   ShaderIDs.clusterBuffer,          basebuffer.clusterBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel,   ShaderIDs.instanceCountBuffer,    basebuffer.instanceCountBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClearCluster_Kernel,  ShaderIDs.instanceCountBuffer,    basebuffer.instanceCountBuffer);
        buffer.DispatchCompute(computer, PipelineBaseBuffer.ClearCluster_Kernel, 1, 1, 1);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel, ShaderIDs.resultBuffer, basebuffer.resultBuffer);
    }

    private static Vector4[] backupFrustumArray = new Vector4[6];
    public static void SetBaseBufferOcc( PipelineBaseBuffer baseBuffer, ComputeShader gpuFrustumShader, Vector4[] frustumCullingPlanes, CommandBuffer buffer )
    {
        var computer = gpuFrustumShader;
        buffer.SetComputeVectorArrayParam(computer, ShaderIDs.planes, frustumCullingPlanes);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.UnsafeCull_Kernel,    ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.UnsafeCull_Kernel,    ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClearCluster_Kernel,  ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.DispatchCompute(computer, PipelineBaseBuffer.ClearCluster_Kernel, 1, 1,  1);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.UnsafeCull_Kernel,    ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
    }

    public static void SetBaseBuffer( PipelineBaseBuffer baseBuffer, ComputeShader gpuFrustumShader, float4* frustumCullingPlanes, CommandBuffer buffer )
    {
        var computer = gpuFrustumShader;
        UnsafeUtility.MemCpy(backupFrustumArray.Ptr(), frustumCullingPlanes, sizeof(float4) * 6);
        buffer.SetComputeVectorArrayParam(computer, ShaderIDs.planes, backupFrustumArray);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClearCluster_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.DispatchCompute(computer, PipelineBaseBuffer.ClearCluster_Kernel, 1, 1, 1);
        buffer.SetComputeBufferParam(computer, PipelineBaseBuffer.ClusterCull_Kernel, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
    }


    public static void SetBaseBufferOcc( PipelineBaseBuffer baseBuffer, ComputeShader gpuFrustumShader, float4* frustumCullingPlanes, CommandBuffer buffer )
    {
        var compute = gpuFrustumShader;
        UnsafeUtility.MemCpy(backupFrustumArray.Ptr(), frustumCullingPlanes, sizeof(float4) * 6);
        buffer.SetComputeVectorArrayParam(compute, ShaderIDs.planes, backupFrustumArray);
        buffer.SetComputeBufferParam(compute, PipelineBaseBuffer.UnsafeCull_Kernel, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        buffer.SetComputeBufferParam(compute, PipelineBaseBuffer.UnsafeCull_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.SetComputeBufferParam(compute, PipelineBaseBuffer.ClearCluster_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.DispatchCompute(compute, PipelineBaseBuffer.ClearCluster_Kernel, 1, 1, 1);
        buffer.SetComputeBufferParam(compute, PipelineBaseBuffer.UnsafeCull_Kernel, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
    }


    public static void UpdateOcclusionBuffer( PipelineBaseBuffer baseBuffer, ComputeShader coreShader, CommandBuffer buffer, HizOcclusionData occlusionData, Vector4[] frustumCullingPlanes )
    {
        buffer.SetComputeVectorArrayParam(coreShader, ShaderIDs.planes, frustumCullingPlanes);
        buffer.SetComputeBufferParam(coreShader,  PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        buffer.SetComputeTextureParam(coreShader, PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs._HizDepthTex, occlusionData.historyDepth);
        buffer.SetComputeBufferParam(coreShader,  PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs.dispatchBuffer, baseBuffer.dispatchBuffer);
        buffer.SetComputeBufferParam(coreShader,  PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs.resultBuffer, baseBuffer.resultBuffer );
        buffer.SetComputeBufferParam(coreShader,  PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer );
        buffer.SetComputeBufferParam(coreShader,  PipelineBaseBuffer.FrustumFilter_Kernel, ShaderIDs.reCheckResult, baseBuffer.reCheckResult );
        ComputeShaderUtility.Dispatch(coreShader, buffer, PipelineBaseBuffer.FrustumFilter_Kernel, baseBuffer.clusterCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ClearOcclusionData( PipelineBaseBuffer baseBuffer, CommandBuffer buffer, ComputeShader coreShader )
    {
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.ClearOcclusionData_Kernel, ShaderIDs.dispatchBuffer, baseBuffer.dispatchBuffer);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.ClearOcclusionData_Kernel, ShaderIDs.instanceCountBuffer, baseBuffer.instanceCountBuffer);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.ClearOcclusionData_Kernel, ShaderIDs.reCheckCount, baseBuffer.reCheckCount);
        buffer.DispatchCompute(coreShader, PipelineBaseBuffer.ClearOcclusionData_Kernel, 1, 1, 1);
    }


    public static void OcclusionRecheck( PipelineBaseBuffer baseBuffer, CommandBuffer buffer, ComputeShader coreShader, HizOcclusionData hizData )
    {
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs.dispatchBuffer, baseBuffer.dispatchBuffer);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs.reCheckResult, baseBuffer.reCheckResult);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs.clusterBuffer, baseBuffer.clusterBuffer);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs.reCheckCount, baseBuffer.reCheckCount);
        buffer.SetComputeBufferParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs.resultBuffer, baseBuffer.resultBuffer);
        buffer.SetComputeTextureParam(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, ShaderIDs._HizDepthTex, hizData.historyDepth);
        buffer.DispatchCompute(coreShader, PipelineBaseBuffer.OcclusionRecheck_Kernel, baseBuffer.dispatchBuffer, 0);
    }

    public static void InitRenderTarget( ref RenderTargets tar, Camera tarcam, CommandBuffer buffer )
    {
        buffer.GetTemporaryRT(RenderTargets.gbufferIndex[0], tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_UNorm, 1, false);
        buffer.GetTemporaryRT(RenderTargets.gbufferIndex[1], tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.R8G8B8A8_UNorm, 1, false);
        buffer.GetTemporaryRT(RenderTargets.gbufferIndex[2], tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.A2B10G10R10_UNormPack32, 1, false);
        buffer.GetTemporaryRT(RenderTargets.gbufferIndex[3], tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat, 1, false);
        buffer.GetTemporaryRT(ShaderIDs._DepthBufferTexture, tarcam.pixelWidth, tarcam.pixelHeight, 32, FilterMode.Bilinear, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear, 1, false);
        buffer.GetTemporaryRT(ShaderIDs._CameraDepthTexture, tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.R32_SFloat, 1, false);
        buffer.GetTemporaryRT(ShaderIDs._BackupMap,          tarcam.pixelWidth, tarcam.pixelHeight, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat, 1, false );
       
        foreach( var i in RenderTargets.gbufferIndex )
        {
            GPUDrivenRenderPipeline.ReleaseRT(i);
        }

        GPUDrivenRenderPipeline.ReleaseRT(ShaderIDs._DepthBufferTexture);
        GPUDrivenRenderPipeline.ReleaseRT(ShaderIDs._CameraDepthTexture);
        GPUDrivenRenderPipeline.ReleaseRT(ShaderIDs._BackupMap);

        tar.renderTargetIdentifier  = RenderTargets.gbufferIndex[3];
        tar.backupIdentifier        = ShaderIDs._BackupMap;
    }

    public static void Dispose(PipelineBaseBuffer baseBuffer)
    {
        void DisposeBuffer(ComputeBuffer bf)
        {
            if (bf != null) bf.Dispose();
        }

        DisposeBuffer(baseBuffer.verticesBuffer);
        DisposeBuffer(baseBuffer.triangleMaterialBuffer);
        DisposeBuffer(baseBuffer.clusterBuffer);
        if (/*GeometryEvent.useHiZ*/ true )
        {
            DisposeBuffer(baseBuffer.reCheckCount);
            DisposeBuffer(baseBuffer.reCheckResult);
            DisposeBuffer(baseBuffer.dispatchBuffer);
        }
        DisposeBuffer(baseBuffer.instanceCountBuffer);
        DisposeBuffer(baseBuffer.resultBuffer);
        if (baseBuffer.moveCountBuffers.isCreated)
        {
            foreach (var i in baseBuffer.moveCountBuffers)
            {
                ComputeBuffer bf = (ComputeBuffer)MUnsafeUtility.GetHookedObject(i);
                MUnsafeUtility.RemoveHookedObject(i);
                bf.Dispose();
            }
            baseBuffer.moveCountBuffers.Dispose();
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteCommandBufferAsync( ref this PipelineCommandData data, CommandBuffer asyncBuffer, ComputeQueueType queueType )
    {
        data.context.ExecuteCommandBufferAsync(asyncBuffer, queueType);
        asyncBuffer.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ExecuteCommandBuffer( ref this PipelineCommandData data )
    {
        data.context.ExecuteCommandBuffer(data.buffer);
        data.buffer.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RenderProceduralCommand(PipelineBaseBuffer baseBuffer, Material material, CommandBuffer buffer )
    {
        buffer.DrawProceduralIndirect(Matrix4x4.identity, material, 0, MeshTopology.Triangles, baseBuffer.instanceCountBuffer, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RunCullDispatching(PipelineBaseBuffer baseBuffer, ComputeShader computeShader, CommandBuffer buffer)
    {
        ComputeShaderUtility.Dispatch(computeShader, buffer, PipelineBaseBuffer.ClusterCull_Kernel, baseBuffer.clusterCount);
    }
}
