Shader "Custom/GlitchEffect"
{   
  Properties
    {
        _Blend("Blend Amount", Range(0, 1)) = 0
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.5
        _GlitchSpeed("Glitch Speed", Range(0, 10)) = 5
        _ColorShift("Color Shift (px)", Range(0, 32)) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Properties
        float _Blend;
        float _GlitchIntensity;
        float _GlitchSpeed;
        float _ColorShift;

        // pseudo-random
        float rand(float2 co) { return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453); }

        // Vertex struct provided by Blit.hlsl: Varyings with texcoord
        float4 Frag(Varyings input) : SV_Target
        {
            float2 uv = input.texcoord;
            // sample the current frame (Blit.hlsl defines _BlitTexture & sampler_LinearClamp)
            float4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

            if (_Blend <= 0.001)
                return original;

            // time
            float t = _Time.y * _GlitchSpeed;

            // generate per-scanline glitch mask (coarse)
            float scan = floor(uv.y * 256.0 + t);
            float chance = rand(float2(scan, t));
            float doGlitch = step(1.0 - _GlitchIntensity, chance);

            // horizontal displacement amount (scaled by Blend & intensity)
            float shiftAmount = (rand(float2(uv.y * 1000.0, t)) - 0.5) * 0.05 * _GlitchIntensity * _Blend;

            // compute glitch UV
            float2 gUV = uv;
            gUV.x += doGlitch * shiftAmount;

            // color channel shift in pixels -> convert px to UV
            float pxShift = _ColorShift * _Blend; // in pixels
            float2 shiftUV = float2(pxShift / _ScreenParams.x, 0);

            // sample channels with slight offsets
            float r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, gUV + shiftUV).r;
            float g = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, gUV).g;
            float b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, gUV - shiftUV).b;

            float4 glitchCol = float4(r, g, b, 1.0);

            // final blend between original and glitch
            return lerp(original, glitchCol, _Blend);
        }

        ENDHLSL

        Pass
        {
            Name "FullscreenGlitch"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
