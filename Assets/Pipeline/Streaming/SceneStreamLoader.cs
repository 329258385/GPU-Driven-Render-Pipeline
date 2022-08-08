﻿using Unity.Mathematics;
using Unity.Collections;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;



public struct Cluster
{
    public Vector3      extent;
    public Vector3      position;
    public int          index;
}


public struct Point
{
    public float3       vertex;
    public float3       normal;
    public float4       tangent;
    public float2       uv0;
}


public unsafe struct SceneStreamLoader
{
    public FileStream           fsm;
    public int                  clusterCount;
    public NativeList<int4x4>   albedoGUIDs;
    public NativeList<int4x4>   normalGUIDs;
    public NativeList<int4x4>   smoGUIDs;
    public NativeList<int4x4>   emissionGUIDs;
    public NativeList<int4x4>   heightGUIDs;
    public NativeList<int4x4>   secondAlbedoGUIDs;
    public NativeList<int4x4>   secondNormalGUIDs;
    public NativeList<int4x4>   secondSpecGUIDs;
    public NativeList<VirtualMaterial.MaterialProperties> allProperties;
    public NativeList<Cluster>  cluster;
    public NativeList<Point>    points;
    public NativeList<int>      triangleMats;

    void LoadClusterCount()
    {
        byte[] bytes        = SceneStreaming.GetByteArray(4);
        fsm.Read(bytes, 0, 4);
        clusterCount = *(int*)bytes.Ptr();
    }

    void SaveClusterCount()
    {
        byte[] bytes        = SceneStreaming.GetByteArray(4);
        *(int*)bytes.Ptr()  = clusterCount;
        fsm.Write(bytes, 0, 4);
    }

    void LoadClusterData(out NativeList<Cluster> cluster, out NativeList<Point> points, out NativeList<int> triangleMats)
    {
        cluster             = new NativeList<Cluster>(clusterCount, clusterCount, Allocator.Persistent);
        points              = new NativeList<Point>(clusterCount * PipelineBaseBuffer.CLUSTERCLIPCOUNT, clusterCount * PipelineBaseBuffer.CLUSTERCLIPCOUNT, Allocator.Persistent);
        triangleMats        = new NativeList<int>(clusterCount * PipelineBaseBuffer.CLUSTERTRIANGLECOUNT, clusterCount * PipelineBaseBuffer.CLUSTERTRIANGLECOUNT, Allocator.Persistent);
        int length          = cluster.Length * sizeof(Cluster) + points.Length * sizeof(Point) + triangleMats.Length * sizeof(int);
        byte[] bytes        = SceneStreaming.GetByteArray(length);
        fsm.Read(bytes, 0, length);
        fixed (byte* b = bytes)
        {
            UnsafeUtility.MemCpy(cluster.unsafePtr, b, cluster.Length * sizeof(Cluster));
            UnsafeUtility.MemCpy(points.unsafePtr, b + cluster.Length * sizeof(Cluster), points.Length * sizeof(Point));
            UnsafeUtility.MemCpy(triangleMats.unsafePtr, b + cluster.Length * sizeof(Cluster) + points.Length * sizeof(Point), triangleMats.Length * sizeof(int));
        }
    }

    void SaveClusterData(NativeList<Cluster> cluster, NativeList<Point> points, NativeList<int> triangleMats)
    {
        int length          = cluster.Length * sizeof(Cluster) + points.Length * sizeof(Point) + triangleMats.Length * sizeof(int);
        byte[] bytes        = SceneStreaming.GetByteArray(length);
        fixed (byte* b = bytes)
        {
            UnsafeUtility.MemCpy(b, cluster.unsafePtr, cluster.Length * sizeof(Cluster));
            UnsafeUtility.MemCpy(b + cluster.Length * sizeof(Cluster), points.unsafePtr, points.Length * sizeof(Point));
            UnsafeUtility.MemCpy(b + cluster.Length * sizeof(Cluster) + points.Length * sizeof(Point), triangleMats.unsafePtr, triangleMats.Length * sizeof(int));
        }
        fsm.Write(bytes, 0, length);
    }

