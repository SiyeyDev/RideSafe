// Halo of the tutorial's 3D target, by inverted hull: back faces pushed out along the normal,
// so what shows around the silhouette is the line. Own copy of the module 1 technique: the
// tutorial must not depend on a module's art.
//
// SRP Batcher: properties stay OUTSIDE UnityPerMaterial on purpose so per-renderer
// MaterialPropertyBlock colours (the pulse) reach the shader.
Shader "RideSafe/Tutorial Halo"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (0.663, 0.886, 0.831, 0.9)
        _OutlineWidth ("Width in metres", Range(0.001, 0.08)) = 0.01
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
            Name "TutorialHalo"
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
