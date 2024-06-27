using UnityEngine;
using UnityEditor;

namespace InTerra
{
	public class InTerra_GUI
	{
		public static void TessellationDistaces(Material targetMat, MaterialEditor editor, ref bool minMax)
		{
			float minDist = targetMat.GetFloat("_TessellationFactorMinDistance");
			float maxDist = targetMat.GetFloat("_TessellationFactorMaxDistance");
			float mipMapLevel = targetMat.GetFloat("_MipMapLevel");
			Vector4 mipMapFade = targetMat.GetVector("_MipMapFade");

			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Tessellation Factor");

				using (new GUILayout.HorizontalScope())
				{
					minDist = Mathf.Clamp(minDist, mipMapFade.z, mipMapFade.w);
					maxDist = Mathf.Clamp(maxDist, mipMapFade.z, mipMapFade.w);

					EditorGUI.BeginChangeCheck();

					EditorGUILayout.LabelField(minDist.ToString("0.0"), GUILayout.Width(33));
					EditorGUILayout.MinMaxSlider(ref minDist, ref maxDist, mipMapFade.z, mipMapFade.w); //The range is the same as for MipMaps
					EditorGUILayout.LabelField(maxDist.ToString("0.0"), GUILayout.Width(33));

					maxDist = minDist + (float)0.001 >= maxDist ? maxDist + (float)0.001 : maxDist;

					if (EditorGUI.EndChangeCheck())
					{
						editor.RegisterPropertyChangeUndo("Tessellation Factor distance");
						targetMat.SetFloat("_TessellationFactorMinDistance", minDist);
						targetMat.SetFloat("_TessellationFactorMaxDistance", maxDist);

					}
				}
				EditorGUILayout.Space();

				MipMapsFading(targetMat, "Mip Maps", editor, ref minMax);
			}
		}


		public static void MipMapsFading(Material targetMat, string label, MaterialEditor editor, ref bool minMax)
		{
			Vector4 mipMapFade = targetMat.GetVector("_MipMapFade");
			float mipMapLevel = targetMat.GetFloat("_MipMapLevel");

			EditorGUI.BeginChangeCheck();
			using (new GUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField(label, GUILayout.MinWidth(75));
				EditorGUILayout.LabelField(new GUIContent() { text = "Bias:", tooltip = "Minimal Mip map level where the fading will starts." }, new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight }, GUILayout.MaxWidth(62));
				mipMapLevel = EditorGUILayout.IntField((int)mipMapLevel, GUILayout.MaxWidth(25));
			}

			mipMapFade = MinMaxValues(targetMat.GetVector("_MipMapFade"), true, ref minMax);

