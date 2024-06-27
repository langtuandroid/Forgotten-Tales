//========================================================
//--------------|         INTERRA         |---------------
//========================================================
//--------------|          3.9.2         |---------------
//========================================================
//--------------| ©  INEFFABILIS ARCANUM  |---------------
//========================================================

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
	using UnityEditor;
	using UnityEngine.Rendering;
#endif

#if USING_HDRP
	using UnityEngine.Rendering.HighDefinition;
#endif
#if USING_URP
	using UnityEngine.Rendering.Universal;
#endif

namespace InTerra
{
	public static class InTerra_Data
	{
		public const string ObjectShaderName = "InTerra/Object into Terrain Integration";
		public const string DiffuseObjectShaderName = "InTerra/Diffuse/Object into Terrain Integration (Diffuse)";
		public const string URPObjectShaderName = "InTerra/URP/Object into Terrain Integration";
		public const string HDRPObjectShaderName = "InTerra/HDRP/Object into Terrain Integration";
		public const string HDRPObjectTessellationShaderName = "InTerra/HDRP Tessellation/Object into Terrain Integration Tessellation";

		public const string TerrainShaderName = "InTerra/Terrain (Standard With Features)";
		public const string DiffuseTerrainShaderName = "InTerra/Diffuse/Terrain (Diffuse With Features)";
		public const string URPTerrainShaderName = "InTerra/URP/Terrain (Lit with Features)";
		public const string HDRPTerrainShaderName = "InTerra/HDRP/Terrain (Lit with Features)";
		public const string HDRPTerrainTessellationShaderName = "InTerra/HDRP Tessellation/Terrain (Lit with Features)";

		public const string TessellationShaderFolder = "InTerra/HDRP Tessellation";

		const string UpdaterName = "InTerra_UpdateAndCheck";
		static public GameObject updater;
		static InTerra_UpdateAndCheck updateScript;

		static bool tracksEnabled; 	
		static Camera trackCamera;
		static Vector3 TrackCameraForwardVec;
		static Vector3 trackCameraPositon;

		static bool shaderVariantWarning;

