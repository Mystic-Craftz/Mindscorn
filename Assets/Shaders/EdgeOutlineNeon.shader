Shader "Custom/EdgeOutlineNeon"
{
    Properties
    {
        _Blend("Blend Amount", Range(0,1)) = 0
        _SceneDarkness("Scene Darkness", Range(0,1)) = 0.45
        _EdgeThreshold("Edge Threshold", Range(0,1)) = 0.18
        _DepthWeight("Depth Weight", Range(0,10)) = 1.8
        _ColorWeight("Color Weight", Range(0,10)) = 1.0
        _SampleRadius("Sample Radius", Range(0.1,4)) = 1.0
        _EdgeColor("Edge Color", Color) = (0,0.949,0.949,1)
        _EdgeIntensity("Edge Intensity", Range(0,10)) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

        float _Blend;
        float _SceneDarkness;
        float _EdgeThreshold;
        float _DepthWeight;
        float _ColorWeight;
        float _SampleRadius;
        float4 _EdgeColor;
        float _EdgeIntensity;

        // helper: sample depth -> linear 0..1
        float SampleLinearDepth(float2 uv)
        {
            return Linear01Depth(SampleSceneDepth(uv), _ZBufferParams);
        }

        // sample color helper
        float3 SampleColor(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb;
        }

        // 3x3 SOBEL on depth (returns scalar)
        float ComputeSobelDepth(float2 uv, float2 texel)
        {
            float d00 = SampleLinearDepth(uv + texel * float2(-1,  1));
            float d10 = SampleLinearDepth(uv + texel * float2( 0,  1));
            float d20 = SampleLinearDepth(uv + texel * float2( 1,  1));
            float d01 = SampleLinearDepth(uv + texel * float2(-1,  0));
            float d11 = SampleLinearDepth(uv + texel * float2( 0,  0));
            float d21 = SampleLinearDepth(uv + texel * float2( 1,  0));
            float d02 = SampleLinearDepth(uv + texel * float2(-1, -1));
            float d12 = SampleLinearDepth(uv + texel * float2( 0, -1));
            float d22 = SampleLinearDepth(uv + texel * float2( 1, -1));

            float gx = (d20 + 2.0*d21 + d22) - (d00 + 2.0*d01 + d02);
            float gy = (d02 + 2.0*d12 + d22) - (d00 + 2.0*d10 + d20);
            return sqrt(gx*gx + gy*gy);
        }

        // 3x3 SOBEL on color (returns scalar)
        float ComputeSobelColor(float2 uv, float2 texel)
        {
            float3 c00 = SampleColor(uv + texel * float2(-1,  1));
            float3 c10 = SampleColor(uv + texel * float2( 0,  1));
            float3 c20 = SampleColor(uv + texel * float2( 1,  1));
            float3 c01 = SampleColor(uv + texel * float2(-1,  0));
            float3 c11 = SampleColor(uv + texel * float2( 0,  0));
            float3 c21 = SampleColor(uv + texel * float2( 1,  0));
            float3 c02 = SampleColor(uv + texel * float2(-1, -1));
            float3 c12 = SampleColor(uv + texel * float2( 0, -1));
            float3 c22 = SampleColor(uv + texel * float2( 1, -1));

            float3 gx = (c20 + 2.0*c21 + c22) - (c00 + 2.0*c01 + c02);
            float3 gy = (c02 + 2.0*c12 + c22) - (c00 + 2.0*c10 + c20);
            // combine vector gradients into scalar
            float g = length(gx) + length(gy);
            return g;
        }

        float4 Frag(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            float3 center = SampleColor(uv);

            if (_Blend < 0.001)
                return float4(center, 1);

            // texel in UV coordinates
            float2 texel = _SampleRadius / _ScreenParams.xy;

            // Compute edges
            float dEdge = ComputeSobelDepth(uv, texel) * _DepthWeight;
            float cEdge = ComputeSobelColor(uv, texel) * _ColorWeight;
            float edgeRaw = saturate(dEdge + cEdge);

            // apply threshold and smoothness
            float edge = smoothstep(_EdgeThreshold * 0.5, _EdgeThreshold, edgeRaw);

            // boost edge for visibility and bloom
            edge *= _EdgeIntensity;

            // final neon color
            float3 neon = _EdgeColor.rgb * edge;

            // darken original scene a bit so neon stands out
            float3 dark = center * (1.0 - _SceneDarkness);

            float3 neonScene = dark + neon;

            float3 outCol = lerp(center, neonScene, _Blend);

            return float4(outCol, 1);
        }

        ENDHLSL

        Pass
        {
            Name "EdgeOutlineNeon"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
