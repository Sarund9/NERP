#ifndef NERP_UNLIT_PASS_INCLUDED
#define NERP_UNLIT_PASS_INCLUDED


#include "../ShaderLibrary/Common.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
	float2 baseUV : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct V2F {
	float4 positionCS : SV_POSITION;
	float2 baseUV : VAR_BASE_UV;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

V2F UnlitPassVertex(Attributes input) {
	V2F output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	float3 positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(positionWS);
	
	output.baseUV = TransformBaseUV(input.baseUV);
	return output;
}

float4 UnlitPassFragment(V2F input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);

	float4 base = GetBase(input.baseUV);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif
	
	return base;
}




#endif
