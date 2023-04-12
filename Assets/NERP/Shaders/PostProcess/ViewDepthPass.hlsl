#ifndef NERP_VIEWDEPTH_PASS_INCLUDED
#define NERP_VIEWDEPTH_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/PostProcess.hlsl"

sampler2D _CameraDepthTexture;

float4 ViewDepthPassFragment(V2F input) : SV_TARGET{

	// zBufferParam = { (f-n)/n, 1, (f-n)/n*f, 1/f }
	float f = 10;
	float n = .1;
	
	float4 zBufferParam = 0;
	zBufferParam.x = (f - n) / n;
	zBufferParam.y = (f - n) / n * f;


	float depth = tex2D(_CameraDepthTexture, input.UV).r;

	depth = Linear01Depth(depth, zBufferParam);

	return depth + .01; //
}


#endif
