//===================================================================================
//====================================| VERTEX |=====================================
//===================================================================================
void SplatmapVert(inout appdata_full v, out Input data)
{
    UNITY_INITIALIZE_OUTPUT(Input, data);
    #if defined(INTERRA_OBJECT) || defined(TRIPLANAR)
        data.worldNormal = UnityObjectToWorldNormal(v.normal);
        data.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
    #endif
    
    //========================== TERRAIN INSTANCING ================================
    #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) && !defined(INTERRA_OBJECT)
        float2 patchVertex = v.vertex.xy;
        float4 instanceData = UNITY_ACCESS_INSTANCED_PROP(Terrain, _TerrainPatchInstanceData);

        float4 uvscale = instanceData.z * _TerrainHeightmapRecipSize;
        float4 uvoffset = instanceData.xyxy * uvscale;
        uvoffset.xy += 0.5f * _TerrainHeightmapRecipSize.xy;
        float2 sampleCoords = (patchVertex.xy * uvscale.xy + uvoffset.xy);

        float hm = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(sampleCoords, 0, 0)));
        v.vertex.xz = (patchVertex.xy + instanceData.xy) * _TerrainHeightmapScale.xz * instanceData.z; 
        v.vertex.y = hm * _TerrainHeightmapScale.y;
        v.vertex.w = 1.0f;

        v.texcoord.xy = (patchVertex.xy * uvscale.zw + uvoffset.zw);
        v.texcoord3 = v.texcoord2 = v.texcoord1 = v.texcoord;  

        #ifdef TERRAIN_INSTANCED_PERPIXEL_NORMAL
            v.normal = float3(0, 1, 0); 
            data.tc.zw = sampleCoords;
        #else
            float3 nor = tex2Dlod(_TerrainNormalmapTexture, float4(sampleCoords, 0, 0)).xyz;
            v.normal = 2.0f * nor - 1.0f;
        #endif

        #ifdef TRIPLANAR
            float3 nor = tex2Dlod(_TerrainNormalmapTexture, float4(sampleCoords, 0, 0)).xyz;
            data.terrainNormals = UnityObjectToWorldNormal(2.0f * nor - 1.0f);
        #endif
    #endif
    //===================================================================================
    float3 wDir = WorldSpaceViewDir(v.vertex);

    #if !defined(INTERRA_OBJECT)
        data.tc.xy = v.texcoord.xy;

        #ifdef TERRAIN_BASE_PASS
            #ifdef UNITY_PASS_META
                data.tc.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
            #endif
        #else
            float4 pos = UnityObjectToClipPos(v.vertex);
            UNITY_TRANSFER_FOG(data, pos);
        #endif
        
        v.tangent.xyz = cross(v.normal, float3(0, 0, 1));
        v.tangent.w = -1;

        #ifdef _TERRAIN_PARALLAX
            data.tangentViewDir = TangentViewDir(v.normal, v.tangent, wDir);
        #endif
    #else
    //================================   OBJECTS INTEGRATION  ==============================
        #ifdef _OBJECT_PARALLAX
            data.tangentViewDirObject = TangentViewDir(normalize(v.normal), normalize(v.tangent), ObjSpaceViewDir(v.vertex));
        #endif

        float2 hmUV = float2 ((data.worldPos.x - _TerrainPosition.x) * (1 / _TerrainSize.x), (data.worldPos.z - _TerrainPosition.z) * (1 / _TerrainSize.z));

        float4 ts = float4(_TerrainHeightmapTexture_TexelSize.x, _TerrainHeightmapTexture_TexelSize.y, 0, 0);
        float4 hsX = _TerrainHeightmapScale.w / _TerrainHeightmapScale.x;
        float4 hsZ = _TerrainHeightmapScale.w / _TerrainHeightmapScale.z;

        float4 heightToNormal;
        float3 terrainNormal;

        heightToNormal[0] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(hmUV + float2(ts * float2(0, -1)), 0, 0))).r * hsZ;
        heightToNormal[1] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(hmUV + float2(ts * float2(-1, 0)), 0, 0))).r * hsX;
        heightToNormal[2] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(hmUV + float2(ts * float2(1, 0)), 0, 0))).r * hsX;
        heightToNormal[3] = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(hmUV + float2(ts * float2(0, 1)), 0, 0))).r * hsZ;

        terrainNormal.x = heightToNormal[1] - heightToNormal[2];
        terrainNormal.z = heightToNormal[0] - heightToNormal[3];
        terrainNormal.y = 1;

        float3 height = UnpackHeightmap(tex2Dlod(_TerrainHeightmapTexture, float4(hmUV, 0, 0)));

        float heightOffset = data.worldPos.y - _TerrainPosition.y + (height * -_TerrainHeightmapScale.y);




        float3 tWeights = pow(abs(terrainNormal), _TriplanarSharpness);
        tWeights = tWeights / (tWeights.x + tWeights.y + tWeights.z);

        data.mainTC_tWeightY_hOffset = float4(v.texcoord.xy, tWeights.y, heightOffset);

        #ifdef _TERRAIN_PARALLAX
            float intersection = smoothstep(_NormIntersect.y, _NormIntersect.x, heightOffset);
            float3 mixedNormal = lerp(data.worldNormal, normalize(terrainNormal), intersection);
            half3 axisSign = sign(mixedNormal);
            half3 tangentY = normalize(cross(mixedNormal, half3(0, 0, axisSign.y)));
            half3 bitangentY = normalize(cross(tangentY, mixedNormal)) * axisSign.y;
            half3x3 tbnY = half3x3(tangentY, bitangentY, mixedNormal);
            data.tangentViewDir = mul(tbnY, wDir);
        #endif

        float3 wTangent = UnityObjectToWorldDir(v.tangent);
        float3 wBTangent = normalize(cross(data.worldNormal, wTangent)) * v.tangent.w * unity_WorldTransformParams.w;
        float3x3 tangentTransform_World = float3x3(wTangent, wBTangent, data.worldNormal);
        data.terrainNormals = mul(tangentTransform_World, terrainNormal);
    //========================================================================================
    #endif
}

