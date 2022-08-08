Shader "myPipeline/VT/VTFeedbackDownScale"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque"}
        Cull Off ZWrite Off ZTest Always

        Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#include "VTFeedback.cginc"	
			#pragma vertex VTVert
			#pragma fragment frag
			float4 frag(vt_v2f i) : SV_Target
			{
				return GetMaxFeedback(i.uv, 2);
			}
			ENDHLSL
		}
		
		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#include "VTFeedback.cginc"	
			#pragma vertex VTVert
			#pragma fragment frag
			float4 frag(vt_v2f i) : SV_Target
			{
				return GetMaxFeedback(i.uv, 4);
			}
			ENDHLSL
		}
		
		Pass
		{
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#include "VTFeedback.cginc"	
			#pragma vertex VTVert
			#pragma fragment frag
			float4 frag(vt_v2f i) : SV_Target
			{
				return GetMaxFeedback(i.uv, 8);
			}
			ENDHLSL
		}
    }
}
