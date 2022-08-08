using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using System;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;



[Serializable]
public struct Pair
{
    public string key;
    public Texture2DArray value;
    public Pair(string key, Texture2DArray value)
    {
        this.key = key;
        this.value = value;
    }
}
[Serializable]
public struct Pair<T, V>
{
    public T key;
    public V value;
    public Pair(T key, V value)
    {
        this.key = key;
        this.value = value;
    }
}

public interface IFunction<A, R>
{
    R Run(ref A a);
}

public interface IFunction<A, B, R>
{
    R Run(ref A a, ref B b);
}


public class PipelineBaseBuffer
{
    public ComputeBuffer        clusterBuffer;         //ObjectInfo
    public ComputeBuffer        instanceCountBuffer;   //uint
    public ComputeBuffer        dispatchBuffer;
    public ComputeBuffer        reCheckResult;
    public ComputeBuffer        resultBuffer;          //uint
    public ComputeBuffer        verticesBuffer;        //Point
    public ComputeBuffer        triangleMaterialBuffer;
    public ComputeBuffer        reCheckCount;        //Point
    public NativeList<int>      moveCountBuffers;
    public int                  clusterCount;
    public int                  prepareClusterCount;
    public const int            INDIRECTSIZE = 20;
    public const int            CLUSTERCLIPCOUNT = 384;
    public const int            CLUSTERTRIANGLECOUNT = CLUSTERCLIPCOUNT / 3;

    public const int            ClusterCull_Kernel = 0;
    public const int            ClearCluster_Kernel = 1;
    public const int            UnsafeCull_Kernel = 2;
    public const int            MoveVertex_Kernel = 3;
    public const int            MoveCluster_Kernel = 4;
    public const int            FrustumFilter_Kernel = 5;
    public const int            OcclusionRecheck_Kernel = 6;
    public const int            ClearOcclusionData_Kernel = 7;
}


public struct PipelineCommandData
{
    //    public Matrix4x4 vp;
    //   public Matrix4x4 inverseVP;
    public CommandBuffer            buffer;
    public ScriptableRenderContext  context;
    public GPUDrivenRenderPipelineAsset resources;
}


public struct OrthoCamera
{
    public float4x4         worldToCameraMatrix;
    public float4x4         localToWorldMatrix;
    public float3           right;
    public float3           up;
    public float3           forward;
    public float3           position;
    public float            size;
    public float            nearClipPlane;
    public float            farClipPlane;
    public float4x4         projectionMatrix;
    public void UpdateTRSMatrix()
    {
        localToWorldMatrix.c0 = new float4(right, 0);
        localToWorldMatrix.c1 = new float4(up, 0);
        localToWorldMatrix.c2 = new float4(forward, 0);
        localToWorldMatrix.c3 = new float4(position, 1);
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorldMatrix);
        worldToCameraMatrix.c0.z = -worldToCameraMatrix.c0.z;
        worldToCameraMatrix.c1.z = -worldToCameraMatrix.c1.z;
        worldToCameraMatrix.c2.z = -worldToCameraMatrix.c2.z;
        worldToCameraMatrix.c3.z = -worldToCameraMatrix.c3.z;
    }
    public void UpdateProjectionMatrix()
    {
        projectionMatrix = Matrix4x4.Ortho(-size, size, -size, size, nearClipPlane, farClipPlane);
    }
}


public struct PerspCam
{
    public float3 right;
    public float3 up;
    public float3 forward;
    public float3 position;
    public float fov;
    public float nearClipPlane;
    public float farClipPlane;
    public float aspect;
    public float4x4 localToWorldMatrix;
    public float4x4 worldToCameraMatrix;
    public float4x4 projectionMatrix;
    public void UpdateTRSMatrix()
    {
        localToWorldMatrix.c0 = float4(right, 0);
        localToWorldMatrix.c1 = float4(up, 0);
        localToWorldMatrix.c2 = float4(forward, 0);
        localToWorldMatrix.c3 = float4(position, 1);
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorldMatrix);
        float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
        worldToCameraMatrix.c0.z = row2.x;
        worldToCameraMatrix.c1.z = row2.y;
        worldToCameraMatrix.c2.z = row2.z;
        worldToCameraMatrix.c3.z = row2.w;
    }
    public void UpdateViewMatrix(float4x4 localToWorld)
    {
        worldToCameraMatrix = MathLib.GetWorldToLocal(ref localToWorld);
        right = localToWorld.c0.xyz;
        up = localToWorld.c1.xyz;
        forward = localToWorld.c2.xyz;
        position = localToWorld.c3.xyz;
        float4 row2 = -float4(worldToCameraMatrix.c0.z, worldToCameraMatrix.c1.z, worldToCameraMatrix.c2.z, worldToCameraMatrix.c3.z);
        worldToCameraMatrix.c0.z = row2.x;
        worldToCameraMatrix.c1.z = row2.y;
        worldToCameraMatrix.c2.z = row2.z;
        worldToCameraMatrix.c3.z = row2.w;
    }
    public void UpdateProjectionMatrix()
    {
        projectionMatrix = Matrix4x4.Perspective(fov, aspect, nearClipPlane, farClipPlane);
    }
}


public struct RenderTargets
{
    public RenderTargetIdentifier renderTargetIdentifier;
    public RenderTargetIdentifier backupIdentifier;
    public static readonly int[] gbufferIndex = new int[]
        {
                Shader.PropertyToID("_CameraGBufferTexture0"),
                Shader.PropertyToID("_CameraGBufferTexture1"),
                Shader.PropertyToID("_CameraGBufferTexture2"),
                Shader.PropertyToID("_CameraGBufferTexture3"),
                ShaderIDs._CameraMotionVectorsTexture
        };
    public RenderTargetIdentifier[] gbufferIdentifier;
    public bool initialized;
    public static RenderTargets Init()
    {
        RenderTargets rt;
        rt.gbufferIdentifier = new RenderTargetIdentifier[gbufferIndex.Length];
        for (int i = 0; i < gbufferIndex.Length; ++i)
        {
            rt.gbufferIdentifier[i] = gbufferIndex[i];
        }
        rt.backupIdentifier = default;
        rt.renderTargetIdentifier = default;
        rt.initialized = true;
        return rt;
    }
}