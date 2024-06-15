#ifdef INTERRA_OBJECT
    void ObjectIntegration_float(float heightOffset, float3 tangentViewDirTerrain, float3 worldViewDir, float3 worldNormal, float3 worldTangent, float3 worldBitangent, float3 worldPos, float4 terrainNormals, float4 objectAlbedo, float4 objectNormal, float4 objectMask, float3 objectEmission, out float3 albedo, out float3 mixedNormal, out float smoothness, out float metallic, out float occlusion, out float3 emission)
#else
    #ifndef TESSELLATION_ON
        #include "InTerra_Functions.hlsl"
    #endif
    void SplatmapMix(float2 splatBaseUV, float3 worldNormal, float3 tangentViewDirTerrain, float3 worldPos, out float3 mixedAlbedo, out float smoothness, out float metallic, out float occlusion, inout float3 mixedNormal)
#endif
{
    float4 mixedDiffuse;
    float3 wTangent;
    float3 wBTangent;
    #include "InTerra_SplatMapControl.hlsl"

    //====================================================================================
    //-----------------------------------  MASK MAPS  ------------------------------------
    //====================================================================================
    SampleMask(mask, uvSplat, blendMask);
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        #ifdef _TERRAIN_TRIPLANAR_ONE
            SampleMaskTOL(mask_front, mask, uvSplat_front);
            SampleMaskTOL(mask_side, mask, uvSplat_side);
        #else
            SampleMask(mask_front, uvSplat_front, blendMask);
            SampleMask(mask_side, uvSplat_side, blendMask);
        #endif 
        MaskWeight(mask, mask_front, mask_side, blendMask, weights, _HeightTransition);
    #endif
    #ifdef _TERRAIN_DISTANCEBLEND		
        SampleMask(dMask, distantUV, dBlendMask);
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleMaskTOL(dMask_front, dMask, distantUV_front);
                SampleMaskTOL(dMask_side, dMask, distantUV_side);
            #else
                SampleMask(dMask_front, distantUV_front, dBlendMask);
                SampleMask(dMask_side, distantUV_side, dBlendMask);
            #endif
        MaskWeight(dMask, dMask_front, dMask_side, dBlendMask, weights, _Distance_HeightTransition);
        #endif
    #endif             

    //========================================================================================
    //------------------------------ HEIGHT MAP SPLAT BLENDINGS ------------------------------
    //========================================================================================
    #if defined(_TERRAIN_BLEND_HEIGHT) && !defined(_LAYERS_ONE) && !defined(TERRAIN_SPLAT_ADDPASS)
        if (_HeightmapBlending == 1)
        {
            HeightBlend(mask, blendMask, _HeightTransition);
            #ifdef _TERRAIN_DISTANCEBLEND
                HeightBlend(dMask, dBlendMask, _Distance_HeightTransition);
            #endif
        }
    #endif 

    //========================================================================================
    //-------------------------------  ALBEDO, SMOOTHNESS & NORMAL ---------------------------
    //========================================================================================
    SampleSplat(uvSplat, blendMask, mask, mixedDiffuse, mixedNormal);
    #ifdef INTERRA_OBJECT
        wTangent = worldTangent;
        wBTangent = worldBitangent;

        mixedNormal = WorldTangent(wTangent, wBTangent, mixedNormal);
    #endif
        
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        float4 frontDiffuse;
        float3 frontNormal;
        float4 sideDiffuse;
        float3 sideNormal;

        #ifdef _TERRAIN_TRIPLANAR_ONE
            SampleSplatTOL(frontDiffuse, frontNormal, mixedDiffuse, mixedNormal, uvSplat_front, blendMask, mask);
            SampleSplatTOL(sideDiffuse, sideNormal, mixedDiffuse, mixedNormal, uvSplat_side, blendMask, mask);
        #else
            SampleSplat(uvSplat_front, blendMask, mask, frontDiffuse, frontNormal);
            SampleSplat(uvSplat_side, blendMask, mask, sideDiffuse, sideNormal);
        #endif 
        mixedDiffuse = (mixedDiffuse * weights.y) + (frontDiffuse * weights.z) + (sideDiffuse * weights.x);
        mixedNormal = TriplanarNormal(mixedNormal, wTangent, wBTangent, frontNormal, sideNormal, weights, flipUV);
    #endif

    #ifdef _TERRAIN_DISTANCEBLEND  
    
        float4 distantDiffuse;   
        float3 distantNormal;

        SampleSplat(distantUV, dBlendMask, dMask, distantDiffuse, distantNormal);
        #ifdef INTERRA_OBJECT
            distantNormal = WorldTangent(wTangent, wBTangent, distantNormal);
        #endif
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            float4 dFrontDiffuse;
            float3 dFontNormal;
            float4 dSideDiffuse;
            float3 dSideNormal;

            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleSplatTOL(dFrontDiffuse, dFontNormal, distantDiffuse, distantNormal, distantUV_front, dBlendMask, dMask);
                SampleSplatTOL(dSideDiffuse, dSideNormal, distantDiffuse, distantNormal, distantUV_side, dBlendMask, dMask);
            #else
                SampleSplat(distantUV_front, dBlendMask, dMask, dFrontDiffuse, dFontNormal);
                SampleSplat(distantUV_side, dBlendMask, dMask, dSideDiffuse, dSideNormal);
            #endif
            distantDiffuse = (distantDiffuse * weights.y) + (dFrontDiffuse * weights.z) + (dSideDiffuse * weights.x);
            distantNormal = TriplanarNormal(distantNormal, wTangent, wBTangent, dFontNormal, dSideNormal, weights, flipUV);
        #endif
                
        float dist = smoothstep(_HT_distance.x, _HT_distance.y, (distance(worldPos, _WorldSpaceCameraPos)));
        distantDiffuse = lerp(mixedDiffuse, distantDiffuse, _HT_cover);
        distantNormal = lerp(mixedNormal, distantNormal, _HT_cover);
        #ifdef _TERRAIN_BASEMAP_GEN            
            mixedDiffuse = distantDiffuse;
        #else
            mixedDiffuse = lerp(mixedDiffuse, distantDiffuse, dist); 
            mixedNormal = lerp(mixedNormal, distantNormal, dist);
        #endif        
    #endif
            
    float3 tint = SAMPLE_TEXTURE2D(_TerrainColorTintTexture, SamplerState_Linear_Repeat, tintUV).rgb;
    #if !defined(TRIPLANAR_TINT) 
        mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, (mixedDiffuse.rgb * tint), _TerrainColorTintStrenght).rgb;
    #endif

    float normalDist = smoothstep(_TerrainNormalTintDistance.x, _TerrainNormalTintDistance.y, (distance(worldPos, _WorldSpaceCameraPos)));
    float3 normalTint = UnpackNormals(SAMPLE_TEXTURE2D(_TerrainNormalTintTexture, sampler_Splat0, normalTintUV), 1);
    #ifdef INTERRA_OBJECT
        normalTint = WorldTangent(wTangent, wBTangent, normalTint);
    #endif   
    mixedNormal = lerp(mixedNormal, BlendNormals(mixedNormal, normalTint), _TerrainNormalTintStrenght * normalDist).rgb;


    //========================================================================================
    //--------------------------------   AMBIENT OCCLUSION   ---------------------------------
    //========================================================================================
    occlusion = 1;
    #if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK)
        occlusion = AmbientOcclusion(mask, blendMask);
        #if defined (_TERRAIN_DISTANCEBLEND)
            float dAo = AmbientOcclusion(dMask, dBlendMask);
            dAo = lerp(occlusion, dAo, _HT_cover);
            occlusion = lerp(occlusion, dAo, dist);
        #endif
    #endif

    //========================================================================================
    //--------------------------------------   METALLIC   ------------------------------------
    //========================================================================================
    metallic = MetallicMask(mask, blendMask);
    #if defined (_TERRAIN_DISTANCEBLEND)
        float dMetallic = MetallicMask(dMask, dBlendMask);
        dMetallic = lerp(metallic, dMetallic, _HT_cover);
        metallic = lerp(metallic, dMetallic, dist);
    #endif


    //========================================================================================
    //---------------------------------------  TRACKS   --------------------------------------
    //========================================================================================
    #if defined(_TRACKS) && !defined(_TERRAIN_BASEMAP_GEN)
        UnpackTrackSplatValues(trackSplats);
        UnpackTrackSplatColor(trackSplatsColor);

        float4 trackColor = TrackSplatValues(tBlendMask, trackSplatsColor);
        float4 trackValues = TrackSplatValues(tBlendMask, trackSplats);

        #ifdef INTERRA_OBJECT
            sampler sc = sampler_Control;
            float2 terrainSize = _TerrainSize.xz * 10;
        #else
            sampler sc = sampler_Control0;
            float2 terrainSize = _TerrainSizeXZPosY.xy;
        #endif
        
        float2 trackDetailUV = (float2(splatBaseUV.x, -splatBaseUV.y) * _TrackDetailTexture_ST.xy  * terrainSize + _TrackDetailTexture_ST.zw);

         #ifdef _TERRAIN_PARALLAX
            float2 trackParallaxOffset = ParallaxOffset(_InTerra_TrackTexture, sc, _ParallaxTrackSteps,  -trackValues.y, trackUV, float3( -tangentViewDirTerrain.x, tangentViewDirTerrain.y, -tangentViewDirTerrain.z), _ParallaxTrackAffineSteps, _MipMapLevel + (lod * (log2(max(_InTerra_TrackTexture_TexelSize.z, _InTerra_TrackTexture_TexelSize.w)) + 1)), 1 );

              trackUV += trackParallaxOffset;
              trackDetailUV += (trackParallaxOffset ) * (_TrackDetailTexture_ST.xy * _InTerra_TrackArea);
        #endif
        float4 trackDetail = SAMPLE_TEXTURE2D(_TrackDetailTexture, SamplerState_Linear_Repeat, trackDetailUV);
        trackDepth = SAMPLE_TEXTURE2D_LOD(_InTerra_TrackTexture, sc, trackUV, 0);
 
        float normalsOffset = _InTerra_TrackTexture_TexelSize.x;
        float texelArea = _InTerra_TrackTexture_TexelSize.x * 100 * _InTerra_TrackArea;
        float normalStrenghts = _TrackNormalStrenght / texelArea;
        float normalEdgeStrenghts = _TrackEdgeNormals / texelArea;

        float4 heights[4];
        heights[0] = (SAMPLE_TEXTURE2D(_InTerra_TrackTexture, SamplerState_Linear_Repeat, trackUV + float2(0.0, normalsOffset)));
        heights[1] = (SAMPLE_TEXTURE2D(_InTerra_TrackTexture, SamplerState_Linear_Repeat, trackUV + float2(normalsOffset, 0.0)));
        heights[2] = (SAMPLE_TEXTURE2D(_InTerra_TrackTexture, SamplerState_Linear_Repeat, trackUV + float2(-normalsOffset, 0.0)));
        heights[3] = (SAMPLE_TEXTURE2D(_InTerra_TrackTexture, SamplerState_Linear_Repeat, trackUV + float2(0.0, -normalsOffset)));

        for (int i = 0; i < 4; ++i)
        {
            heights[i] *= float4(1.0f, 1.0f, normalStrenghts * 2, normalEdgeStrenghts * 2);
        }

        float3 edgeNormals = (float3(float2(heights[2].a - heights[1].a, heights[0].a - heights[3].a), 1.0f));
        float3 trackNormal =  float3(float2(heights[2].b - heights[1].b, heights[0].b - heights[3].b), 1.0f);        

        float4 trackDetailNormal = SAMPLE_TEXTURE2D(_TrackDetailNormalTexture, SamplerState_Linear_Repeat, trackDetailUV);
        trackDetailNormal.xyz = UnpackNormalScale(float4(trackDetailNormal.x, trackDetailNormal.y, 0, 1-trackDetailNormal.w), _TrackDetailNormalStrenght);
        trackDetailNormal.z += 1e-5f;

        float heightSum = HeightSum(mask, blendMask);
        float trackHeightMap = saturate(trackDepth.b + _TrackHeightOffset);
        float2 trackIntersect = float2(trackDepth.b, 1 - trackDepth.b);

        trackIntersect *= (1 / (pow(2, float2(trackHeightMap, heightSum) * (-(_TrackHeightTransition)))) + 1) * 0.5;
        trackIntersect /= (trackIntersect.r + trackIntersect.g);

        trackNormal = (lerp(trackNormal, normalize(lerp(trackDetailNormal.xyz, trackNormal, 0.5f)), trackValues.a)) * trackIntersect.r;
        float trackEdge = saturate(pow(abs(trackDepth.a), _TrackEdgeSharpness));
  
        float track = trackIntersect.r * (1 - trackDist);
        float colorOpacity = saturate(track * trackColor.a);
        float normalOpacity = saturate(trackValues.z * (trackEdge + track)) * (1 - trackDist);

        trackNormal = normalize(lerp(edgeNormals, trackNormal, trackDepth.b));
        trackNormal.z += 1e-5f;
        trackColor = lerp(trackColor, (trackColor * trackDetail), trackValues.a);

        #ifdef INTERRA_OBJECT
            trackNormal.xy *= -1;
            edgeNormals.xy *= -1;
            trackNormal = WorldTangent(wTangent, wBTangent, trackNormal);
        #endif

        mixedNormal = lerp(mixedNormal, trackNormal, normalOpacity);
        mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, trackColor.rgb, colorOpacity);
        mixedDiffuse.a = lerp(mixedDiffuse.a, trackValues.x, track);
        occlusion = lerp(occlusion, _TrackAO, track);
    #endif

    mixedDiffuse.a = lerp(mixedDiffuse.a, 1.0f, _InTerra_GlobalSmoothness);

    //=======================================================================================
    //==============================|   OBJECT INTEGRATION   |===============================
    //=======================================================================================
    #ifdef INTERRA_OBJECT	
        float steepWeights = _SteepIntersection == 1 ? saturate(worldNormal.y + _Steepness) : 1;
        float intersect1 = smoothstep(_Intersection.y, _Intersection.x, heightOffset) * steepWeights;
        float intersect2 = smoothstep(_Intersection2.y, _Intersection2.x, heightOffset) * (1 - steepWeights);
        float intersection = intersect1 + intersect2;
        float intersectNormal = smoothstep(_NormIntersect.y, _NormIntersect.x, heightOffset);
         
        objectMask.rgba = objectMask.rgba * _MaskMapRemapScale.rgba + _MaskMapRemapOffset.rgba;
        objectAlbedo.a = _HasMask == 1 ? objectMask.a : _Smoothness;
        objectAlbedo.a = _GlobalSmoothnessDisabled ? objectAlbedo.a : lerp(objectAlbedo.a, 1.0f, _InTerra_GlobalSmoothness);
        float objectMetallic = _HasMask == 1 ? objectMask.r : _Metallic;
        float objectAo = _HasMask == 1 ? objectMask.g : _Ao;
        float height = objectMask.b;

        float sSum = 0.5f;
        if (_HeightmapBlending == 1)
        {
            sSum = lerp(HeightSum(mask, blendMask), 1, intersection);
        }

        float2 heightIntersect = (1 / (1 * pow(2, float2(((1 - intersection) * height), (intersection * sSum)) * (-(_Sharpness)))) + 1) * 0.5;
        heightIntersect /= (heightIntersect.r + heightIntersect.g);

        float3 mainNorm = UnpackNormalScale(objectNormal, _NormalScale);

        // avoid risk of NaN when normalizing.
        mainNorm.z += 1e-5f;
        
        float3 dt = float3(0, 0, 0);
        #ifdef _OBJECT_DETAIL
            UNITY_BRANCH if (_HasDetailAlbedo > 0)
            {
                float3 dt = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, _DetailUV).rgb;
                objectAlbedo.rgb = lerp(objectAlbedo.rgb, half(2.0) * dt, _DetailStrenght).rgb;
            }

            float3 mainNormD = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, _DetailUV), _DetailNormalMapScale);
            mainNorm = (lerp(mainNorm, BlendNormalRNM(mainNorm, mainNormD), _DetailStrenght));
        #endif

        mixedDiffuse = lerp(mixedDiffuse, objectAlbedo, heightIntersect.r);

        float3 terrainNormal = (mixedNormal.z * terrainNormals.xyz) + 1e-5f;
        terrainNormal.xy = mixedNormal.xy + terrainNormal.xy;
        mixedNormal = lerp(mixedNormal, terrainNormal, intersectNormal);
        mixedNormal = lerp(mixedNormal, mainNorm, heightIntersect.r);

        metallic = lerp(metallic, objectMetallic, heightIntersect.r);
        occlusion = lerp(occlusion, objectAo, heightIntersect.r);
        albedo = mixedDiffuse.rgb;
        emission = 0;

        UNITY_BRANCH if (_EmissionEnabled > 0)
        {
            emission = lerp(0, objectEmission, heightIntersect.r);
        }
    #else
        mixedAlbedo = mixedDiffuse.rgb;
    #endif

    smoothness = mixedDiffuse.a; 
    //=========================================================================================             
}