		public static void UpdateTerrainData(bool UpdateDictionary)
		{
			Terrain[] terrains = Terrain.activeTerrains;
			if (terrains.Length > 0)
			{
				DictionaryMaterialTerrain materialTerrain = GetUpdaterScript().MaterialTerrain;

				if (UpdateDictionary)
				{
					//======= DICTIONARY OF MATERIALS WITH INTERRA SHADERS AND SUM POSITIONS OF RENDERERS WITH THAT MATERIAL =========
					Dictionary<Material, Vector3> matPos = new Dictionary<Material, Vector3>();
					#if UNITY_2023_1_OR_NEWER
						MeshRenderer[] renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
					#else
						MeshRenderer[] renderers = Object.FindObjectsOfType<MeshRenderer>();
					#endif
					foreach (MeshRenderer rend in renderers)
					{
						if (rend != null && rend.bounds != null)
						{
							foreach (Material mat in rend.sharedMaterials)
							{
								if (CheckObjectShader(mat))
								{
									if (!matPos.ContainsKey(mat))
									{
										matPos.Add(mat, new Vector3(rend.bounds.center.x, rend.bounds.center.z, 1));
									}
									else
									{
										Vector3 sumPos = matPos[mat];
										sumPos.x += rend.bounds.center.x;
										sumPos.y += rend.bounds.center.z;
										sumPos.z += 1;
										matPos[mat] = sumPos;
									}
								}
							}
						}
					}

					//===================== DICTIONARY OF MATERIALS AND TERRAINS WHERE ARE PLACED =========================

					//---- Temporal Dicitonary Copy -----
					Dictionary<Material, Terrain> tempMatTerDict = new();
					foreach (Material mt in materialTerrain.Keys)
					{
						if (!tempMatTerDict.ContainsKey(mt))
						{
							tempMatTerDict.Add(mt, materialTerrain[mt]);
						}
					}
					//---------------------------------
					materialTerrain.Clear();

					foreach (Material mat in matPos.Keys)
					{
						Vector2 averagePos = matPos[mat] / matPos[mat].z;
						foreach (Terrain terrain in terrains)
						{
							if (!materialTerrain.ContainsKey(mat))
							{ 
								if (mat.GetFloat("_CustomTerrainSelection") > 0 && tempMatTerDict.ContainsKey(mat))
								{
									materialTerrain.Add(mat, tempMatTerDict[mat]);
								}
								else
								{
									if (CheckPosition(terrain, averagePos))
									{
										materialTerrain.Add(mat, terrain);
									}
								}
							}
							if (CheckTerrainShaderContains(terrain, "InTerra/HDRP"))
							{
								terrain.materialTemplate.renderQueue = 2225;
							}
							if (CheckTerrainShaderContains(terrain, "InTerra/URP"))
							{
								#if UNITY_2022_2_OR_NEWER
									terrain.materialTemplate.EnableKeyword("UNITY_2022_2_OR_NEWER");
								#else
									terrain.materialTemplate.DisableKeyword("UNITY_2022_2_OR_NEWER");
								#endif
							}
						}

						if (!materialTerrain.ContainsKey(mat))
						{
							materialTerrain.Add(mat, null);
						}
					}	
				}

				//================================================================================
				//--------------------|    SET TERRAINS DATA TO MATERIALS    |--------------------
				//================================================================================
				foreach (Material mat in materialTerrain.Keys)
				{
					Terrain terrain = materialTerrain[mat];
					if (terrain != null && terrain.materialTemplate != null && CheckObjectShader(mat))
					{		
						mat.SetVector("_TerrainSize", terrain.terrainData.size);
						mat.SetVector("_TerrainPosition", terrain.transform.position);
						mat.SetVector("_TerrainHeightmapScale", new Vector4(terrain.terrainData.heightmapScale.x, terrain.terrainData.heightmapScale.y / (32766.0f / 65535.0f), terrain.terrainData.heightmapScale.z, terrain.terrainData.heightmapScale.y));
						mat.SetTexture("_TerrainHeightmapTexture", terrain.terrainData.heightmapTexture);

						//-------------------|  InTerra Keywords  |------------------
						string[] keywords = new string[]
						{   "_TERRAIN_MASK_MAPS",
							"_TRACKS",
							"_TERRAIN_DISTANCEBLEND",
							"_TERRAIN_NORMAL_IN_MASK",
							"_TERRAIN_PARALLAX",
							"_TERRAIN_TINT_TEXTURE"
						};

						if (CheckTerrainShaderContains(terrain, "InTerra/HDRP"))
						{
							if (terrain.terrainData.alphamapTextureCount > 1 && !(mat.IsKeywordEnabled("_LAYERS_ONE") && mat.IsKeywordEnabled("_LAYERS_TWO"))) mat.EnableKeyword("_LAYERS_EIGHT"); else mat.DisableKeyword("_LAYERS_EIGHT");
							terrain.materialTemplate.SetFloat("_HeightmapBlending", terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_BLEND_HEIGHT") ? 1.0f : 0.0f);
						}

						if (CheckTerrainShader(terrain.materialTemplate.shader))
						{
							TerrainKeywordsToMaterial(terrain, mat, keywords);

							//------------------|  InTerra Properties  |------------------
							string[] floatProperties = new string[]
							{   "_HT_distance_scale",
								"_HT_cover",
								"_HeightTransition",
								"_Distance_HeightTransition",
								"_TriplanarOneToAllSteep",
								"_TriplanarSharpness",
								"_TerrainColorTintStrenght",
								"_TerrainNormalTintStrenght",
								"_HeightmapBlending",	
								"_TrackAO",
								"_TrackDetailNormalStrenght",
								"_TrackNormalStrenght",
								"_TrackHeightOffset",
								"_TrackHeightTransition",
								"_ParallaxTrackAffineSteps",
								"_ParallaxTrackSteps",
								"_TrackEdgeNormals",
								"_TrackEdgeSharpness",
								"_Gamma"						
							};
							SetTerrainFloatsToMaterial(terrain, mat, floatProperties);

							string[] textureProperties = new string[]
							{   "_TerrainColorTintTexture",
								"_TerrainNormalTintTexture",
								"_TrackDetailTexture",
								"_TrackDetailNormalTexture"
							};
							SetTerrainTextureToMaterial(terrain, mat, textureProperties);

							SetTerrainVectorToMaterial(terrain, mat, "_HT_distance");
							SetTerrainVectorToMaterial(terrain, mat, "_TerrainNormalTintDistance");


							if ((mat.IsKeywordEnabled("_TERRAIN_PARALLAX") || terrain.materialTemplate.shader.name.Contains(TessellationShaderFolder)) && terrain.materialTemplate.shader.name != DiffuseTerrainShaderName)
							{
								mat.SetFloat("_MipMapLevel", terrain.materialTemplate.GetFloat("_MipMapLevel"));
								SetTerrainVectorToMaterial(terrain, mat, "_MipMapFade");
							}

							if (mat.shader.name == HDRPObjectTessellationShaderName && terrain.materialTemplate.shader.name.Contains(TessellationShaderFolder))
							{
								float terrainMaxDisplacement = terrain.materialTemplate.GetFloat("_TessellationMaxDisplacement");
								float objectMaxDisplacement = mat.GetFloat("_TessellationObjMaxDisplacement");

								mat.SetFloat("_TessellationMaxDisplacement", terrainMaxDisplacement > objectMaxDisplacement ? terrainMaxDisplacement : objectMaxDisplacement);

								string[] tessProperties = new string[]
								{   "_TessellationFactorMinDistance",
									"_TessellationFactorMaxDistance",
									"_Tessellation_HeightTransition",
									"_TessellationShadowQuality",
									"_TrackTessallationHeightTransition",
									"_TrackTessallationHeightOffset"
								};
								SetTerrainFloatsToMaterial(terrain, mat, tessProperties);
							}
						}
						else
						{
							#if (USING_HDRP || USING_URP)
								DisableKeywords(mat, keywords);
								mat.EnableKeyword("_TERRAIN_MASK_MAPS");
								if (terrain.materialTemplate.IsKeywordEnabled("_TERRAIN_BLEND_HEIGHT")) mat.SetFloat("_HeightmapBlending", 1); else mat.SetFloat("_HeightmapBlending", 0);
								mat.SetFloat("_HeightTransition", 60 - 60 * terrain.materialTemplate.GetFloat("_HeightTransition"));
							#else
								DisableKeywords(mat, keywords);							
							#endif
						}

						bool hasNormalMap = false;

						//----------- ONE PASS ------------
						if (!mat.IsKeywordEnabled("_LAYERS_TWO") && !mat.IsKeywordEnabled("_LAYERS_ONE") && !mat.IsKeywordEnabled("_LAYERS_EIGHT"))
						{
							int passNumber = (int)mat.GetFloat("_PassNumber");

							for (int i = 0; (i + (passNumber * 4)) < terrain.terrainData.alphamapLayers && i < 4; i++)
							{
								TerrainLaeyrDataToMaterial(terrain.terrainData.terrainLayers[i + (passNumber * 4)], i, mat);
								hasNormalMap = terrain.terrainData.terrainLayers[i + (passNumber * 4)].normalMapTexture || hasNormalMap;
							}

							if (terrain.terrainData.alphamapTextureCount > passNumber) mat.SetTexture("_Control", terrain.terrainData.alphamapTextures[passNumber]);
							if (passNumber > 0) mat.SetFloat("_HeightmapBlending", 0);
						}

						//----------- ONE PASS (EIGHT LAYERS) ------------
						if (mat.IsKeywordEnabled("_LAYERS_EIGHT"))
						{
							int passNumber = (int)mat.GetFloat("_PassNumber");

							for (int i = 0; (i + (passNumber * 4)) < terrain.terrainData.alphamapLayers && i < 8; i++)
							{
								TerrainLaeyrDataToMaterial(terrain.terrainData.terrainLayers[i + (passNumber * 4)], i, mat);
								hasNormalMap = terrain.terrainData.terrainLayers[i + (passNumber * 4)].normalMapTexture || hasNormalMap;
							}

							if (terrain.terrainData.alphamapTextureCount > passNumber) mat.SetTexture("_Control", terrain.terrainData.alphamapTextures[0]);
							if (terrain.terrainData.alphamapTextureCount > passNumber) mat.SetTexture("_Control1", terrain.terrainData.alphamapTextures[1]);
							if (passNumber > 0) mat.SetFloat("_HeightmapBlending", 0);
						}

						//----------- ONE LAYER ------------
						if (mat.IsKeywordEnabled("_LAYERS_ONE"))
						{
							#if UNITY_EDITOR //The TerrainLayers in Editor are referenced by GUID, in Build by TerrainLayers array index
								TerrainLayer terainLayer = TerrainLayerFromGUID(mat, "TerrainLayerGUID_1");
								TerrainLaeyrDataToMaterial(terainLayer, 0, mat);
								hasNormalMap = terainLayer && terainLayer.normalMapTexture;
							#else
								int layerIndex1 = (int)mat.GetFloat("_LayerIndex1");
								CheckLayerIndex(terrain, 0, mat, ref layerIndex1);
								TerrainLaeyrDataToMaterial(terrain.terrainData.terrainLayers[layerIndex1], 0, mat);	
								hasNormalMap = terrain.terrainData.terrainLayers[layerIndex1].normalMapTexture;
							#endif
						}

						//----------- TWO LAYERS ------------
						if (mat.IsKeywordEnabled("_LAYERS_TWO"))
						{
							#if UNITY_EDITOR
								TerrainLayer terainLayer1 = TerrainLayerFromGUID(mat, "TerrainLayerGUID_1");
								TerrainLayer terainLayer2 = TerrainLayerFromGUID(mat, "TerrainLayerGUID_2");
								TerrainLaeyrDataToMaterial(terainLayer1, 0, mat);
								TerrainLaeyrDataToMaterial(terainLayer2, 1, mat);
								int layerIndex1 = terrain.terrainData.terrainLayers.ToList().IndexOf(terainLayer1);
								int layerIndex2 = terrain.terrainData.terrainLayers.ToList().IndexOf(terainLayer2);
								hasNormalMap = terainLayer1 && terainLayer2 && (terainLayer1.normalMapTexture || terainLayer2.normalMapTexture);
							#else
								int layerIndex1 = (int)mat.GetFloat("_LayerIndex1"); 
								int layerIndex2 = (int)mat.GetFloat("_LayerIndex2");
								CheckLayerIndex(terrain, 0, mat, ref layerIndex1);
								CheckLayerIndex(terrain, 1, mat, ref layerIndex2);
								TerrainLaeyrDataToMaterial(terrain.terrainData.terrainLayers[layerIndex1], 0, mat);
								TerrainLaeyrDataToMaterial(terrain.terrainData.terrainLayers[layerIndex2], 1, mat);	
								hasNormalMap = terrain.terrainData.terrainLayers[layerIndex1].normalMapTexture || terrain.terrainData.terrainLayers[layerIndex2].normalMapTexture;
							#endif

							mat.SetFloat("_ControlNumber", layerIndex1 % 4);

							if (terrain.terrainData.alphamapTextureCount > layerIndex1 / 4) mat.SetTexture("_Control", terrain.terrainData.alphamapTextures[layerIndex1 / 4]);
							if (layerIndex1 > 3 || layerIndex2 > 3) mat.SetFloat("_HeightmapBlending", 0);
						}

						if ((mat.shader.name != DiffuseObjectShaderName) && mat.GetFloat("_DisableTerrainParallax") == 1)
						{
							mat.DisableKeyword("_TERRAIN_PARALLAX");
						}

						if (mat.GetFloat("_DisableDistanceBlending") == 1)
						{
							mat.DisableKeyword("_TERRAIN_DISTANCEBLEND");
						}

						if (hasNormalMap) { mat.EnableKeyword("_NORMALMAP"); } else { mat.DisableKeyword("_NORMALMAP"); }

						if (mat.shader.name == DiffuseObjectShaderName)
						{
							if (mat.GetTexture("_BumpMap")) { mat.EnableKeyword("_OBJECT_NORMALMAP"); } else { mat.DisableKeyword("_OBJECT_NORMALMAP"); }
						}
					}
				}
			}
			TerrainMaterialUpdate();
		}

		//============================================================================
		//-------------------------|		FUNCTIONS		|-------------------------
		//============================================================================
		public static bool CheckPosition(Terrain terrain, Vector2 position)
		{
			return terrain != null && terrain.terrainData != null
			&& terrain.GetPosition().x <= position.x && (terrain.GetPosition().x + terrain.terrainData.size.x) > position.x
			&& terrain.GetPosition().z <= position.y && (terrain.GetPosition().z + terrain.terrainData.size.z) > position.y;
		}

		public static bool CheckObjectShader(Material mat)
		{
			return mat && mat.shader && mat.shader.name != null
			&& (mat.shader.name == ObjectShaderName
			 || mat.shader.name == DiffuseObjectShaderName
			 || mat.shader.name == URPObjectShaderName
			 || mat.shader.name == HDRPObjectShaderName
			 || mat.shader.name == HDRPObjectTessellationShaderName);
		}

		public static bool CheckTerrainShader(Shader shader)
		{
			return shader.name == TerrainShaderName
				|| shader.name == DiffuseTerrainShaderName
				|| shader.name == URPTerrainShaderName
				|| shader.name.Contains(HDRPTerrainShaderName)
				|| shader.name.Contains(HDRPTerrainTessellationShaderName);
		}

		public static bool CheckTerrainShaderContains(Terrain terrain, string name)
		{
			return terrain
				&& terrain.materialTemplate
				&& terrain.materialTemplate.shader
				&& terrain.materialTemplate.shader.name != null
				&& terrain.materialTemplate.shader.name.Contains(name);
		}

	#if UNITY_EDITOR
		public static TerrainLayer TerrainLayerFromGUID(Material mat, string tag)
			{
				return (TerrainLayer)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(mat.GetTag(tag, false)), typeof(TerrainLayer));
			}
			public static TerrainData TerrainDataFromGUID(Material mat, string tag)
			{
				return (TerrainData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(mat.GetTag(tag, false)), typeof(TerrainData));
			}
		#endif

