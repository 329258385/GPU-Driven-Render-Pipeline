Shader "myPipeline/VT/DebugMipmap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}
		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#include "VTDebug.cginc"	
			#pragma vertex VTVert
			#pragma fragment frag

			sampler2D _MainTex;

			float4 frag(vt_v2f i) : SV_Target
			{
				return VTDebugMipmap(_MainTex, i.uv);
			}
			ENDHLSL
		}
    }
}
