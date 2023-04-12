Shader "NERP/Post/ViewDepth"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        
    }
    SubShader
    {
        ZTest Off
        ZWrite Off

        Pass {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PostProcessVertex
            #pragma fragment ViewDepthPassFragment
            #include "ViewDepthPass.hlsl"

            ENDHLSL
        }
    }
}