		public static void TerrainLaeyrDataToMaterial(TerrainLayer tl, int n, Material mat)
		{
			bool diffuse = mat.shader.name == DiffuseObjectShaderName;

			if (!diffuse)
			{
			#if UNITY_EDITOR
				if (tl)
				{
					TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tl.diffuseTexture)) as TextureImporter;
					if (importer && importer.DoesSourceTextureHaveAlpha())
					{
						tl.smoothness = 1;
					}
				}
			#endif
				if (n < 4)
				{
					Vector4 smoothness = mat.GetVector("_TerrainSmoothness"); smoothness[n] = tl ? tl.smoothness : 0;
					Vector4 metallic = mat.GetVector("_TerrainMetallic"); metallic[n] = tl ? tl.metallic : 0;
					Vector4 normScale = mat.GetVector("_TerrainNormalScale"); normScale[n] = tl ? tl.normalScale : 1;
					mat.SetVector("_TerrainNormalScale", normScale);
					mat.SetVector("_TerrainSmoothness", smoothness);
					mat.SetVector("_TerrainMetallic", metallic);

				}
				else
				{
					Vector4 smoothness1 = mat.GetVector("_TerrainSmoothness1"); smoothness1[n - 4] = tl ? tl.smoothness : 0;
					Vector4 metallic1 = mat.GetVector("_TerrainMetallic1"); metallic1[n - 4] = tl ? tl.metallic : 0;
					Vector4 normScale1 = mat.GetVector("_TerrainNormalScale1"); normScale1[n - 4] = tl ? tl.normalScale : 1;
					mat.SetVector("_TerrainNormalScale1", normScale1);
					mat.SetVector("_TerrainSmoothness1", smoothness1);
					mat.SetVector("_TerrainMetallic1", metallic1);
				}
			}

