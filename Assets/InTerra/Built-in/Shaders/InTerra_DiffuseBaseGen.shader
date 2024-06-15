Shader "Hidden/InTerra/InTerra-Diffuse-BaseGen"
{   
    Properties
    {
        [HideInInspector] _DstBlend("DstBlend", Float) = 0.0
        [HideInInspector] _HT_distance_scale("Scale",   Range(0,0.55)) = 0.2
        [HideInInspector] _HT_cover("Cover strenght",   Range(0,1)) = 0.6
    }

    SubShader
    {
        CGINCLUDE

        #include "UnityCG.cginc"

        #pragma multi_compile_local __ _TERRAIN_TRIPLANAR_ONE _TERRAIN_TRIPLANAR
        #pragma multi_compile_local __ _TERRAIN_DISTANCEBLEND  
        #pragma multi_compile_local __ _TERRAIN_TINT_TEXTURE
        #pragma multi_compile_local __ _LAYERS_TWO

        #define _TERRAIN_BLEND_HEIGHT
        #define DIFFUSE
        #define TERRAIN_BASEGEN

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
                #else
                    float2 uv[4];
                    half4 splat[4], mask[4];
                #endif                   
                   
                uv[0] = i.texcoord1;
                uv[1] = i.texcoord2;
                #ifndef _LAYERS_TWO
                    uv[2] = i.texcoord3;
                    uv[3] = i.texcoord4;
                #endif

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
                #endif

                _DiffuseRemapScale0 += _DiffuseRemapOffset0;
                _DiffuseRemapScale1 += _DiffuseRemapOffset1;
                #if !defined(_LAYERS_TWO)
                    _DiffuseRemapScale2 += _DiffuseRemapOffset2;
                    _DiffuseRemapScale3 += _DiffuseRemapOffset3;
                #endif

                SampleSplat(splat, uv, 0, mask);
                #if defined(DIFFUSE) && defined(_TERRAIN_BLEND_HEIGHT) && !defined(ONE_LAYER) && !defined(TERRAIN_SPLAT_ADDPASS)
                    if (_NumLayersCount <= 4 && _HeightmapBlending == 1)
                    {
                        HeightBlend(splat, splat_control, _HeightTransition);
                    }
                #endif

                SplatWeight(mixedDiffuse, splat, splat_control);

                #ifdef _TERRAIN_DISTANCEBLEND 
                    SampleSplat(dSplat, distantUV, 0, dMask);                                            
                    #if defined(DIFFUSE) && defined(_TERRAIN_BLEND_HEIGHT) && !defined(ONE_LAYER) && !defined(TERRAIN_SPLAT_ADDPASS)
                        if (_NumLayersCount <= 4 && _HeightmapBlending == 1)
                        {
                            HeightBlend(dSplat, dSplat_control, _Distance_HeightTransition);
                        }
                    #endif

                    SplatWeight(distantDiffuse, dSplat, dSplat_control);
                    mixedDiffuse = lerp(mixedDiffuse, distantDiffuse, _HT_cover);
                #endif

                #if defined(_TERRAIN_TINT_TEXTURE) && !defined(_TERRAIN_TRIPLANAR_ONE) 
                    half3 tint = tex2D(_TerrainColorTintTexture, i.texcoord5 * _TerrainColorTintTexture_ST.xy + _TerrainColorTintTexture_ST.zw);
                    mixedDiffuse.rgb = lerp(mixedDiffuse.rgb, ((mixedDiffuse.rgb) * (tint)), _TerrainColorTintStrenght).rgb;
                #endif 

                mixedDiffuse.a = splat_control.r;
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

                #ifdef _TERRAIN_DISTANCEBLEND
                    float4 dSplat0 = tex2D(_Splat0, uv * (_DiffuseRemapOffset0.r + 1) * _HT_distance_scale);
                    splat0 = lerp(splat0, dSplat0, _HT_cover);
                #endif

                return splat0;
            }
            ENDCG
        }          
    }
    Fallback Off
}
