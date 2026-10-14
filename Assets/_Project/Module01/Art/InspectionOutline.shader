// Contorno de la pieza del vehiculo que toca revisar, por casco invertido: se dibujan las
// caras traseras desplazadas hacia fuera por la normal, y lo que asoma alrededor de la silueta
// es la linea.
//
// Elegido asi, y no con un Renderer Feature de pantalla completa, por dos razones. En VR una
// pasada de pantalla completa con multiview es cara y da problemas; y un Renderer Feature
// obliga a tocar PC_Renderer y Mobile_Renderer, que es configuracion de todo el proyecto, para
// un efecto que afecta a una pieza cada vez.
//
// OJO con el SRP Batcher: ignora los MaterialPropertyBlock. Las propiedades viven FUERA del
// CBUFFER UnityPerMaterial A PROPOSITO, para que este shader NO sea compatible con el batcher
// y el color por renderer llegue. Si alguien las mete dentro, los cinco puntos se delinean
// todos del mismo color y el verde de "resuelto" deja de verse.
Shader "RideSafe/Inspection Outline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1, 0.42, 0.05, 1)
        _OutlineWidth ("Grosor en metros", Range(0.001, 0.08)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "InspectionOutline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // El grosor se aplica en espacio de mundo y no en el del objeto: las piezas del
                // FBX no vienen a la misma escala, y en espacio de objeto la rueda saldria con
                // un contorno de distinto grosor que el manubrio.
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionCS = TransformWorldToHClip(positionWS + normalWS * _OutlineWidth);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_OutlineColor.rgb, _OutlineColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
