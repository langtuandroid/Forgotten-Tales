// Made with Amplify Shader Editor v1.9.3.3
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Hidden/Baking Barrels"
{
	Properties
    {
		[HideInInspector] _AlphaCutoff("Alpha Cutoff ", Range(0, 1)) = 0.5
		[HideInInspector] _EmissionColor("Emission Color", Color) = (1,1,1,1)
		_BaseColorMap("Base Color Map", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_AmbientOcclusion("Ambient Occlusion", 2D) = "white" {}
		_SpecularSmoothness("Specular Smoothness", 2D) = "white" {}
		_Normal("Normal", 2D) = "bump" {}
		_Cutoff("Cutoff", Float) = 0.5
		[HideInInspector] _texcoord( "", 2D ) = "white" {}

    }

    SubShader
    {
		LOD 0

		

        Tags { "RenderPipeline"="HDRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
		HLSLINCLUDE
		#pragma target 4.5
		ENDHLSL

		
		Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode"="ForwardOnly" }

			Blend One Zero
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref 2
				WriteMask 7
				Comp Always
				Pass Replace
			}


            HLSLPROGRAM
			#define AI_ALPHATEST_ON 1
			#define ASE_SRP_VERSION 140007

			#pragma exclude_renderers glcore gles gles3 xboxseries playstation ps5 
			#pragma multi_compile_instancing

			#pragma vertex Vert
			#pragma fragment Frag

			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION

			#ifndef SHADERCONFIG_CS_HLSL
			#define SHADERCONFIG_CS_HLSL

			#define PROBEVOLUMESEVALUATIONMODES_DISABLED (0)
			#define PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP (1)
			#define PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS (2)

			#define SHADEROPTIONS_CAMERA_RELATIVE_RENDERING 0

			#endif

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"

            #define SHADERPASS SHADERPASS_FORWARD
			#pragma multi_compile USE_FPTL_LIGHTLIST USE_CLUSTERED_LIGHTLIST
			#pragma multi_compile SHADOW_LOW SHADOW_MEDIUM SHADOW_HIGH
			#pragma multi_compile_fragment AREA_SHADOW_MEDIUM AREA_SHADOW_HIGH
			//#define USE_LEGACY_UNITY_MATRIX_VARIABLES

			// newer HDRP versions need legacy matrices to render with command buffers properly???
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesMatrixDefsHDCamera.hlsl"


			float4x4 glstate_matrix_projection;
			float4x4 unity_MatrixV;
			float4x4 unity_MatrixInvV;
			float4x4 unity_MatrixVP;

			#undef UNITY_MATRIX_V
			#define UNITY_MATRIX_V     unity_MatrixV
			#undef UNITY_MATRIX_I_V
			#define UNITY_MATRIX_I_V   unity_MatrixInvV
			#undef UNITY_MATRIX_P
			#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
			#undef UNITY_MATRIX_VP
			#define UNITY_MATRIX_VP    unity_MatrixVP

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"

			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"

			sampler2D _BaseColorMap;
			sampler2D _Normal;
			sampler2D _AmbientOcclusion;
			sampler2D _Mask;
			sampler2D _SpecularSmoothness;
			CBUFFER_START( UnityPerMaterial )
			float4 _BaseColorMap_ST;
			float4 _Normal_ST;
			float4 _AmbientOcclusion_ST;
			float4 _Mask_ST;
			float4 _SpecularSmoothness_ST;
			float _Cutoff;
			CBUFFER_END


			struct GraphVertexInput
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_tangent : TANGENT;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct GraphVertexOutput
			{
				float4 position : POSITION;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			
			GraphVertexOutput Vert( GraphVertexInput v )
			{
				UNITY_SETUP_INSTANCE_ID( v );
				GraphVertexOutput o;
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				float3 ase_worldTangent = TransformObjectToWorldDir(v.ase_tangent.xyz);
				o.ase_texcoord1.xyz = ase_worldTangent;
				float3 ase_worldNormal = TransformObjectToWorldNormal(v.normal.xyz);
				o.ase_texcoord2.xyz = ase_worldNormal;
				float ase_vertexTangentSign = v.ase_tangent.w * ( unity_WorldTransformParams.w >= 0.0 ? 1.0 : -1.0 );
				float3 ase_worldBitangent = cross( ase_worldNormal, ase_worldTangent ) * ase_vertexTangentSign;
				o.ase_texcoord3.xyz = ase_worldBitangent;
				float3 objectToViewPos = TransformWorldToView(TransformObjectToWorld(v.vertex.xyz));
				float eyeDepth = -objectToViewPos.z;
				o.ase_texcoord.z = eyeDepth;
				
				o.ase_texcoord.xy = v.ase_texcoord.xy;
				o.ase_color = v.ase_color;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				o.ase_texcoord.w = 0;
				o.ase_texcoord1.w = 0;
				o.ase_texcoord2.w = 0;
				o.ase_texcoord3.w = 0;
				v.vertex.xyz +=  float3( 0, 0, 0 ) ;
				o.position = TransformObjectToHClip( v.vertex.xyz );
				return o;
			}

			void Frag( GraphVertexOutput IN ,
				out half4 outGBuffer0 : SV_Target0,
				out half4 outGBuffer1 : SV_Target1,
				out half4 outGBuffer2 : SV_Target2,
				out half4 outGBuffer3 : SV_Target3,
				out half4 outGBuffer4 : SV_Target4,
				out half4 outGBuffer5 : SV_Target5,
				out half4 outGBuffer6 : SV_Target6,
				out half4 outGBuffer7 : SV_Target7,
				out float outDepth : SV_Depth
			)
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				float2 uv_BaseColorMap = IN.ase_texcoord.xy * _BaseColorMap_ST.xy + _BaseColorMap_ST.zw;
				float4 tex2DNode179 = tex2D( _BaseColorMap, uv_BaseColorMap );
				float4 appendResult188 = (float4(tex2DNode179.rgb , 1.0));
				
				float2 uv_Normal = IN.ase_texcoord.xy * _Normal_ST.xy + _Normal_ST.zw;
				float3 temp_cast_1 = (0.35).xxx;
				float3 temp_cast_2 = (-0.15).xxx;
				float2 uv_AmbientOcclusion = IN.ase_texcoord.xy * _AmbientOcclusion_ST.xy + _AmbientOcclusion_ST.zw;
				float occlusion146 = tex2D( _AmbientOcclusion, uv_AmbientOcclusion ).r;
				float3 specular152 = (float4(0,0,0,0)).rgb;
				float2 uv_Mask = IN.ase_texcoord.xy * _Mask_ST.xy + _Mask_ST.zw;
				float3 smoothstepResult166 = smoothstep( temp_cast_1 , temp_cast_2 , ( ( 1.0 - ( occlusion146 * saturate( (IN.ase_color.r*0.6 + 0.79) ) ) ) + specular152 + ( 1.0 - tex2D( _Mask, uv_Mask ).r ) ));
				float3 paintMask172 = smoothstepResult166;
				float3 lerpResult182 = lerp( UnpackNormalScale( tex2D( _Normal, uv_Normal ), 1.0f ) , float3(0,0,1) , ( paintMask172 * occlusion146 ));
				float3 ase_worldTangent = IN.ase_texcoord1.xyz;
				float3 ase_worldNormal = IN.ase_texcoord2.xyz;
				float3 ase_worldBitangent = IN.ase_texcoord3.xyz;
				float3 tanToWorld0 = float3( ase_worldTangent.x, ase_worldBitangent.x, ase_worldNormal.x );
				float3 tanToWorld1 = float3( ase_worldTangent.y, ase_worldBitangent.y, ase_worldNormal.y );
				float3 tanToWorld2 = float3( ase_worldTangent.z, ase_worldBitangent.z, ase_worldNormal.z );
				float3 tanNormal8_g4 = lerpResult182;
				float3 worldNormal8_g4 = float3(dot(tanToWorld0,tanNormal8_g4), dot(tanToWorld1,tanNormal8_g4), dot(tanToWorld2,tanNormal8_g4));
				float eyeDepth = IN.ase_texcoord.z;
				float temp_output_4_0_g4 = ( -1.0 / UNITY_MATRIX_P[2].z );
				float temp_output_7_0_g4 = ( ( eyeDepth + temp_output_4_0_g4 ) / temp_output_4_0_g4 );
				float4 appendResult11_g4 = (float4((worldNormal8_g4*0.5 + 0.5) , temp_output_7_0_g4));
				
				float2 uv_SpecularSmoothness = IN.ase_texcoord.xy * _SpecularSmoothness_ST.xy + _SpecularSmoothness_ST.zw;
				float smoothness163 = tex2D( _SpecularSmoothness, uv_SpecularSmoothness ).a;
				float2 appendResult168 = (float2(specular152.x , smoothness163));
				float2 appendResult175 = (float2(0.1 , 0.1));
				float2 lerpResult181 = lerp( appendResult168 , appendResult175 , paintMask172.xy);
				float4 appendResult186 = (float4(lerpResult181 , occlusion146 , paintMask172.x));
				

				outGBuffer0 = appendResult188;
				outGBuffer1 = appendResult11_g4;
				outGBuffer2 = appendResult186;
				outGBuffer3 = 0;
				outGBuffer4 = 0;
				outGBuffer5 = 0;
				outGBuffer6 = 0;
				outGBuffer7 = 0;
				float alpha = ( tex2DNode179.a - _Cutoff );
				#if AI_ALPHATEST_ON
					clip( alpha );
				#endif
				outDepth = IN.position.z;
			}
            ENDHLSL
        }
		

	}
	
	CustomEditor "ASEMaterialInspector"
	Fallback Off
}
/*ASEBEGIN
Version=19303
Node;AmplifyShaderEditor.VertexColorNode;137;-112,-416;Inherit;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;138;-112,-256;Float;False;Constant;_Float2;Float 2;5;0;Create;True;0;0;0;False;0;False;0.6;0.6;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;139;-112,-176;Float;False;Constant;_Float5;Float 5;6;0;Create;True;0;0;0;False;0;False;0.79;0.79;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;140;1075.038,430.8724;Inherit;True;Property;_AmbientOcclusion;Ambient Occlusion;2;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ScaleAndOffsetNode;141;112,-384;Inherit;False;3;0;FLOAT;0;False;1;FLOAT;1;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;146;1396.323,452.1531;Float;False;occlusion;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;201;1000.061,-18.00831;Inherit;False;Constant;_Color0;Color 0;8;0;Create;True;0;0;0;False;0;False;0,0,0,0;0,0,0,0;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.CommentaryNode;145;1356.598,148.5763;Inherit;False;413;136;Fake Specular;2;152;148;;1,1,1,1;0;0
Node;AmplifyShaderEditor.SaturateNode;143;320,-384;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;144;288,-464;Inherit;False;146;occlusion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SwizzleNode;204;1212.937,-8.25379;Inherit;False;FLOAT3;0;1;2;3;1;0;COLOR;0,0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;149;480,-416;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;147;323.5097,-263.2831;Inherit;True;Property;_Mask;Mask;1;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;152;1575.698,195.5763;Float;False;specular;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;154;656,-416;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;153;649,-320;Inherit;False;152;specular;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.OneMinusNode;155;662.1307,-234.8912;Inherit;False;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;159;880,-336;Inherit;False;3;3;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;2;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;156;866.045,-206.9775;Float;False;Constant;_Float4;Float 4;4;0;Create;True;0;0;0;False;0;False;0.35;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;157;862,-136;Float;False;Constant;_Float3;Float 3;5;0;Create;True;0;0;0;False;0;False;-0.15;-0.15;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SmoothstepOpNode;166;1072,-288;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0.01,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;142;1067.451,220.9808;Inherit;True;Property;_SpecularSmoothness;Specular Smoothness;3;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;163;1559.012,312.069;Float;False;smoothness;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode;172;1258.546,-295.1975;Float;False;paintMask;-1;True;1;0;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.GetLocalVarNode;173;1614.378,11.82494;Inherit;False;146;occlusion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;171;1613.214,-65.84857;Inherit;False;172;paintMask;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.RangedFloatNode;167;1911.172,372.0628;Float;False;Constant;_paintSmoothness;paintSmoothness;7;0;Create;True;0;0;0;False;0;False;0.1;0.8;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;160;1923.133,286.3705;Float;False;Constant;_paintSpecular;paintSpecular;6;0;Create;True;0;0;0;False;0;False;0.1;0.1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;164;1918.826,211.2424;Inherit;False;163;smoothness;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode;165;1928.153,138.1094;Inherit;False;152;specular;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;176;1882.888,-64.68533;Inherit;False;2;2;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;180;1550.003,-288.6853;Inherit;True;Property;_Normal;Normal;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector3Node;178;1864.888,-208.6854;Float;False;Constant;_Vector0;Vector 0;8;0;Create;True;0;0;0;False;0;False;0,0,1;0,0,0;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.DynamicAppendNode;175;2156.21,234.7327;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode;168;2157.623,142.9789;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;174;2153.524,345.4126;Inherit;False;172;paintMask;1;0;OBJECT;0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.LerpOp;182;2080,-288;Inherit;False;3;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.SamplerNode;179;2098.127,-539.7466;Inherit;True;Property;_BaseColorMap;Base Color Map;0;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;189;2252.76,-345.62;Float;False;Constant;_Alpha1;Alpha1;5;0;Create;True;0;0;0;False;0;False;1;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;191;2304.634,-100.3721;Float;False;Property;_Cutoff;Cutoff;5;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp;181;2362.322,258.7671;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode;185;2344.418,183.5672;Inherit;False;146;occlusion;1;0;OBJECT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;188;2468.469,-537.683;Inherit;False;FLOAT4;4;0;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleSubtractOpNode;190;2507.044,-119.4029;Inherit;False;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode;193;2462.403,-288;Inherit;False;Pack Normal Depth;-1;;4;8e386dbec347c9f44befea8ff816d188;0;1;12;FLOAT3;0,0,0;False;3;FLOAT4;0;FLOAT3;14;FLOAT;15
Node;AmplifyShaderEditor.DynamicAppendNode;186;2560.099,303.1633;Inherit;False;FLOAT4;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.ScaleNode;148;1390.778,202.6121;Inherit;False;-0.5;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode;192;2874.208,-309.8376;Float;False;True;-1;2;ASEMaterialInspector;0;27;Hidden/Baking Barrels;5b7fbe5f8e132bd40b11a10c99044f79;True;ForwardOnly;0;0;ForwardOnly;10;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;RenderPipeline=HDRenderPipeline;RenderType=Opaque=RenderType;Queue=Geometry=Queue=0;True;5;False;0;False;True;1;1;False;;0;False;;0;1;False;;0;False;;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;True;True;True;True;0;False;;False;False;False;False;False;False;False;True;True;2;False;;255;False;;7;False;;7;False;;3;False;;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;1;False;;True;3;False;;True;True;0;False;;0;False;;True;1;LightMode=ForwardOnly;False;True;6;d3d11;metal;vulkan;xboxone;ps4;switch;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;141;0;137;1
WireConnection;141;1;138;0
WireConnection;141;2;139;0
WireConnection;146;0;140;1
WireConnection;143;0;141;0
WireConnection;204;0;201;0
WireConnection;149;0;144;0
WireConnection;149;1;143;0
WireConnection;152;0;204;0
WireConnection;154;0;149;0
WireConnection;155;0;147;1
WireConnection;159;0;154;0
WireConnection;159;1;153;0
WireConnection;159;2;155;0
WireConnection;166;0;159;0
WireConnection;166;1;156;0
WireConnection;166;2;157;0
WireConnection;163;0;142;4
WireConnection;172;0;166;0
WireConnection;176;0;171;0
WireConnection;176;1;173;0
WireConnection;175;0;160;0
WireConnection;175;1;167;0
WireConnection;168;0;165;0
WireConnection;168;1;164;0
WireConnection;182;0;180;0
WireConnection;182;1;178;0
WireConnection;182;2;176;0
WireConnection;181;0;168;0
WireConnection;181;1;175;0
WireConnection;181;2;174;0
WireConnection;188;0;179;0
WireConnection;188;3;189;0
WireConnection;190;0;179;4
WireConnection;190;1;191;0
WireConnection;193;12;182;0
WireConnection;186;0;181;0
WireConnection;186;2;185;0
WireConnection;186;3;174;0
WireConnection;192;0;188;0
WireConnection;192;1;193;0
WireConnection;192;2;186;0
WireConnection;192;8;190;0
ASEEND*/
//CHKSM=89B6CB92934F526BF22723D4A6C85F7E88BD4114