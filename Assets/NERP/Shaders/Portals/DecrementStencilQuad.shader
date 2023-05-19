Shader "NERP/Portals/DecrementStencilQuad"
{
    Properties
    {
        
    }
    SubShader
    {
        
        Pass
        {
            Name "Stencil Quad --"
            Cull Off
            //ZWrite On
            Blend Zero One // This affects depth, solution: must only affect color

            /*
            Pre Pass:
            - Sets stencil to _StencilID
            Post Pass:
            - Sets stencil to _StencilID

            */

            HLSLPROGRAM
            #pragma vertex StencilQuadVertex
            #pragma fragment StencilQuadFragment

            int _StencilID;

            #include "StencilQuadPass.hlsl"
            ENDHLSL
            
            Stencil
            {
                Comp Always
                Pass DecrSat
                Fail Keep
            }

        }

    }
}
