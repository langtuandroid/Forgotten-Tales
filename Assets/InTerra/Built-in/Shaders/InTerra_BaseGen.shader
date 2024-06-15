Shader "Hidden/InTerra/InTerra-BaseGen"
{    
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
        [HideInInspector] _HT_distance_scale("Scale",   Range(0,0.55)) = 0.2
        [HideInInspector] _HT_cover("Cover strenght",   Range(0,1)) = 0.6
        [HideInInspector] _HeightmapBlending("", Float) = 1
    }

    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        #pragma multi_compile_local __ _TERRAIN_MASK_MAPS _TERRAIN_NORMAL_IN_MASK
        #pragma multi_compile_local __ _TERRAIN_TRIPLANAR_ONE _TERRAIN_TRIPLANAR
        #pragma multi_compile_local __ _TERRAIN_DISTANCEBLEND           
        #pragma multi_compile_local __ _LAYERS_TWO

        #define TERRAIN_BASEGEN
        #define _TERRAIN_BLEND_HEIGHT
        #include "InTerra_InputsAndFunctions.cginc"

        struct appdata_t
        {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
        };

        float2 ComputeControlUV(float2 uv)
        {
            // adjust splatUVs so the edges of the terrain tile lie on pixel centers
            return (uv * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
        }

        ENDCG

        Pass
        {
            Tags
            {
                "Name" = "_MainTex"
                "Format" = "RGBA32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One[_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                #ifndef _LAYERS_TWO
                    float2 texcoord3 : TEXCOORD3;
                    float2 texcoord4 : TEXCOORD4;
                #endif
                float2 texcoord5 : TEXCOORD5;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = ComputeControlUV(uv);
                o.texcoord1 = TRANSFORM_TEX(uv, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(uv, _Splat1);
                #ifndef _LAYERS_TWO
                    o.texcoord3 = TRANSFORM_TEX(uv, _Splat2);
                    o.texcoord4 = TRANSFORM_TEX(uv, _Splat3);
                #endif
                o.texcoord5 = uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {

                float4 splat_control = tex2D(_Control, i.texcoord0);

                half4 mixedDiffuse = 1;

                #ifdef _LAYERS_TWO
                    float2 uv[2];
                    half4 splat[2], mask[2];
                    splat_control.g = (1 - splat_control.r);
                    half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, 0, 0);
                #else
                    float2 uv[4];
                    half4 splat[4], mask[4];
                    half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
                #endif

                _DiffuseRemapScale0 += _DiffuseRemapOffset0;
                _DiffuseRemapScale1 += _DiffuseRemapOffset1;
                #if !defined(_LAYERS_TWO)
                    _DiffuseRemapScale2 += _DiffuseRemapOffset2;
                    _DiffuseRemapScale3 += _DiffuseRemapOffset3;
                #endif
                   
                uv[0] = i.texcoord1;
                uv[1] = i.texcoord2;
                #ifndef _LAYERS_TWO
                    uv[2] = i.texcoord3;
                    uv[3] = i.texcoord4;
                #endif
                SampleMask(mask, uv);

                #ifdef _TERRAIN_DISTANCEBLEND
                    float4 dSplat_control = splat_control;
                    float4 distantDiffuse;
                        #ifdef _LAYERS_TWO
                        float2 distantUV[2];
                        half4 dSplat[2], dMask[2];
                    #else
                        float2 distantUV[4];
                        half4 dSplat[4], dMask[4];
                    #endif

                    DistantUV(distantUV, uv);
                    SampleMask(dMask, distantUV);                                                     
                #endif

                //---------- HEIGHT MAP SPLAT BLENDINGS ---------
                #if defined(_TERRAIN_BLEND_HEIGHT)
                    if (_NumLayersCount <= 4 && _HeightmapBlending == 1)
                    {
                        HeightBlend(mask, splat_control, _HeightTransition);
                        #ifdef _TERRAIN_DISTANCEBLEND
                            HeightBlend(dMask, dSplat_control, _Distance_HeightTransition);
                        #endif
                    }
                #endif
                //-------------------------------------------------

                SampleSplat(splat, uv, defaultSmoothness, mask);
                SplatWeight(mixedDiffuse, splat, splat_control);
                   
                #ifdef _TERRAIN_DISTANCEBLEND 
                    SampleSplat(dSplat, distantUV, defaultSmoothness, mask);
                    SplatWeight(distantDiffuse, dSplat, dSplat_control);
                    mixedDiffuse = lerp(mixedDiffuse, distantDiffuse, _HT_cover);
                #endif 
                #ifndef _TERRAIN_TRIPLANAR_ONE
                    half3 tint = tex2D(_TerrainColorTintTexture, i.texcoord5 * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw);
                    mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, ((mixedDiffuse.rgb) * (tint)), _TerrainColorTintStrenght).rgb;
                #endif
                return  mixedDiffuse; 
            }
            ENDCG
        }

        Pass
        {
            // _NormalMap pass will get ignored by terrain basemap generation code. Put here so that the VTC can use it to generate cache for normal maps.
            Tags
            {
                "Name" = "_NormalMap"
                "Format" = "A2R10G10B10"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One[_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            sampler2D _Normal0, _Normal1, _Normal2, _Normal3;

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                #ifndef _LAYERS_TWO
                    float2 texcoord3 : TEXCOORD3;
                    float2 texcoord4 : TEXCOORD4;
                #endif
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = ComputeControlUV(uv);
                o.texcoord1 = TRANSFORM_TEX(uv, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(uv, _Splat1);
                #ifndef _LAYERS_TWO
                    o.texcoord3 = TRANSFORM_TEX(uv, _Splat2);
                    o.texcoord4 = TRANSFORM_TEX(uv, _Splat3);
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 alpha = tex2D(_Control, i.texcoord0);

                float3 normal;
                normal = UnpackNormalWithScale(tex2D(_Normal0, i.texcoord1), _NormalScale0) * alpha.x;
                normal += UnpackNormalWithScale(tex2D(_Normal1, i.texcoord2), _NormalScale1) * alpha.y;
                #ifndef _LAYERS_TWO
                    normal += UnpackNormalWithScale(tex2D(_Normal2, i.texcoord3), _NormalScale2) * alpha.z;
                    normal += UnpackNormalWithScale(tex2D(_Normal3, i.texcoord4), _NormalScale3) * alpha.w;
                #endif
                return float4(normal.xyz * 0.5f + 0.5f, 1.0f);
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "Name" = "_MetallicTex"
                "Format" = "RGBA32"
                "Size" = "1/4"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One[_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                #ifndef _LAYERS_TWO
                    float2 texcoord3 : TEXCOORD3;
                    float2 texcoord4 : TEXCOORD4;
                #endif
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = ComputeControlUV(TRANSFORM_TEX(v.texcoord, _Control));
                o.texcoord1 = TRANSFORM_TEX(uv, _Splat0);
                o.texcoord2 = TRANSFORM_TEX(uv, _Splat1);
                #ifndef _LAYERS_TWO
                    o.texcoord3 = TRANSFORM_TEX(uv, _Splat2);
                    o.texcoord4 = TRANSFORM_TEX(uv, _Splat3);
                #endif
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 splat_control = tex2D(_Control, i.texcoord0);

                half metallic = 0;
                half ao = 1;
                half4 mixedDiffuse = 1;
                #ifdef _LAYERS_TWO
                    float2 uv[2];
                    half4 mask[2];
                    splat_control.g = (1 - splat_control.r);
                    half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, 0, 0);
                #else
                    float2 uv[4];
                    half4 mask[4];
                    half4 defaultSmoothness = half4(_Smoothness0, _Smoothness1, _Smoothness2, _Smoothness3);
                #endif                   

                uv[0] = i.texcoord1;
                uv[1] = i.texcoord2;
                #ifndef _LAYERS_TWO
                    uv[2] = i.texcoord3;
                    uv[3] = i.texcoord4;
                #endif

                SampleMask(mask, uv); 

                #ifdef _TERRAIN_DISTANCEBLEND
                    float4 dSplat_control = splat_control;
                    #ifdef _LAYERS_TWO
                        float2 distantUV[2];
                        half4 dMask[2];
                    #else
                        float2 distantUV[4];
                        half4 dMask[4];
                    #endif

                    DistantUV(distantUV, uv);
                    SampleMask(dMask, distantUV);                                                     
                #endif

                            
                //---------- HEIGHT MAP SPLAT BLENDINGS ---------
                #if defined(_TERRAIN_BLEND_HEIGHT)
                    if (_NumLayersCount <= 4 && _HeightmapBlending == 1)
                    {
                        HeightBlend(mask, splat_control, _HeightTransition);
                        #ifdef _TERRAIN_DISTANCEBLEND
                            HeightBlend(dMask, dSplat_control, _Distance_HeightTransition);
                        #endif
                    }
                #endif
                        
                //---------------- METALLIC -------------------
                #if defined(_TERRAIN_MASK_MAPS) && !defined(_TERRAIN_NORMAL_IN_MASK)
                    metallic = MetallicMask(mask, splat_control);
                    #if defined (_TERRAIN_DISTANCEBLEND)
                        half dMetallic = MetallicMask(dMask, dSplat_control);
                        metallic = lerp(metallic, dMetallic, _HT_cover);
                    #endif
                #else
                    metallic = Metallic(splat_control);
                    #if defined (_TERRAIN_DISTANCEBLEND)
                        half dMetallic = Metallic(dSplat_control);
                        metallic = lerp(metallic, dMetallic, _HT_cover);
                    #endif
                #endif  

                //---------------- AO -------------------
                #if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK)
                    ao = AmbientOcclusion(mask, splat_control); 
                    #if defined (_TERRAIN_DISTANCEBLEND)
                        half dAo = AmbientOcclusion(dMask, dSplat_control);
                        ao = lerp(ao, dAo, _HT_cover);
                    #endif
                #endif                    

                return float4(metallic, ao, splat_control.r, 0);
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "Name" = "_TriplanarTex"
                "Format" = "RGBA32"
                "Size" = "1"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One[_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
                
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = TRANSFORM_TEX(uv, _Splat0);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord0;                                
                                
                float4 splat0 = tex2D(_Splat0, uv);
                float4 mask0 = tex2D(_Mask0, uv);

                #ifdef _TERRAIN_DISTANCEBLEND
                    float4 dSplat0 = tex2D(_Splat0, uv * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale);
                    splat0 = lerp(splat0, dSplat0, _HT_cover);

                    float4 dMask0 = tex2D(_Mask0, uv * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale);
                    mask0 = lerp(mask0, dMask0, _HT_cover); 
                #endif

                #if defined(_TERRAIN_MASK_MAPS) && !defined(_TERRAIN_NORMAL_IN_MASK)
                    #ifdef _TERRAIN_NORMAL_IN_MASK
                        mask0.rb = mask0.rb * _MaskMapRemapScale0.gb + _MaskMapRemapOffset0.gb;
                    #else
                        mask0.rgba = mask0.rgba * _MaskMapRemapScale0.rgba + _MaskMapRemapOffset0.rgba;
                    #endif
                        splat0.a = mask0.a;
                #else
                    splat0.a = splat0.a * _Smoothness0;
                #endif

                return splat0;
            }
            ENDCG
        }

        Pass
        {
            Tags
            {
                "Name" = "_Triplanar_MetallicAO"
                "Format" = "RGBA32"
                "Size" = "1/4"
            }

            ZTest Always Cull Off ZWrite Off
            Blend One[_DstBlend]
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord0 : TEXCOORD0;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float2 uv = TRANSFORM_TEX(v.texcoord, _Control);
                o.texcoord0 = TRANSFORM_TEX(uv, _Splat0);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord0;                                                               
                float4 mask0 = tex2D(_Mask0, uv);
                half ao = 1;
                half metallic = _Metallic0;

                #if defined(_TERRAIN_MASK_MAPS) || defined(_TERRAIN_NORMAL_IN_MASK)
                    #ifdef _TERRAIN_DISTANCEBLEND
                        float4 dMask0 = tex2D(_Mask0, uv * _HT_distance_scale);
                        mask0 = lerp(mask0, dMask0, _HT_cover);
                    #endif                            
                    #ifdef _TERRAIN_NORMAL_IN_MASK
                        ao = mask0.r * _MaskMapRemapScale0.g + _MaskMapRemapOffset0.g;
                    #else
                        metallic = mask0.r * _MaskMapRemapScale0.r + _MaskMapRemapOffset0.r;
                        ao = mask0.g * _MaskMapRemapScale0.g + _MaskMapRemapOffset0.g;                                   
                    #endif
                #endif
                              
                return float4(metallic, ao, 0, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