			if (EditorGUI.EndChangeCheck())
			{
				editor.RegisterPropertyChangeUndo("InTerra Mip Maps Fading");
				targetMat.SetVector("_MipMapFade", mipMapFade);
				targetMat.SetFloat("_MipMapLevel", mipMapLevel);
			}			
		}

		public static Vector4 MinMaxValues(Vector4 intersection, bool distanceRange, ref bool minMax)
		{
			GUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(intersection.x.ToString("0.0"), GUILayout.Width(33));
			EditorGUILayout.MinMaxSlider(ref intersection.x, ref intersection.y, intersection.z, intersection.w);
			EditorGUILayout.LabelField(intersection.y.ToString("0.0"), GUILayout.Width(33));
			GUILayout.EndHorizontal();

			if (distanceRange)
            {
				EditorGUI.indentLevel = 1;
				minMax = EditorGUILayout.Foldout(minMax, "Adjust Distance Range", true);
			}
			else
            {
				EditorGUI.indentLevel = 2;
				minMax = EditorGUILayout.Foldout(minMax, "Adjust Range", true);
			}

			EditorGUI.indentLevel = 0;
			if (minMax)
			{
				GUILayout.BeginHorizontal();

				GUIStyle rightAlignment = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
				EditorGUILayout.LabelField("Min:", rightAlignment, GUILayout.Width(45));
				intersection.z = EditorGUILayout.DelayedFloatField(intersection.z, GUILayout.MinWidth(50));

				EditorGUILayout.LabelField("Max:", rightAlignment, GUILayout.Width(45));
				intersection.w = EditorGUILayout.DelayedFloatField(intersection.w, GUILayout.MinWidth(50));

				GUILayout.EndHorizontal();
			}

			intersection.x = Mathf.Clamp(intersection.x, intersection.z, intersection.w);
			intersection.y = Mathf.Clamp(intersection.y, intersection.z, intersection.w);

			intersection.y = intersection.x + (float)0.001 >= intersection.y ? intersection.y + (float)0.001 : intersection.y;

			return intersection;
		}

		public static void TrackMaterialEditor(Material targetMat, MaterialEditor materialEditor, ref bool minMax)
		{
			InTerra_TracksShaderGUI.TrackType trackType = InTerra_TracksShaderGUI.TrackType.Default;
			using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Track Type", GUILayout.MaxWidth(80));				
				if (targetMat.IsKeywordEnabled("_FOOTPRINTS"))
				{
					trackType = InTerra_TracksShaderGUI.TrackType.Footprints;
				}
				else if (targetMat.IsKeywordEnabled("_TRACKS"))
				{
					trackType = InTerra_TracksShaderGUI.TrackType.WheelTracks;
				}
				EditorGUI.BeginChangeCheck();
				trackType = (InTerra_TracksShaderGUI.TrackType)EditorGUILayout.EnumPopup(trackType);
				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra Shader Variant");
					SetKeyword("_TRACKS", trackType == InTerra_TracksShaderGUI.TrackType.WheelTracks);
					SetKeyword("_FOOTPRINTS", trackType == InTerra_TracksShaderGUI.TrackType.Footprints);
				}
			}
			if (trackType != InTerra_TracksShaderGUI.TrackType.Default)
			{
				MaterialProperty heightmap = MaterialEditor.GetMaterialProperty(new Material[] { targetMat }, "_HeightTex");
				using (new GUILayout.HorizontalScope())
				{
					Rect textureRect = EditorGUILayout.GetControlRect(GUILayout.MinWidth(50));
					materialEditor.TexturePropertyMiniThumbnail(textureRect, heightmap, "Heightmap", "Heightmap of track or footprint");

					using (new GUILayout.VerticalScope())
					{
						KeywordToggle("Invert", "_INVERT", "Invert Heightmap texture.");
						KeywordToggle("Rotate textures by 90°", "_ORIENTATION", "");
					}
				}

				materialEditor.ShaderProperty(MaterialEditor.GetMaterialProperty(new Material[] { targetMat }, "_TerrainTrackContrast"), LabelAndTooltip("Contrast", "Contrast of Heightmap"));
				using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
				{

					materialEditor.TextureScaleOffsetProperty(heightmap);
				}
			}
			

			using (new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.LabelField("Edge Fading", new GUIStyle(EditorStyles.boldLabel));

				Vector4 edgeFading = targetMat.GetVector("_EdgeFading");

				EditorGUI.BeginChangeCheck();

				edgeFading = InTerra_GUI.MinMaxValues(edgeFading, false, ref minMax);

				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra Track Edge Fading");
					targetMat.SetVector("_EdgeFading", edgeFading);
				}
			}			
			
			void KeywordToggle(string label, string keyword, string tooltip)
			{
				bool toggle = targetMat.IsKeywordEnabled(keyword);
				EditorGUI.BeginChangeCheck();
				toggle = EditorGUILayout.ToggleLeft(LabelAndTooltip(label, tooltip), toggle);
				if (EditorGUI.EndChangeCheck())
				{
					materialEditor.RegisterPropertyChangeUndo("InTerra Track " + label);
					SetKeyword(keyword, toggle);
				}
			}

			void SetKeyword(string name, bool set)
			{
				if (set) targetMat.EnableKeyword(name); else targetMat.DisableKeyword(name);
			}

		}

		static GUIContent LabelAndTooltip(string label, string tooltip)
		{
			return new GUIContent() { text = label, tooltip = tooltip };
		}

	}
}

