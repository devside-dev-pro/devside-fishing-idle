// Eau stylisée URP : dégradé de profondeur, vaguelettes procédurales qui dérivent,
// crêtes claires, et anneau d'écume elliptique autour du bateau (qui vit à l'origine).
// Placé dans Resources/ pour être embarqué dans les builds (chargé via Shader.Find).
Shader "Devside/StylizedWater"
{
    Properties
    {
        _ColorShallow("Couleur surface", Color) = (0.16, 0.55, 0.62, 1)
        _ColorDeep("Couleur profonde", Color) = (0.05, 0.15, 0.35, 1)
        _DepthBlend("Profondeur (0-1)", Range(0, 1)) = 0
        _FoamColor("Couleur ecume", Color) = (0.92, 0.98, 1, 1)
        _FoamRadius("Rayon ecume", Float) = 3.4
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorShallow;
                half4 _ColorDeep;
                half4 _FoamColor;
                float _DepthBlend;
                float _FoamRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float2 p = input.positionWS.xz;

                half3 base = lerp(_ColorShallow.rgb, _ColorDeep.rgb, saturate(_DepthBlend));

                // Deux couches de vaguelettes qui dérivent dans des directions opposées.
                float n = vnoise(p * 0.9 + float2(t * 0.18, t * 0.12)) * 0.6
                        + vnoise(p * 2.3 - float2(t * 0.26, t * 0.2)) * 0.4;
                half3 color = base * (0.9 + 0.2 * n);

                // Crêtes claires sur les sommets du bruit.
                float crest = smoothstep(0.66, 0.78, n);
                color = lerp(color, base * 0.35 + half3(0.6, 0.65, 0.68), crest * 0.4);

                // Écume elliptique discrète au ras de la coque (bateau à l'origine, allongé en x).
                float d = length(float2(p.x * 0.55, p.y));
                float ring = smoothstep(_FoamRadius, _FoamRadius - 0.2, d)
                           * smoothstep(_FoamRadius - 0.7, _FoamRadius - 0.4, d);
                float foamNoise = vnoise(p * 4.5 + float2(t * 0.35, -t * 0.3));
                color = lerp(color, _FoamColor.rgb, ring * (0.12 + 0.3 * foamNoise));

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
