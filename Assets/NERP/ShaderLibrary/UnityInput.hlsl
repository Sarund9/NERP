#ifndef NERP_UNITY_INPUT_INCLUDED
#define NERP_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
real4 unity_WorldTransformParams;

// Needed to be added for latest versions
// what the f### is this
float4x4 unity_PrevObjectToWorld;
float4x4 unity_PrevWorldToObject;

float3 _WorldSpaceCameraPos;


CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

#endif
