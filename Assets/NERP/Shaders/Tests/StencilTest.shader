Shader "NERP/Test/Stencil"
{
	
	Properties
	{
		[IntRange] _StencilID("Stencil ID", Range(0, 255)) = 0
	}
	
	SubShader
	{
		Stencil
		{
			Ref[_StencilID]
			Comp Equal
			//Fail Replace
			/*Pass Keep
			Fail Keep*/
		}
		Pass
		{
			ZTest Off
			

			HLSLPROGRAM
			#pragma target 3.5
			
			#pragma vertex StencilPassVertex
			#pragma fragment StencilPassFragment
			#include "StencilTestPass.hlsl"
			ENDHLSL

		}
	}
}