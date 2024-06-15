Shader "InTerra/Terrain (Standard With Features)" 
{
    Properties {
        [HideInInspector] _MainTex ("BaseMap (RGB)", 2D) = "white" {}
        [HideInInspector] _Color ("Main Color", Color) = (1,1,1,1)
        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
		_HT_distance("Distance",  vector) = (3,10,0,25)
        _HT_distance_scale("Scale",   Range(0,0.5)) = 0.25
        _HT_cover("Cover strenght",   Range(0,1)) = 0.6 
        _HeightTransition("Height blending Sharpness",   Range(0,60)) = 50
        _Distance_HeightTransition("Distance Height blending Sharpness ", Range(0,60)) = 10        
        _TriplanarSharpness("Triplanar Sharpness",   Range(4,10)) = 9
        _ParallaxAffineStepsTerrain("", Float) = 3
        _MipMapFade("Parallax MipMap fade",  vector) = (3,15,0,35)
        _MipMapLevel("Parallax MipMap level", Float) = 0
        _TerrainColorTintTexture("Color Tint Texture", 2D) = "white" {}
        _TerrainColorTintStrenght("Color Tint Strenght", Range(1, 0)) = 0
        _TerrainNormalTintTexture("Additional Normal Texture", 2D) = "bump" {}
        _TerrainNormalTintStrenght("Additional Normal Strenght", Range(0, 1)) = 0.0
        _TerrainNormalTintDistance("Additional Normal Distance",  vector) = (3,10,0,25)
        [HideInInspector] _TerrainSizeXZPosY("",  Vector) = (0,0,0)
        [HideInInspector] _NumLayersCount("", Float) = 0
        [HideInInspector] _TriplanarOneToAllSteep("", Float) = 0
        _HeightmapBlending("", Float) = 1
        _GlobalSmoothness("", Range(0,1)) = 0

        _TrackAO("", Range(0,1)) = 0.8
        _TrackEdgeNormals("Track Edge Normals", Float) = 2
        _TrackDetailTexture("Track Color Detail Texture", 2D) = "white" {}
        [Normal] _TrackDetailNormalTexture("Track Normal Detail Texture", 2D) = "bump" {}
        _TrackDetailNormalStrenght("Track Detail Normal Strenght", Float) = 1
        _TrackNormalStrenght("Track Normal Strenght", Float) = 1
        _TrackEdgeSharpness("Track Edge Normals", Range(0.001,4)) = 1
        _TrackHeightOffset("Track Heightmap Offset", Range(-1,1)) = 0
        _TrackMultiplyStrenght("Track Multiply strenght", Float) = 3
        _TrackHeightTransition("Track Normal Strenght", Range(0, 60)) = 20
        _ParallaxTrackAffineSteps("", Float) = 3
        _ParallaxTrackSteps("", Float) = 5
        _Gamma("", Float) = 0
    }

    SubShader { 
        Tags {
            "Queue" = "Geometry-100"
            "RenderType" = "Opaque" 
            "TerrainCompatible" = "True"
        }

        CGPROGRAM
        #pragma surface surf Standard vertex:SplatmapVert finalcolor:SplatmapFinalColor finalgbuffer:SplatmapFinalGBuffer addshadow fullforwardshadows
        #pragma instancing_options assumeuniformscaling nomatrices nolightprobe nolightmap forwardadd
        #pragma multi_compile_fog // needed because finalcolor oppresses fog code generation. 

        #define _ALPHATEST_ON //you can delete this line if you are not using Terrain Hole
        #define _NORMALMAP //you can delete this line if you are not using normal maps

        #pragma target 3.0
        #include "UnityPBSLighting.cginc"
                
        #pragma shader_feature_local __ _TERRAIN_MASK_MAPS _TERRAIN_NORMAL_IN_MASK
        #pragma shader_feature_local __ _TERRAIN_TRIPLANAR_ONE _TERRAIN_TRIPLANAR

        #pragma shader_feature_local _TERRAIN_DISTANCEBLEND        
        #pragma shader_feature_local _TRACKS
        #pragma shader_feature_local _TERRAIN_PARALLAX

        #pragma shader_feature_local _LAYERS_TWO

        #define _TERRAIN_BLEND_HEIGHT
        #define TERRAIN_INSTANCED_PERPIXEL_NORMAL
        #define TERRAIN_SURFACE_OUTPUT SurfaceOutputStandard   

        #include "InTerra_InputsAndFunctions.cginc"
        #include "InTerra_Mixing.cginc"
       
        //============================================================================
        //---------------------------------  SURFACE ---------------------------------
        //============================================================================
        void surf (Input IN, inout SurfaceOutputStandard o) {
            half weight;
            fixed4 mixedDiffuse;

            #ifdef _LAYERS_TWO
                half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, 0, 0);
            #else
                half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
            #endif

            SplatmapMix(IN, defaultSmoothness, weight, mixedDiffuse, o.Normal, o.Occlusion, o.Metallic);
            o.Albedo = mixedDiffuse.rgb;
            o.Alpha = weight;
            o.Smoothness = mixedDiffuse.a;
        }
        ENDCG

        UsePass "Hidden/Nature/Terrain/Utilities/PICKING"
        UsePass "Hidden/Nature/Terrain/Utilities/SELECTION"
    }

    Dependency "AddPassShader"    = "Hidden/InTerra/InTerra-AddPass"
    Dependency "BaseMapShader"    = "Hidden/InTerra/InTerra-Base"
    Dependency "BaseMapGenShader" = "Hidden/InTerra/InTerra-BaseGen"

    Fallback "Nature/Terrain/Diffuse"
	
	CustomEditor "InTerra.InTerra_TerrainShaderGUI" 
}