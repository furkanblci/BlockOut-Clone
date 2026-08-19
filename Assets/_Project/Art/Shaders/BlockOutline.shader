// Seçili bloğun beyaz konturu.
//
// NEDEN KENDİ SHADER'IMIZ VAR (yalnız TEK bir satır için):
// Kontur, bloğun etrafını saran düz bir halka. Halka bloğun EN GENİŞ olduğu
// yükseklikte duruyor, komşu blokların üst yüzü ise ondan daha yukarıda.
// Normal derinlik testiyle komşuya değen kenarlarda kontur komşunun ALTINDA
// kalıyor: ölçtüm, 2x2 bir blokta yalnız sağ ve üst kenar görünüyordu, sol ve
// alt kenar sıfır piksel. Referansta kontur DÖRT kenarı da sarıyor.
//
// Çözüm `ZTest Always`. URP'nin hazır Unlit shader'ında `_ZTest` diye bir
// malzeme özelliği YOK (kontrol edildi: yalnız _ZWrite, _Cull, _Blend... var),
// yani derinlik testi malzemeden kapatılamıyor. Bu yüzden dört satırlık kendi
// geçişimiz.
//
// DERS (build'de kaybolan shader): `Shader.Find` editörde HER ZAMAN çalışır,
// çünkü editörde bütün shader'lar yüklüdür. Build'de ise hiçbir malzemenin
// kullanmadığı shader ELENİR ve nesne pembe çıkar. Bu yüzden bu shader'a bir
// malzeme asset'i (Resources/BlockOutline.mat) bağlı; malzeme Resources'ta
// olduğu için shader de onun bağımlılığı olarak build'e giriyor.
Shader "BlockOut/Outline"
{
    Properties
    {
        [MainColor] _BaseColor ("Renk", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Outline"

            ZTest Always
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
