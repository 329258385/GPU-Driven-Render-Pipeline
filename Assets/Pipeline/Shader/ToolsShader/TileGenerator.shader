﻿Shader "Unlit/TileGenerator"
{
    SubShader
    {
        CGINCLUDE
        #pragma exclude_renderers gles
        #include "UnityCG.cginc"
        #include "../CGINC/Procedural.cginc"
        ENDCG
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Stencil
            {
                Ref 1
                WriteMask 15
                Pass replace
                comp always
            }

            ZTest Equal
            Cull back
            ZWrite off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "../CGINC/ProceduralGeometry.cginc"
            ENDCG
        }

        Pass
        {
            Stencil
            {
                Ref 1
                WriteMask 15
                Pass replace
                comp always
            }

            ZTest Less
            Cull back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "../CGINC/ProceduralGeometry.cginc"
            ENDCG
        }

        Pass
        {
            ZTest Less
            Cull back
            CGPROGRAM
            #pragma vertex vert_depth
            #pragma fragment frag_depth
            struct v2f
            {
                float4 vertex   : SV_POSITION;
            };

            v2f vert_depth(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                float3 v        = getVertex( vertexID, instanceID );
                float4 worldPos = float4(v, 1);

                v2f o;
                o.vertex        = mul( UNITY_MATRIX_VP, worldPos );
                return o;
            }

            float frag_depth(v2f i) : SV_TARGET0
            {
                return i.vertex.z;
            }
            ENDCG
        }
    }
}
