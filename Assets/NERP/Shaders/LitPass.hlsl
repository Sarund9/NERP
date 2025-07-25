#ifndef NERP_LIT_PASS_INCLUDED
#define NERP_LIT_PASS_INCLUDED


// Common.hlsl included in LitInput
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/GI.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 baseUV : TEXCOORD0;
	GI_ATTRIBUTE_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct V2F {
	float4 positionCS : SV_POSITION;
	float3 positionWS : VAR_POSITION;
	float3 normalWS : VAR_NORMAL;
#if defined(_NORMAL_MAP)
	float4 tangentWS : VAR_TANGENT;
#endif
	float2 baseUV : VAR_BASE_UV;
	float2 detailUV : VAR_DETAIL_UV;
	GI_VARYINGS_DATA
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

V2F LitPassVertex(Attributes input) {
	V2F output;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	TRANSFER_GI_DATA(input, output);
	output.positionWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(output.positionWS);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_NORMAL_MAP)
	output.tangentWS =
		float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
#endif

	output.baseUV = TransformBaseUV(input.baseUV);
	output.detailUV = TransformDetailUV(input.baseUV);
	return output;
}


float4 LitPassFragment(V2F input) : SV_TARGET {
	UNITY_SETUP_INSTANCE_ID(input);

	ClipLOD(input.positionCS.xy, unity_LODFade.x);

	float4 base = GetBase(input.baseUV, input.detailUV);
#if defined(_CLIPPING)
	clip(base.a - GetCutoff(input.baseUV));
#endif
	
	Surface surface;
	surface.position = input.positionWS;
#if defined(_NORMAL_MAP)
	surface.normal = NormalTangentToWorld(
		GetNormalTS(input.baseUV, input.detailUV), input.normalWS, input.tangentWS
	);
	surface.interpolatedNormal = input.normalWS;
#else
	surface.normal = normalize(input.normalWS);
	surface.interpolatedNormal = surface.normal;
#endif
	surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
	surface.depth = -TransformWorldToView(input.positionWS).z;
	surface.color = base.rgb;
	surface.alpha = base.a;
	surface.metallic = GetMetallic(input.baseUV);
	surface.occlusion = GetOcclusion(input.baseUV);
	surface.smoothness = GetSmoothness(input.baseUV, input.detailUV);
	surface.fresnelStrength = GetFresnel(input.baseUV);
	surface.dither = InterleavedGradientNoise(input.positionCS.xy, 0);

#if defined(_PREMULTIPLY_ALPHA)
	BRDF brdf = GetBRDF(surface, true);
#else
	BRDF brdf = GetBRDF(surface);
#endif
	GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
	float3 color = GetLighting(surface, brdf, gi);
	color += GetEmission(input.baseUV);
	return float4(color, surface.alpha);
}




#endif
