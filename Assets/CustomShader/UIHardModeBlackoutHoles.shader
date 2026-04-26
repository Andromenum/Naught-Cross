Shader "UI/Hard Mode Blackout Holes"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 1)

        _Aspect ("Aspect", Float) = 1
        _HoleCount ("Hole Count", Float) = 0

        _EdgeWobbleStrength ("Edge Wobble Strength", Float) = 0.06
        _EdgeWobbleFrequency ("Edge Wobble Frequency", Float) = 16
        _EdgeWobbleSpeed ("Edge Wobble Speed", Float) = 6
        _EdgeRandomness ("Edge Randomness", Range(0, 1)) = 0.85
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 4.5

        _Hole0 ("Hole 0", Vector) = (0, 0, 0, 0)
        _Hole1 ("Hole 1", Vector) = (0, 0, 0, 0)
        _Hole2 ("Hole 2", Vector) = (0, 0, 0, 0)
        _Hole3 ("Hole 3", Vector) = (0, 0, 0, 0)
        _Hole4 ("Hole 4", Vector) = (0, 0, 0, 0)
        _Hole5 ("Hole 5", Vector) = (0, 0, 0, 0)
        _Hole6 ("Hole 6", Vector) = (0, 0, 0, 0)
        _Hole7 ("Hole 7", Vector) = (0, 0, 0, 0)
        _Hole8 ("Hole 8", Vector) = (0, 0, 0, 0)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            float _Aspect;
            float _HoleCount;

            float _EdgeWobbleStrength;
            float _EdgeWobbleFrequency;
            float _EdgeWobbleSpeed;
            float _EdgeRandomness;
            float _EdgeNoiseScale;

            float4 _Hole0;
            float4 _Hole1;
            float4 _Hole2;
            float4 _Hole3;
            float4 _Hole4;
            float4 _Hole5;
            float4 _Hole6;
            float4 _Hole7;
            float4 _Hole8;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash(i);
                float b = Hash(i + float2(1.0, 0.0));
                float c = Hash(i + float2(0.0, 1.0));
                float d = Hash(i + float2(1.0, 1.0));

                float x1 = lerp(a, b, f.x);
                float x2 = lerp(c, d, f.x);

                return lerp(x1, x2, f.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;

                value += ValueNoise(p) * amplitude;
                p *= 2.03;
                amplitude *= 0.5;

                value += ValueNoise(p) * amplitude;
                p *= 2.01;
                amplitude *= 0.5;

                value += ValueNoise(p) * amplitude;

                return value;
            }

            float OrganicEdgeWobble(float2 delta, float seed)
            {
                float angle = atan2(delta.y, delta.x);
                float timeValue = _Time.y * _EdgeWobbleSpeed;

                float waveA = sin(angle * _EdgeWobbleFrequency + timeValue + seed);
                float waveB = sin(angle * (_EdgeWobbleFrequency * 0.43) - timeValue * 1.29 + seed * 2.41);
                float waveC = sin(angle * (_EdgeWobbleFrequency * 1.77) + timeValue * 0.61 + seed * 4.73);

                float wavePattern = waveA * 0.45 + waveB * 0.35 + waveC * 0.20;

                float dist = max(length(delta), 0.0001);
                float2 dir = delta / dist;

                float2 movingNoiseA = dir * _EdgeNoiseScale;
                movingNoiseA += float2(seed * 6.13, seed * 2.91);
                movingNoiseA += float2(timeValue * 0.12, -timeValue * 0.17);

                float2 movingNoiseB = dir * (_EdgeNoiseScale * 1.73);
                movingNoiseB += float2(seed * 1.91, seed * 8.37);
                movingNoiseB += float2(-timeValue * 0.09, timeValue * 0.14);

                float noiseA = Fbm(movingNoiseA) * 2.0 - 1.0;
                float noiseB = Fbm(movingNoiseB) * 2.0 - 1.0;

                float randomPattern = noiseA * 0.7 + noiseB * 0.3;

                return lerp(wavePattern, randomPattern, _EdgeRandomness);
            }

            float HoleReveal(float2 uv, float4 hole, float seed)
            {
                float2 delta = uv - hole.xy;

                delta.x *= _Aspect;

                float dist = length(delta);
                float radius = hole.z;
                float softness = max(0.0001, hole.w);

                float wobble = OrganicEdgeWobble(delta, seed) * _EdgeWobbleStrength * radius;
                float animatedRadius = radius + wobble;

                return 1.0 - smoothstep(animatedRadius - softness, animatedRadius, dist);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.texcoord) * input.color;

                float reveal = 0.0;

                if (_HoleCount > 0.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole0, 1.0));
                if (_HoleCount > 1.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole1, 2.0));
                if (_HoleCount > 2.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole2, 3.0));
                if (_HoleCount > 3.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole3, 4.0));
                if (_HoleCount > 4.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole4, 5.0));
                if (_HoleCount > 5.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole5, 6.0));
                if (_HoleCount > 6.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole6, 7.0));
                if (_HoleCount > 7.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole7, 8.0));
                if (_HoleCount > 8.5) reveal = max(reveal, HoleReveal(input.texcoord, _Hole8, 9.0));

                color.a *= 1.0 - saturate(reveal);

                return color;
            }

            ENDCG
        }
    }
}