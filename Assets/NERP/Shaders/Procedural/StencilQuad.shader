Shader "NERP/Procedural/StencilQuad"
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
            ZWrite On
            Blend One Zero

            /*
            Pre Pass:
            - Sets stencil to _StencilID
            Post Pass:
            - Sets stencil to _StencilID

            */

            HLSLPROGRAM
            #pragma vertex StencilQuadVertex
            #pragma fragment StencilQuadFragment
            #include "StencilQuadPass.hlsl"
            ENDHLSL
            
            Stencil
            {
                Comp Always
                Pass IncrSat
                Fail Keep
            }

        }

    }
}
