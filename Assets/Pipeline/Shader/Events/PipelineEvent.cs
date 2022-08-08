using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using System;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Rendering;


#if UNITY_EDITOR
using UnityEditor;
    [CustomEditor(typeof(PipelineEvent), true )]
    public class EventEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            PipelineEvent evt = serializedObject.targetObject as PipelineEvent;
            evt.Enabled       = EditorGUILayout.Toggle("Enabled", evt.Enabled);
            EditorUtility.SetDirty(evt);
            base.OnInspectorGUI();
        }
    }
#endif

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true )]
public class RequireEventAttribute : Attribute
{
    public Type[]           events { get; private set; }
    public RequireEventAttribute( params Type[] allEvents )
    {
        events              = allEvents;
    }
}

[System.Serializable]
public unsafe abstract class PipelineEvent : ScriptableObject
{
    [HideInInspector]
    [SerializeField]
    private bool                    enabled = false;
    private bool                    initialized = false;


    private NativeList<UIntPtr>     dependedEvents;
    private NativeList<UIntPtr>     dependingEvents;

    public bool Enabled
    {
        get { return enabled; }
        set { enabled = value; }
    }

    public void Prepare()
    {
        RequireEventAttribute requireEvt = GetType().GetCustomAttribute<RequireEventAttribute>(true);
        if( requireEvt != null )
        {
            foreach( var t in requireEvt.events )
            {
                PipelineEvent targetevt = GPUDrivenRenderPipeline.GetEvent(t);
                if (targetevt != null)
                {
                    targetevt.dependedEvents.Add(new UIntPtr(MUnsafeUtility.GetManagedPtr(this)));
                    dependingEvents.Add(new UIntPtr(MUnsafeUtility.GetManagedPtr(targetevt)));
                }
            }
        }
    }

    public void InitDependEventsList()
    {
        dependedEvents  = new NativeList<UIntPtr>(10, Unity.Collections.Allocator.Persistent);
        dependingEvents = new NativeList<UIntPtr>(10, Unity.Collections.Allocator.Persistent);
    }

    public void DisposeDependEventsList()
    {
        dependedEvents.Dispose();
        dependingEvents.Dispose();
    }

    protected virtual void OnEnable()
    {

    }

    protected virtual void OnDisable()
    {

    }

    public void CheckInit(GPUDrivenRenderPipelineAsset resources)
    {

        initialized = true;
        Init(resources);
    }

    public void DisposeEvent()
    {
        if (!initialized) return;
        initialized = false;
        OnDisable();
        Dispose();
    }

    public void InitEvent(GPUDrivenRenderPipelineAsset resources)
    {

    }

    public virtual void FrameUpdate( PipelineCamera cam, ref PipelineCommandData data) 
    {
        
    }


    public virtual void PreRenderFrame(PipelineCamera cam, ref PipelineCommandData data) 
    {
        
    }


    protected abstract void Init(GPUDrivenRenderPipelineAsset resources);
    protected abstract void Dispose();
    public abstract bool CheckProperty();
}


[MovedFrom("UnityEngine.Rendering.LWRP")] 
public abstract class ScriptableRenderPass : ScriptableObject
{
    public ScriptableRenderPass()
    {
       
    }

    /// <summary>
    /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
    /// This method should be called inside Configure.
    /// </summary>
    /// <param name="colorAttachment">Color attachment identifier.</param>
    /// <param name="depthAttachment">Depth attachment identifier.</param>
    /// <seealso cref="Configure"/>
    public void ConfigureTarget(RenderTargetIdentifier colorAttachment, RenderTargetIdentifier depthAttachment)
    {
       
    }

    /// <summary>
    /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
    /// This method should be called inside Configure.
    /// </summary>
    /// <param name="colorAttachment">Color attachment identifier.</param>
    /// <param name="depthAttachment">Depth attachment identifier.</param>
    /// <seealso cref="Configure"/>
    public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments, RenderTargetIdentifier depthAttachment)
    {

    }

    /// <summary>
    /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
    /// This method should be called inside Configure.
    /// </summary>
    /// <param name="colorAttachment">Color attachment identifier.</param>
    /// <seealso cref="Configure"/>
    public void ConfigureTarget(RenderTargetIdentifier colorAttachment)
    {
        
    }

    /// <summary>
    /// Configures render targets for this render pass. Call this instead of CommandBuffer.SetRenderTarget.
    /// This method should be called inside Configure.
    /// </summary>
    public void ConfigureTarget(RenderTargetIdentifier[] colorAttachments)
    {
        ConfigureTarget(colorAttachments, BuiltinRenderTextureType.CameraTarget);
    }

    /// <summary>
    /// Configures clearing for the render targets for this render pass. Call this inside Configure.
    /// </summary>
    //public void ConfigureClear(ClearFlag clearFlag, Color clearColor)
    //{

    //}

    /// <summary>
    /// Creates <c>DrawingSettings</c> based on current the rendering state.
    /// </summary>
    //public DrawingSettings CreateDrawingSettings(ShaderTagId shaderTagId, ref RenderingData renderingData, SortingCriteria sortingCriteria)
    //{
    //    Camera camera = renderingData.cameraData.camera;
    //    SortingSettings sortingSettings = new SortingSettings(camera) { criteria = sortingCriteria };
    //    DrawingSettings settings = new DrawingSettings(shaderTagId, sortingSettings)
    //    {
    //        perObjectData = renderingData.perObjectData,
    //        mainLightIndex = renderingData.lightData.mainLightIndex,
    //        enableDynamicBatching = renderingData.supportsDynamicBatching,

    //        // Disable instancing for preview cameras. This is consistent with the built-in forward renderer. Also fixes case 1127324.
    //        enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
    //    };
    //    return settings;
    //}

    // TODO: Remove this. Currently only used by FinalBlit pass.
    //internal void SetRenderTarget(
    //    CommandBuffer cmd,
    //    RenderTargetIdentifier colorAttachment,
    //    RenderBufferLoadAction colorLoadAction,
    //    RenderBufferStoreAction colorStoreAction,
    //    ClearFlag clearFlags,
    //    Color clearColor,
    //    TextureDimension dimension)
    //{
    //    if (dimension == TextureDimension.Tex2DArray)
    //        CoreUtils.SetRenderTarget(cmd, colorAttachment, clearFlags, clearColor, 0, CubemapFace.Unknown, -1);
    //    else
    //        CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlags, clearColor);
    //}
}