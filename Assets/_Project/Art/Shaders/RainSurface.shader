Shader "FeedTheNight/RainSurface"
{
    Properties
    {
        // ── Base Surface ─────────────────────────────────────────────────────
        [MainTexture] _BaseMap       ("Albedo (RGB)", 2D)             = "white" {}
        [MainColor]   _BaseColor     ("Base Color", Color)            = (1,1,1,1)
        _BumpMap                     ("Normal Map (Base)", 2D)        = "bump"  {}
        _BumpScale                   ("Normal Strength", Range(0,2))  = 1.0

        _MetallicGlossMap            ("Metallic (R) Smoothness (A)", 2D) = "white" {}
        _Metallic                    ("Metallic", Range(0,1))         = 0.0
        _Glossiness                  ("Smoothness (Dry)", Range(0,1)) = 0.3

        // ── Rain / Wetness Controls ──────────────────────────────────────────
        [Header(Wetness)]
        _Wetness                     ("Wetness", Range(0,1))          = 0.0
        _WetSmoothness               ("Smoothness (Wet)", Range(0,1)) = 0.92
        _WetDarken                   ("Albedo Darkening (Wet)", Range(0,1)) = 0.35

        [Header(Ripples Layer 1)]
        _RippleMap                   ("Ripple Normal Map", 2D)        = "bump"  {}
        _RippleScale                 ("Ripple Tiling", Float)         = 4.0
        _RippleSpeed                 ("Ripple Speed", Float)          = 0.08
        _RippleStrength              ("Ripple Strength", Range(0,2))  = 0.6

        [Header(Ripples Layer 2)]
        _RippleScale2                ("Ripple 2 Tiling", Float)       = 2.5
        _RippleSpeed2                ("Ripple 2 Speed", Float)        = 0.05
        _RippleStrength2             ("Ripple 2 Strength", Range(0,2)) = 0.4

        [Header(Rain Streaks)]
        _StreakMap                   ("Streak Normal Map", 2D)        = "bump"  {}
        _StreakScale                 ("Streak Tiling", Float)         = 3.0
        _StreakSpeed                 ("Streak Speed (Y scroll)", Float) = 0.18
        _StreakStrength              ("Streak Strength", Range(0,2))  = 0.5

        // ── URP Internal ────────────────────────────────────────────────────
        [HideInInspector] _Surface  ("__surface", Float) = 0.0
        [HideInInspector] _Blend    ("__blend",   Float) = 0.0
        [HideInInspector] _AlphaClip("__clip",    Float) = 0.0
        [HideInInspector] _SrcBlend ("__src",     Float) = 1.0
        [HideInInspector] _DstBlend ("__dst",     Float) = 0.0
        [HideInInspector] _ZWrite   ("__zw",      Float) = 1.0
        [HideInInspector] _Cull     ("__cull",    Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"       = "Geometry"
        }
        LOD 300

        // ── Forward Lit Pass ─────────────────────────────────────────────────
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull   [_Cull]
            ZWrite [_ZWrite]
            Blend  [_SrcBlend] [_DstBlend]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex   RainVert
            #pragma fragment RainFrag

            // URP keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ── Samplers ──────────────────────────────────────────────────────
            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_MetallicGlossMap); SAMPLER(sampler_MetallicGlossMap);
            TEXTURE2D(_RippleMap);      SAMPLER(sampler_RippleMap);
            TEXTURE2D(_StreakMap);      SAMPLER(sampler_StreakMap);

            // ── CBuffer ───────────────────────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _RippleMap_ST;
                float4 _StreakMap_ST;

                float  _BumpScale;
                float  _Metallic;
                float  _Glossiness;

                float  _Wetness;
                float  _WetSmoothness;
                float  _WetDarken;

                float  _RippleScale;
                float  _RippleScale2;
                float  _RippleSpeed;
                float  _RippleSpeed2;
                float  _RippleStrength;
                float  _RippleStrength2;

                float  _StreakScale;
                float  _StreakSpeed;
                float  _StreakStrength;
            CBUFFER_END

            // ── Structs ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float4 tangentWS   : TEXCOORD3;  // w = tangent sign
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Helpers ───────────────────────────────────────────────────────

            // Blend two tangent-space normals (UDN blend – cheap and artifact-free)
            float3 BlendNormalsUDN(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z));
            }

            // ── Vertex ────────────────────────────────────────────────────────
            Varyings RainVert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.normalWS   = vni.normalWS;
                OUT.tangentWS  = float4(vni.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.fogFactor  = ComputeFogFactor(vpi.positionCS.z);
                return OUT;
            }

            // ── Fragment ──────────────────────────────────────────────────────
            half4 RainFrag(Varyings IN) : SV_Target
            {
                // ── Base Albedo ───────────────────────────────────────────────
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // ── Metallic & Smoothness ─────────────────────────────────────
                half4 mrSample   = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, IN.uv);
                half  metallic   = mrSample.r * _Metallic;
                half  smoothness = lerp(_Glossiness, _WetSmoothness, _Wetness);

                // ── Base Normal ───────────────────────────────────────────────
                float3 baseNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);

                // ── Ripple Layer 1 ────────────────────────────────────────────
                float2 uvRipple1 = IN.uv * _RippleScale + float2(_Time.y * _RippleSpeed,
                                                                   _Time.y * _RippleSpeed * 0.7);
                float3 ripple1 = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, uvRipple1),
                    _RippleStrength * _Wetness);

                // ── Ripple Layer 2 (different angle for interference pattern) ─
                float2 uvRipple2 = IN.uv * _RippleScale2 + float2(-_Time.y * _RippleSpeed2,
                                                                    _Time.y * _RippleSpeed2 * 1.3);
                float3 ripple2 = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_RippleMap, sampler_RippleMap, uvRipple2),
                    _RippleStrength2 * _Wetness);

                // ── Rain Streaks (vertical scroll) ────────────────────────────
                float2 uvStreak = IN.uv * _StreakScale + float2(0.0, -_Time.y * _StreakSpeed);
                float3 streaks = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_StreakMap, sampler_StreakMap, uvStreak),
                    _StreakStrength * _Wetness);

                // ── Combine Normals (UDN blend) ───────────────────────────────
                float3 rippleCombined = BlendNormalsUDN(ripple1, ripple2);
                float3 finalNormalTS  = BlendNormalsUDN(baseNormal, rippleCombined);
                finalNormalTS = BlendNormalsUDN(finalNormalTS, streaks);

                // ── Tangent → World Space ─────────────────────────────────────
                float3 bitangentWS = cross(IN.normalWS, IN.tangentWS.xyz) * IN.tangentWS.w;
                float3x3 TBN = float3x3(IN.tangentWS.xyz, bitangentWS, IN.normalWS);
                float3 normalWS = normalize(mul(finalNormalTS, TBN));

                // ── Wet Darkening ─────────────────────────────────────────────
                half3 albedo = baseColor.rgb * (1.0 - _WetDarken * _Wetness);

                // ── Lighting (URP PBR) ────────────────────────────────────────
                InputData inputData = (InputData)0;
                inputData.positionWS    = IN.positionWS;
                inputData.normalWS      = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord   = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord      = IN.fogFactor;
                inputData.bakedGI       = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo      = albedo;
                surfaceData.metallic    = metallic;
                surfaceData.smoothness  = smoothness;
                surfaceData.normalTS    = finalNormalTS;
                surfaceData.occlusion   = 1.0;
                surfaceData.alpha       = baseColor.a;

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        // ── Shadow Caster Pass ───────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        // ── Depth Only Pass ──────────────────────────────────────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
