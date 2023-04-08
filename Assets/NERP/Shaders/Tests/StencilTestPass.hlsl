#ifndef NERP_STENCILTEST_PASS_INCLUDED
#define NERP_STENCILTEST_PASS_INCLUDED


#include "../../ShaderLibrary/Common.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
};

struct V2F {
	float4 positionCS : SV_POSITION;
};

V2F StencilPassVertex(Attributes input) {
	V2F output;
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	
	return output;
}

float4 StencilPassFragment(V2F input) : SV_TARGET {
	
	return float4(0, 1, 0, 1);
}




#endif
