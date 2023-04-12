#ifndef NERP_STENCILQUAD_PASS_INCLUDED
#define NERP_STENCILQUAD_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"

struct V2F {
	float3 positionWS : VAR_POSITION;
	float4 positionCS : SV_POSITION;
	float depth : TEXCOORD0;
};

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

V2F StencilToDepthPassVertex(uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID) {
	V2F output;

	float3 positionOS = IndexOfQuad(vertex_id);
	positionOS.z = 0;
	output.positionCS = float4(positionOS, 0);

	return output;
}


void StencilToDepthPassFragment(V2F input
	//, out float4 col : SV_TARGET
	, out float depth : SV_DEPTH
) {
	//col = float4(1, 0, 0, 1);
	depth = 0; // 0 == far
}




#endif
