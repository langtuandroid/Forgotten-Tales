// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuceedStudio_Utils
{
    [Overlay(typeof(SceneView), "bonesviewer", "Bones Viewer")]
    public class BonesViewerOverlay : IMGUIOverlay
    {
        private bool isBones = false;
        private bool isBlackBones = true;
        private bool isOptions = false;
        private bool isNormals = false;
        private bool isHelp = false;
        private float radius = 0.02f;
        private float lastRadius = 0.02f;
        private float currentRadiusMultiplier = -1f;
        private float normalsSize = 0.025f;
        private bool rigDrawn = false;
        private bool updating = false;
        private bool hasNoRig = false;
        private bool hasManyRigs = false;
        private bool performanceMode = false;
        private int performanceModeMin = 10;
        private bool polys = false;
        private bool lines = false;

        private const int OVERLAY_WIDTH = 115;
        private const int THIRD_WIDTH = 35;

        #region Colors
        private bool[] isColors = { true, false, false };

        //Global
        private static Color lineColor = Color.white;
        private static Color guiSelectedBgColor = new Color(1f, 1f, 1f, 0.15f);
        private static Color handleSphereColor_selected = Color.white;

        //Green
        private static Color handleTriColor_green = new Color(0f, 0.9f, 0f, 0.2f);
        private static Color handleLineColor_green = new Color(0f, 0.45f, 0f, 0.8f);
        private static Color handleSphereColor_green = new Color(0f, 0.9f, 0f, 0.5f);
        private static Color handleTriColor_yellow = new Color(0.7f, 0.9f, 0f, 0.2f);
        private static Color handleLineColor_yellow = new Color(0.35f, 0.45f, 0f, 0.8f);
        private static Color handleSphereColor_yellow = new Color(0.7f, 0.9f, 0f, 0.8f);
        private Color[] colorsGreen = new Color[] { handleTriColor_green, handleLineColor_green, handleSphereColor_green, handleTriColor_yellow, handleLineColor_yellow, handleSphereColor_yellow };

        //Red
        private static Color handleTriColor_red = new Color(0.9f, 0f, 0f, 0.2f);
        private static Color handleLineColor_red = new Color(0.45f, 0f, 0f, 0.8f);
        private static Color handleSphereColor_red = new Color(0.9f, 0f, 0f, 0.5f);
        private static Color handleTriColor_orange = new Color(0.9f, 0.7f, 0f, 0.2f);
        private static Color handleLineColor_orange = new Color(0.45f, 0.35f, 0f, 0.8f);
        private static Color handleSphereColor_orange = new Color(0.9f, 0.7f, 0f, 0.8f);
        private Color[] colorsRed = new Color[] { handleTriColor_red, handleLineColor_red, handleSphereColor_red, handleTriColor_orange, handleLineColor_orange, handleSphereColor_orange };

        //Blue
        private static Color handleTriColor_blue = new Color(0f, 0.9f, 0.9f, 0.2f);
        private static Color handleLineColor_blue = new Color(0f, 0.45f, 0.45f, 0.8f);
        private static Color handleSphereColor_blue = new Color(0f, 0.9f, 0.9f, 0.5f);
        private static Color handleTriColor_blueLight = new Color(0.7f, 0.9f, 0.9f, 0.2f);
        private static Color handleLineColor_blueLight = new Color(0.35f, 0.45f, 0.45f, 0.8f);
        private static Color handleSphereColor_blueLight = new Color(0.7f, 0.9f, 0.9f, 0.8f);
        private Color[] colorsBlue = new Color[] { handleTriColor_blue, handleLineColor_blue, handleSphereColor_blue, handleTriColor_blueLight, handleLineColor_blueLight, handleSphereColor_blueLight };

        private Color[] currentColors = null;
        #endregion

        #region Icons
        private string icon_joint_guid = "1a66e6f50b32e3146b6d5b99b5c1ac38";
        private string icon_normals_guid = "f2e150b2f5e6e5d4ab6d998dfe6833c5";
        private string icon_settings = "d__Popup@2x";
        private string icon_help = "d__Help@2x";
        private string icon_warning = "console.warnicon.sml";
        private Texture icon_joint = null;
        private Texture icon_normals = null;
        #endregion

        private List<SkinnedMeshRenderer> smrs = new List<SkinnedMeshRenderer>();
        private List<Transform> currentBones = new List<Transform>();
        private bool isAlt = false;
        private bool isAdd = false;

        public BonesViewerOverlay()
        {

        }

#region GUI
        public override void OnCreated()
        {
            base.OnCreated();

            currentColors = colorsGreen;
            lineColor.a = 0.2f;

            TryGetIcons();

            performanceModeMin = EditorPrefs.GetInt("LuceedStudio.BonesAssistant.MaxHierarchies", 10);
        }

        private void TryGetIcons()
        {
            icon_joint = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_joint_guid), typeof(Texture)) as Texture;
            icon_normals = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_normals_guid), typeof(Texture)) as Texture;
        }

        public override void OnGUI()
        {
            if (icon_joint == null || icon_normals == null)
            {
                TryGetIcons();
            }

            GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

#region Show bones Button
            string viewButtonText = isBones ? "Hide bones" : "Show bones";
            string viewButtonTooltip = isBones ? "Hide bones" : "Show bones of the scene skinned meshes";
            GUIContent viewButtonContent = new GUIContent(viewButtonText, viewButtonTooltip);
            if (icon_joint != null)
            {
                viewButtonContent = new GUIContent(viewButtonText, icon_joint, viewButtonTooltip);
            }

            EditorGUI.BeginChangeCheck();
            isBones = GUILayout.Toggle(isBones, viewButtonContent, GUI.skin.button, GUILayout.Width(OVERLAY_WIDTH), GUILayout.Height(25));
            if (EditorGUI.EndChangeCheck())
            {
                CheckCanView();
            }
#endregion

#region Expand Foldout
            GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

            GUIContent optionContent = EditorGUIUtility.IconContent(icon_settings);
            optionContent.text = "Options";
            optionContent.tooltip = isOptions ? "Collapse to hide extra options" : "Expand to show extra options";

            Rect expandFoldoutRect = GUILayoutUtility.GetLastRect();
            expandFoldoutRect.y += 7;
            expandFoldoutRect.height = 15;
            GUILayout.Space(15);

            isOptions = EditorGUI.Foldout(expandFoldoutRect, isOptions, optionContent, true);
#endregion

#region Options
            if (isOptions)
            {
                //White BG
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect optionBgRect = new Rect(lastRect.x - 1, lastRect.y - 3, 115 + 7, 194);
                EditorGUI.DrawRect(optionBgRect, guiSelectedBgColor);

#region Radius slider
                if (!isBones)
                {
                    GUI.enabled = false;
                }

                string bonesRadiusTooltip = "Radius of bones representation";
                GUIContent bonesRadiusContent = new GUIContent("Bones radius", bonesRadiusTooltip);
                if (icon_joint != null)
                {
                    bonesRadiusContent = new GUIContent("Bones radius", icon_joint, bonesRadiusTooltip);
                }
                EditorGUILayout.LabelField(bonesRadiusContent, GUILayout.Width(OVERLAY_WIDTH), GUILayout.Height(20));
                radius = EditorGUILayout.Slider(radius, 0.005f, 0.05f, GUILayout.Width(OVERLAY_WIDTH));
#endregion

#region Normals button
                GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

                string normalsButtonTooltip = "Show bones normals";
                GUIContent normalsContent = new GUIContent("Normals", normalsButtonTooltip);
                if (icon_normals != null)
                {
                    normalsContent = new GUIContent("Normals", icon_normals, normalsButtonTooltip);
                }

                isNormals = EditorGUILayout.ToggleLeft(normalsContent, isNormals, GUILayout.Width(OVERLAY_WIDTH), GUILayout.Height(20));
                //canViewNormals = GUILayout.Toggle(canViewNormals, normalsContent, GUI.skin.button, GUILayout.Width(OVERLAY_WIDTH), GUILayout.Height(25));
#endregion

#region Normals size slider
                if (!isNormals)
                {
                    GUI.enabled = false;
                }

                normalsSize = EditorGUILayout.Slider(normalsSize, 0.005f, 0.1f, GUILayout.Width(OVERLAY_WIDTH));

                GUI.enabled = true;
#endregion

#region Bones color
                if (!isBones)
                {
                    GUI.enabled = false;
                }

                GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

                GUIContent colorContent = new GUIContent(EditorGUIUtility.IconContent("d_SceneViewRGB"));
                colorContent.text = "Bones Color";
                colorContent.tooltip = "Change bones color.";
                EditorGUILayout.LabelField(colorContent, GUILayout.Width(OVERLAY_WIDTH));

                using (new GUILayout.HorizontalScope())
                {
                    int buttonHeight = 20;

                    //Red
                    EditorGUI.BeginChangeCheck();
                    GUIContent redButton = EditorGUIUtility.IconContent("sv_icon_dot6_pix16_gizmo");
                    isColors[1] = GUILayout.Toggle(isColors[1], redButton, GUI.skin.button, GUILayout.Width(THIRD_WIDTH), GUILayout.Height(buttonHeight));
                    if (EditorGUI.EndChangeCheck())
                    {
                        CheckColor(1);
                    }

                    GUILayout.Space(1.5f);

                    
                    //Green
                    EditorGUI.BeginChangeCheck();
                    GUIContent greenButton = EditorGUIUtility.IconContent("sv_icon_dot3_pix16_gizmo");
                    isColors[0] = GUILayout.Toggle(isColors[0], greenButton, GUI.skin.button, GUILayout.Width(THIRD_WIDTH), GUILayout.Height(buttonHeight));
                    if (EditorGUI.EndChangeCheck())
                    {
                        CheckColor(0);
                    }

                    GUILayout.Space(1.5f);

                    //Blue
                    EditorGUI.BeginChangeCheck();
                    GUIContent blueButton = EditorGUIUtility.IconContent("sv_icon_dot1_pix16_gizmo");
                    isColors[2] = GUILayout.Toggle(isColors[2], blueButton, GUI.skin.button, GUILayout.Width(THIRD_WIDTH), GUILayout.Height(buttonHeight));
                    if (EditorGUI.EndChangeCheck())
                    {
                        CheckColor(2);
                    }
                }

                GUI.enabled = true;

#endregion

#region Black bones
                if (!isBones)
                {
                    GUI.enabled = false;
                }

                GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

                GUIContent blackBoneContent = new GUIContent("Unused bones", "Show unused black bones.");
                if (isBlackBones)
                {
                    blackBoneContent.tooltip = "Hide unused black bones.";
                }

                isBlackBones = EditorGUILayout.ToggleLeft(blackBoneContent, isBlackBones, GUILayout.Width(OVERLAY_WIDTH));

                GUI.enabled = true;
#endregion
            }

            #endregion

#region Help
            GUIUtils.DrawUILine(lineColor, padding: 5, width: OVERLAY_WIDTH);

            GUIContent helpFoldoutContent = EditorGUIUtility.IconContent(icon_help);
            helpFoldoutContent.text = "Help";
            helpFoldoutContent.tooltip = isOptions ? "Collapse to hide help" : "Expand to show help";

            Rect helpFoldoutRect = GUILayoutUtility.GetLastRect();
            helpFoldoutRect.y += 7;
            helpFoldoutRect.height = 15;
            helpFoldoutRect.width = OVERLAY_WIDTH - 20;
            GUILayout.Space(18);

            isHelp = EditorGUI.Foldout(helpFoldoutRect, isHelp, helpFoldoutContent, true);

            if (isHelp)
            {
                //Get help content
                string helpString = "Click on 'Show Bones' to visualize skinned meshes bones.";
                bool helpWarningIcon = false;
                if (isBones)
                {
                    if (hasManyRigs)
                    {
                        if (smrs.Count > performanceModeMin)
                        {
                            helpString = "More than " + performanceModeMin + " bones hierarchies:\nBones are now only visible if you are close enough.";
                            helpWarningIcon = true;
                            performanceMode = true;
                        }
                        else
                        {
                            helpString = performanceModeMin + " bones hierarchies.\nBones are always visible.";
                            if (smrs.Count < performanceModeMin)
                            {
                                helpString = "Less than " + helpString;
                            }

                            helpWarningIcon = false;
                            performanceMode = false;
                        }
                    }
                    else
                    {
                        helpString = "Click on points to select bones.";
                        performanceMode = false;
                    }
                }
                else if (hasNoRig)
                {
                    helpString = "No skinned mesh in this scene. No bones can be displayed.";
                    helpWarningIcon = true;
                }

                GUIContent helpContent = new GUIContent(helpString);
                if (helpWarningIcon)
                {
                    helpContent = EditorGUIUtility.IconContent(icon_warning);
                    helpContent.text = helpString;
                }

                float helpHeight = EditorStyles.helpBox.CalcHeight(helpContent, OVERLAY_WIDTH);

                //White BG
                Rect lastRect = GUILayoutUtility.GetLastRect();
                Rect helpBgRect = new Rect(lastRect.x - 1, lastRect.y - 3, 115 + 7, helpHeight + 26);
                helpBgRect.height += isBones ? 42 : 0;
                EditorGUI.DrawRect(helpBgRect, guiSelectedBgColor);

                //Max hierarchies
                if (isBones)
                {
                    GUIContent maxContent = new GUIContent(EditorGUIUtility.IconContent("d_AvatarMask On Icon"));
                    maxContent.text = "Max hierarchies: ";
                    maxContent.tooltip = "Performance mode will be active from this number of bones hierarchies.";
                    GUILayout.Label(maxContent, GUILayout.Height(20), GUILayout.Width(OVERLAY_WIDTH - 2));
                    EditorGUI.BeginChangeCheck();
                    performanceModeMin = (int)EditorGUILayout.Slider(performanceModeMin, 5, 30, GUILayout.Width(OVERLAY_WIDTH));
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorPrefs.SetInt("LuceedStudio.BonesAssistant.MaxHierarchies", performanceModeMin);

                        if (smrs.Count >= 10 || (performanceModeMin < 10 && smrs.Count >= performanceModeMin))
                        {
                            isHelp = true;
                            hasManyRigs = true;
                        }
                    }
                }

                //Help box
                GUILayout.Label(helpContent, EditorStyles.helpBox, GUILayout.Width(OVERLAY_WIDTH));
            }
            else
            {
                hasNoRig = false;
            }
        }
