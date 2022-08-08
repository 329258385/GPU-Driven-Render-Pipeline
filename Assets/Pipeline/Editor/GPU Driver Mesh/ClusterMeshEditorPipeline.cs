using System;
using UnityEngine;
using UnityEditor;
using Random = Unity.Mathematics.Random;








public class ClusterMeshEditorPipeline : EditorWindow
{
    [MenuItem("GPU Drivern / Cluster Mesh Pipeline")]
    private static void CreateWizard()
    {
        ClusterMeshEditorPipeline window = (ClusterMeshEditorPipeline)GetWindow(typeof(ClusterMeshEditorPipeline));
        window.Show();
    }

    private Vector2Int      tileScale = new Vector2Int( 16, 32 );
    private float           alias = 0.5f;
    private Vector2         randomUVScale = new Vector2( 0.2f, 0.2f );
    private Vector2Int      resolution = new Vector2Int( 1024, 1024 );
    private string          path = "Assets/Textures/Test.asset";
    private Material        testMat;



    private void OnGUI()
    {
        tileScale           = EditorGUILayout.Vector2IntField("Tile count:", tileScale );
        alias               = EditorGUILayout.Slider("Alias Offset: ", alias, 0, 1);
        randomUVScale       = EditorGUILayout.Vector2Field("Tile UV's Scale: ", randomUVScale);
        resolution          = EditorGUILayout.Vector2IntField("Resolution: ", resolution);
        path                = EditorGUILayout.TextField("Path: ", path);
        testMat             = EditorGUILayout.ObjectField("Test Material: ", testMat, typeof(Material), false) as Material;
        if( GUILayout.Button("Build Cluster Mesh"))
        {
            Texture2D tex   = new Texture2D( tileScale.x, tileScale.y, TextureFormat.RGHalf, false, true );
            Color[] colors  = new Color[tileScale.x * tileScale.y ];
            Random rand     = new Random((uint)Guid.NewGuid().GetHashCode() );
            for( int x = 0; x < tileScale.x; ++x )
            {
                for( int y = 0; y < tileScale.y; ++y )
                {
                    colors[y * tileScale.x + x] = new Color(rand.NextFloat() * (1 - randomUVScale.x), rand.NextFloat() * (1 - randomUVScale.y), 0);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            RenderTexture rt = new RenderTexture( new RenderTextureDescriptor
            {
                width       = resolution.x,
                height      = resolution.y,
                volumeDepth = 1,
                dimension   = UnityEngine.Rendering.TextureDimension.Tex2D,
                colorFormat = RenderTextureFormat.RGHalf,
                msaaSamples = 1,
                enableRandomWrite = true
            });
            rt.Create();

            Material mat    = new Material( Shader.Find("Hidden/TileGenerator"));
            mat.SetTexture("_RandomTex", tex);
            mat.SetFloat("_TileAlias", alias);
            mat.SetVector("_UVScale", randomUVScale);
            Graphics.Blit(null, rt, mat, 0);

            Texture2D resultTex         = new Texture2D(resolution.x, resolution.y, TextureFormat.RGHalf, false, true);
            ComputeBuffer dataBuffer    = new ComputeBuffer(resolution.x * resolution.y, 16);
            ComputeShader computeShader = Resources.Load<ComputeShader>("ReadRTData");
            computeShader.SetTexture(0, "_TargetTexture", rt);
            computeShader.SetBuffer(0, "_TextureDatas", dataBuffer);
            computeShader.SetInt("_Width", resolution.x);
            computeShader.SetInt("_Height", resolution.y);
            computeShader.Dispatch(0, resolution.x / 8, resolution.y / 8, 1);

            Color[] allColors = new Color[resolution.x * resolution.y];
            dataBuffer.GetData(allColors);
            resultTex.SetPixels(allColors);
            AssetDatabase.CreateAsset(resultTex, path);

            DestroyImmediate(tex);
            DestroyImmediate(mat);
            DestroyImmediate(rt);
            dataBuffer.Dispose();

            if (testMat)
                testMat.SetTexture("_UVTex", resultTex);
        }
    }
}
