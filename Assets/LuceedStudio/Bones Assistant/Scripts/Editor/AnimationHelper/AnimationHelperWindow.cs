// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using LuceedStudio_Utils;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using Unity.VisualScripting;

namespace LuceedStudio_BonesAssistant
{
    public class AnimationHelperWindow : EditorWindow
    {
        private static EditorWindow humanoidAnimationWindow = null;
        private static AnimationWindow animationWindow = null;

        private Animator animator;
        private AnimationClip clip;
        private AnimationClip currentAnimationWindowClip = null;
        private float currentAnimationWindowTime = 0f;
        private int clipIndex = 0;
        private Animator currentParentAnimator = null;

        private bool noController = false;
        private bool clipReadOnly = false;
        private bool readyToEdit = false;
        private bool animationInit = false;

        private bool isFoldoutInfo = true;
        private bool isFoldoutTool = true;
        private bool isFoldoutMode = true;
        private bool isFoldoutBone = true;
        private bool isModeInfos = true;
        private List<bool> isRenamings = new List<bool>();
        private string currentRenamingName = "";

        private bool isSceneHandles = true;
        private bool isSceneHandlesInfo = true;

        private static string icon_aeWin_guid = "51cf0438f6597bb46993ff546b8af7a7";
        private static Texture icon_aeWin;

        public static void OpenHumanoidAnimationWindow()
        {
            //Icon
            icon_aeWin = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_aeWin_guid), typeof(Texture)) as Texture;

            //Find inspector window
            EditorWindow inspectorWindow = AnimationHelperInfo.GetEditorWindow("Inspector");

