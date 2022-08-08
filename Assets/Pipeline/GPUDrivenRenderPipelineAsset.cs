using System.Collections.Generic;
using UnityEngine.Rendering;
using System;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
#endif



[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class RenderingPathAttribute : Attribute
{
    public GPUDrivenRenderPipelineAsset.CameraRenderingPath path { get; private set; }
    public RenderingPathAttribute(GPUDrivenRenderPipelineAsset.CameraRenderingPath path)
    {
        this.path = path;
    }
}


public unsafe sealed class GPUDrivenRenderPipelineAsset : RenderPipelineAsset
{
    /// <summary>
    /// 
    /// </summary>
    [SerializeField] GPUDrivenForwardRendererData[] m_RenderDataList = new GPUDrivenForwardRendererData[1];

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum RendererType
    {
        Custom,
        ForwardRenderer,
    }

#if UNITY_EDITOR
    public static GPUDrivenRenderPipelineAsset Create(GPUDrivenForwardRendererData renderData = null )
    {
        // Create Universal RP Asset
        var instance        = CreateInstance<GPUDrivenRenderPipelineAsset>();
        if (renderData != null)
            instance.m_RenderDataList[0] = renderData;
        else
            instance.m_RenderDataList[0] = CreateInstance<GPUDrivenForwardRendererData>();
        return instance;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
    internal class CreateGPUDrivenRenderPipelineAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            AssetDatabase.CreateAsset(Create(CreateRendererAsset(pathName, RendererType.ForwardRenderer)), pathName);
        }
    }


    [MenuItem("Assets/Create/Rendering/GPUDriven Render Pipeline/Pipeline Asset (Forward Renderer)")]
    static void CreateGPUDrivenPipelineAsset()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0,  CreateInstance<CreateGPUDrivenRenderPipelineAsset>(),
                                                                    "GPUDriverRenderPipelineAsset.asset", null, null);
    }


    static GPUDrivenForwardRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true)
    {
        GPUDrivenForwardRendererData data = CreateInstance<GPUDrivenForwardRendererData>();
        string dataPath;
        if (relativePath)
            dataPath =
                $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_Renderer{Path.GetExtension(path)}";
        else
            dataPath = path;
        AssetDatabase.CreateAsset(data, dataPath);
        return data;
    }
#endif

    protected override RenderPipeline CreatePipeline()
    {
        return new GPUDrivenRenderPipeline(this);
    }

    public enum CameraRenderingPath
    {
        GPUDeferred, Bake, Unlit
    }

    public LoadingThread        loadingThread;
    public ClusterMeshInfo      clustermeshs;
    public PipelineEvent[]      availiableEvents;
    public PipelineShaders      shaders = new PipelineShaders();
    public PipelineEvent[][]    allEvents { get; private set; }

    public static PipelineEvent[] GetAllEvents( Type[] types, Dictionary<Type, PipelineEvent> dict )
    {
        PipelineEvent[] events = new PipelineEvent[types.Length];
        for( int i = 0; i < events.Length; ++i )
        {
            events[i] = dict[types[i]];
        }
        return events;
    }


    private static NativeArray<UIntPtr> GetAllPath()
    {
        NativeList<UIntPtr> pool    = new NativeList<UIntPtr>(10, Allocator.Temp);
        NativeList<int> typePool    = new NativeList<int>(10, Allocator.Temp);

        FieldInfo[] allInfos        = typeof(AllEvents).GetFields();
        foreach (var i in allInfos)
        {
            RenderingPathAttribute but = i.GetCustomAttribute(typeof(RenderingPathAttribute)) as RenderingPathAttribute;
            if (but != null && i.FieldType == typeof(Type[]))
            {
                pool.Add(new UIntPtr(MUnsafeUtility.GetManagedPtr(i)));
                typePool.Add((int)but.path);
            }
        }

        NativeArray<UIntPtr> final  = new NativeArray<UIntPtr>(pool.Length, Allocator.Temp, NativeArrayOptions.ClearMemory);
        for (int i = 0; i < pool.Length; ++i)
        {
            final[typePool[i]] = pool[i];
        }
        return final;
    }

    public void SetRenderingPath()
    {
        NativeArray<UIntPtr> allCollection  = GetAllPath();
        allEvents                           = new PipelineEvent[allCollection.Length][];
        Dictionary<Type, PipelineEvent> evtDict = new Dictionary<Type, PipelineEvent>(availiableEvents.Length);
        foreach (var i in availiableEvents)
        {
            evtDict.Add(i.GetType(), i);
        }
        for (int i = 0; i < allCollection.Length; ++i)
        {
            FieldInfo tp = MUnsafeUtility.GetObject<FieldInfo>(allCollection[i].ToPointer());
            Type[] tt = tp.GetValue(null) as Type[];
            allEvents[i] = GetAllEvents(tt, evtDict);
        }
    }


    public Cubemap              diffuseIBL;
    public Cubemap              specularIBL;
    public Texture              brdfLut;
    public Texture              blueNoiseTex;

    [SerializeField]
    public CSMSetting           csmSetting;

    //protected override RenderPipeline CreatePipeline()
    //{
    //    GPUDrivenRenderPipeline rp  = new GPUDrivenRenderPipeline();
    //    rp.diffuseIBL               = diffuseIBL;
    //    rp.specularIBL              = specularIBL;
    //    rp.brdfLut                  = brdfLut;
    //    rp.blueNoiseTex             = blueNoiseTex;
    //    rp.csmsetting               = csmSetting;

    //    return rp;
    //}
}
