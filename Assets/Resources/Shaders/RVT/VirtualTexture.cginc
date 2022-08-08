#ifndef VIRTUAL_TEXTURE_INCLUDED
#define VIRTUAL_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"







struct vt_appdata
{
	float4 vertex			: POSITION;
	float2 texcoord			: TEXCOORD0;
};

struct vt_v2f
{
	float4 pos				: SV_POSITION;
	float2 uv				: TEXCOORD0;
};

// 设置预渲染着色器参数
// x: page size					页表大小(单位: 页)
// y: vertual texture size		虚拟贴图大小(单位: 像素) 256 * 256 * scale
// z: max mipmap level			最大mipmap等级
// w: mipmap level bias
float4 						_VTFeedbackParam;

// xy: page count
// z:  max mipmap level
float4 						_VTPageParam;

/// 超大纹理参数
// x: padding size
// y: center size
// zw: 1 / tile count
float4 						_VTTileParam;


/// 真实世界大小参数
// x: world size min x
// y: world size min y
// z: world size max x
// w: world size max y
float4 						_VTWorldRect;

sampler2D 					_VTLookupTex;
sampler2D 					_VTDiffuse;
sampler2D 					_VTNormal;
	


vt_v2f vt_vertfrompos( vt_appdata v )
{
	vt_v2f o;

	VertexPositionInputs Attributes = GetVertexPositionInputs(v.vertex.xyz);
	o.pos							= Attributes.positionCS;
	float2 worldPos					= Attributes.positionWS.xz;
	o.uv 							= ( worldPos - _VTWorldRect.xy ) / _VTWorldRect.zw;
	return o;
}



vt_v2f VTVert( vt_appdata v )
{
	vt_v2f o;
    o.pos						= TransformObjectToHClip( v.vertex.xyz );
    o.uv						= v.texcoord;
    return o;
}


float2 VTTransferUV(float2 uv)
{
	float2 uvInt 				= uv - frac(uv * _VTPageParam.x) * _VTPageParam.y;
	float4 page 				= tex2D(_VTLookupTex, uvInt) * 255;
	float2 inPageOffset			= frac(uv * exp2(_VTPageParam.z - page.b));
    return (page.rg * (_VTTileParam.y + _VTTileParam.x * 2) + inPageOffset * _VTTileParam.y + _VTTileParam.x) / _VTTileParam.zw;
}

float4 VTTex2DDiffuse(float2 uv)
{
    //return fixed4(uv, 0, 1);
	return tex2D(_VTDiffuse, uv);
}

float4 VTTex2D1(float2 uv)
{
    return tex2D(_VTNormal, uv);
}

float4 VTTex2D(float2 uv)
{
    return VTTex2DDiffuse(uv);
}

#endif
