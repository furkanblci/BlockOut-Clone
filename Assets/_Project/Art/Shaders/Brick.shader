// Tuğla shader'ı: taban rengi × vertex AO × sabit yönlü yumuşak ışık.
//
// DERS (neden özel shader?): URP Lit, vertex renklerini kullanmaz ve mobilde
// gereğinden pahalıdır. Bizim stilimizde gölge yok, ışık tek yönlü ve sabit —
// o zaman aydınlatmayı tek satır nokta çarpımıyla hesaplamak hem daha ucuz
// hem sanat yönü açısından daha kontrollü. Tuğlanın köşe renklerine gömülü
// sahte AO (BrickMeshBuilder'da üretilir) burada çarpan olarak devreye girer.
//
// DERS (SRP Batcher): Materyal değişkenleri CBUFFER_START(UnityPerMaterial)
// içinde tanımlanmalıdır. Aksi halde Unity bu shader'ı batch'leyemez ve
// 8 paylaşımlı materyal avantajımız boşa gider.
Shader "BlockOut/Brick"
{
    Properties
    {
        _BaseColor("Taban Rengi", Color) = (1, 1, 1, 1)
        _LightDir("Isik Yonu", Vector) = (0.35, 1, -0.3, 0)
        _Ambient("Ortam Isigi", Range(0, 1)) = 0.55
        _RimStrength("Kenar Parlakligi", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half3  normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
                half4  color       : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _LightDir;
                half _Ambient;
                half _RimStrength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(positionWS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 normal = normalize(IN.normalWS);
                half3 light = normalize(_LightDir.xyz);

                // Yarım-lambert: gölgede kalan yüzler tamamen kararmaz, oyuncak hissi verir.
                half ndl = saturate(dot(normal, light));
                half shade = lerp(_Ambient, 1.0h, ndl);

                // Hafif kenar parlaklığı: tuğlanın silueti zeminden ayrılsın.
                half3 view = normalize(IN.viewDirWS);
                half rim = pow(1.0h - saturate(dot(normal, view)), 3.0h) * _RimStrength;

                half3 albedo = _BaseColor.rgb * IN.color.rgb;
                return half4(albedo * shade + rim, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
