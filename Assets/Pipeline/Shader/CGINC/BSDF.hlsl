#ifndef __BSDF_INCLUDE__
#define __BSDF_INCLUDE__
#include "Common.hlsl"
#include "Montcalo.hlsl"


#define TRANSMISSION_WRAP_ANGLE ( PI/12)
#define TRANSMISSION_WRAP_LIGHT cos(PI/2 - TRANSMISSION_WRAP_ANGLE)

struct BSDFContext
{
	float	NoL;
	float	NoV;
	float	NoH;
	float   LoH;
	float	VoH;
};


//// 发现重定向
float3 Unity_NormalBlend_Reoriented_float(float3 A, float3 B)
{
	float3 t = A.xyz + float3( 0.0,  0.0, 1);
	float3 u = B.xyz * float3(-1.0, -1.0, 0);
	return (t / t.z) * dot(t, v) - u;
}

void InitGeoData(inout BSDFContext LightData, float3 N, float3 V)
{
	LightData.NoV = dot(N, V);
	N			  = LightData.NoV < 0.0f ? N + V * (-LightData.NoV + 1e-5f) : N;
	LightData.NoV = dot(N, V);
}

void InitLightingData(inout BSDFContext LightData, float3 N, float3 V, float3 L, float3 H)
{
	LightData.NoL = saturate(dot(N, L));
	LightData.NoH = saturate(dot(N, H));
	LightData.LoH = saturate(dot(L, H));
	LightData.VoH = saturate(dot(V, H));
}

/// 切线和副发现与各个方向的dot值
struct AnisoBSDFContext
{
	float ToH;
	float ToL;
	float ToV;
	float BoH;
	float BoL;
	float BoV;
};

void Init_Aniso(inout AnisoBSDFContext LightData, float3 Tangent, float3 Bitangent, float3 H, float3 L, float3 V)
{
	LightData.ToH = dot(Tangent, H);
	LightData.ToL = dot(Tangent, L);
	LightData.ToV = dot(Tangent, V);

	LightData.BoH = dot(Bitangent, H);
	LightData.BoL = dot(Bitangent, L);
	LightData.BoV = dot(Bitangent, V);
}

/// [IOR <------> Fresnel] 【折射率 《------》 菲涅尔】
float IorToFresnel(float transmittedIor, float incidentIor)
{
	return pow2(transmittedIor - incidentIor) / pow2(transmittedIor + incidentIor);
}

float3 IorToFresnel(float3 transmittedIor, float3 incidentIor)
{
	return pow2(transmittedIor - incidentIor) / pow2(transmittedIor + incidentIor);
}

float FresnelToIOR(float fresnel0)
{
	return (1 + pow2(fresnel0)) / (1 - pow2(fresnel0));
}

float3 FresnelToIOR(float3 fresnel0)
{
	return (1 + pow2(fresnel0)) / (1 - pow2(fresnel0));
}


float F_Schlick(float F0, float F90, float HoV)
{
	return F0 + (F90 - F0) * pow5(1 - HoV);
}

float3 F_Schlick(float3 F0, float3 F90, float HoV)
{
	float FC = pow5(1 - HoV);
	return saturate(50 * F0.g) * FC + (1 - FC) * F0;
}


//////////////////////////////////////////////////////////////
/// Specular
float D_GGX(float NoH, float Roughness)
{
	Roughness		= pow4( Roughness );
	float D			= (NoH * Roughness - NoH ) * NoH + 1;
	return Roughness / (PI * pow2(D));
}

float D_Beckmann(float NoH, float Roughness)
{
	Roughness		= pow4( clamp( Roughness, 0.08, 1 ));
	NoH				= pow2( NoH );
	return exp((NoH - 1) / (Roughness * NoH)) / (PI * Roughness * NoH);
}

float2 D_Beckmann(float NoH, float2 Roughness)
{
	Roughness		= pow4(Roughness);
	NoH				= pow2(NoH);
	return exp((NoH - 1) / (Roughness * NoH)) / (PI * Roughness * NoH);
}


/// 个向异性
float D_AnisotropyGGX(float ToH, float BoH, float NoH, float RoughnessT, float RoughnessB)
{
	float D			= ToH * ToH / pow2(RoughtnessT) + BoH * BoH / pow2(RoughnessB) + pow2( NoH );
	return 1 / (RoughnessT * RoughnessB * pow2(D));
}
#endif