			mat.SetTexture("_Splat" + n.ToString(), tl ? tl.diffuseTexture : null);
			mat.SetTexture("_Normal" + n.ToString(), tl ? tl.normalMapTexture : null);

			mat.SetTexture("_Mask" + n.ToString(), tl ? tl.maskMapTexture : null);
			mat.SetVector("_SplatUV" + n.ToString(), tl ? new Vector4(tl.tileSize.x, tl.tileSize.y, tl.tileOffset.x, tl.tileOffset.y) : new Vector4(1, 1, 0, 0));
			mat.SetVector("_MaskMapRemapScale" + n.ToString(), tl ? tl.maskMapRemapMax - tl.maskMapRemapMin : new Vector4(1, 1, 1, 1));
			mat.SetVector("_MaskMapRemapOffset" + n.ToString(), tl ? tl.maskMapRemapMin : new Vector4(0, 0, 0, 0));
			mat.SetVector("_DiffuseRemapScale" + n.ToString(), tl ? tl.diffuseRemapMax : new Vector4(1, 1, 1, 1));
			mat.SetVector("_DiffuseRemapOffset" + n.ToString(), tl ? tl.diffuseRemapMin : new Vector4(0, 0, 0, 0));
			mat.SetColor("_Specular" + n.ToString(), tl ? tl.specular : new Color(0, 0, 0, 0));