            //Init window if null
            if (humanoidAnimationWindow == null)
            {
                //Init window docked to inspector window if found
                if (inspectorWindow != null)
                {
                    humanoidAnimationWindow = GetWindow<AnimationHelperWindow>("Animation Helper", inspectorWindow.GetType());
                }
                else
                {
                    humanoidAnimationWindow = GetWindow<AnimationHelperWindow>("Animation Helper");
                }

                if (humanoidAnimationWindow != null && icon_aeWin != null)
                {
                    GUIContent titleContent = new GUIContent("Animation Helper", icon_aeWin);
                    humanoidAnimationWindow.titleContent = titleContent;
                }
            }
        }

        public static void RepaintCurrentInstance()
        {
            if (humanoidAnimationWindow == null)
            {
                humanoidAnimationWindow = AnimationHelperInfo.GetEditorWindow("Animation Helper");
            }

            if (humanoidAnimationWindow != null)
            {
                humanoidAnimationWindow.Repaint();
            }
        }

        private void OnFocus()
        {
            GetAnimationWindow();

            if (!animationInit)
            {
                GetAnimationReferences();
            }
        }

        private void OnEnable()
        {
            GetAnimationWindow();

            if (!animationInit)
            {
                GetAnimationReferences();
            }
        }

        private void OnDisable()
        {
            AnimationHelperInfo.UpdateOverlayVisibility(false);

            EditorApplication.update -= OnEditorUpdate;
            animationWindow = null;
            animator = null;
            clip = null;
        }

        private void OnLostFocus()
        {
            if (isRenamings.Count > 0)
            {
                for (int i = 0; i < isRenamings.Count; i++)
                {
                    isRenamings[i] = false;
                }
            }
        }

        private void OnSelectionChange()
        {
            if (!animationInit)
            {
                GetAnimationReferences();

                if (Selection.activeGameObject != null)
                {
                    Transform currentSelectedChild = Selection.activeGameObject.transform;
                    currentParentAnimator = AnimationHelperInfo.FindComponentInParent<Animator>(currentSelectedChild);
                }
            }
            else
            {
                if (animator == null)
                {
                    UpdateEditAnimationState(false);
                    return;
                }

                if (Selection.activeGameObject != null)
                {
                    Transform selectedTransform = Selection.activeGameObject.transform;
                    if (selectedTransform.IsBone())
                    {
                        Tools.hidden = true;

                        AnimationHelperInfo.SetCurrentSelectedBone(selectedTransform);
                        isFoldoutBone = true;
                    }
                    else
                    {
                        Tools.hidden = false;

                        AnimationHelperInfo.SetCurrentSelectedBone();

                        if (!selectedTransform.IsChildOf(animator.transform))
                        {
                            UpdateEditAnimationState(false);
                        }
                    }
                }

                AnimationHelperInfo.UpdateCurrentSliderValues();
                Repaint();
            }
        }

        private void OnEditorUpdate()
        {
            if (animationWindow != null)
            { 
                if (animationWindow.animationClip != currentAnimationWindowClip)
                {
                    OnAnimationWindowClipChanged();
                }

                if (animationWindow.time != currentAnimationWindowTime)
                {
                    currentAnimationWindowTime = animationWindow.time;

                    OnAnimationWindowTimeChanged();
                }
            }

            /*
            if (animationInit)
            {
                AnimationHelperInfo.EditorDeltaTime += 0.001f;
            }
            else
            {
                AnimationHelperInfo.EditorDeltaTime  = 0f;
            }
            */
        }

        private void OnAnimationWindowClipChanged()
        {
            if (animator == null || animationWindow == null) return;

            if (animationInit)
            {
                Selection.activeGameObject = animator.gameObject;
                UpdateEditAnimationState(false);
                return;
            }

            clip = animationWindow.animationClip;
            currentAnimationWindowClip = clip;

            if (clip == null)
            {
                return;
            }

            if (clip.hideFlags.HasFlag(HideFlags.NotEditable))
            {
                clipReadOnly = true;
            }
            else
            {
                clipReadOnly = false;
            }

            for (int i = 0; i < animator.runtimeAnimatorController.animationClips.Length; i++)
            {
                if (animator.runtimeAnimatorController.animationClips[i] == clip)
                {
                    clipIndex = i;
                    break;
                }
            }

            Repaint();
        }

        private void OnAnimationWindowTimeChanged()
        {
            if (animationInit)
            {
                AnimationHelperInfo.UpdateCurrentSliderValues();
                AnimationHelperInfo.UpdateCurrentRootBonePositionAndRotation();
                Repaint();
            }
        }

        private void OnUndoRedo()
        {
            if (animationInit)
            {
                AnimationHelperInfo.UpdateCurrentSliderValues();
                AnimationHelperInfo.UpdateRootValues(false);
                AnimationHelperInfo.UndoOffsetValues();
                Repaint();
            }
        }

        private void GetAnimationWindow(bool canCreate = false)
        {
            if (animationWindow == null)
            {
                animationWindow = AnimationHelperInfo.GetEditorWindow("Animation") as AnimationWindow;

                if (canCreate && animationWindow == null)
                {
                    EditorWindow consoleWindow = AnimationHelperInfo.GetEditorWindow("Console");

                    if (consoleWindow != null)
                    {
                        animationWindow = GetWindow<AnimationWindow>(consoleWindow.GetType());
                    }
                    else
                    {
                        animationWindow = GetWindow<AnimationWindow>();
                    }
                }

                if (animationWindow != null)
                {
                    EditorApplication.update -= OnEditorUpdate;
                    EditorApplication.update += OnEditorUpdate;
                    currentAnimationWindowTime = animationWindow.time;
                }
            }
        }

        private void GetAnimationReferences()
        {
            if (animationInit)
            {
                return;
            }

            if (animationWindow == null)
            {
                GetAnimationWindow();
            }

            if (Selection.activeGameObject != null)
            {
                if (Selection.activeGameObject.scene.IsValid())
                {
                    animator = Selection.activeGameObject.GetComponent<Animator>();

                    if (animator != null)
                    {
                        if (animator.runtimeAnimatorController == null)
                        {
                            noController = true;
                        }
                        else
                        {
                            noController = false;
                        }
                    }
                }
                else
                {
                    animator = null;
                }
            }
            else
            {
                animator = null;
            }

            if (animationWindow != null)
            {
                OnAnimationWindowClipChanged();
            }
            else
            {
                clip = null;
                clipReadOnly = false;
            }

            Repaint();
        }

        private void UpdateEditAnimationState(bool state)
        {
            animationInit = state;

            if (state)
            {
                if (animationWindow != null)
                {
                    animationWindow.Focus();
                    animationWindow.animationClip = clip;
                    animationWindow.previewing = true;
                    animationWindow.recording = true;
                }
                else
                {
                    UpdateEditAnimationState(false);
                    return;
                }

                AnimationHelperInfo.MapHumanBones(animator);

                Undo.undoRedoPerformed -= OnUndoRedo;
                Undo.undoRedoPerformed += OnUndoRedo;

                SceneView.duringSceneGui -= DuringSceneGUI;
                SceneView.duringSceneGui += DuringSceneGUI;

                isFoldoutInfo = true;
                isFoldoutMode = true;

                isSceneHandles = EditorPrefs.GetBool("LuceedStudio.BonesAssistant.IsSceneHandles", isSceneHandles);
                isSceneHandlesInfo = EditorPrefs.GetBool("LuceedStudio.BonesAssistant.IsSceneHandlesInfo", isSceneHandlesInfo);
                isModeInfos = EditorPrefs.GetBool("LuceedStudio.BonesAssistant.IsModeInfos", true);
                AnimationHelperInfo.CurrentRootCorrectionMode = EditorPrefs.GetInt("LuceedStudio.BonesAssistant.RootCorrectionMode", 1);
            }
            else
            {
                AnimationHelperInfo.ClearHumanBones();

                Undo.undoRedoPerformed -= OnUndoRedo;
                EditorApplication.update -= OnEditorUpdate;

                if (animationWindow != null)
                {
                    animationWindow.previewing = false;
                    animationWindow.recording = false;
                    animationWindow = null;
                }

                Tools.hidden = false;
            }

            currentParentAnimator = null;
            AnimationHelperInfo.CurrentMirrorMode = 0;
            AnimationHelperInfo.CurrentKeyframeMode = 0;
            AnimationHelperInfo.IsFoldoutPose = AnimationHelperInfo.IsCurrentClipEmpty();

            AnimationHelperInfo.UpdateOverlayVisibility(state, animationWindow, clip, animator);

            GetAnimationReferences();
        }

        private void DuringSceneGUI(SceneView view)
        {
            if (animator == null || !animator.isHuman || !animationInit || !isSceneHandles)
            {
                return;
            }

            Transform currentBone = AnimationHelperInfo.CurrentlyEditingBone;
            if (currentBone != null)
            {
                Vector3 currentBonePosition = currentBone.position;
                float size = HandleUtility.GetHandleSize(currentBonePosition);

                //Store mirror bone info in case of force both side
                Transform currentMirrorBone = AnimationHelperInfo.CurrentlyEditingMirrorBone;
                Vector3 currentMirrorBonePosition = Vector3.zero;
                if (currentMirrorBone != null)
                {
                    currentMirrorBonePosition = currentMirrorBone.position;
                }

                bool canMirror = AnimationHelperInfo.HasMirrorBone && AnimationHelperInfo.CurrentMirrorMode == 1;
                bool canBothSide = AnimationHelperInfo.HasMirrorBone && AnimationHelperInfo.CurrentMirrorMode == 2 && currentMirrorBone != null;

                Vector3 cameraAxis = view.camera.transform.forward;
                Color outlineColor = Color.black;
                outlineColor.a = 0.35f;
                int count = AnimationHelperInfo.CurrentMuscleIndexes.Count;

                if (isSceneHandlesInfo)
                {
                    string boneName = currentBone.name;

                    Texture mirrorTexture = null;
                    if (canMirror)
                    {
                        mirrorTexture = EditorGUIUtility.FindTexture("d_Mirror");
                    }

                    Texture keyframeTexture = null;
                    if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                    {
                        keyframeTexture = EditorGUIUtility.FindTexture("d_animationkeyframe");
                    }

                    string infoName = "Modify current keyframe";

                    if (canMirror)
                    {
                        if (AnimationHelperInfo.CurrentKeyframeMode == 0)
                        {
                            infoName = "Modify mirror keyframe";
                        }
                        else
                        {
                            infoName = "Offset all mirror property keyframes";
                        }
                    }
                    else if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                    {
                        infoName = "Offset all property keyframes";
                    }

                    DrawHandleInformation(currentBonePosition, size, boneName, infoName, mirrorTexture, keyframeTexture);

                    //If Mirror mode or Both side mode
                    if (canBothSide)
                    {
                        currentMirrorBonePosition = currentMirrorBone.position;

                        mirrorTexture = EditorGUIUtility.FindTexture("d_Mirror");
                        string mirrorBoneName = currentMirrorBone.name;
                        string mirrorInfoName = "& Modify mirror keyframe";
                        if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                        {
                            mirrorInfoName = "& Offset all mirror property keyframes";
                        }

                        DrawHandleInformation(currentMirrorBonePosition, size, mirrorBoneName, mirrorInfoName, mirrorTexture, keyframeTexture, true);
                    }
                }

                //Root position handles
                if (AnimationHelperInfo.CurrentIsHips)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (!AnimationHelperInfo.CurrentAnimatorIsRootMotion && (i == 0 || i == 2))
                        {
                            continue;
                        }

                        //Axis
                        string posPropertyName = "RootT." + "xyz"[i];
                        string rotPropertyName = "RootQ." + "xyz"[i];
                        Vector3 worldAxis = Vector3.right;
                        Vector3 axis = currentBone.right;
                        if (i == 0)
                        {
                            Handles.color = Handles.xAxisColor;
                        }
                        else if (i == 1)
                        {
                            worldAxis = Vector3.up;
                            axis = currentBone.up;

                            Handles.color = Handles.yAxisColor;
                        }
                        else if (i == 2)
                        {
                            worldAxis = Vector3.forward;
                            axis = currentBone.forward;

                            Handles.color = Handles.zAxisColor;
                        }

                        EditorGUI.BeginChangeCheck();
                        Vector3 newPosition = currentBonePosition;
                        newPosition = Handles.Slider(i + 1, newPosition, worldAxis, size * 0.8f, Handles.ArrowHandleCap, 0.1f);
                        if (EditorGUI.EndChangeCheck())
                        {
                            float value = newPosition[i];

                            if (AnimationHelperInfo.CurrentKeyframeMode == 0)
                            {
                                AnimationHelperInfo.OffsetKeyframe(0, value, i, posPropertyName);
                            }
                            else if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                            {
                                AnimationHelperInfo.OffsetAllKeyframes(0, value, i, posPropertyName, setOffsetValue: true);
                            }

                            AnimationHelperInfo.UpdateCurrentSliderValues();
                            Repaint();
                        }

                        if (AnimationHelperInfo.CurrentAnimatorIsRootMotion)
                        {
                            EditorGUI.BeginChangeCheck();
                            Quaternion rot = Quaternion.identity;
                            rot = Handles.Disc(i + 11, rot, currentBonePosition, axis, size, true, 0.1f);
                            if (EditorGUI.EndChangeCheck())
                            {
                                float value = rot[i];
                                //Debug.Log("rot: " + rot + " - value: " + value);

                                if (AnimationHelperInfo.CurrentKeyframeMode == 0)
                                {
                                    AnimationHelperInfo.OffsetKeyframe(0, value, i + 3, rotPropertyName);
                                }
                                else if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                                {
                                    AnimationHelperInfo.OffsetAllKeyframes(0, value, i + 3, rotPropertyName, setOffsetValue: true);
                                }

                                AnimationHelperInfo.UpdateCurrentSliderValues();
                                Repaint();
                            }
                        }
                    }

                    //Black rotation handle outline
                    Handles.color = outlineColor;
                    Handles.DrawWireDisc(currentBonePosition, cameraAxis, size, 2f);
                }
                else
                {
                    if (AnimationHelperInfo.CurrentKeyframeMode == 0)
                    {
                        //Human pose version
                        HumanPose editedPose = new HumanPose();
                        Quaternion prevRotation = AnimationHelperInfo.CurrentBoneRotation;
                        Quaternion rotation = currentBone.rotation;

                        EditorGUI.BeginChangeCheck();
                        rotation = Handles.RotationHandle(rotation, currentBonePosition);
                        if (EditorGUI.EndChangeCheck())
                        {
                            currentBone.rotation = rotation;

                            //Update pose
                            AnimationHelperInfo.GetEditGameObjectHumanPose(ref editedPose);
                            for (int i = 0; i < count; i++)
                            {
                                int currentMuscleIndex = AnimationHelperInfo.CurrentMuscleIndexes[i];
                                if (currentMuscleIndex < 0)
                                {
                                    continue;
                                }

                                if (canMirror)
                                {
                                    currentMuscleIndex = AnimationHelperInfo.GetMirrorMuscleIndex(currentMuscleIndex, i);
                                }

                                float muscleValue = editedPose.muscles[currentMuscleIndex];
                                muscleValue = Mathf.Clamp(muscleValue, -1f, 1f);

                                if (AnimationHelperInfo.CurrentKeyframeMode == 0)
                                {
                                    AnimationHelperInfo.SetKeyframe(currentMuscleIndex, muscleValue, i, forceNoMirror: true);
                                }
                                else
                                {
                                    Quaternion rotOffset = Quaternion.Inverse(prevRotation) * rotation;
                                    muscleValue = -rotOffset[i];
                                    AnimationHelperInfo.OffsetAllKeyframes(currentMuscleIndex, muscleValue, i, setOffsetValue: true, forceNoMirror: true);
                                }

                            }

                            AnimationHelperInfo.UpdateCurrentSliderValues();

                            if (!AnimationHelperInfo.CurrentIsHips && AnimationHelperInfo.CurrentRootCorrectionMode == 1)
                            {
                                AnimationHelperInfo.UpdateRootValues();
                                AnimationHelperInfo.CorrectRootPositionAndRotation();
                            }

                            Repaint();
                        }

                        //If force both side, draw fake greyed disc on the mirror side
                        if (canBothSide)
                        {
                            Color mirrorDiscColor = Color.grey;
                            mirrorDiscColor.a = 0.7f;
                            Handles.color = mirrorDiscColor;

                            for (int i = 0; i < 3; i++)
                            {
                                Vector3 axis = currentMirrorBone.right;
                                if (i == 1)
                                {
                                    axis = currentMirrorBone.up;
                                }
                                else if (i == 2)
                                {
                                    axis = currentMirrorBone.forward;
                                }

                                Handles.Disc(-1, Quaternion.identity, currentMirrorBonePosition, axis, size, true, 0.1f);
                            }

                            //handle outline
                            mirrorDiscColor.a = 0.35f;
                            Handles.color = mirrorDiscColor;
                            Handles.DrawWireDisc(currentMirrorBonePosition, cameraAxis, size, 2f);
                        }
                    }
                }
                
            }
        }

        private void DrawHandleInformation(Vector3 position, float size, string mainText, string infoText = "", Texture firstIcon = null, Texture secondIcon = null, bool isBothSide = false)
        {
            bool hasInfo = infoText != "";

            Vector3 labelPos = position + (Vector3.up * (size * 1.25f));

            GUIStyle handleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };

            GUIStyle infoLabelStyle = new GUIStyle(handleLabelStyle)
            {
                fontStyle = FontStyle.Italic,
                fontSize = 10
            };

            if (isBothSide)
            {
                handleLabelStyle.normal.textColor = Color.grey;
                infoLabelStyle.normal.textColor = Color.grey;
            }

            Handles.Label(labelPos, mainText, handleLabelStyle);

            if (hasInfo)
            {
                Vector3 labelInfoPos = labelPos;
                labelInfoPos += Vector3.up * (size * 0.25f);

                Handles.Label(labelInfoPos, infoText, infoLabelStyle);
            }

            if (firstIcon != null)
            {
                Vector3 labelIconPos = labelPos;
                float upOffset = hasInfo ? 0.5f : 0.25f;
                labelIconPos += Vector3.up * (size * upOffset);

                if (secondIcon != null)
                {
                    labelIconPos += Vector3.right * (size * 0.1f);
                    Handles.Label(labelIconPos, firstIcon);

                    labelIconPos += -Vector3.right * (size * 0.2f);
                    Handles.Label(labelIconPos, secondIcon);
                }
                else
                {
                    Handles.Label(labelIconPos, firstIcon);
                }
            }
            else if (secondIcon != null)
            {
                Vector3 labelIconPos = labelPos;
                float upOffset = hasInfo ? 0.5f : 0.25f;
                labelIconPos += Vector3.up * (size * upOffset);

                Handles.Label(labelIconPos, secondIcon);
            }

            if (!AnimationHelperInfo.CurrentIsHips && AnimationHelperInfo.CurrentKeyframeMode == 1)
            {
                Vector3 labelOffsetPos = labelPos;
                labelOffsetPos += Vector3.down * (size * 0.4f);
                labelOffsetPos += Vector3.right * (size * 0.8f);

                GUIStyle labelOffsetStyle = new GUIStyle(handleLabelStyle)
                {
                    fontStyle = FontStyle.Normal,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };

                GUIContent labelOffsetContent = new GUIContent(EditorGUIUtility.IconContent("d_console.warnicon.inactive.sml"));
                labelOffsetContent.text = "Please use sliders\nin global offset mode";

                Handles.Label(labelOffsetPos, labelOffsetContent, labelOffsetStyle);
            }
        }

        private void OnGUI()
        {
            if (animationInit)
            {
                if (animator == null || animationWindow == null || clip == null)
                {
                    UpdateEditAnimationState(false);
                }
            }

            //Header
            Rect headerRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 30);
            EditorGUI.LabelField(headerRect, "Animation Helper Window", GUIUtils.LabelCenterBold);
            EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 1f, 0.1f));
            GUILayout.Space(30);
            GUIUtils.DrawUILine(padding:0);

            GUILayout.Space(10);

            //Animation References
            EditorGUILayout.LabelField("Animation References", GUIUtils.LabelBold);
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (animator != null)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Animator");
                        GUI.enabled = false;
                        EditorGUILayout.ObjectField(animator, typeof(Animator), false);
                        GUI.enabled = true;
                    }

                    GUILayout.Space(5);

                    if (clip != null)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Animation clip");
                            GUI.enabled = false;
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                            GUI.enabled = true;
                        }

                        //Choose clip in list
                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("Choose a clip from the clips list: ");

                            string[] clips = new string[animator.runtimeAnimatorController.animationClips.Length];
                            for (int i = 0; i < animator.runtimeAnimatorController.animationClips.Length; i++)
                            {
                                clips[i] = animator.runtimeAnimatorController.animationClips[i].name;
                            }

                            EditorGUI.BeginChangeCheck();
                            clipIndex = EditorGUILayout.Popup(clipIndex, clips);
                            if (EditorGUI.EndChangeCheck())
                            {
                                AnimationClip selectedClip = animator.runtimeAnimatorController.animationClips[clipIndex];
                                animationWindow.animationClip = selectedClip;
                                OnAnimationWindowClipChanged();

                                Repaint();
                            }
                        }

                        if (clipReadOnly)
                        {
                            EditorGUILayout.HelpBox("Wait! This clip is read-only, you have to duplicate it so you can edit it.", MessageType.Error);
                            if (GUILayout.Button("Click here to duplicate this clip"))
                            {
                                AnimationClip newClip = AnimationHelperInfo.DuplicateClipAndAddToController(clip, animator);
                                animationWindow.animationClip = newClip;
                                OnAnimationWindowClipChanged();

                                Repaint();
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Animation clip");

                        if (animationWindow == null)
                        {
                            EditorGUILayout.HelpBox("No animation window. You need to have an animation window opened.", MessageType.Error);
                            if (GUILayout.Button("Click here to create an animation window"))
                            {
                                GetAnimationWindow(true);

                                if (!animationInit)
                                {
                                    GetAnimationReferences();
                                }
                                Repaint();
                            }
                        }
                        else
                        {
                            if (animationWindow.animationClip == null)
                            {
                                if (noController)
                                {
                                    EditorGUILayout.HelpBox("No animator controller. If you forgot to add it to the animator component, come back here when you added it.", MessageType.Error);
                                    if (GUILayout.Button("If you don't have any, Click here to create an animator controller"))
                                    {
                                        AnimatorController controller = AnimationHelperInfo.CreateAnimatorController();
                                        animator.runtimeAnimatorController = controller;

                                        if (!animationInit)
                                        {
                                            GetAnimationReferences();
                                        }
                                        Repaint();
                                    }
                                }
                                else
                                {
                                    AnimationClip firstClip = animator.runtimeAnimatorController.animationClips[0];
                                    if (firstClip != null)
                                    {
                                        clip = firstClip;
                                        animationWindow.animationClip = clip;
                                        Repaint();
                                    }
                                    else
                                    {
                                        EditorGUILayout.HelpBox("No animation clip. If you forgot to add it to the animator controller, come back here when you added it.", MessageType.Error);
                                        if (GUILayout.Button("If you don't have any, Click here to create an animation clip"))
                                        {
                                            clip = AnimationHelperInfo.CreateAnimationClip();
                                            AnimationHelperInfo.AddClipToController(animator, clip);
                                            animationWindow.animationClip = clip;

                                            if (!animationInit)
                                            {
                                                GetAnimationReferences();
                                            }
                                            Repaint();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Animator");
                    EditorGUILayout.HelpBox("Please select a gameobject with an animator component.", MessageType.Error);

                    if (currentParentAnimator == null)
                    {
                        if (Selection.activeGameObject != null)
                        {
                            Transform currentSelectedChild = Selection.activeGameObject.transform;
                            currentParentAnimator = AnimationHelperInfo.FindComponentInParent<Animator>(currentSelectedChild);
                        }
                    }

                    if (currentParentAnimator != null)
                    {
                        GameObject currentParentGameObject = currentParentAnimator.gameObject;
                        if (GUILayout.Button("Do you wish to select the " + currentParentGameObject.name + " animator?"))
                        {
                            Selection.activeGameObject = currentParentGameObject;
                        }
                    }
                }
            }

            GUIUtils.DrawUILine();

            readyToEdit = animator != null && clip != null && !clipReadOnly;

            string clipName = "[clip not found]";
            if (clip != null)
            {
                clipName = "'" + clip.name + "'";
            }

            GUIStyle bigButtonStyle = GUI.skin.button;
            bigButtonStyle.fontStyle = FontStyle.Bold;

            if (!animationInit)
            {
                EditorGUILayout.LabelField("Animation", GUIUtils.LabelBold);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (!readyToEdit)
                    {
                        string fixString = "Please fix all errors above to edit animation.";
                        EditorGUILayout.HelpBox(fixString, MessageType.Error);

                        GUI.enabled = false;
                    }

                    string buttonString = "Edit " + clipName + " animation clip";
                    if (GUILayout.Button(buttonString, bigButtonStyle, GUILayout.Height(30)))
                    {
                        if (readyToEdit)
                        {
                            UpdateEditAnimationState(true);
                        }
                    }

                    GUI.enabled = true;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Humanoid Animation", GUIUtils.LabelBold);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    //Info box
                    GUIUtils.DrawUILine(Color.black, 1, -1, 0);
                    isFoldoutInfo = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldoutInfo, "Info", GUIUtils.FoldoutLabel);
                    GUIUtils.DrawUILine(GUIUtils.SubtleBlack, 1, -5, 0);
                    if (isFoldoutInfo)
                    {
                        GUILayout.Space(5);
                        EditorGUILayout.HelpBox(GetCurrentInfoString(), MessageType.Info);

                        if (AnimationHelperInfo.CurrentClipIsMirror)
                        {
                            GUILayout.Space(5);
                            EditorGUILayout.HelpBox("This animation clip is mirrored, we might modify mirrored parameters to help you.\nI do not suggest you to animate an already mirrored clip.", MessageType.Warning);
                        }
                    }
                    GUILayout.Space(5);
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    if (animator.isHuman)
                    {
                        //Tool box
                        GUIUtils.DrawUILine(Color.black, 1, -1, 0);
                        isFoldoutTool = true;
                        isFoldoutTool = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldoutTool, "Tool", GUIUtils.FoldoutLabel);
                        GUIUtils.DrawUILine(GUIUtils.SubtleBlack, 1, -5, 0);
                        if (isFoldoutTool)
                        {
                            GUILayout.Space(5);

                            using (new GUILayout.HorizontalScope())
                            {
                                GUIContent rotateHandleContent = new GUIContent(EditorGUIUtility.IconContent("d_RotateTool On"));
                                rotateHandleContent.text = " Scene handles";

                                EditorGUI.BeginChangeCheck();
                                isSceneHandles = GUILayout.Toggle(isSceneHandles, rotateHandleContent, GUI.skin.button);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    SceneView.lastActiveSceneView.Repaint();
                                    EditorPrefs.SetBool("LuceedStudio.BonesAssistant.IsSceneHandles", isSceneHandles);
                                }

                                if (!isSceneHandles)
                                {
                                    GUI.enabled = false;
                                }
                                GUIContent handleInfoContent = new GUIContent(EditorGUIUtility.IconContent("Info"));
                                handleInfoContent.text = " Handles infos";

                                EditorGUI.BeginChangeCheck();
                                isSceneHandlesInfo = GUILayout.Toggle(isSceneHandlesInfo, handleInfoContent, GUI.skin.button);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    SceneView.lastActiveSceneView.Repaint();
                                    EditorPrefs.SetBool("LuceedStudio.BonesAssistant.IsSceneHandlesInfo", isSceneHandlesInfo);
                                }

                                GUI.enabled = true;
                            }

                        }
                        GUILayout.Space(5);
                        EditorGUILayout.EndFoldoutHeaderGroup();

                        //Mode box
                        GUIUtils.DrawUILine(Color.black, 1, -1, 0);
                        isFoldoutMode = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldoutMode, "Modes", GUIUtils.FoldoutLabel);
                        if (AnimationHelperInfo.CurrentMirrorMode != 0 || AnimationHelperInfo.CurrentKeyframeMode != 0)
                        {
                            isFoldoutMode = true;
                        }
                        GUIUtils.DrawUILine(GUIUtils.SubtleBlack, 1, -5, 0);
                        if (isFoldoutMode)
                        {
                            GUIContent modeInfosContent = new GUIContent(EditorGUIUtility.IconContent("Info"));

                            using (new GUILayout.HorizontalScope())
                            {
                                GUILayout.FlexibleSpace();

                                EditorGUI.BeginChangeCheck();
                                isModeInfos = GUILayout.Toggle(isModeInfos, modeInfosContent, GUI.skin.button);
                                if (EditorGUI.EndChangeCheck())
                                {
                                    EditorPrefs.SetBool("LuceedStudio.BonesAssistant.IsModeInfos", isModeInfos);
                                    Repaint();
                                }
                            }

                            GUILayout.Space(5);

                            using (new GUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Mirror mode: ", GUILayout.Width(150));

                                EditorGUI.BeginChangeCheck();
                                AnimationHelperInfo.CurrentMirrorMode = GUILayout.Toolbar(AnimationHelperInfo.CurrentMirrorMode, new string[] { "Default", "Mirror", "Both sides" });
                                if (EditorGUI.EndChangeCheck())
                                {
                                    AnimationHelperInfo.UpdateCurrentBoneInfo();
                                    AnimationHelperInfo.UpdateCurrentSliderValues();
                                    AnimationHelperInfo.UpdateGlobalOffsetValues();
                                }
                            }

                            string mirrorModeInfo = "Mirror Default mode is the normal intuitive mode: Selected bones values will be edited.";
                            MessageType mirrorMessageType = MessageType.Info;
                            if (AnimationHelperInfo.CurrentMirrorMode == 1)
                            {
                                mirrorModeInfo = "Mirror mode is self explanatory: Other side bones values will be edited.";
                                mirrorMessageType = MessageType.Warning;
                            }
                            else if (AnimationHelperInfo.CurrentMirrorMode == 2)
                            {
                                mirrorModeInfo = "Both sides mode means selected bones AND other side bones values will be edited. That way you can do both at the same time.";
                                mirrorMessageType = MessageType.Warning;
                            }

                            if (isModeInfos)
                            {
                                EditorGUILayout.HelpBox(mirrorModeInfo, mirrorMessageType);
                            }

                            GUILayout.Space(10);

                            using (new GUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Root correction mode: ", GUILayout.Width(150));

                                EditorGUI.BeginChangeCheck();
                                AnimationHelperInfo.CurrentRootCorrectionMode = GUILayout.Toolbar(AnimationHelperInfo.CurrentRootCorrectionMode, new string[] { "None", "Correction" });
                                if (EditorGUI.EndChangeCheck())
                                {
                                    EditorPrefs.SetInt("LuceedStudio.BonesAssistant.RootCorrectionMode", AnimationHelperInfo.CurrentRootCorrectionMode);
                                    Repaint();
                                }
                            }

                            string rootCorrectionModeInfo = "None Root correction mode is a mode where root is not corrected at all. Not recommended for humanoid animations.";
                            MessageType rootCorrectionMessageType = MessageType.Warning;
                            if (AnimationHelperInfo.CurrentRootCorrectionMode == 1)
                            {
                                rootCorrectionModeInfo = "Root Correction mode is a mode where the root is corrected to stay in place when you rotate other bones.";
                                rootCorrectionMessageType = MessageType.Info;
                            }

                            if (isModeInfos)
                            {
                                EditorGUILayout.HelpBox(rootCorrectionModeInfo, rootCorrectionMessageType);
                            }

                            GUILayout.Space(10);

                            using (new GUILayout.HorizontalScope())
                            {
                                EditorGUILayout.LabelField("Keyframe mode: ", GUILayout.Width(150));

                                EditorGUI.BeginChangeCheck();
                                AnimationHelperInfo.CurrentKeyframeMode = GUILayout.Toolbar(AnimationHelperInfo.CurrentKeyframeMode, new string[] { "Default", "Global Offset" });
                                if (EditorGUI.EndChangeCheck())
                                {
                                    AnimationHelperInfo.UpdateCurrentSliderValues();
                                    AnimationHelperInfo.UpdateGlobalOffsetValues();
                                }
                            }

                            string keyframeModeInfo = "Keyframe Default mode is the normal intuitive mode: Keyframe on your animation timeline position will be (absolutely) edited/created.";
                            MessageType keyframeMessageType = MessageType.Info;
                            if (AnimationHelperInfo.CurrentKeyframeMode == 1)
                            {
                                keyframeModeInfo = "Keyframe Global offset mode means ALL the keyframes of the selected property will be (relatively) offseted.";
                                keyframeMessageType = MessageType.Warning;
                            }

                            if (isModeInfos)
                            {
                                EditorGUILayout.HelpBox(keyframeModeInfo, keyframeMessageType);
                            }
                        }
                        GUILayout.Space(5);
                        EditorGUILayout.EndFoldoutHeaderGroup();

                        //Bone
                        bool hasBone = AnimationHelperInfo.CurrentSelectedBone != null;
                        if (!hasBone)
                        {
                            GUI.enabled = false;
                            isFoldoutBone = true;
                        }

                        GUIUtils.DrawUILine(Color.black, 1, -1, 0);
                        isFoldoutBone = EditorGUILayout.BeginFoldoutHeaderGroup(isFoldoutBone, "Bone sliders", GUIUtils.FoldoutLabel);
                        GUIUtils.DrawUILine(GUIUtils.SubtleBlack, 1, -5, 0);
                        if (isFoldoutBone)
                        {
                            GUILayout.Space(5);

                            EditorGUI.indentLevel++;
                            if (hasBone)
                            {
                                if (AnimationHelperInfo.CurrentIsHips && !AnimationHelperInfo.CurrentAnimatorIsRootMotion)
                                {
                                    EditorGUILayout.HelpBox("Root motion is not enabled, you cannot modify root position and rotation values.\n" +
                                            "You can just modify Root Y Position.\n" +
                                            "Enable 'Apply Root Motion' on your animator component to access other properties.", MessageType.Error);
                                }

                                AnimationHelperInfo.DrawBoneSlidersSection(GUIUtils.SubtleBlack);
                            }
                            else
                            {
                                EditorGUILayout.LabelField("No Bone selected.", GUIUtils.LabelBold);
                            }
                            EditorGUI.indentLevel--;
                        }
                        GUILayout.Space(5);
                        EditorGUILayout.EndFoldoutHeaderGroup();
                        GUI.enabled = true;

                        //Pose
                        GUIUtils.DrawUILine(Color.black, 1, -1, 0);
                        AnimationHelperInfo.IsFoldoutPose = EditorGUILayout.BeginFoldoutHeaderGroup(AnimationHelperInfo.IsFoldoutPose, "Pose preset", GUIUtils.FoldoutLabel);
                        GUIUtils.DrawUILine(GUIUtils.SubtleBlack, 1, -5, 0);
                        if (AnimationHelperInfo.IsFoldoutPose)
                        {
                            GUILayout.Space(5);

                            EditorGUI.indentLevel++;
                            bool isClipEmpty = AnimationHelperInfo.IsCurrentClipEmpty();
                            GUIStyle buttonStyle = GUIUtils.GetIndentButtonStyle(EditorGUI.indentLevel);

                            BonesAssistantResources resInst = BonesAssistantResources.Instance;

                            //Fix renaming list
                            if (resInst.SavedPoses.Count != isRenamings.Count)
                            {
                                isRenamings.Clear();
                                for (int i = 0; i < resInst.SavedPoses.Count; i++)
                                {
                                    isRenamings.Add(false);
                                }
                            }

                            if (resInst != null)
                            {
                                EditorGUI.indentLevel--;
                                DrawButtonsGrid(BonesAssistantResources.Instance);
                                EditorGUI.indentLevel++;
                            }
                            EditorGUI.indentLevel--;
                        }
                        GUILayout.Space(5);
                        EditorGUILayout.EndFoldoutHeaderGroup();
                    }
                }

                GUILayout.Space(10);

                EditorGUILayout.LabelField("Animation", GUIUtils.LabelBold);
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    string buttonString = "Stop editing '" + clip.name + "' animation clip";
                    if (GUILayout.Button(buttonString, bigButtonStyle, GUILayout.Height(30)))
                    {
                        UpdateEditAnimationState(false);
                    }
                }
            }
        }

        private void DrawButtonsGrid(BonesAssistantResources resInst)
        {
            float BTN_WIDTH = 100;
            float width = EditorGUIUtility.currentViewWidth;
            int count = resInst.SavedPoses.Count + 3;
            int colCount = Mathf.FloorToInt((float)width / BTN_WIDTH) - 1;
            colCount = Mathf.Clamp(colCount, 1, count);
            float space = (width - ((BTN_WIDTH + 10) * colCount)) / 2;
            int currentRow = 0;

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (i == (colCount * currentRow))
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(space);
                    }

                    if (i == 0)
                    {
                        DrawButton(-1, "TPose", () => AnimationHelperInfo.SetHumanoidPose());
                    }
                    else if (i == 1)
                    {
                        DrawButton(-2, "Prefab Pose", () => AnimationHelperInfo.SetHumanoidPose(true));
                    }
                    else if (i == count - 1)
                    {
                        DrawButton(-3, "Save Pose", () => AnimationHelperInfo.SaveCurrentPose());
                    }
                    else
                    {
                        int index = i - 2;
                        string poseCustomName = resInst.SavedPoses[index].Name;

                        DrawButton(index, poseCustomName);
                    }

                    if (i == colCount * (currentRow + 1) - 1)
                    {
                        GUILayout.EndHorizontal();
                        currentRow++;
                    }
                }
                if (count > colCount && colCount != 1 && count != colCount * currentRow)
                {
                    GUILayout.EndHorizontal();
                }
            }

            void DrawButton(int index, string name, Action callback = null)
            {
                Color initialGuiColor = GUI.color;
                Color guiColor = initialGuiColor;
                if (index == -1 || index == -2)
                {
                    guiColor = new Color(0f, 0.2f, 1f);
                }
                else if (index == -3)
                {
                    guiColor = new Color(0f, 1f, 0.2f);
                }

                GUI.color = guiColor;

                using (new GUILayout.HorizontalScope(GUI.skin.box, GUILayout.Width(BTN_WIDTH)))
                {
                    GUI.color = initialGuiColor;

                    using (new GUILayout.VerticalScope())
                    {
                        if (index < 0)
                        {
                            GUIContent nameContent = null;
                            if (index == -1)
                            {
                                nameContent = new GUIContent(EditorGUIUtility.IconContent("TextMesh Icon"));
                                nameContent.text = name;
                            }
                            else if (index == -2)
                            {
                                nameContent = new GUIContent(EditorGUIUtility.IconContent("d_Prefab Icon"));
                                nameContent.text = name;
                            }
                            else if (index == -3)
                            {
                                nameContent = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus"));
                                nameContent.text = "New pose";
                            }

                            EditorGUILayout.LabelField(nameContent, GUILayout.Width(BTN_WIDTH), GUILayout.Height(20));
                        }
                        else
                        {
                            if (isRenamings[index])
                            {
                                currentRenamingName = EditorGUILayout.TextField(currentRenamingName, GUILayout.Width(BTN_WIDTH), GUILayout.Height(20));

                                Event currentEvent = Event.current;
                                bool clickOutside = (!GUILayoutUtility.GetLastRect().Contains(currentEvent.mousePosition) && currentEvent.type == EventType.MouseDown);
                                bool keyEnter = (currentEvent.isKey && currentEvent.keyCode == KeyCode.Return);
                                if (clickOutside || keyEnter)
                                {
                                    if (currentRenamingName != name)
                                    {
                                        resInst.RenamePose(index, currentRenamingName);
                                    }

                                    isRenamings[index] = false;
                                    currentRenamingName = "";
                                    Repaint();
                                }
                            }
                            else
                            {
                                GUIContent nameContent = new GUIContent(EditorGUIUtility.IconContent("Custom"));
                                nameContent.text = name;

                                if (GUILayout.Button(nameContent, EditorStyles.boldLabel, GUILayout.Width(BTN_WIDTH), GUILayout.Height(20)))
                                {
                                    for (int r = 0; r < isRenamings.Count; r++)
                                    {
                                        isRenamings[r] = false;
                                    }

                                    currentRenamingName = name;
                                    isRenamings[index] = true;
                                }
                            }
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUIUtility.SetIconSize(new Vector2(20, 20));
                            int loadBtnWidth = 100;
                            GUIContent loadContent = new GUIContent(EditorGUIUtility.IconContent("d_Avatar Icon"));
                            if (index == -3)
                            {
                                loadContent = new GUIContent(EditorGUIUtility.IconContent("d_SaveAs"));
                            }
                            else if (index >= 0)
                            {
                                loadBtnWidth = 75;
                                GUILayout.FlexibleSpace();
                                loadContent = new GUIContent(EditorGUIUtility.IconContent("d_AvatarMask On Icon"));
                            }

                            loadContent.text = " Load pose";
                            loadContent.tooltip = "Load " + name + " pose";

                            if (index == -3)
                            {
                                loadContent.text = " Save pose";
                                loadContent.tooltip = "Save current pose";
                            }

                            GUIStyle loadBtnStyle = new GUIStyle(GUI.skin.button);
                            loadBtnStyle.wordWrap = true;
                            if (GUILayout.Button(loadContent, loadBtnStyle, GUILayout.Width(loadBtnWidth), GUILayout.Height(45)))
                            {
                                if (callback != null)
                                {
                                    callback();
                                }
                                else
                                {
                                    AnimationHelperInfo.LoadPose(index);
                                }
                            }
                            EditorGUIUtility.SetIconSize(Vector2.zero);

                            if (index >= 0)
                            {
                                GUIContent dropdownContent = new GUIContent(EditorGUIUtility.IconContent("d_icon dropdown"));
                                if (GUILayout.Button(dropdownContent, GUILayout.Width(25), GUILayout.Height(45)))
                                {
                                    GenericMenu sceneMenu = new GenericMenu();

                                    GUIContent selectContent = new GUIContent("Override pose");
                                    sceneMenu.AddItem(selectContent, false, () => AnimationHelperInfo.OverridePose(index));

                                    sceneMenu.AddSeparator("");

                                    GUIContent removeContent = new GUIContent("Remove pose");
                                    sceneMenu.AddItem(removeContent, false, () =>
                                    {
                                        resInst.DeletePose(index);
                                        isRenamings.RemoveAt(index);
                                    });

                                    sceneMenu.ShowAsContext();
                                }

                                GUILayout.FlexibleSpace();
                            }
                        }
                    }
                }
            }
        }

        private string GetCurrentInfoString()
        {
            string infoString = "";

            if (animator.isHuman)
            {
                if (AnimationHelperInfo.CurrentSelectedBone != null)
                {
                    infoString = "Use sliders below to animate your character.";
                }
                else
                {
                    infoString = "Select any bones to show related animation sliders.\nUse Bones Viewer overlay for easier bones selection.";
                }
            }
            else
            {
                infoString = "This is not a humanoid, you don't have to use this window to animate it.";
            }

            return infoString;
        }
    }
}

