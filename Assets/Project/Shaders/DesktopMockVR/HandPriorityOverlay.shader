Shader "FirstHand/DesktopMockVR/HandPriorityOverlay"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.62, 0.78, 1.0, 1)
        _RimColor("Rim Color", Color) = (0.85, 0.98, 1.0, 1)
        _Alpha("Alpha", Range(0, 1)) = 0.35
        _RimPower("Rim Power", Range(0.5, 8)) = 2.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Overlay+20"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HandPriorityOverlay"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _Alpha;
                float _RimPower;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(saturate(1.0 - dot(normalize(input.normalWS), viewDir)), _RimPower);
                float3 color = lerp(_BaseColor.rgb, _RimColor.rgb, rim);
                float alpha = saturate(_Alpha * (0.55 + rim * 0.65));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
