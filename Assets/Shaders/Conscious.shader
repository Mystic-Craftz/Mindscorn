Shader "Custom/Conscious"
{   
    Properties
    {
        _Blend ("Blend", Range(0, 1)) = 0
        _Color ("Color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        LOD 100
        
        ZTest Always 
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ConsciousnessEffect"
            
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);

            float _Blend;
            float4 _Color;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                
                // Fullscreen triangle vertex shader - simplified version
                float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
                float2 uv = GetFullScreenTriangleTexCoord(input.vertexID);
                
                output.positionCS = pos;
                output.uv = uv;
                
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Sample the source texture - no UV flipping
                half4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, input.uv);
                
                // Blend towards black based on _Blend value
                return lerp(sceneColor, _Color, _Blend);
            }
            ENDHLSL
        }
    }
}