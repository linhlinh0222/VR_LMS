Shader "FirstHand/DesktopMockVR/MarineWater"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.23, 0.72, 0.82, 1)
        _DeepColor("Deep Color", Color) = (0.015, 0.14, 0.22, 1)
        _FoamColor("Foam Color", Color) = (0.86, 0.96, 1, 1)
        _Alpha("Alpha", Range(0, 1)) = 0.72
        _TimeMultiplier("Time Multiplier", Float) = 1
        _AmplitudeMultiplier("Amplitude Multiplier", Float) = 1
        _Chaos("Chaos", Range(0, 1)) = 0.25
        _WaveScale("Wave Scale", Float) = 0.17
        _WaveHeight("Wave Height", Float) = 0.22
        _WaveSpeed("Wave Speed", Float) = 0.52
        _RippleStrength("Ripple Strength", Range(0, 1)) = 0.35
        _FoamAmount("Foam Amount", Range(0, 1)) = 0.36
        _FoamCutoff("Foam Cutoff", Range(0, 1)) = 0.68
        _FresnelPower("Fresnel Power", Range(0.5, 8)) = 3.1
        _SpecularPower("Specular Power", Range(8, 256)) = 96
        _SpecularIntensity("Specular Intensity", Range(0, 3)) = 1.05
        _DistantWindVector("Distant Wind Vector", Vector) = (0.94, 0.34, 0, 0)
        _LocalWindVector("Local Wind Vector", Vector) = (-0.4, 0.91, 0, 0)
        _CurrentVector("Current Vector", Vector) = (0, 1, 0.25, 0)
        _SunDirection("Sun Direction", Vector) = (-0.35, 0.72, 0.22, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float wave01 : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _Alpha;
                float _TimeMultiplier;
                float _AmplitudeMultiplier;
                float _Chaos;
                float _WaveScale;
                float _WaveHeight;
                float _WaveSpeed;
                float _RippleStrength;
                float _FoamAmount;
                float _FoamCutoff;
                float _FresnelPower;
                float _SpecularPower;
                float _SpecularIntensity;
                float4 _DistantWindVector;
                float4 _LocalWindVector;
                float4 _CurrentVector;
                float4 _SunDirection;
            CBUFFER_END

            static const float TwoPi = 6.28318530718;

            float2 SafeNormalize(float2 value, float2 fallback)
            {
                float lengthSq = dot(value, value);
                return lengthSq > 0.0001 ? value * rsqrt(lengthSq) : normalize(fallback);
            }

            float AddWave(float2 position, float2 direction, float wavelength, float speed, float amplitude, float time, inout float2 derivative)
            {
                float2 dir = normalize(direction);
                float frequency = TwoPi / max(wavelength, 0.001);
                float phase = dot(position, dir) * frequency + time * speed;
                float s = sin(phase);
                float c = cos(phase);
                derivative += dir * (c * frequency * amplitude);
                return s * amplitude;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float2 currentDirection = SafeNormalize(_CurrentVector.xy, float2(0.0, 1.0));
                float2 distantWind = SafeNormalize(_DistantWindVector.xy, float2(0.94, 0.34));
                float2 localWind = SafeNormalize(_LocalWindVector.xy, float2(-0.4, 0.91));
                float2 crossSwell = SafeNormalize(lerp(float2(-distantWind.y, distantWind.x), -distantWind, _Chaos), float2(-0.34, 0.94));
                float2 chopSwell = SafeNormalize(lerp(float2(localWind.y, -localWind.x), localWind, 1.0 - _Chaos * 0.65), float2(0.91, 0.4));
                float currentOffset = _Time.y * _CurrentVector.z;
                float2 wavePosition = (positionWS.xz + currentDirection * currentOffset) * _WaveScale;
                float time = _Time.y * _WaveSpeed * _TimeMultiplier;
                float amplitude = _WaveHeight * _AmplitudeMultiplier;

                float2 derivative = 0;
                float wave = 0;
                wave += AddWave(wavePosition, distantWind, 5.8, 1.00, 0.52, time, derivative);
                wave += AddWave(wavePosition, crossSwell, 3.2, 1.55, 0.28 * lerp(0.75, 1.4, _Chaos), time, derivative);
                wave += AddWave(wavePosition, localWind, 1.35, 2.25, 0.13 * _RippleStrength, time, derivative);
                wave += AddWave(wavePosition, chopSwell, 9.4, 0.72, 0.20 * lerp(0.55, 1.25, _Chaos), time, derivative);

                positionWS.y += wave * amplitude;
                float2 worldDerivative = derivative * _WaveScale * amplitude;

                output.positionWS = positionWS;
                output.normalWS = normalize(float3(-worldDerivative.x, 1.0, -worldDerivative.y));
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.wave01 = saturate(wave * 0.43 + 0.5);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetCameraPositionWS() - input.positionWS);
                float3 sunDir = normalize(_SunDirection.xyz);
                float horizonMix = saturate(distance(input.positionWS.xz, GetCameraPositionWS().xz) / 75.0);
                float3 waterColor = lerp(_ShallowColor.rgb, _DeepColor.rgb, horizonMix);

                float ndv = saturate(dot(normalWS, viewDir));
                float fresnel = pow(1.0 - ndv, _FresnelPower);
                float diffuse = 0.62 + saturate(dot(normalWS, sunDir)) * 0.28;
                float3 halfDir = normalize(sunDir + viewDir);
                float specular = pow(saturate(dot(normalWS, halfDir)), _SpecularPower) * _SpecularIntensity;

                float2 foamUv = input.positionWS.xz * 0.12;
                float foamLines = sin(foamUv.x * 4.3 + foamUv.y * 1.8 + _Time.y * 0.55)
                    * sin(foamUv.x * -1.6 + foamUv.y * 3.1 - _Time.y * 0.42);
                float foam = smoothstep(_FoamCutoff, 1.0, input.wave01) * smoothstep(0.2, 0.82, foamLines * 0.5 + 0.5);
                foam = saturate(foam * _FoamAmount);

                float3 color = waterColor * diffuse;
                color = lerp(color, _FoamColor.rgb, foam);
                color += _FoamColor.rgb * specular;
                color += lerp(float3(0.03, 0.11, 0.16), _ShallowColor.rgb, 0.65) * fresnel * 0.45;

                float alpha = saturate(_Alpha + fresnel * 0.14 + foam * 0.18);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
