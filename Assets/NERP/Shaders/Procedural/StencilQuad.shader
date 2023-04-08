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
            ZWrite Off

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
