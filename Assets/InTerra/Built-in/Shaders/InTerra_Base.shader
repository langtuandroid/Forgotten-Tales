Shader "Hidden/InTerra/InTerra-Base" {
    Properties{
        _MainTex("Base (RGB) Smoothness (A)", 2D) = "white" {}
        _TriplanarTex("splat0", 2D) = "white" {}
        _MetallicTex("Metallic (R), A.Occlussion (B)", 2D) = "white" {}
        _Triplanar_MetallicAO("Metallic (R)", 2D) = "white" {}
        
        // used in fallback on old cards
        _Color("Main Color", Color) = (1,1,1,1)

        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
        [HideInInspector] _TerrainSizeXZPosY("",  Vector) = (0,0,0)
        [HideInInspector] _TriplanarOneToAllSteep("", Float) = 0
        [HideInInspector] _TriplanarSharpness("Triplanar Sharpness",   Range(3,10)) = 8
        _HT_distance_scale("Scale",   Range(0,0.5)) = 0.25
    }

    SubShader{
        Tags {
            "RenderType" = "Opaque"
            "Queue" = "Geometry-100"
        }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma target 3.0

        #pragma multi_compile_local __ _ALPHATEST_ON

        #pragma shader_feature_local _TERRAIN_TRIPLANAR_ONE
        #pragma shader_feature_local _TERRAIN_DISTANCEBLEND

        #define TERRAIN_BASE_PASS
        #define TERRAIN_INSTANCED_PERPIXEL_NORMAL

        #include "InTerra_InputsAndFunctions.cginc"
        #include "InTerra_Mixing.cginc"

        #include "UnityPBSLighting.cginc"

        sampler2D _MainTex;
        sampler2D _MetallicTex;
        sampler2D _TriplanarTex;
        sampler2D _Triplanar_MetallicAO;

        void surf(Input IN, inout SurfaceOutputStandard o) {
            #ifdef _ALPHATEST_ON
                ClipHoles(IN.tc.xy);
            #endif
            half4 albedo = tex2D(_MainTex, IN.tc.xy);
            half4 metallicAO = tex2D(_MetallicTex, IN.tc.xy);

            #ifdef _TERRAIN_TRIPLANAR_ONE
                if (_NumLayersCount <= 4)
                {
                    float3  weights;
                    #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) 
                        weights = abs(IN.terrainNormals);
                    #else
                        weights = abs(IN.worldNormal);
                    #endif
                    weights = pow(weights, _TriplanarSharpness);
                    weights = weights / (weights.x + weights.y + weights.z);

                    float2 frontUV = TerrainFrontUV(IN.worldPos, _MainTex_ST, IN.tc.xy);
                    float2 sideUV = TerrainSideUV(IN.worldPos, _MainTex_ST, IN.tc.xy);

                    half4 cFront = tex2D(_TriplanarTex, frontUV);
                    half4 cSide = tex2D(_TriplanarTex, sideUV);

                    TriplanarBase(albedo, cFront, cSide, weights, metallicAO.b, _TriplanarOneToAllSteep);
                    half4 tint = tex2D(_TerrainColorTintTexture, IN.tc.xy * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw);
                    albedo.rgb = lerp(albedo.rgb, ((albedo.rgb) * (tint)), _TerrainColorTintStrenght).rgb;

                    half4 mAoFront = tex2D(_Triplanar_MetallicAO, frontUV);
                    half4 mAoSide = tex2D(_Triplanar_MetallicAO, sideUV);                    
                    TriplanarBase(metallicAO, mAoFront, mAoSide, weights, metallicAO.b, _TriplanarOneToAllSteep);
                }
            #endif                                     

            o.Albedo = albedo.rgb;
            o.Alpha = 1;
            o.Smoothness = lerp(albedo.a, 1.0f, _InTerra_GlobalSmoothness);
            o.Metallic = metallicAO.r;
            o.Occlusion = metallicAO.g;

            #if defined(INSTANCING_ON) && defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
                o.Normal = float3(0, 0, 1); // make sure that surface shader compiler realizes we write to normal, as UNITY_INSTANCING_ENABLED is not defined for SHADER_TARGET_SURFACE_ANALYSIS.
            #endif

            #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
                o.Normal = normalize(tex2D(_TerrainNormalmapTexture, IN.tc.zw).xyz * 2 - 1).xzy;
            #endif
        }
        ENDCG

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }
    FallBack "Diffuse"
}