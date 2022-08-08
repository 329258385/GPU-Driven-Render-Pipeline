using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;




namespace Tools
{
    public class MipmapsSharperImporter : AssetPostprocessor
    {
        bool    m_isReadable;
        bool    m_importing;
        void OnPostprocessTexture( Texture2D texture )
        {
            if( texture != null )
            {
                for( int mip = 1; mip < texture.mipmapCount; mip++ )
                {
                    shaper(texture, mip);
                }

                texture.Apply(false, !m_isReadable);
                TextureImporter textureImporter = (TextureImporter)assetImporter;
                textureImporter.isReadable      = m_isReadable;
            }
        }


        private void shaper( Texture2D tex, int mip )
        {
            if (mip == 0) return;
            float _shapness = 0.07f * 2;
            var clr         = tex.GetPixels();
            int w           = tex.width / ( 1 << mip );
            int h           = tex.height /( 1 << mip );
            int halfRange   = 1;

            for( int x = 0; x < w; x++ )
            {
                for( int y = 0; y < h; y++ )
                {
                    var color = clr[y * w + x];
                    Color sum = Color.black;
                    for( int i = -halfRange; i < halfRange; i++ )
                    {
                        for( int j = -halfRange; j < halfRange; j++ )
                        {
                            sum += clr[Mathf.Clamp(y + j, 0, h - 1) * w + Mathf.Clamp(x + i, 0, w - 1)];
                        }
                    }

                    //八领域拉普拉斯算子 sobel8
                    Color sobel8 = color * Mathf.Pow(halfRange * 2 + 1, 2) - sum;
                    var addcolor = sobel8 * _shapness;
                    color += addcolor;
                    clr[y * w + x] = color;
                }
            }

            tex.SetPixels(clr, mip);
        }
    }
}
