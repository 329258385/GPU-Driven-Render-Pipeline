#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine;
using System.Collections.Generic;





[MovedFrom("UnityEngine.Rendering.LWRP")]
public abstract class GPUDrivenForwardRendererData : ScriptableObject
{
#if UNITY_EDITOR
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
    internal class CreateGPUDrivenRendererAsset : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            var instance = CreateInstance<GPUDrivenForwardRendererData>();
            AssetDatabase.CreateAsset(instance, pathName);
            Selection.activeObject = instance;
        }
    }

    [MenuItem("Assets/Create/Rendering/GPUDriven Render Pipeline/Forward Renderer")]
    static void CreateForwardRendererData()
    {
        ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateGPUDrivenRendererAsset>(), "GPUDriverRendererData.asset", null, null);
    }
#endif

    internal bool                                   isInvalidated { get; set; }

    /// <summary>
    /// 渲染特性,最大10个
    /// </summary>
    [SerializeField] internal List<GPUDrivenScriptableRendererFeature> 
                                                    mRenderFeatures = new List<GPUDrivenScriptableRendererFeature>(10);

    public List<GPUDrivenScriptableRendererFeature> RenderFeatures
    {
        get => mRenderFeatures;
    }

    /// <summary>
    /// Creates the instance of the GPUDrivernScriptableRenderer.
    /// </summary>
    protected abstract GPUDrivernScriptableRenderer Create();
}
