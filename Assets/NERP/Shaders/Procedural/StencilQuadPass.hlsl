#ifndef NERP_STENCILQUAD_PASS_INCLUDED
#define NERP_STENCILQUAD_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"

struct V2F {
	float3 positionWS : VAR_POSITION;
	float4 positionCS : SV_POSITION;

	uint vertex_id : TEXTCOORD0;
	uint instance_id : TEXTCOORD1;
	float3 cameraPositionWS : TEXTCOORD2;
};

StructuredBuffer<float4x4> _PortalPlanes;

float3 IndexOfQuad(uint index) {
	float3 pos;
	pos.z = 0;

	// Desmos function:
	// C\left(10\sin\left(\frac{x\pi}{a}\right)-1\right)
	float xfactor = (4 * sin(index * PI / 3)) - 1;
	pos.x = clamp(xfactor, -1, 1);

	float yfactor = (2 * float(index)) - 3;
	pos.y = clamp(yfactor, -1, 1);

	return pos;
}

float3 _PortalExtents;
float3 _PortalForward;

V2F StencilQuadVertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID) {
	V2F output;

	float3 positionBase = IndexOfQuad(vertex_id);
	float4x4 transform = _PortalPlanes[instance_id];
	
	float3 positionOS = mul(transform, float4(positionBase, 1)) * _PortalExtents * .995f;

	output.positionWS = TransformObjectToWorld(positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	
	output.vertex_id = vertex_id;
	output.instance_id = instance_id;
	

	return output;
}

// Discards the pixel if it's on the same half as the camera
bool Discard(float3 cposWS, float3 opos, float3 posWS, float3 pfwdWS)
{
	// Camera <- Object vector
	float3 dir = cposWS - opos;

	float cdot = dot(dir, pfwdWS);
	// Is camera in front or back of portal
	float sdot = sign(cdot);

	// Local Fragment position
	float3 fposOS = TransformWorldToObject(posWS);

	// Is fragment in portal's front or back
	float sfpz = sign(fposOS.z);

	// If fragment is close to the central plane, drawAnyways
	float drawAnyways = abs(fposOS.z) < .02;

	// If fragment is on the side of the camera
	float equals = sfpz != sdot;

	// Final result
	float cut = max(drawAnyways, equals);

	// discard
	return cut < .5;
}


float4 StencilQuadFragment(V2F input) : SV_TARGET{
	
	if (Discard(
		// Camera Position in WS
		_WorldSpaceCameraPos,
		// Object Position in WS
		TransformObjectToWorld(float4(0, 0, 0, 1)).xyz,
		// Fragment Position in WS
		input.positionWS,
		// Forward portal normal
		_PortalForward
	))
		discard;

	return 1;
}




#endif
