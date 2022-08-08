#ifndef _PROCEDURAL_GEOMETRY
#define _PROCEDURAL_GEOMETRY
#include "Procedural.cginc"



StructuredBuffer<MaterialProperties> _MaterialBuffer;
struct v2f
{
	float4 vertex			: SV_POSITION;
	float2 uv				: TEXCOORD0;
	float4 worldTangent		: TEXCOORD1;
	float4 worldBinormal	: TEXCOORD2;
	float4 worldNormal		: TEXCOORD3;
	float3 screenUV			: TEXCOORD4;
	nointerpolation uint materialID : TEXCOORD5;
};


float4 SampleTex(Texture2DArray<float4> tex, SamplerState samp, float2 uv, int index, float4 defaultValue)
{
	[branch]
	if (index < 0) return defaultValue;
	return tex.Sample(samp, float3(uv, index));
}


v2f vert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
{
	uint materialID;
	Point v			= getVertexWithMat(vertexID, instanceID, materialID);
	float4 worldPos = float4(v.vertex, 1);

	v2f o;
	o.vertex		= mul(UNITY_MATRIX_VP, worldPos );
	o.worldTangent	= float4(v.tangent.xyz, worldPos.x);
	o.worldNormal	= float4(v.normal, worldPos.z);
	o.worldBinormal = float4(cross(o.worldNormal.xyz, o.worldTangent.xyz) * v.tangent.w, worldPos.y);
	o.screenUV		= ComputeScreenPos(o.vertex).xyw;
	o.uv			= v.uv0;
	o.materialID	= materialID;
	return o;
}


void frag(v2f IN,	out float4 outGBuffer1 : SV_Target0,
					out float4 outGBuffer2 : SV_Target1,
					out float4 outGBuffer3 : SV_Target2,
					out float4 outEmission : SV_Target3 )
{

	float depth				= IN.vertex.z;
	float linearEye			= LinearEyeDepth(depth);
	float2 screenUV			= IN.screenUV.xy / IN.screenUV.z;
	
	float3 worldPos			= float3(IN.worldTangent.w, IN.worldBinormal.w, IN.worldNormal.w);
	float3 worldViewDir		= normalize(_WorldSpaceCameraPos - worldPos.xyz);

	IN.worldTangent.xyz		= normalize(IN.worldTangent.xyz);
	IN.worldBinormal.xyz	= normalize(IN.worldBinormal.xyz);
	IN.worldNormal.xyz		= normalize(IN.worldNormal.xyz);

	/// ÇÐÏß¿Õ¼ä
	float3x3 wdMatrixNormalized = float3x3(IN.worldTangent.xyz, IN.worldBinormal.xyz, IN.worldNormal.xyz);
	float3x3 wdMatrix		= float3x3((IN.worldTangent.xyz) * matProp._NormalIntensity.x, (IN.worldBinormal.xyz) * matProp._NormalIntensity.y, (IN.worldNormal.xyz));

	///Surface Shader
	SurfaceOutputStandardSpecular o;
}
#endif
