Shader "NERP/Procedural/StencilDecrement"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID", Range(0, 255)) = 0
    }
    SubShader
    {
        
        Pass
        {
            Cull Off
            //ZWrite Off
            ZTest Off
            Blend Zero One

            HLSLPROGRAM
            #pragma vertex StencilDecrementPassVertex
            #pragma fragment StencilDecrementPassFragment
            #include "StencilDecrementPass.hlsl"
            ENDHLSL
            
            Stencil
            {
                Ref[_StencilID]
			    Comp Equal
                Pass DecrSat
                Fail Keep
            }

        }

    }
}
