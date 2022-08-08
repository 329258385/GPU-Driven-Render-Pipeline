﻿using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using System;
using UnityEngine.Rendering;
using System.Reflection;
using Unity.Collections;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using System.Threading;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;



[Serializable]
public sealed unsafe class TexturePool
{
    public int maximumPoolCapacity = 50;
    public RenderTexture rt { get; private set; }
    public int LeftedTexs
    {
        get { return indexPool.Length; }
    }
    private int                             streamingIndex;
    private NativeArray<int>                usageCount;
    private NativeList<int>                 indexPool;
    private Dictionary<AssetReference, int> guidToIndex;
    private ClusterMeshInfo                 clusterRes;

    public void Init(int streamingIndex, GraphicsFormat format, int resolution, ClusterMeshInfo clusterRes)
    {
        this.clusterRes         = clusterRes;
        this.streamingIndex     = streamingIndex;
        const int targetLevel   = 2;
        rt = new RenderTexture(resolution, resolution, 0, format, targetLevel);
        rt.useMipMap            = targetLevel > 1;
        rt.autoGenerateMips     = false;
        rt.dimension            = TextureDimension.Tex2DArray;
        rt.volumeDepth          = maximumPoolCapacity;
        rt.enableRandomWrite    = true;
        rt.filterMode           = FilterMode.Trilinear;
        rt.wrapMode             = TextureWrapMode.Repeat;
        rt.anisoLevel           = 16;
        rt.Create();
        indexPool               = new NativeList<int>(maximumPoolCapacity, maximumPoolCapacity, Allocator.Persistent);
        for (int i = 0; i < maximumPoolCapacity; ++i)
        {
            indexPool[i]        = i;
        }
        usageCount              = new NativeArray<int>(maximumPoolCapacity, Allocator.Persistent, NativeArrayOptions.ClearMemory);
        guidToIndex             = new Dictionary<AssetReference, int>(maximumPoolCapacity);
    }

    public void Dispose()
    {
        UnityEngine.Object.DestroyImmediate(rt);
        usageCount.Dispose();
        indexPool.Dispose();
        guidToIndex = null;
    }



    public int GetTex(int4x4 guidValue, ref NativeList<bool> flagList, bool isNormal = false)
    {
        AssetReference guid = clusterRes.GetReference(ref guidValue);
        int index;
        if (guidToIndex.TryGetValue(guid, out index))
        {
            usageCount[index]++;
        }
        else
        {
            if (indexPool.Length <= 0)
            {
                Debug.Log("Texture Pool out of Range!!");
                return 0;
            }
            index = indexPool[indexPool.Length - 1];
            indexPool.RemoveLast();
            usageCount[index] = 1;
            guidToIndex.Add(guid, index);
            int flagIndex = flagList.Length;
            flagList.Add(false);
            clusterRes.AddLoadCommand(guid, rt, index, isNormal, ref flagList, flagIndex);
        }

        return index;
    }

    public void RemoveTex(int4x4 guidValue)
    {
        AssetReference guid = clusterRes.GetReference(ref guidValue);
        int index;
        if (guidToIndex.TryGetValue(guid, out index))
        {
            usageCount[index]--;
            if (usageCount[index] <= 0)
            {
                indexPool.Add(index);
                guidToIndex.Remove(guid);
            }
        }
    }
}
