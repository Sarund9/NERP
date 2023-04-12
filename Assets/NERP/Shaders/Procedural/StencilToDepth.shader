Shader "NERP/Procedural/StencilToDepth"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [IntRange] _StencilID ("Stencil ID", Range(0, 255)) = 0
    }
    SubShader
    {
        
        Pass
        {
            Cull Off
            //ZWrite Off
            ZTest Off

            HLSLPROGRAM
            #pragma vertex StencilToDepthPassVertex
            #pragma fragment StencilToDepthPassFragment
            #include "StencilToDepthPass.hlsl"
            ENDHLSL
            
            Stencil
            {
                Ref[_StencilID]
			    Comp Equal
            }

        }

    }
}