//==============================================================================================
//==================================|   FRAGMENT MIXING    |====================================
//==============================================================================================
#ifdef DIFFUSE
    void SplatmapMix(Input IN, half4 defaultAlpha, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal)
#else
    #ifdef INTERRA_OBJECT
        void SplatmapMix(Input IN, half4 defaultAlpha, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal, out float ao, out half metallic, out half3 emission)
    #else
        void SplatmapMix(Input IN, half4 defaultAlpha, out half weight, out fixed4 mixedDiffuse, inout fixed3 mixedNormal, out float ao, out half metallic)
    #endif
#endif
{
    half4 splat_control = 1;
    mixedDiffuse = 1;
    #ifdef  PARALLAX
        float lod = smoothstep(_MipMapFade.x, _MipMapFade.y, (distance(IN.worldPos, _WorldSpaceCameraPos)));
    #endif

    #ifdef INTERRA_OBJECT       
        float2 mainUV = TRANSFORM_TEX(IN.mainTC_tWeightY_hOffset.xy, _MainTex);
        #ifdef _OBJECT_PARALLAX
            float2 mainParallaxOffset = 0;
            mainParallaxOffset = ParallaxOffset(_MainMask, _ParallaxSteps, _ParallaxHeight, IN.mainTC_tWeightY_hOffset.xy, normalize(IN.tangentViewDirObject), _ParallaxAffineSteps, _MipMapLevel + (_MipMapLevel + (lod * _MipMapCount)), 0);
            mainUV += mainParallaxOffset;
        #endif
        #if !defined(_LAYERS_ONE)
            float4 objectAlbedo = UNITY_SAMPLE_TEX2D(_MainTex, mainUV) * _Color;
        #else
            float4 objectAlbedo = tex2D(_MainTex, mainUV) * _Color;
        #endif
        weight = 0;
    #endif	

    float2 uvSplat[_LAYER_COUNT];
     #ifdef _LAYERS_ONE    
        half4 splat, mask;
        #ifdef TRIPLANAR
            float2 uvSplat_front[_LAYER_COUNT], uvSplat_side[_LAYER_COUNT];
            half4 splat_front, splat_side, mask_front, mask_side;
        #endif
        #ifdef _TERRAIN_DISTANCEBLEND
            float2 distantUV[_LAYER_COUNT];
            half4 dSplat, dMask;
            #ifdef TRIPLANAR
                float2 distantUV_front[_LAYER_COUNT], distantUV_side[_LAYER_COUNT];
                half4 dSplat_front, dSplat_side, dMask_front, dMask_side;
            #endif
        #endif
    #else        
        half4 splat[_LAYER_COUNT], mask[_LAYER_COUNT];
        #ifdef TRIPLANAR
            float2 uvSplat_front[_LAYER_COUNT], uvSplat_side[_LAYER_COUNT];
            half4 splat_front[_LAYER_COUNT], splat_side[_LAYER_COUNT], mask_front[_LAYER_COUNT], mask_side[_LAYER_COUNT];
        #endif
        #ifdef _TERRAIN_DISTANCEBLEND
            float2 distantUV[_LAYER_COUNT];
            half4 dSplat[_LAYER_COUNT], dMask[_LAYER_COUNT];
            #ifdef TRIPLANAR
                float2 distantUV_front[_LAYER_COUNT], distantUV_side[_LAYER_COUNT];
                half4 dSplat_front[_LAYER_COUNT], dSplat_side[_LAYER_COUNT], dMask_front[_LAYER_COUNT], dMask_side[_LAYER_COUNT];
            #endif
        #endif
    #endif
    #ifdef _TRACKS
        float4 trackSplats[_LAYER_COUNT];
        float4 trackSplatsColor[_LAYER_COUNT];
        float4 trackDepth = 0;
    #endif

    #if defined(_ALPHATEST_ON) && !defined(INTERRA_OBJECT)
        ClipHoles(IN.tc.xy);
    #endif

    #ifndef INTERRA_OBJECT 
        //In Diffuse remap alphas channels there are the values for parallax mapping, in red channel of _DiffuseRemapOffset there are the values of Layers scales adjust
        //Unity is subtracting the _DiffuseRemapOffset from _DiffuseRemapScale for Terrain shaders, but since the values are used for other purpose than originally there is need to add them back
        _DiffuseRemapScale0 += _DiffuseRemapOffset0;
        _DiffuseRemapScale1 += _DiffuseRemapOffset1;
        #if !defined(_LAYERS_TWO)
            _DiffuseRemapScale2 += _DiffuseRemapOffset2;
            _DiffuseRemapScale3 += _DiffuseRemapOffset3;
        #endif
    #endif

    //--------------------------- SPLAT MAP CONTROL ----------------------------------
    #ifdef INTERRA_OBJECT
        float2 terrainUV = (IN.worldPos.xz - _TerrainPosition.xz) * (1 / _TerrainSize.xz);
        #ifndef _LAYERS_ONE     
            float2 splatMapUV = (terrainUV * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
        #endif
        half2 tintUV = terrainUV * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw;
        half2 normalTintUV = terrainUV * _TerrainNormalTintTexture_ST.xy + _TerrainNormalTintTexture_ST.zw;
    #else
        float2 splatMapUV = (IN.tc.xy * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
        half2 tintUV = IN.tc.xy * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw;
        half2 normalTintUV = IN.tc.xy * _TerrainNormalTintTexture_ST.xy + _TerrainNormalTintTexture_ST.zw;
    #endif

    #if !defined(_LAYERS_ONE)
        splat_control = tex2D(_Control, splatMapUV);
    #endif

    #if !defined(INTERRA_OBJECT)
        weight = dot(splat_control, half4(1, 1, 1, 1));
        #if !defined(SHADER_API_MOBILE) && defined(TERRAIN_SPLAT_ADDPASS)
            clip(weight == 0.0f ? -1 : 1);
        #endif
        splat_control /= (weight + 1e-3f);
    #endif

    #if defined(_TERRAIN_BLEND_HEIGHT)
        splat_control = (splat_control.r + splat_control.g + splat_control.b + splat_control.a == 0.0f ? 1.0f : splat_control); //this is preventing the black area when more than one pass
    #endif
 
    #if defined(INTERRA_OBJECT) || defined(TRIPLANAR) || defined(_TERRAIN_TRIPLANAR_ONE)
        #ifdef UNITY_INSTANCING_ENABLED
            float3 wNormal = IN.terrainNormals;
        #else
            float3 wNormal = WorldNormalVector(IN, float3(0, 0, 1));
        #endif
        float3 flipUV = wNormal < 0 ? -1 : 1;
        float3  weights = abs(wNormal);
        weights = pow(weights, _TriplanarSharpness);
        weights = weights / (weights.x + weights.y + weights.z);

        #if defined(INTERRA_OBJECT)
            TriplanarOneToAllSteep(splat_control, (1 - IN.mainTC_tWeightY_hOffset.z), weight);
        #else
            TriplanarOneToAllSteep(splat_control, (1 - weights.y), weight);
        #endif
    #endif

    #if defined(_LAYERS_TWO)
        splat_control.r = _ControlNumber == 0 ? splat_control.r : _ControlNumber == 1 ? splat_control.g : _ControlNumber == 2 ? splat_control.b : splat_control.a;
        splat_control.g = 1 - splat_control.r;
    #endif

    #ifdef TERRAIN_SPLAT_ADDPASS
        _HeightmapBlending = 0;
    #endif

    #ifdef _TERRAIN_DISTANCEBLEND
        float4 dSplat_control = splat_control;
    #endif


    //================================================================================ 
    //-------------------------------------- UVs -------------------------------------
    //================================================================================
    #ifdef INTERRA_OBJECT
        #if !defined(_OBJECT_TRIPLANAR)
            _SteepDistortion = wNormal.y > 0.5 ? 0 : (1 - wNormal.y) * _SteepDistortion;
            UvSplatDistort(uvSplat, IN.worldPos.xz - _TerrainPosition.xz, objectAlbedo * _SteepDistortion);
        #else
            UvSplat(uvSplat, IN.worldPos.xz - _TerrainPosition.xz);
        #endif
    #else
        uvSplat[0] = TRANSFORM_TEX(IN.tc.xy, _Splat0);
        uvSplat[1] = TRANSFORM_TEX(IN.tc.xy, _Splat1);
        #if !defined(_LAYERS_TWO)
            uvSplat[2] = TRANSFORM_TEX(IN.tc.xy, _Splat2);
            uvSplat[3] = TRANSFORM_TEX(IN.tc.xy, _Splat3);
        #endif
    #endif   
        
    //--------------------- TRIPLANAR UV ------------------------
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        #ifdef INTERRA_OBJECT
            float offsetZ = _DisableOffsetY == 1 ? -flipUV.z * IN.worldPos.y : IN.mainTC_tWeightY_hOffset.w * -flipUV.z + (IN.worldPos.z);
            float offsetX = _DisableOffsetY == 1 ? -flipUV.x * IN.worldPos.y : IN.mainTC_tWeightY_hOffset.w * -flipUV.x + (IN.worldPos.x);
              
            UvSplatFront(uvSplat_front, IN.worldPos.x - _TerrainPosition.x, offsetZ - _TerrainPosition.z, flipUV);
            UvSplatSide(uvSplat_side, IN.worldPos.z - _TerrainPosition.z, offsetX - _TerrainPosition.x, flipUV);
        #else
            uvSplat_front[0] = TerrainFrontUV(IN.worldPos, _Splat0_ST, uvSplat[0]);
            uvSplat_side[0] = TerrainSideUV(IN.worldPos, _Splat0_ST, uvSplat[0]);

            #if !defined(_TERRAIN_TRIPLANAR_ONE)
                uvSplat_front[1] = TerrainFrontUV(IN.worldPos, _Splat1_ST, uvSplat[1]);
                uvSplat_side[1] = TerrainSideUV(IN.worldPos, _Splat1_ST, uvSplat[1]);

                #if !defined(_LAYERS_TWO)
                    uvSplat_front[2] = TerrainFrontUV(IN.worldPos, _Splat2_ST, uvSplat[2]);
                    uvSplat_side[2] = TerrainSideUV(IN.worldPos, _Splat2_ST, uvSplat[2]);
                
                    uvSplat_front[3] = TerrainFrontUV(IN.worldPos, _Splat3_ST, uvSplat[3]);
                    uvSplat_side[3] = TerrainSideUV(IN.worldPos, _Splat3_ST, uvSplat[3]);
                #endif
            #endif
        #endif
    #endif

    //-------------------- PARALLAX OFFSET -------------------------                   
    #ifdef _TERRAIN_PARALLAX
        ParallaxUV(uvSplat, normalize(IN.tangentViewDir), lod, 0);
    #endif

    //--------------------- DISTANCE UV ------------------------
    #ifdef _TERRAIN_DISTANCEBLEND
        DistantUV(distantUV, uvSplat);
        #ifdef TRIPLANAR
            #ifdef _TERRAIN_TRIPLANAR_ONE
                distantUV_front[0] = uvSplat_front[0] * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale;
                distantUV_side[0] = uvSplat_side[0] * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale;
            #else
                DistantUV(distantUV_front, uvSplat_front);
                DistantUV(distantUV_side, uvSplat_side);
            #endif  
        #endif
    #endif


    //====================================================================================
    //-----------------------------------  MASK MAPS  ------------------------------------
    //====================================================================================
    #if defined(TERRAIN_MASK) && !defined(DIFFUSE)
        SampleMask(mask, uvSplat);
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleMaskTOL(mask_front, mask, uvSplat_front[0]);
                SampleMaskTOL(mask_side, mask, uvSplat_side[0]);
            #else
                SampleMask(mask_front, uvSplat_front);
                SampleMask(mask_side, uvSplat_side);                
            #endif 
            MaskWeight(mask, mask_front, mask_side, weights);
        #endif
        #ifdef _TERRAIN_DISTANCEBLEND		
            SampleMask(dMask, distantUV);
            #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                    SampleMaskTOL(dMask_front, dMask, distantUV_front[0]);
                    SampleMaskTOL(dMask_side, dMask, distantUV_side[0]);
                #else
                    SampleMask(dMask_front, distantUV_front);
                    SampleMask(dMask_side, distantUV_side);
                #endif
               MaskWeight(dMask, dMask_front, dMask_side, weights);
            #endif
        #endif
    #endif 

    //========================================================================================
    //------------------------------ HEIGHT MAP SPLAT BLENDINGS ------------------------------
    //========================================================================================
    #if defined(_TERRAIN_BLEND_HEIGHT) && !defined(_LAYERS_ONE) && !defined(TERRAIN_SPLAT_ADDPASS) && !defined(DIFFUSE)
    if (_HeightmapBlending == 1)
    {
        HeightBlend(mask, splat_control, _HeightTransition);
        #ifdef _TERRAIN_DISTANCEBLEND
            HeightBlend(dMask, dSplat_control, _Distance_HeightTransition);
        #endif
    }
    #endif
        

    //========================================================================================
    //--------------------------------  ALBEDO & SMOOTHNESS  ---------------------------------
    //========================================================================================
    SampleSplat(splat, uvSplat, defaultAlpha, mask);
    #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
        #ifdef _TERRAIN_TRIPLANAR_ONE
            SampleSplatTOL(splat_front, splat, uvSplat_front[0], defaultAlpha, splat_control, mask[0]);
            SampleSplatTOL(splat_side, splat, uvSplat_side[0], defaultAlpha, splat_control, mask[0]);
        #else
            SampleSplat(splat_front, uvSplat_front, defaultAlpha, mask);
            SampleSplat(splat_side, uvSplat_side, defaultAlpha, mask);
        #endif 
        TriplanarWeight(splat, splat_front, splat_side, weights);
    #endif
    #if defined(DIFFUSE) && defined(_TERRAIN_BLEND_HEIGHT) && !defined(TERRAIN_SPLAT_ADDPASS) && !defined(_LAYERS_ONE)
        if (_HeightmapBlending == 1)
        {
            HeightBlend(splat, splat_control, _HeightTransition);
        }
    #endif    
    SplatWeight(mixedDiffuse, splat, splat_control);

    #ifdef _TERRAIN_DISTANCEBLEND
        SampleSplat(dSplat, distantUV, defaultAlpha, dMask);
        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            #ifdef _TERRAIN_TRIPLANAR_ONE
                SampleSplatTOL(dSplat_front, dSplat, distantUV_front[0], defaultAlpha, dSplat_control, dMask[0]);
                SampleSplatTOL(dSplat_side, dSplat, distantUV_side[0], defaultAlpha, dSplat_control, dMask[0]);
            #else
                SampleSplat(dSplat_front, distantUV_front, defaultAlpha,  dMask);
                SampleSplat(dSplat_side, distantUV_side, defaultAlpha,  dMask);                
            #endif
            TriplanarWeight(dSplat, dSplat_front, dSplat_side, weights);            
        #endif
        #if defined(DIFFUSE) && defined(_TERRAIN_BLEND_HEIGHT) && !defined(TERRAIN_SPLAT_ADDPASS) && !defined(_LAYERS_ONE)
            if (_HeightmapBlending == 1)
            {
                HeightBlend(dSplat, dSplat_control, _Distance_HeightTransition);
            }
        #endif
        half4 distantDiffuse;
        SplatWeight(distantDiffuse, dSplat, dSplat_control);
            
        float dist = smoothstep(_HT_distance.x, _HT_distance.y, (distance(IN.worldPos, _WorldSpaceCameraPos)));
        distantDiffuse = lerp(mixedDiffuse, distantDiffuse, _HT_cover);  
        mixedDiffuse = lerp(mixedDiffuse, distantDiffuse, dist);
    #endif

    #if defined(TERRAIN_BASE_PASS)
        half4 tint = tex2D(_TerrainColorTintTexture, tintUV);
    #else
        half4 tint = UNITY_SAMPLE_TEX2D_SAMPLER(_TerrainColorTintTexture, _Splat0, tintUV);
        float3 normalTint = UnpackNormalWithScale(UNITY_SAMPLE_TEX2D_SAMPLER(_TerrainNormalTintTexture, _Splat0, normalTintUV), 1.0f);
    #endif
    mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, ((mixedDiffuse.rgb) * (tint)), _TerrainColorTintStrenght).rgb;

    //========================================================================================
    //------------------------------------  NORMAL MAPS   ------------------------------------
    //========================================================================================
    #ifndef TERRAIN_BASE_PASS
        float3 wTangent = WorldNormalVector(IN, float3(1, 0, 0));
        float3 wBTangent = WorldNormalVector(IN, float3(0, 1, 0));
    #endif

    #if defined(_NORMALMAP) || defined(_TERRAIN_NORMAL_IN_MASK)      
        half4 normalScale;

        #ifdef INTERRA_OBJECT
            normalScale = _TerrainNormalScale;
        #else
            #ifdef _LAYERS_TWO
                normalScale = half4(_NormalScale0, _NormalScale1, 1, 1);
            #else
                normalScale = half4(_NormalScale0, _NormalScale1, _NormalScale2, _NormalScale3);
            #endif            
        #endif

        #if defined(_TERRAIN_NORMAL_IN_MASK) && defined(TERRAIN_MASK)
            mixedNormal = MaskNormal(mask, splat_control, normalScale);
        #else
            mixedNormal = SampleNormal(uvSplat, splat_control, normalScale);
        #endif
        #ifdef INTERRA_OBJECT
            mixedNormal = WorldTangent(wTangent, wBTangent, mixedNormal);
        #endif

        #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
            half3 normalFront;
            half3 normalSide;
            #ifdef _TERRAIN_NORMAL_IN_MASK
                #ifdef _TERRAIN_TRIPLANAR_ONE
                    normalFront = MaskNormalTOL(mask_front, mixedNormal, splat_control, normalScale);
                    normalSide = MaskNormalTOL(mask_side, mixedNormal, splat_control, normalScale);
                #else
                    normalFront = MaskNormal(mask_front, splat_control, normalScale);
                    normalSide = MaskNormal(mask_side, splat_control, normalScale);
                #endif 
            #else
                #ifdef _TERRAIN_TRIPLANAR_ONE
                    normalFront = SampleNormalTOL(uvSplat_front, mixedNormal, splat_control, normalScale);
                    normalSide = SampleNormalTOL(uvSplat_side, mixedNormal, splat_control, normalScale);
                #else
                    normalFront = SampleNormal(uvSplat_front, splat_control, normalScale);
                    normalSide = SampleNormal(uvSplat_side, splat_control, normalScale);
                #endif 
            #endif 
            mixedNormal = TriplanarNormal(mixedNormal, wTangent, wBTangent, normalFront, normalSide, weights, flipUV);
        #endif      

        #ifdef _TERRAIN_DISTANCEBLEND	           
            #if !defined(_TERRAIN_NORMAL_IN_MASK)
                half3 dNormal = SampleNormal(distantUV, dSplat_control, normalScale);
            #else
                half3 dNormal = MaskNormal(dMask, dSplat_control, normalScale);
            #endif

            #ifdef INTERRA_OBJECT                
                dNormal = WorldTangent(wTangent, wBTangent, dNormal);              
            #endif

            #if defined(TRIPLANAR) && !defined(TERRAIN_SPLAT_ADDPASS)
                #ifdef _TERRAIN_NORMAL_IN_MASK
                    #ifdef _TERRAIN_TRIPLANAR_ONE
                        normalFront = MaskNormalTOL(dMask_front, dNormal, dSplat_control, normalScale);
                        normalSide = MaskNormalTOL(dMask_side, dNormal, dSplat_control, normalScale);
                    #else
                        normalFront = MaskNormal(dMask_front, dSplat_control, normalScale);
                        normalSide = MaskNormal(dMask_side, dSplat_control, normalScale);
                    #endif 
                #else
                    #ifdef _TERRAIN_TRIPLANAR_ONE
                        normalFront = SampleNormalTOL(distantUV_front, dNormal, dSplat_control, normalScale);
                        normalSide = SampleNormalTOL(distantUV_side, dNormal, dSplat_control, normalScale);
                    #else
                        normalFront = SampleNormal(distantUV_front, dSplat_control, normalScale);
                        normalSide = SampleNormal(distantUV_side, dSplat_control, normalScale);
                    #endif 
                #endif 
                dNormal = TriplanarNormal(dNormal, wTangent, wBTangent, normalFront, normalSide, weights, flipUV);
            #endif   
            mixedNormal = lerp(mixedNormal, (lerp(mixedNormal, dNormal, _HT_cover)), dist);        
        #endif           
        mixedNormal.z += 1e-5f; // to avoid nan after normalizing


    #endif 

    #if defined(_TERRAIN_TINT_TEXTURE) || !defined(DIFFUSE)
        #ifdef INTERRA_OBJECT
            normalTint = WorldTangent(wTangent, wBTangent, normalTint);
        #endif
        float normalDist = smoothstep(_TerrainNormalTintDistance.x, _TerrainNormalTintDistance.y, (distance(IN.worldPos, _WorldSpaceCameraPos)));
        #if !defined(TERRAIN_BASE_PASS)
            mixedNormal = lerp(mixedNormal, BlendNormals(mixedNormal, normalTint), _TerrainNormalTintStrenght * normalDist).rgb;
        #endif
    #endif
        

    #ifndef DIFFUSE
        //========================================================================================
        //--------------------------------   AMBIENT OCCLUSION   ---------------------------------
        //========================================================================================  
        #if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK)
            ao = AmbientOcclusion(mask, splat_control); 
            #if defined (_TERRAIN_DISTANCEBLEND)
                half dAo = AmbientOcclusion(dMask, dSplat_control);
                dAo = lerp(ao, dAo, _HT_cover);
                ao = lerp(ao, dAo, dist);
            #endif
        #else
            ao = 1;
        #endif


        //========================================================================================
        //--------------------------------------   METALLIC   ------------------------------------
        //========================================================================================
        #if defined(_TERRAIN_MASK_MAPS) && !defined(_TERRAIN_NORMAL_IN_MASK)
            metallic = MetallicMask(mask, splat_control);
            #if defined (_TERRAIN_DISTANCEBLEND)
                half dMetallic = MetallicMask(dMask, dSplat_control);
                dMetallic = lerp(metallic, dMetallic, _HT_cover);
                metallic = lerp(metallic, dMetallic, dist);
            #endif
        #else
            metallic = Metallic(splat_control);
            #if defined (_TERRAIN_DISTANCEBLEND)
                half dMetallic = Metallic(dSplat_control);
                dMetallic = lerp(metallic, dMetallic, _HT_cover);
                metallic = lerp(metallic, dMetallic, dist);
            #endif
        #endif
    #endif

    //========================================================================================
    //---------------------------------------  TRACKS   --------------------------------------
    //========================================================================================
    #if defined(_TRACKS) && !defined(_TERRAIN_BASEMAP_GEN) 
        float halfTrackArea = _InTerra_TrackArea * 0.5f;
        float _InTerra_TrackFading = 30;
        float2 trackUV = float2((IN.worldPos.x - _InTerra_TrackPosition.x) + (halfTrackArea), -(IN.worldPos.z - _InTerra_TrackPosition.z - (halfTrackArea))) * 1.0f / _InTerra_TrackArea;

        float trackDist = smoothstep(_InTerra_TrackArea - _InTerra_TrackFading, _InTerra_TrackArea - 1.0f, (distance(IN.worldPos, float3(_WorldSpaceCameraPos.x, _WorldSpaceCameraPos.y, _WorldSpaceCameraPos.z))));

        UnpackTrackSplatValues(trackSplats);
        UnpackTrackSplatColor(trackSplatsColor);

        float4 trackColor = TrackSplatValues(splat_control, trackSplatsColor) ;
        float4 trackValues = TrackSplatValues(splat_control, trackSplats);

        #ifdef INTERRA_OBJECT
            float2 trackDetailUV = float2(terrainUV.x, -terrainUV.y) * _TrackDetailTexture_ST.xy * _TerrainSize.xz + _TrackDetailTexture_ST.zw;
        #else
            float2 trackDetailUV = float2(IN.tc.x, -IN.tc.y) * _TrackDetailTexture_ST.xy * _TerrainSizeXZPosY.xy + _TrackDetailTexture_ST.zw;
        #endif
       
         #ifdef _TERRAIN_PARALLAX
            float3  tangentViewDirTerrain = normalize(IN.tangentViewDir);
            float2 trackParallaxOffset = ParallaxOffset(_InTerra_TrackTexture, _ParallaxTrackSteps, -trackValues.y , trackUV, float3(-tangentViewDirTerrain.x, tangentViewDirTerrain.y, -tangentViewDirTerrain.z), _ParallaxTrackAffineSteps, 0, 1);

              trackUV += trackParallaxOffset;
              trackDetailUV += (trackParallaxOffset) * (_TrackDetailTexture_ST.xy * _InTerra_TrackArea);
        #endif

        #ifdef INTERRA_OBJECT
            trackDepth = tex2D(_InTerra_TrackTexture, trackUV);
        #else
            trackDepth = tex2D(_InTerra_TrackTexture, trackUV);
        #endif
                
       float4 trackDetail = UNITY_SAMPLE_TEX2D_SAMPLER(_TrackDetailTexture, _Splat0, trackDetailUV);

        #if defined(DIFFUSE)
            float heightSum = HeightSum(splat, splat_control);
        #else
            float heightSum = HeightSum(mask, splat_control);
        #endif    
        
        float trackHeightMap = saturate(trackDepth.b + _TrackHeightOffset);
        float2 trackIntersect = float2(trackDepth.b, 1- trackDepth.b);
        trackIntersect *= (1 / (pow(2, float2(trackHeightMap, heightSum) * (-(_TrackHeightTransition)))) + 1) * 0.5;
        trackIntersect /= (trackIntersect.r + trackIntersect.g);

        float trackEdge = saturate(pow(trackDepth.a, _TrackEdgeSharpness));
        float track = trackIntersect.r * (1 - trackDist);
        float colorOpacity = saturate(track * trackColor.a);

        trackColor = lerp(trackColor, (trackColor * trackDetail), trackValues.a);
        mixedDiffuse.rgb = lerp(mixedDiffuse, trackColor, colorOpacity);
        mixedDiffuse.a = lerp(mixedDiffuse.a, trackValues.x, track);
       
        #if defined(_NORMALMAP) || defined(_TERRAIN_NORMAL_IN_MASK)
            float3 trackDetailNormal = UnpackNormalWithScale(UNITY_SAMPLE_TEX2D_SAMPLER(_TrackDetailNormalTexture, _Splat0, trackDetailUV), _TrackDetailNormalStrenght);
            trackDetailNormal.y *= -1;

            float normalsOffset = _InTerra_TrackTexture_TexelSize * 2.0f;
            float texelArea = _InTerra_TrackTexture_TexelSize * 100 * _InTerra_TrackArea;
            float normalStrenghts = _TrackNormalStrenght / texelArea;
            float normalEdgeStrenghts = _TrackEdgeNormals / texelArea;

            float4 heights[4];
             heights[0] = (tex2D(_InTerra_TrackTexture, trackUV + float2(0.0, normalsOffset)));
             heights[1] = (tex2D(_InTerra_TrackTexture, trackUV + float2(normalsOffset, 0.0)));
             heights[2] = (tex2D(_InTerra_TrackTexture, trackUV + float2(-normalsOffset, 0.0)));
             heights[3] = (tex2D(_InTerra_TrackTexture, trackUV + float2(0.0, -normalsOffset)));

             for (int i = 0; i < 4; ++i)
             {
                 heights[i] *= float4(1.0f ,1.0f, normalStrenghts, normalEdgeStrenghts);
             }

            float3 edgeNormals = float3(float2(heights[1].a - heights[2].a, heights[3].a - heights[0].a), 1.0f);
            float3 trackNormal = float3(float2(heights[1].b - heights[2].b, heights[3].b - heights[0].b), 1.0f)  ;
            trackNormal = (lerp((trackNormal), BlendNormals(trackDetailNormal, trackNormal), trackValues.a)) * trackIntersect.r;
            trackNormal = normalize(lerp(edgeNormals, trackNormal, trackDepth.b));

            trackNormal.z += 1e-5f;
            trackNormal = (lerp(edgeNormals, trackNormal, trackDepth.b));
 
            float normalOpacity = saturate(trackValues.z * (trackEdge + track)) * (1 - trackDist);

            #ifdef INTERRA_OBJECT
                trackNormal = WorldTangent(wTangent, wBTangent, trackNormal);
            #endif
            mixedNormal = lerp(mixedNormal, trackNormal, normalOpacity);
        #endif       

        #ifndef DIFFUSE
            ao = lerp(ao, _TrackAO, track);
        #endif
    #endif

    #ifndef DIFFUSE
        mixedDiffuse.a = lerp(mixedDiffuse.a, 1.0f, _InTerra_GlobalSmoothness);
    #endif
    //=======================================================================================
    //==============================|   OBJECT INTEGRATION   |===============================
    //=======================================================================================
    #ifdef INTERRA_OBJECT	
        float steepWeights = _SteepIntersection == 1 ? saturate(wNormal.y + _Steepness) : 1;
        float intersect1 = smoothstep(_Intersection.y, _Intersection.x, IN.mainTC_tWeightY_hOffset.w) * steepWeights;
        float intersect2 = smoothstep(_Intersection2.y, _Intersection2.x, IN.mainTC_tWeightY_hOffset.w) * (1 - steepWeights);
        float intersection = intersect1 + intersect2;
        

        #ifdef DIFFUSE
            half height = objectAlbedo.a * _MaskMapRemapScale.b + _MaskMapRemapOffset.b;
        #else 	
            half4 objectMask = tex2D(_MainMask, mainUV);
            objectMask.rgba = objectMask.rgba * _MaskMapRemapScale.rgba + _MaskMapRemapOffset.rgba;
            objectAlbedo.a = _HasMask == 1 ? objectMask.a : _Glossiness;
            objectAlbedo.a = _GlobalSmoothnessDisabled ? objectAlbedo.a : lerp(objectAlbedo.a, 1.0f, _InTerra_GlobalSmoothness);
            
            half objectMetallic = _HasMask == 1 ? objectMask.r : _Metallic;
            half objectAo = _HasMask == 1 ? objectMask.g : _Ao;
            half height = objectMask.b;
        #endif 

        half sSum;
        #ifdef _TERRAIN_BLEND_HEIGHT
            #ifdef DIFFUSE
                sSum = lerp(HeightSum(splat, splat_control), 1, intersection);
            #else 
                sSum = lerp(HeightSum(mask, splat_control), 1, intersection);
            #endif
        #else 	
            sSum = 0.5;
        #endif 

        float2 heightIntersect = (1 / (1 * pow(2, float2(((1 - intersection) * height), (intersection * sSum)) * (-(_Sharpness)))) + 1) * 0.5;
        heightIntersect /= (heightIntersect.r + heightIntersect.g);

        #ifdef _OBJECT_DETAIL
            float2 detailUV = TRANSFORM_TEX(IN.mainTC_tWeightY_hOffset.xy, _DetailAlbedoMap);
            #if defined (_OBJECT_PARALLAX) 
                detailUV += mainParallaxOffset * (_DetailAlbedoMap_ST.xy / _MainTex_ST.xy);
            #endif
            #if !defined(_LAYERS_ONE)
                half3 detailTexture = UNITY_SAMPLE_TEX2D_SAMPLER(_DetailAlbedoMap, _MainTex, detailUV);
                fixed3 mainNormD = UnpackNormalWithScale(UNITY_SAMPLE_TEX2D_SAMPLER(_DetailNormalMap, _MainTex, detailUV), _DetailNormalMapScale);
            #else
                half4 detailTexture = tex2D(_DetailAlbedoMap, detailUV);
                fixed3 mainNormD = UnpackNormalWithScale(tex2D(_DetailNormalMap, detailUV), _DetailNormalMapScale);
            #endif              
            objectAlbedo.rgb = lerp(objectAlbedo.rgb, objectAlbedo.rgb * (detailTexture * unity_ColorSpaceDouble.r), _DetailStrenght);
        #endif

        fixed3 mainNorm = fixed3(0,0,1);
        #if !defined(_LAYERS_ONE)
            mainNorm = UnpackNormalWithScale(UNITY_SAMPLE_TEX2D_SAMPLER(_BumpMap, _MainTex, mainUV), _BumpScale);
        #else
            mainNorm = UnpackNormalWithScale(tex2D(_BumpMap, mainUV), _BumpScale);
        #endif          

        #ifdef _OBJECT_DETAIL          
            mainNorm = (lerp(mainNorm, BlendNormals(mainNorm, mainNormD), _DetailStrenght));
        #endif 

        float3 terrainNormal = float3(mixedNormal.z * IN.terrainNormals) + 1e-5f;
        terrainNormal.xy += mixedNormal.xy;

        float intersectNormal = smoothstep(_NormIntersect.y, _NormIntersect.x, IN.mainTC_tWeightY_hOffset.w);
        mixedNormal = lerp(mixedNormal, terrainNormal, intersectNormal);

        mixedDiffuse = lerp(mixedDiffuse, objectAlbedo, heightIntersect.r);

        mixedNormal = lerp(mixedNormal, mainNorm, heightIntersect.r);

        
        #ifndef DIFFUSE
            metallic = lerp(metallic, objectMetallic, heightIntersect.r);
            ao = lerp(ao, objectAo, heightIntersect.r);

            emission = 0;
            UNITY_BRANCH if (_EmissionEnabled > 0)
            {
                float2 emissionUV = TRANSFORM_TEX(IN.mainTC_tWeightY_hOffset.xy, _EmissionMap);
                #if defined (_OBJECT_PARALLAX) 
                    emissionUV += mainParallaxOffset * (_EmissionMap_ST.xy / _MainTex_ST.xy);
                #endif
                #if !defined(_LAYERS_ONE)
                    half4 emissionTexture = UNITY_SAMPLE_TEX2D_SAMPLER(_EmissionMap, _MainTex, emissionUV);
                #else
                    half4 emissionTexture = tex2D(_EmissionMap, emissionUV);
                #endif  
                
                emission = lerp(0, emissionTexture * _EmissionColor, heightIntersect.r);
            }
        #endif           
    #endif
            
    //=========================================================================================
    

    #if defined(INSTANCING_ON) && defined(SHADER_TARGET_SURFACE_ANALYSIS) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
        mixedNormal = float3(0, 0, 1); // make sure that surface shader compiler realizes we write to normal, as UNITY_INSTANCING_ENABLED is not defined for SHADER_TARGET_SURFACE_ANALYSIS.
    #endif

    #if defined(UNITY_INSTANCING_ENABLED) && !defined(SHADER_API_D3D11_9X) && defined(TERRAIN_INSTANCED_PERPIXEL_NORMAL)
        float3 geomNormal = normalize(tex2D(_TerrainNormalmapTexture, IN.tc.zw).xyz * 2 - 1);
        #if defined(_NORMALMAP) || defined(_TERRAIN_NORMAL_IN_MASK)
            float3 geomTangent = normalize(cross(geomNormal, float3(0, 0, 1)));
            float3 geomBitangent = normalize(cross(geomTangent, geomNormal));
            mixedNormal = mixedNormal.x * geomTangent
                          + mixedNormal.y * geomBitangent
                          + mixedNormal.z * geomNormal;
        #else
            mixedNormal = geomNormal;
        #endif
        mixedNormal = mixedNormal.xzy;
    #endif
}

#ifndef TERRAIN_SURFACE_OUTPUT
    #define TERRAIN_SURFACE_OUTPUT SurfaceOutput
#endif

void SplatmapFinalColor(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 color)
{
    color *= o.Alpha;
    #ifdef TERRAIN_SPLAT_ADDPASS
        UNITY_APPLY_FOG_COLOR(IN.fogCoord, color, fixed4(0,0,0,0));
    #else
        UNITY_APPLY_FOG(IN.fogCoord, color);
    #endif
}

void SplatmapFinalPrepass(Input IN, TERRAIN_SURFACE_OUTPUT o, inout fixed4 normalSpec)
{
    normalSpec *= o.Alpha;
}

void SplatmapFinalGBuffer(Input IN, TERRAIN_SURFACE_OUTPUT o, inout half4 outGBuffer0, inout half4 outGBuffer1, inout half4 outGBuffer2, inout half4 emission)
{
    UnityStandardDataApplyWeightToGbuffer(outGBuffer0, outGBuffer1, outGBuffer2, o.Alpha);
    emission *= o.Alpha;
}