			if (mat.HasProperty("_LayerHasMask"))
			{
				mat.SetFloat("_LayerHasMask" + n.ToString(), tl ? (float)(tl.maskMapTexture ? 1.0 : 0.0) : (float)0.0);
			}
		}

		public static void CheckLayerIndex(Terrain terrain, int n, Material mat, ref int layerIndex)
		{
			bool diffuse = mat.shader.name == DiffuseObjectShaderName;
			foreach (TerrainLayer tl in terrain.terrainData.terrainLayers)
			{
				bool equal = tl && mat.GetTexture("_Splat" + n.ToString()) == tl.diffuseTexture
				&& mat.GetTexture("_Normal" + n.ToString()) == tl.normalMapTexture
				&& mat.GetVector("_TerrainNormalScale")[n] == tl.normalScale
				&& mat.GetTexture("_Mask" + n.ToString()) == tl.maskMapTexture
				&& mat.GetVector("_SplatUV" + n.ToString()) == new Vector4(tl.tileSize.x, tl.tileSize.y, tl.tileOffset.x, tl.tileOffset.y)
				&& mat.GetVector("_MaskMapRemapScale" + n.ToString()) == tl.maskMapRemapMax - tl.maskMapRemapMin
				&& mat.GetVector("_MaskMapRemapOffset" + n.ToString()) == tl.maskMapRemapMin
				&& mat.GetVector("_DiffuseRemapScale" + n.ToString()) == tl.diffuseRemapMax
				&& mat.GetVector("_DiffuseRemapOffset" + n.ToString()) == tl.diffuseRemapMin;

				bool equalMetallicSmooth = diffuse || tl && mat.GetVector("_TerrainMetallic")[n] == tl.metallic
				&& mat.GetVector("_TerrainSmoothness")[n] == tl.smoothness;

				if (equal && equalMetallicSmooth)
				{
					layerIndex = terrain.terrainData.terrainLayers.ToList().IndexOf(tl);
					mat.SetFloat("_LayerIndex" + (n + 1).ToString(), layerIndex);
				}
			}
		}

		static void SetTerrainFloatsToMaterial(Terrain terrain, Material mat, string[] properties)
		{
			foreach (string prop in properties)
			{
				mat.SetFloat(prop, terrain.materialTemplate.GetFloat(prop));
			}
		}

		static void SetTerrainVectorToMaterial(Terrain terrain, Material mat, string value)
		{
			mat.SetVector(value, terrain.materialTemplate.GetVector(value));
		}

		static void SetTerrainTextureToMaterial(Terrain terrain, Material mat, string[] textures)
		{
			foreach (string texture in textures)
			{
				mat.SetTexture(texture, terrain.materialTemplate.GetTexture(texture));
				mat.SetTextureScale(texture, terrain.materialTemplate.GetTextureScale(texture));
				mat.SetTextureOffset(texture, terrain.materialTemplate.GetTextureOffset(texture));
			}
		}

		static void TerrainKeywordsToMaterial(Terrain terrain, Material mat, string[] keywords)
		{
			foreach (string keyword in keywords)
			{
				if (terrain.materialTemplate.IsKeywordEnabled(keyword))
				{
					mat.EnableKeyword(keyword);
				}
				else
				{
					mat.DisableKeyword(keyword);
				}
			}
		}

		static void DisableKeywords(Material mat, string[] keywords)
		{
			foreach (string keyword in keywords)
			{
				mat.DisableKeyword(keyword);
			}
		}

		
		public static InTerra_UpdateAndCheck GetUpdaterScript()
		{
			if (updateScript == null)
			{
				if (!updater)
				{
					if (!GameObject.Find(UpdaterName))
					{
						updater = new GameObject(UpdaterName);
						updater.AddComponent<InTerra_UpdateAndCheck>();

						updater.hideFlags = HideFlags.HideInInspector;
						updater.hideFlags = HideFlags.HideInHierarchy;
					}
					else
					{
						updater = GameObject.Find(UpdaterName);
					}
				}
				updateScript = updater.GetComponent<InTerra_UpdateAndCheck>();
			}
			return (updateScript);
		}

		public static void TracksUpdate()
		{
			updateScript = GetUpdaterScript();

			if (updater != null)
			{
				if (trackCamera == null)
				{
					if (!updater.TryGetComponent<Camera>(out Camera c))
                    {
						trackCamera = updater.AddComponent<Camera>();
						InTerra_TracksCameraSettings.SetTrackCamera(updater.GetComponent<Camera>());
					}
					else
                    {
						trackCamera = updater.GetComponent<Camera>();

					}					
				}
				else
				{
					#if UNITY_EDITOR
						var view = SceneView.lastActiveSceneView;
					 	if (view != null && EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.ToString() == " (UnityEditor.SceneView)" && UnityEditorInternal.InternalEditorUtility.isApplicationActive)
							{
								trackCameraPositon = view.camera.transform.position;
								TrackCameraForwardVec = view.camera.transform.forward;
							}
							else if (EditorWindow.focusedWindow != null && EditorWindow.focusedWindow.ToString() == " (UnityEditor.GameView)" )
							{
								TrackCameraForwardVec = Camera.main.transform.forward;
								trackCameraPositon = Camera.main.transform.position;
							}
					#else
							TrackCameraForwardVec = Camera.main.transform.forward;
							trackCameraPositon = Camera.main.transform.position;
					#endif

					Vector3 tcPos = trackCameraPositon + TrackCameraForwardVec * Mathf.Round((updateScript.TrackArea * 0.5f));
					float roundIndex = updateScript.TrackArea / (updateScript.TrackTextureSize * 0.2f);
					tcPos.x = Mathf.Round(tcPos.x / roundIndex) * roundIndex;
					tcPos.y = Mathf.Round(tcPos.y / roundIndex) * roundIndex;
					tcPos.z = Mathf.Round(tcPos.z / roundIndex) * roundIndex;

					updater.transform.position = new Vector3(tcPos.x, Terrain.activeTerrain.GetPosition().y - 10.0f - updateScript.TrackDepthIndex, tcPos.z);
					Shader.SetGlobalVector("_InTerra_TrackPosition", tcPos);
					Shader.SetGlobalFloat("_InTerra_TrackArea", updateScript.TrackArea);
					Shader.SetGlobalTexture("_InTerra_TrackTexture", updateScript.TrackTexture);

					trackCamera.targetTexture = updateScript.TrackTexture;
					trackCamera.orthographicSize = updateScript.TrackArea * 0.5f;			
				}
			}
		}

		public static void CheckAndUpdate()
		{
			Terrain[] terrains = Terrain.activeTerrains;
			
			if (terrains.Length > 0)
			{
				updateScript = GetUpdaterScript();
				DictionaryMaterialTerrain materialTerrain = updateScript.MaterialTerrain;

				if (materialTerrain != null && materialTerrain.Count > 0)
				{
					Material mat = materialTerrain.Keys.First();

					if (mat && materialTerrain[mat] && !mat.GetTexture("_TerrainHeightmapTexture") && materialTerrain[mat].terrainData.heightmapTexture.IsCreated())
					{
						UpdateTerrainData(InTerra_Setting.DictionaryUpdate);
					}
				}
				else if (!updateScript.FirstInit)
				{
					if (!InTerra_Setting.DisableAllAutoUpdates) UpdateTerrainData(true);
					updateScript.FirstInit = true;
				}

				if (tracksEnabled)
				{
					TracksUpdate();
				}
				#if UNITY_EDITOR
					TerrainMaterialUpdate();
				#endif
			}
		}


		static public void CreateTrackRenderTexture()
		{
			if(updateScript.TrackTexture != null)
            {
				updateScript.TrackTexture.Release();
			}

			int tracksTexSize = updateScript.TrackTextureSize;
			updateScript.TrackTexture = new RenderTexture(tracksTexSize, tracksTexSize, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { name = "TrackTexture", enableRandomWrite = true};

			updateScript.TrackTexture.Create();
		}


		static void TerrainMaterialUpdate()
		{
			Terrain[] terrains = Terrain.activeTerrains;
			tracksEnabled = false;
		

			foreach (Terrain terrain in terrains)
			{
				if (terrain && terrain.terrainData && terrain.materialTemplate)
				{
					terrain.materialTemplate.SetVector("_TerrainSizeXZPosY", new Vector3(terrain.terrainData.size.x, terrain.terrainData.size.z, terrain.transform.position.y));

					if(terrain.materialTemplate.IsKeywordEnabled("_TRACKS"))
                    {
						tracksEnabled = true;
						#if UNITY_EDITOR
							terrain.materialTemplate.SetFloat("_Gamma", (PlayerSettings.colorSpace == ColorSpace.Gamma ? 1.0f : 0.0f));
						#endif
                    }						
				}			
			}
		}

		#if UNITY_EDITOR
			public static void CenterOnMainWin(this UnityEditor.EditorWindow aWin)
			{
				var main = EditorGUIUtility.GetMainWindowPosition();
				var pos = aWin.position;
				float w = (main.width - pos.width) * 0.5f;
				float h = (main.height - pos.height) * 0.5f;
				pos.x = main.x + w;
				pos.y = main.y + h;
				aWin.position = pos;
			}
		#endif
	}

	//The Serialized Dictionary is based on christophfranke123 code from this page https://answers.unity.com/questions/460727/how-to-serialize-dictionary-with-unity-serializati.html
	[System.Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[SerializeField]
		private List<TKey> keys = new List<TKey>();

		[SerializeField]
		private List<TValue> values = new List<TValue>();

		// save the dictionary to lists
		public void OnBeforeSerialize()
		{
			keys.Clear();
			values.Clear();
			foreach (KeyValuePair<TKey, TValue> pair in this)
			{
				keys.Add(pair.Key);
				values.Add(pair.Value);
			}
		}

		// load dictionary from lists
		public void OnAfterDeserialize()
		{
			this.Clear();
			if (keys.Count != values.Count)
				throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

			for (int i = 0; i < keys.Count; i++)
				this.Add(keys[i], values[i]);
		}

	}
}
