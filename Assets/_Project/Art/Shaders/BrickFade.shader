// Tuğla shader'ının SAYDAMLAŞABİLEN kardeşi: kapı silinirken kullanılıyor.
//
// NEDEN AYRI BİR SHADER (ve neden renk yürütmek yetmedi):
// Kapı sönerken eskiden rengi çerçevenin rengine yürütülüyordu; mantık şuydu
// "kapının arkasında zaten çerçeve var, o renge varmak saydamlaşmakla aynı
// pikseli verir". ÖLÇÜM bunu yalanladı — kapı barı çerçevenin İÇİNE, oyun
// alanının üstüne taşıyor (pah payı + kamera paralaksı ile hücrenin ~%23'ü
// kadar). O bandın arkasında çerçeve değil ZEMİN var. Referansta (turuncu
// kuzey kapısı, sönme bitmiş kare 1700) aynı sütunda her piksel arkasındaki
// şeye dönüyor:
//     y=400 → (66,56,163)  çerçevenin üst yüzü
//     y=420 → (39,31,108)  çerçevenin iç pahı
//     y=430 → (27,21,84)   ZEMİN
// Yani referans kapıyı bir renge boyamıyor, gerçekten saydamlaştırıyor.
// Bizim yöntemimiz zemine taşan bantta çerçeve rengini basıyordu: sönme
// bittiğinde ekranda hâlâ mor-mavi bir kutu duruyor, çizici kapatılınca o
// kutu bir anda yok oluyordu. Kullanıcının gördüğü "sanki resetleniyor ve
// öyle tekrar kayboluyor" tam olarak buydu.
//
// DERS (bir geçişin hedefi sabit bir renk değil, ARKADAKİ PİKSELDİR):
// "Arkasında ne var" sorusunun tek bir cevabı olduğunu varsaymak, cevabın
// değiştiği yerde sessizce yanlış oluyor.
//
// PEKİ NEDEN İKİ GEÇİŞ: Saydamlık daha önce denenmiş ve bırakılmıştı, çünkü
// alfa harmanlaması nesnenin KENDİ arka yüzeylerini de gösteriyor: kapı
// barının koyu kenar plakası ile parlak yüz plakası üst üste binen yerlerde
// iki kez harmanlanıyor ve mesh'in iç kenarları uzun çizgiler olarak
// görünüyordu. Çözüm klasik: önce yalnız DERİNLİĞE yaz (renge dokunma),
// sonra `ZTest LEqual` ile boya. Derinlik zaten yazılı olduğu için her piksel
// bir kez harmanlanıyor; arkada kalan yüzeyler testi geçemiyor.
//
// URP'de iki geçişin sırası `LightMode` etiketinden geliyor: opak nesne
// döngüsü önce `SRPDefaultUnlit`, sonra `UniversalForward` etiketli geçişleri
// çiziyor. Derinlik geçişini birinciye koymak, sırayı garanti ediyor.
Shader "BlockOut/BrickFade"
{
    Properties
    {
        _BaseColor("Taban Rengi", Color) = (1, 1, 1, 1)
        _LightDir("Isik Yonu", Vector) = (0.4, 1, -0.35, 0)
        _Ambient("Ortam Isigi", Range(0, 1)) = 0.62
        _RimStrength("Kenar Parlakligi", Range(0, 1)) = 0.12
        _Specular("Spekuler Guc", Range(0, 2)) = 0.85
        _Gloss("Parlaklik Keskinligi", Range(4, 128)) = 42
        _Saturation("Renk Doygunlugu", Range(1, 2)) = 1.18
        _Fade("Gorunurluk", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        // 1) YALNIZ DERİNLİK. Renge dokunmuyor; amacı her pikselde EN YAKIN
        //    yüzeyin derinliğini yazmak ki ikinci geçişte arkada kalanlar
        //    elensin.
        Pass
        {
            Name "FadeDepth"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ColorMask 0
            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _LightDir;
                half _Ambient;
                half _RimStrength;
                half _Specular;
                half _Gloss;
                half _Saturation;
                half _Fade;
            CBUFFER_END

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 frag() : SV_Target
            {
                // Tamamen sönmüşken derinliğe de yazmasın: arkasındaki şeyin
                // kendi harmanlaması bozulmasın.
                clip(_Fade - 0.002h);
                return 0;
            }
            ENDHLSL
        }

        // 2) RENK. Tuğla shader'ıyla AYNI ışıklandırma — geçişin ilk
        //    karesinde parlaklık sıçraması olmasın diye.
        Pass
        {
            Name "FadeForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

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
                half _Specular;
                half _Gloss;
                half _Saturation;
                half _Fade;
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
                half3 view = normalize(IN.viewDirWS);

                half ndl = saturate(dot(normal, light));
                half shade = lerp(_Ambient, 1.0h, ndl);

                half3 halfDir = normalize(light + view);
                half spec = pow(saturate(dot(normal, halfDir)), _Gloss) * _Specular * ndl;
                half rim = pow(1.0h - saturate(dot(normal, view)), 4.0h) * _RimStrength;

                half3 albedo = _BaseColor.rgb * IN.color.rgb;
                half grey = dot(albedo, half3(0.299h, 0.587h, 0.114h));
                albedo = lerp(half3(grey, grey, grey), albedo, _Saturation);

                return half4(saturate(albedo * shade + spec + rim), _Fade);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
