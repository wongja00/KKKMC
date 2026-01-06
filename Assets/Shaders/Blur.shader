Shader "Custom/Blur"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 1.0
        _BlurStrength ("Blur Strength", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _BlurSize;
                float _BlurStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 texelSize = _MainTex_TexelSize.xy * _BlurSize;
                
                // 가우시안 블러 커널 (9-tap)
                half4 color = half4(0, 0, 0, 0);
                float totalWeight = 0.0;
                
                // 가우시안 가중치
                float weights[9] = {
                    0.0625, 0.125, 0.0625,
                    0.125,  0.25,  0.125,
                    0.0625, 0.125, 0.0625
                };
                
                int index = 0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 offset = float2(x, y) * texelSize * _BlurStrength;
                        half4 sample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + offset);
                        color += sample * weights[index];
                        totalWeight += weights[index];
                        index++;
                    }
                }
                
                color /= totalWeight;
                return color;
            }
            ENDHLSL
        }
    }
}