    void LoadGUIDArray(ref NativeList<int4x4> arr)
    {
        byte[] cacheArray = SceneStreaming.GetByteArray(4);
        fsm.Read(cacheArray, 0, 4);
        int len = *(int*)cacheArray.Ptr();
        if (arr.isCreated)
            arr.Clear();
        else arr = new NativeList<int4x4>(len, Allocator.Persistent);
        cacheArray = SceneStreaming.GetByteArray(sizeof(int4x4) * len);
        fsm.Read(cacheArray, 0, sizeof(int4x4) * len);
        int4x4* arrPtr = (int4x4*)cacheArray.Ptr();
        arr.AddRange(arrPtr, len);
    }

    void SaveGUIDArray(NativeList<int4x4> arr)
    {
        byte[] cacheArray = SceneStreaming.GetByteArray(arr.Length * sizeof(int4x4) + sizeof(int));
        int* intPtr = (int*)cacheArray.Ptr();
        *intPtr = arr.Length;
        UnsafeUtility.MemCpy(intPtr + 1, arr.unsafePtr, sizeof(int4x4) * arr.Length);
        fsm.Write(cacheArray, 0, arr.Length * sizeof(int4x4) + sizeof(int));
    }

    void LoadMaterialArray(ref NativeList<VirtualMaterial.MaterialProperties> arr)
    {
        byte[] cacheArray = SceneStreaming.GetByteArray(4);
        fsm.Read(cacheArray, 0, 4);
        int len = *(int*)cacheArray.Ptr();
        if (arr.isCreated)
            arr.Clear();
        else arr = new NativeList<VirtualMaterial.MaterialProperties>(len, Allocator.Persistent);
        cacheArray = SceneStreaming.GetByteArray(sizeof(VirtualMaterial.MaterialProperties) * len);
        fsm.Read(cacheArray, 0, sizeof(VirtualMaterial.MaterialProperties) * len);
        VirtualMaterial.MaterialProperties* arrPtr = (VirtualMaterial.MaterialProperties*)cacheArray.Ptr();
        arr.AddRange(arrPtr, len);
    }

    void SaveMaterialArray(NativeList<VirtualMaterial.MaterialProperties> arr)
    {
        byte[] cacheArray = SceneStreaming.GetByteArray(arr.Length * sizeof(VirtualMaterial.MaterialProperties) + sizeof(int));
        int* intPtr = (int*)cacheArray.Ptr();
        *intPtr = arr.Length;
        UnsafeUtility.MemCpy(intPtr + 1, arr.unsafePtr, sizeof(VirtualMaterial.MaterialProperties) * arr.Length);
        fsm.Write(cacheArray, 0, arr.Length * sizeof(VirtualMaterial.MaterialProperties) + sizeof(int));
    }

    public bool LoadAll(int leftedCluster)
    {
        fsm.Position = 0;
        LoadClusterCount();
        if (clusterCount > leftedCluster) return false;
        LoadGUIDArray(ref albedoGUIDs);
        LoadGUIDArray(ref normalGUIDs);
        LoadGUIDArray(ref smoGUIDs);
        LoadGUIDArray(ref emissionGUIDs);
        LoadGUIDArray(ref heightGUIDs);
        LoadGUIDArray(ref secondAlbedoGUIDs);
        LoadGUIDArray(ref secondNormalGUIDs);
        LoadGUIDArray(ref secondSpecGUIDs);
        LoadMaterialArray(ref allProperties);
        LoadClusterData(out cluster, out points, out triangleMats);
        return true;
    }

    public void SaveAll()
    {
        SaveClusterCount();
        SaveGUIDArray(albedoGUIDs);
        SaveGUIDArray(normalGUIDs);
        SaveGUIDArray(smoGUIDs);
        SaveGUIDArray(emissionGUIDs);
        SaveGUIDArray(heightGUIDs);
        SaveGUIDArray(secondAlbedoGUIDs);
        SaveGUIDArray(secondNormalGUIDs);
        SaveGUIDArray(secondSpecGUIDs);
        SaveMaterialArray(allProperties);
        SaveClusterData(cluster, points, triangleMats);
    }

    public void Dispose()
    {
        albedoGUIDs.Dispose();
        normalGUIDs.Dispose();
        smoGUIDs.Dispose();
        emissionGUIDs.Dispose();
        heightGUIDs.Dispose();
        secondAlbedoGUIDs.Dispose();
        secondNormalGUIDs.Dispose();
        secondSpecGUIDs.Dispose();
        allProperties.Dispose();
        cluster.Dispose();
        points.Dispose();
        triangleMats.Dispose();
        if (fsm != null) fsm.Dispose();
    }
}