#endregion

#endregion

        private void CheckCanView()
        {
            if (isBones)
            {
                if (!rigDrawn)
                {
                    Clear();
                    GetAllRigs();

                    if (smrs.Count <= 0)
                    {
                        isBones = false;
                        isHelp = true;
                        hasNoRig = true;
                    }
                    else
                    {
                        rigDrawn = true;

                        SceneView.duringSceneGui -= DuringSceneGUI;
                        SceneView.duringSceneGui += DuringSceneGUI;
                    }
                }
            }
            else
            {
                if (rigDrawn)
                {
                    rigDrawn = false;
                    hasNoRig = false;
                    hasManyRigs = false;

                    Clear();
                    SceneView.duringSceneGui -= DuringSceneGUI;
                }
            }
        }

        private void GetAllRigs()
        {
            GameObject[] currentSceneGameObjects = new GameObject[1];
            PrefabStage currentPrefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (currentPrefabStage != null)
            {
                currentSceneGameObjects[0] = currentPrefabStage.prefabContentsRoot;
            }
            else
            {
                currentSceneGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            }

            foreach (GameObject go in currentSceneGameObjects)
            {
                SkinnedMeshRenderer[] foundSmrs = go.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (SkinnedMeshRenderer foundSmr in foundSmrs)
                {
                    if (!foundSmr.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    if (!smrs.Contains(foundSmr))
                    {
                        AddSmr(foundSmr);
                    }
                }
            }

            currentRadiusMultiplier = GetBiggerSmrExtentsMagnitude();

            if ( smrs.Count >= 10 || (performanceModeMin < 10 && smrs.Count >= performanceModeMin) )
            {
                isHelp = true;
                hasManyRigs = true;
            }
        }

        private void AddSmr(SkinnedMeshRenderer foundSmr)
        {
            //Add smr
            smrs.Add(foundSmr);

            //Add all bones (to avoid drawing it twice)
            for (int i = 0; i < foundSmr.bones.Length; i++)
            {
                Transform bone = foundSmr.bones[i];

                if (!currentBones.Contains(bone))
                {
                    currentBones.Add(bone);
                }
            }
        }

        private void DuringSceneGUI(SceneView view)
        {
            if (updating)
            {
                return;
            }

            Event evt = Event.current;

            isAlt = evt.alt;
            isAdd = evt.control || evt.shift;

            if (smrs.Count > 0)
            {
                polys = true;
                lines = true;

                if (smrs.Count > 1)
                {
                    lines = false;

                    if (smrs.Count > 5)
                    {
                        polys = false;
                    }
                }

                DrawAllBones(view);
            }
        }

        private void Clear()
        {
            smrs.Clear();
            currentBones.Clear();
        }

        private bool IsSelected(GameObject obj, bool child)
        {
            if (Selection.activeGameObject == obj)
            {
                return true;
            }

            for (int i = 0; i < Selection.gameObjects.Length; i++)
            {
                if (Selection.gameObjects[i] == obj)
                {
                    return true;
                }

                if (child)
                {
                    if (obj.transform.IsChildOf(Selection.gameObjects[i].transform))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsBone(Transform bone)
        {
            for (int i = 0; i < currentBones.Count; i++)
            {
                if (currentBones[i] == bone)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSmr(Transform bone)
        {
            for (int i = 0; i < smrs.Count; i++)
            {
                if (smrs[i] == null)
                {
                    continue;
                }

                if (smrs[i].transform == bone)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasBoneChild(Transform bone)
        {
            Transform[] allChild = bone.GetComponentsInChildren<Transform>();
            if (allChild.Length > 0)
            {
                for (int i = 0; i < allChild.Length; i++)
                {
                    if (currentBones.Contains(allChild[i]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float GetBiggerSmrExtentsMagnitude()
        {
            float biggerSmrExtentsMagnitude = -1f;
            for (int i = 0; i < smrs.Count; i++)
            {
                SkinnedMeshRenderer smr = smrs[i];
                if (smr == null || !smr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float magnitude = smrs[i].bounds.extents.magnitude;

                if (magnitude > biggerSmrExtentsMagnitude)
                {
                    biggerSmrExtentsMagnitude = magnitude;
                }
            }

            return biggerSmrExtentsMagnitude;
        }

        private void CheckColor(int colorIndex)
        {
            //Check if another color is already selected
            bool otherColorSelected = false;
            for (int i = 0; i < isColors.Length; i++)
            {
                if (i != colorIndex && isColors[i] == true)
                {
                    otherColorSelected = true;
                    break;
                }
            }

            //If another color is already selected and enabling clicked one, set all others to false
            if (otherColorSelected)
            {
                if (isColors[colorIndex])
                {
                    for (int i = 0; i < isColors.Length; i++)
                    {
                        if (i != colorIndex)
                        {
                            isColors[i] = false;
                        }
                    }
                }
            }
            else //If no other is selected, always set to true because you cannot have no colors selected
            {
                isColors[colorIndex] = true;
            }

            //Set current colors
            switch (colorIndex)
            {
                case 0: currentColors = colorsGreen; break;
                case 1: currentColors = colorsRed; break;
                case 2: currentColors = colorsBlue; break;
            }
        }

#region Draw
        private void DrawAllBones(SceneView view)
        {
            if (!isBones) return;

            for (int i = 0; i < currentBones.Count; i++)
            {
                Transform bone = currentBones[i];
                if (bone == null || !bone.gameObject.activeSelf)
                {
                    continue;
                }

                if (performanceMode)
                {
                    if (Vector3.Distance(view.camera.transform.position, bone.transform.position) < 4)
                    {
                        DrawBone(i, bone, currentRadiusMultiplier);
                    }
                }
                else
                {
                    DrawBone(i, bone, currentRadiusMultiplier);
                }
            }
        }

        private void DrawBone(int i, Transform bone, float smrExtentsMagnitude, bool blackColor = false)
        {
            GameObject boneObject = bone.gameObject;

            if (!boneObject.activeSelf)
            {
                return;
            }

            bool isSelected = IsSelected(boneObject, false);
            bool isChildSelected = IsSelected(boneObject, true);

            //Draw normals
            if (isNormals)
            {
                DrawAxisGizmo(bone, normalsSize);
            }

            //Handle
            if (bone.childCount <= 0)
            {
                if (blackColor)
                {
                    Handles.color = Color.black;
                    if (isSelected)
                    {
                        Handles.color = Color.grey;
                    }
                }
                else
                {
                    Handles.color = isChildSelected ? currentColors[5] : currentColors[2];
                    if (isSelected)
                    {
                        Handles.color = handleSphereColor_selected;
                    }
                }

                //Last joint radius
                float distance = Vector3.Distance(bone.parent.position, bone.position);
                float lerpValue = Mathf.InverseLerp(0, 1, distance);
                float relativeRadius = radius * smrExtentsMagnitude;
                float jointRadius = Mathf.Lerp(relativeRadius / 5, relativeRadius * 2, lerpValue);
                lastRadius = jointRadius * 2;

                if (isAlt)
                {
                    Handles.SphereHandleCap(i, bone.position, bone.rotation, lastRadius, EventType.Repaint);
                }
                else if (Handles.Button(bone.position, bone.rotation, lastRadius, lastRadius / 2, Handles.SphereHandleCap))
                {
                    if (isAdd)
                    {
                        List<GameObject> selection = new List<GameObject>(Selection.gameObjects);
                        selection.Add(boneObject);
                        Selection.objects = selection.ToArray();
                    }
                    else
                    {
                        Selection.activeGameObject = boneObject;
                    }
                }

                return;
            }

            for (int c = 0; c < bone.childCount; c++)
            {
                Transform boneChild = bone.GetChild(c);

                if (IsSmr(boneChild))
                {
                    continue;
                }

                Vector3 bonePosition = bone.position;
                Vector3 boneChildPosition = boneChild.position;
                Vector3 direction = (boneChild.position - bone.position).normalized;
                Vector3 referenceUp = direction == bone.up ? bone.forward : bone.up;
                referenceUp = direction == -bone.up ? bone.forward : referenceUp;
                Vector3 directionUp = Vector3.Cross(direction, referenceUp).normalized;
                Vector3 directionRight = Vector3.Cross(directionUp, direction).normalized;

                float distance = Vector3.Distance(bonePosition, boneChildPosition);
                float lerpValue = Mathf.InverseLerp(0, 1, distance);
                float relativeRadius = radius * smrExtentsMagnitude;
                float jointRadius = Mathf.Lerp(relativeRadius / 5, relativeRadius * 2, lerpValue);
                lastRadius = jointRadius * 2;
                bonePosition += direction * jointRadius;

                //Draw not considered bone transforms
                /*
                bool notBoneChild = !IsBone(boneChild);
                if (notBoneChild)
                {
                    DrawBone(i, boneChild, smrExtentsMagnitude, true);
                }
                */

                //Handle
                if (c == 0)
                {
                    if (blackColor)
                    {
                        Handles.color = Color.black;
                        if (isSelected)
                        {
                            Handles.color = Color.grey;
                        }
                    }
                    else
                    {
                        Handles.color = isChildSelected ? currentColors[5] : currentColors[2];
                        if (isSelected)
                        {
                            Handles.color = handleSphereColor_selected;
                        }
                    }
                    

                    if (isAlt)
                    {
                        Handles.SphereHandleCap(i, bone.position, bone.rotation, lastRadius, EventType.Repaint);
                    }
                    else if (Handles.Button(bone.position, bone.rotation, lastRadius, lastRadius / 2, Handles.SphereHandleCap))
                    {
                        if (isAdd)
                        {
                            List<GameObject> selection = new List<GameObject>(Selection.gameObjects);
                            selection.Add(boneObject);
                            Selection.objects = selection.ToArray();
                        }
                        else
                        {
                            Selection.activeGameObject = boneObject;
                        }
                    }
                }

                if (!boneChild.gameObject.activeSelf || !IsBone(boneChild))
                {
                    bool drawBlack = true;
                    if (HasBoneChild(boneChild))
                    {
                        drawBlack = false;
                    }

                    if (drawBlack)
                    {
                        if (isBlackBones)
                        {
                            DrawBone(0, boneChild, smrExtentsMagnitude, drawBlack);
                        }
                    }
                    else
                    {
                        DrawBone(0, boneChild, smrExtentsMagnitude, drawBlack);
                    }
                }

                if (blackColor)
                {
                    if (isBlackBones)
                    {
                        Handles.color = Color.black;
                        if (isSelected)
                        {
                            Handles.color = Color.grey;
                        }

                        Vector3[] lineBone = { bonePosition, boneChild.position };

                        float lineWidth = (jointRadius * 115) / HandleUtility.GetHandleSize(bonePosition);
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, lineWidth, lineBone);
                    }
                }
                else
                {
                    Handles.color = isChildSelected ? currentColors[3] : currentColors[0];

                    if (polys)
                    {
                        Vector3 vertice0 = bonePosition + (directionUp * jointRadius) + (directionRight * jointRadius);
                        Vector3 vertice1 = bonePosition + (-directionUp * jointRadius) + (directionRight * jointRadius);
                        Vector3 vertice2 = bonePosition + (-directionUp * jointRadius) + (-directionRight * jointRadius);
                        Vector3 vertice3 = bonePosition + (directionUp * jointRadius) + (-directionRight * jointRadius);

                        //First tri
                        Vector3[] jointTriVertices0 = new Vector3[3];
                        jointTriVertices0[0] = vertice0;
                        jointTriVertices0[1] = vertice1;
                        jointTriVertices0[2] = boneChildPosition;

                        //Second tri
                        Vector3[] jointTriVertices1 = new Vector3[3];
                        jointTriVertices1[0] = vertice1;
                        jointTriVertices1[1] = vertice2;
                        jointTriVertices1[2] = boneChildPosition;

                        //Third tri
                        Vector3[] jointTriVertices2 = new Vector3[3];
                        jointTriVertices2[0] = vertice2;
                        jointTriVertices2[1] = vertice3;
                        jointTriVertices2[2] = boneChildPosition;

                        //Fourth tri
                        Vector3[] jointTriVertices3 = new Vector3[3];
                        jointTriVertices3[0] = vertice3;
                        jointTriVertices3[1] = vertice0;
                        jointTriVertices3[2] = boneChildPosition;

                        Handles.DrawAAConvexPolygon(jointTriVertices0);
                        Handles.DrawAAConvexPolygon(jointTriVertices1);
                        Handles.DrawAAConvexPolygon(jointTriVertices2);
                        Handles.DrawAAConvexPolygon(jointTriVertices3);

                        if (lines)
                        {
                            Vector3[] line0 = { vertice0, boneChildPosition };
                            Vector3[] line1 = { vertice1, boneChildPosition };
                            Vector3[] line2 = { vertice2, boneChildPosition };
                            Vector3[] line3 = { vertice3, boneChildPosition };
                            Vector3[] line4 = { vertice0, vertice1 };
                            Vector3[] line5 = { vertice1, vertice2 };
                            Vector3[] line6 = { vertice2, vertice3 };
                            Vector3[] line7 = { vertice3, vertice0 };

                            Handles.color = isChildSelected ? currentColors[4] : currentColors[1];
                            float lineWidth = 3;
                            Handles.DrawAAPolyLine(lineWidth, line0);
                            Handles.DrawAAPolyLine(lineWidth, line1);
                            Handles.DrawAAPolyLine(lineWidth, line2);
                            Handles.DrawAAPolyLine(lineWidth, line3);
                            Handles.DrawAAPolyLine(lineWidth, line4);
                            Handles.DrawAAPolyLine(lineWidth, line5);
                            Handles.DrawAAPolyLine(lineWidth, line6);
                            Handles.DrawAAPolyLine(lineWidth, line7);
                        }
                    }
                    else
                    {
                        Color tempColor = Handles.color;
                        tempColor.a = 0.65f;
                        Handles.color = tempColor;

                        Vector3[] lineBone = { bonePosition, boneChild.position };

                        float lineWidth = (jointRadius * 115) / HandleUtility.GetHandleSize(bonePosition);
                        Handles.DrawAAPolyLine(EditorGUIUtility.whiteTexture, lineWidth, lineBone);
                    }
                }
            }
        }

        private void DrawAxisGizmo(Transform t, float size)
        {
            Handles.color = Color.red;
            Vector3[] xLine = { t.position, t.position + t.right * size };
            Handles.DrawAAPolyLine(4, xLine);

            Handles.color = Color.green;
            Vector3[] yLine = { t.position, t.position + t.up * size };
            Handles.DrawAAPolyLine(4, yLine);

            Handles.color = Color.blue;
            Vector3[] zLine = { t.position, t.position + t.forward * size };
            Handles.DrawAAPolyLine(4, zLine);
        }
#endregion

    }
}

