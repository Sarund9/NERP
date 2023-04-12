Shader "NERP/Procedural/DepthQuad"
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
            Blend Zero One

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
                Ref[_StencilID]
                Comp Always
                Pass Replace
                Fail Keep
            }

        }

    }
}
