// Holograma verde del EPP ya seleccionado. Lo pidio el cliente el 2026-10-16: el elemento
// elegido se queda en la mesa marcado en vez de desaparecer, para poder volver a pulsarlo y
// deshacer la eleccion.
//
// Es un RELLENO, no un contorno. RideSafe/Inspection Outline y RideSafe/Tutorial Halo son los
// dos siluetas por casco invertido y sirven para decir "mira AQUI"; esto tiene que decir "este
// ya lo llevas", que es un estado del objeto entero. Se dibuja sobre una copia de la malla con
// el renderer original apagado (ver SelectedHolo.cs), asi que lo que se ve es solo esto.
//
// Sin pasada de pantalla completa y sin Renderer Feature, por lo mismo que el contorno: en VR
// una pasada con multiview es cara, y un Renderer Feature obliga a tocar PC_Renderer y
// Mobile_Renderer, que es configuracion de todo el proyecto, para un efecto de un objeto.
//
// OJO con el SRP Batcher: ignora los MaterialPropertyBlock. Las propiedades viven FUERA del
// CBUFFER UnityPerMaterial A PROPOSITO, para que este shader NO sea compatible con el batcher y
// el color por renderer llegue. Si alguien las mete dentro, los trece EPP se pintan todos del
// mismo color y deja de poder cambiarse por elemento.
Shader "RideSafe/Selected Holo"
{
    Properties
    {
        _HoloColor ("Color", Color) = (0.46, 0.83, 0.54, 0.9)
        _RimPower ("Fuerza del borde", Range(0.5, 8)) = 2.5
        _Fill ("Relleno de la cara", Range(0, 1)) = 0.28
        _ScanScale ("Lineas por metro", Range(0, 300)) = 70
        _ScanStrength ("Fuerza de las lineas", Range(0, 1)) = 0.22
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
            Name "SelectedHolo"
            Tags { "LightMode" = "UniversalForward" }

            // Las dos caras: un holograma se ve por dentro, y sin esto un objeto abierto —unas
            // gafas, un casco— se ve hueco por el lado que mira al aprendiz.
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _HoloColor;
            float _RimPower;
            float _Fill;
            float _ScanScale;
            float _ScanStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                // Con Cull Off la normal de las caras traseras apunta al otro lado: en valor
                // absoluto el borde se calcula igual por las dos caras.
                float facing = abs(dot(normalize(IN.normalWS), viewDir));

                // Borde de fresnel: lo que da la silueta brillante del holograma.
                float rim = pow(saturate(1.0 - facing), _RimPower);

                // Lineas horizontales en espacio de MUNDO y no de pantalla: asi se quedan
                // pegadas al objeto y no reptan cuando el aprendiz mueve la cabeza, que en VR
                // se nota y marea.
                float scan = _ScanScale > 0.0
                    ? (0.5 + 0.5 * sin(IN.positionWS.y * _ScanScale))
                    : 1.0;

                float alpha = saturate(_Fill + rim) * _HoloColor.a;
                alpha *= lerp(1.0, scan, _ScanStrength);

                // El borde tambien aclara el color, para que no sea un tinte plano.
                half3 rgb = _HoloColor.rgb * (1.0 + rim);

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
