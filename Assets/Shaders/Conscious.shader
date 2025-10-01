Shader "Custom/ConsciousFull"
{
    Properties
    {
        _Blend ("Blend (global)", Range(0,1)) = 0
        _FadeColor ("Fade Color", Color) = (0,0,0,1)

        _BlurAmount ("Blur Amount", Range(0,8)) = 0.0
        _RadialSmear ("Radial Smear Strength", Range(0,2)) = 0.0
        _DoubleStrength ("Double Vision Strength", Range(0,1)) = 0.0
        _Chroma ("Chromatic Aberration", Range(0,8)) = 0.0

        _Vignette ("Vignette Strength", Range(0,1)) = 0.25
        _VignetteSoftness ("Vignette Softness", Range(0.1,3)) = 1.4

        _EyeClose ("Eye Close Amount", Range(0,1)) = 0.0
        _EyeSoftness ("Eyelid Softness", Range(0.0,1.0)) = 0.15

        _DoubleOffset ("Double Offset (px)", Range(0,40)) = 8

        _WobbleStrength ("Wobble Strength", Range(0,1)) = 0.0
        _WobbleSpeed ("Wobble Speed", Range(0,8)) = 1.0
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
            Name "ConsciousnessFullPass"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { uint vertexID : SV_VertexID; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            float _Blend;
            float4 _FadeColor;
            float _BlurAmount;
            float _RadialSmear;
            float _DoubleStrength;
            float _Chroma;
            float _Vignette;
            float _VignetteSoftness;
            float _EyeClose;
            float _EyeSoftness;
            float _DoubleOffset;
            float _WobbleStrength;
            float _WobbleSpeed;

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = GetFullScreenTriangleVertexPosition(IN.vertexID);
                OUT.uv = GetFullScreenTriangleTexCoord(IN.vertexID);
                return OUT;
            }

            float3 SampleChromatic(float2 uv, float2 offset)
            {
                float2 ofsR = offset * (_Chroma * 0.5);
                float2 ofsB = -offset * (_Chroma * 0.5);
                half4 r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + ofsR);
                half4 g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                half4 b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + ofsB);
                return float3(r.r, g.g, b.b);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                // EARLY OUT: if blend is effectively zero, just return source pixel (cheap).
                if (_Blend <= 1e-4)
                {
                    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, IN.uv);
                }

                float2 uv = IN.uv;
                float2 texel = _BlitTexture_TexelSize.xy;
                float2 center = float2(0.5, 0.5);
                float2 toCenter = center - uv;
                float dist = length(toCenter);

                // WOBBLE (time-based small UV jitter)
                if (_WobbleStrength > 1e-5)
                {
                    float t = _Time.y * _WobbleSpeed;
                    float wob = sin(uv.y * 12.0 + t) * 0.5 + cos(uv.x * 8.0 + t * 1.3) * 0.5;
                    float2 wobOffset = float2(wob * _WobbleStrength * _Blend * 0.0025, wob * _WobbleStrength * _Blend * 0.002);
                    uv += wobOffset;
                }

                // Eyelid mask -> closes from top & bottom into center
                float topT = smoothstep(0.5 - _EyeClose - _EyeSoftness, 0.5 - _EyeClose + _EyeSoftness, uv.y);
                float botT = 1.0 - smoothstep(0.5 + _EyeClose - _EyeSoftness, 0.5 + _EyeClose + _EyeSoftness, uv.y);
                float eyeMask = saturate(min(topT, botT));
                eyeMask = lerp(eyeMask, 0.0, _Blend);

                // Vignette factor
                float vign = smoothstep(0.5, _VignetteSoftness * 0.5 + 0.5, dist * (1.0 + _Vignette));
                vign = saturate(1.0 - vign * _Vignette);

                // base sample
                float3 baseColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv).rgb;

                // Blur approximation (cheap multi-tap)
                float blur = _BlurAmount * _Blend;
                float3 blurCol = baseColor;
                if (blur > 0.001)
                {
                    const int TAP = 7;
                    float2 offsets[TAP];
                    offsets[0] = float2(0,0);
                    offsets[1] = float2(1,0);
                    offsets[2] = float2(-1,0);
                    offsets[3] = float2(0,1);
                    offsets[4] = float2(0,-1);
                    offsets[5] = float2(1,1);
                    offsets[6] = float2(-1,-1);

                    float w[TAP];
                    w[0] = 0.4; w[1] = 0.12; w[2] = 0.12; w[3] = 0.12; w[4] = 0.12; w[5] = 0.06; w[6] = 0.06;

                    float2 offsScale = texel * blur * 0.8;
                    blurCol = float3(0,0,0);
                    for (int i=0;i<TAP;i++)
                        blurCol += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + offsets[i] * offsScale).rgb * w[i];
                }

                // Radial smear
                float radialMix = saturate(_RadialSmear * _Blend);
                float3 radialCol = blurCol;
                if (radialMix > 0.0001)
                {
                    const int STEPS = 5;
                    float3 acc = float3(0,0,0);
                    float tot = 0;
                    for (int i=1;i<=STEPS;i++)
                    {
                        float t = i / (float)STEPS;
                        float2 sampleUV = uv + toCenter * (t * radialMix * 0.08);
                        acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, sampleUV).rgb * (1.0 - (t * 0.6));
                        tot += (1.0 - (t * 0.6));
                    }
                    radialCol = lerp(blurCol, acc / tot, radialMix);
                }

                // Double vision
                float doubleMix = saturate(_DoubleStrength * _Blend);
                float3 doubleCol = radialCol;
                if (doubleMix > 0.0001)
                {
                    float px = _DoubleOffset * texel.x;
                    float2 offset = float2(px, 0);
                    float3 left = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv - offset).rgb;
                    float3 right = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + offset).rgb;
                    doubleCol = lerp(doubleCol, lerp(left, right, 0.5), doubleMix);
                }

                // Chromatic aberration
                float chromaMix = saturate(_Chroma * _Blend * 0.02);
                float3 chromaCol = doubleCol;
                if (chromaMix > 0.00001)
                {
                    float2 chromaOff = normalize(max(abs(toCenter), 1e-6)) * chromaMix;
                    chromaCol = SampleChromatic(uv, chromaOff);
                }

                float3 finalCol = chromaCol * vign;

                // eyelids -> lerp to fade color where closed
                finalCol = lerp(_FadeColor.rgb, finalCol, eyeMask);

                // small global fade to fadeColor (keeps old behavior)
                finalCol = lerp(finalCol, _FadeColor.rgb, _Blend * 0.25);

                return half4(finalCol, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
