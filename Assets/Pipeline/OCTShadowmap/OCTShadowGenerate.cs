using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;






[RequireComponent(typeof(Light))]
public class OCTShadowGenerate : MonoBehaviour
{
    public Shader           depthCopy;
    private RenderTexture   renderTexture;
    public int              rtSize = 2048;


    public List<BOCTree.int3> GenerateShadow( int xLen, int yLen, int zLen, Vector3 offsetWpos, int unitsPerMeter )
    {
        List<BOCTree.int3> shadows  = new List<BOCTree.int3>();
        var cmr                     = GetComponent<Camera>();
        cmr.enabled                 = false;
        cmr.aspect                  = 1;


        renderTexture = RenderTexture.GetTemporary(rtSize, rtSize, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        cmr.targetTexture = renderTexture;
        cmr.RenderWithShader(depthCopy, "");

        Texture2D texDepth = new Texture2D(rtSize, rtSize, TextureFormat.RGBAFloat, false);
        RenderTexture.active = renderTexture;
        texDepth.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texDepth.Apply();
        RenderTexture.active = null;

        var colors = texDepth.GetPixels();
        for( int x = 0; x < xLen; x++ )
            for( int y = 0; y < yLen; y++ )
                for( int z = 0; z < zLen; z++ )
                {
                    var wPos    = new Vector3(x, y, z) / unitsPerMeter + offsetWpos;
                    var depthUV = (GL.GetGPUProjectionMatrix(cmr.projectionMatrix, false) * cmr.worldToCameraMatrix).MultiplyPoint3x4(wPos);
                    depthUV = (depthUV + new Vector3(1, 1, 0)) / 2; // [-1, 1] --> [0, 1]
                    if (depthUV.x < 0 || depthUV.x > 1) continue;
                    if (depthUV.y < 0 || depthUV.y > 1) continue;
                    depthUV *= rtSize;              // 转成像素坐标

                    int colIndex = ((int)depthUV.y) * rtSize + ((int)depthUV.x);
                    if (colIndex < 0 || colIndex >= rtSize * rtSize) continue;
                    
                    var smDis = colors[colIndex].r;
                    var itemDis = -cmr.worldToCameraMatrix.MultiplyPoint(wPos).z;
                    if (smDis <= 0) continue;

                    bool inShadow = itemDis > smDis + 0.5f;
                    if (inShadow)
                    {
                        shadows.Add(new BOCTree.int3() { x = x, y = y, z = z });
                    }
                }

        return shadows;
    }

    void OnDestroy()
    {
        GetComponent<Camera>().targetTexture = null;
        RenderTexture.ReleaseTemporary(renderTexture);
    }
}

