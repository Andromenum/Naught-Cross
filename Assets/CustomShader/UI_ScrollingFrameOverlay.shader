Shader "UI/ScrollingFrameFromSprite_3Tint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame Sprite", 2D) = "white" {}
        _DistortTex ("Distortion Texture", 2D) = "gray" {}

        _OverlayAlpha ("Overlay Alpha", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,3)) = 1.05

        _ScrollX ("Scroll X", Range(-5,5)) = 0.08
        _ScrollY ("Scroll Y", Range(-5,5)) = 0.02

        _SecondaryScrollX ("Secondary Scroll X", Range(-5,5)) = -0.05
        _SecondaryScrollY ("Secondary Scroll Y", Range(-5,5)) = 0.04

        _DistortStrength ("Distort Strength", Range(0,0.2)) = 0.015
        _DistortTiling ("Distort Tiling", Float) = 2.0

        _BlendAmount ("Blend Amount", Range(0,1)) = 0.65

        _TintA ("Tint A", Color) = (1,0,0,1)
        _TintB ("Tint B", Color) = (0,1,0,1)
        _TintC ("Tint C", Color) = (0,0,1,1)

        _TintStrength ("Tint Strength", Range(0,1)) = 0.35
        _TintCycleSpeed ("Tint Cycle Speed", Range(0,5)) = 0.2

        _WhiteThreshold ("White Threshold", Range(0,1)) = 0.72
        _WhiteSuppression ("White Suppression", Range(0,1)) = 0.65
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_DistortTex);
            SAMPLER(sampler_DistortTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DistortTex_ST;

                float _OverlayAlpha;
                float _Brightness;

                float _ScrollX;
                float _ScrollY;
                float _SecondaryScrollX;
                float _SecondaryScrollY;

                float _DistortStrength;
                float _DistortTiling;
                float _BlendAmount;

                float4 _TintA;
                float4 _TintB;
                float4 _TintC;
                float _TintStrength;
                float _TintCycleSpeed;

                float _WhiteThreshold;
                float _WhiteSuppression;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half3 GetThreeColorTint(float phase, half3 colorA, half3 colorB, half3 colorC)
            {
                phase = frac(phase);

                if (phase < 0.3333333h)
                {
                    half t = phase / 0.3333333h;
                    return lerp(colorA, colorB, t);
                }
                else if (phase < 0.6666667h)
                {
                    half t = (phase - 0.3333333h) / 0.3333334h;
                    return lerp(colorB, colorC, t);
                }
                else
                {
                    half t = (phase - 0.6666667h) / 0.3333333h;
                    return lerp(colorC, colorA, t);
                }
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                half maskAlpha = baseSample.a * IN.color.a;
                if (maskAlpha <= 0.001h)
                    discard;

                float2 distortUV = frac(IN.uv * _DistortTiling + float2(0.03, -0.02) * _Time.y);
                half2 distortSample = SAMPLE_TEXTURE2D(_DistortTex, sampler_DistortTex, distortUV).rg;
                float2 distort = (distortSample - 0.5h) * 2.0h * _DistortStrength;

                float2 scrollA = frac(IN.uv + float2(_ScrollX, _ScrollY) * _Time.y + distort);
                float2 scrollB = frac(IN.uv + float2(_SecondaryScrollX, _SecondaryScrollY) * _Time.y - distort);

                half4 scrolledA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrollA);
                half4 scrolledB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, scrollB);

                half3 movingColor = lerp(scrolledA.rgb, scrolledB.rgb, _BlendAmount);

                half flow = frac(
                    _Time.y * _TintCycleSpeed +
                    IN.uv.x * 0.7h +
                    IN.uv.y * 0.5h
                );

                half3 tintColor = GetThreeColorTint(flow, _TintA.rgb, _TintB.rgb, _TintC.rgb);

                movingColor = lerp(movingColor, movingColor * tintColor * 1.35h, _TintStrength);

                half luminance = dot(movingColor, half3(0.299h, 0.587h, 0.114h));
                half whiteMask = smoothstep(_WhiteThreshold, 1.0h, luminance);

                half3 coloredVersion = movingColor * tintColor * 1.4h;
                movingColor = lerp(movingColor, coloredVersion, whiteMask * _WhiteSuppression);

                half3 finalColor = lerp(baseSample.rgb, movingColor, 0.75h);
                finalColor *= _Brightness;
                finalColor *= IN.color.rgb;

                half finalAlpha = maskAlpha * _OverlayAlpha;
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}