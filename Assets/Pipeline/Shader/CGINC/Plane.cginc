﻿#ifndef __PLANE_INCLUDE__
#define __PLANE_INCLUDE__


//// ½ΊΔΜε
struct Capsule
{
	float3			direction;
	float3			position;
	float			radius;
};

//// Τ²Χ¶Με
struct Cone
{
	float3			vertex;
	float3			direction;
	float			height;
	float			radius;
};


inline float4 GetPlane(float3 normal, float3 inPoint)
{
	return float4(normal, -dot(normal, inPoint));
}


inline float4 GetPlane(float3 a, float3 b, float3 c)
{
	float3 normal = normalize(cross(b - a, c - a));
	return float4(normal, -dot(normal, a));
}


inline float4 GetPlane(float4 a, float4 b, float4 c)
{
	a /= a.w;
	b /= b.w;
	c /= c.w;
	float3 normal = normalize(cross(b.xyz - a.xyz, c.xyz - a.xyz));
	return float4(normal, -dot(normal, a.xyz));
}

/// ??????
inline float GetDistanceToPlane(float4 plane, float3 inPoint)
{
	return dot(plane.xyz, inPoint) + plane.w;
}

/// ???????????
float BoxIntersect(float3 extent, float3 position, float4 planes[6])
{
	float result = 1;
	for (uint i = 0; i < 6; i++)
	{
		float4 plane	= planes[i];
		float3 normal	= abs(plane.xyz);
		result			*= ((dot(position, plane.xyz) - dot(normal, extent)) < -plane.w);
	}
	return result;
}

/// ???????????
float BoxIntersect(float3 extent, float3x3 localtoworld, float position, float4 planes[6])
{
	float result = 1;
	for (uint i = 0; i < 6; i++)
	{
		float4 plane	= planes[i];
		float3 normal   = abs(mul(localtoworld, plane.xyz));
		result			*= ((dot(position, plane.xyz) - dot(normal, extent)) < -plane.w );
	}
	return result;
}


/// ?????????
float SphereIntersect( float4 sphere, float4 planes[6] )
{
	[unroll]
	for( uint i = 0; i < 6; i++ )
	{
		if( GetDistanceToPlane(planes[i], sphere.xyz ) > sphere.w )
			return 0;
	}
	return 1;
}

/// ???????
inline float SphereIntersect( float4 sphere, float4 plane )
{
	return (GetDistanceToPlane( sphere.xyz, plane ) < sphere.w);
}


inline float PointInsidePlane(float3 vertex, float4 plane)
{
	return (dot(plane.xyz, vertex) + plane.w) < 0;
}


inline float SphereInsidePlane(float4 sphere, float4 plane)
{
	return(dot(plane.xyz, sphere.xyz) + plane.w) < sphere.w;
}


inline float ConeInsidePlane(Cone cone, float4 plane)
{
	float3 m = cross(cross(plane.xyz, cone.direction), cone.direction);
	float3 q = cone.vertex + cone.direction * cone.height + normalize(m) * cone.radius;
	return PointInsidePlane(cone.vertex, plane) + PointInsidePlane(q, plane);
}


inline float CapsuleInsidePlane(Capsule cap, float4 plane)
{
	float4 sphere0 = float4(cap.position + cap.direction, cap.radius);
	float4 sphere1 = float4(cap.position - cap.direction, cap.radius);
	return SphereInsidePlane(sphere0, plane) + SphereInsidePlane(sphere1, plane);
}

float ConeIntersect(Cone cone, float4 planes[6])
{
	[unroll]
	for (uint i = 0; i < 6; i++)
	{
		if (ConeInsidePlane(cone, planes[i]) < 0.5)
			return 0;
	}
	return 1;
}

inline float ConeIntersect(Cone cone, float4 plane)
{
	return ConeInsidePlane(cone, plane);
}
#endif
