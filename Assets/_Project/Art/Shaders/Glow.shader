// Katkılı ışık shader'ı: hale, ışık hüzmesi, parıltı.
//
// DERS (neden ÖZEL shader?): Bu efektin materyali önce
// `URP/Unlit`, sonra `URP/Particles/Unlit`, sonra `Sprites/Default` ile
// kurulmaya çalışıldı. İlk ikisi ayarlar doğru yazılmasına rağmen yüzeyi
// OPAK bastı (ekranda kapının yanında siyah bir kare ve ortasında parlak bir
// top); üçüncüsü doğru harmanladı ama derinlik testi yüzünden ışınlar bloğun
// İÇİNDE kaldıkları için hiç görünmediler — oysa referansta ışınlar bloğun
// ÖNÜNDE duruyor.
//
// Yani ihtiyacımız olan üç şeyin (katkılı harmanlama, köşe rengiyle
// renklenme, derinlik testini atlayabilme) üçünü birden veren hazır bir
// shader yoktu. Otuz satırlık bir shader, üç hazır shader'ın davranışını
// tahmin etmeye çalışmaktan hem kısa hem kesin.
//
// DERS (bir davranışı ayarla değil, KODLA garanti et): Hazır shader'ların
// harmanlama durumu sürüme, anahtar kelimelere ve materyal doğrulayıcısına
// bağlı; bizim yazdığımız `Blend` satırı hiçbirine bağlı değil.
Shader "BlockOut/Glow"
{
    Properties
    {
        _MainTex("Doku", 2D) = "white" {}
        _Color("Renk", Color) = (1, 1, 1, 1)
        // 4 = LEqual (normal), 8 = Always (her şeyin önünde).
        _ZTest("Derinlik Testi", Float) = 8
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "RenderPipeline" = "UniversalPipeline"
        }

        // KATKILI: ışık arkadaki renge EKLER. Sönerken siyah eklemek hiçbir
        // şey yapmaz, yani hüzme iz bırakmadan kaybolur.
        Blend SrcAlpha One
        ZWrite Off
        ZTest [_ZTest]
        Cull Off
        Lighting Off

        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                half4  color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _ZTest;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                // Parçacık sisteminin `startColor`'ı köşe rengiyle gelir.
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 c = tex * _Color * IN.color;
                // Alfa harmanlama çarpanı olarak kullanılıyor (Blend SrcAlpha
                // One); rengi ayrıca alfayla çarpmıyoruz, yoksa sönme iki kez
                // uygulanır ve ışık olması gerekenden hızlı kaybolur.
                return c;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
