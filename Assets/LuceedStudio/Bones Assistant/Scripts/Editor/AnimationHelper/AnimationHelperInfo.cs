// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using LuceedStudio_Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LuceedStudio_BonesAssistant
{
    public static class AnimationHelperInfo
    {
        public static bool IsHumanoidAnimationWindow    { get; private set; } = false;
        public static AnimationWindow AnimationWindow   { get; private set; } = null;
        public static AnimationClip CurrentClip         { get; private set; } = null;
        public static Animator CurrentAnimator          { get; private set; } = null;
        public static Transform CurrentSelectedBone     { get; private set; } = null;
        public static List<int> CurrentMuscleIndexes    { get; private set; } = new List<int>();
        public static Transform CurrentRootBone         { get; private set; } = null;
        public static Vector3 CurrentRootBonePosition   { get; private set; } = Vector3.zero;
        public static Quaternion CurrentRootBoneRotation{ get; private set; } = Quaternion.identity;

        public static Transform CurrentlyEditingBone        { get; private set; } = null;
        public static Transform CurrentlyEditingMirrorBone  { get; private set; } = null;
        public static HumanBodyBones CurrentHumanBone;
        public static int CurrentHumanBoneIndex = 0;

        public static int CurrentMirrorMode = 0;
        public static int CurrentKeyframeMode = 0;
        public static int CurrentRootCorrectionMode = 1;
        public static bool HasMirrorBone                { get; private set; } = false;
        private static bool IsForceMirror               { get { return CurrentMirrorMode == 1 ? true : false; } }
        private static bool IsForceBothSide             { get { return CurrentMirrorMode == 2 ? true : false; } }
        public static bool IsFoldoutPose = false;

        public static Dictionary<Transform, HumanBodyBones> HumanBones = new Dictionary<Transform, HumanBodyBones>();
        public static Vector3 MuscleValues = Vector3.zero;
        public static List<List<float>> CurrentKeyframesValues = new List<List<float>>();
        public static List<List<float>> CurrentMirrorKeyframesValues = new List<List<float>>();
        public static List<List<float>> CurrentLeftHandFingersValues = new List<List<float>>();
        public static List<List<float>> CurrentRightHandFingersValues = new List<List<float>>();
        public static List<List<float>> CurrentRootPositionsValues = new List<List<float>>();
        public static List<List<float>> CurrentRootRotationsValues = new List<List<float>>();
        public static Vector3 OffsetValues = Vector3.zero;
        public static Vector3 OffsetSecondValues = Vector3.zero;
        public static Vector2 LeftHandFingersValues = Vector2.zero;
        public static Vector2 RightHandFingersValues = Vector2.zero;
        public static Vector3 RootPositionValues = Vector3.zero;
        public static Vector3 RootRotationValues = Vector3.zero;
        public static Quaternion CurrentBoneRotation = Quaternion.identity;
        public static bool CurrentIsHips = false;
        public static bool CurrentClipIsMirror = false;
        public static bool CurrentAnimatorIsRootMotion = false;
        private static bool mustRootCorrection = false;

        private static Animator currentAnimator = null;
        private static Avatar currentAvatar = null;

        public static void UpdateOverlayVisibility(bool visible, AnimationWindow aw = null, AnimationClip clip = null, Animator animator = null)
        {
            AnimationWindow = aw;
            CurrentClip = clip;
            CurrentAnimator = animator;

            IsHumanoidAnimationWindow = visible;

            //Current clip is mirror
            if (CurrentClip != null)
            {
                AnimationClipSettings clipSettings = AnimationUtility.GetAnimationClipSettings(CurrentClip);
                if (clipSettings.mirror)
                {
                    CurrentClipIsMirror = true;
                }
                else
                {
                    CurrentClipIsMirror = false;
                }
            }
            else
            {
                CurrentClipIsMirror = false;
            }

            //Current animation is root motion
            if (CurrentAnimator != null)
            {
                CurrentAnimatorIsRootMotion = CurrentAnimator.applyRootMotion;
            }
            else
            {
                CurrentAnimatorIsRootMotion = false;
            }

            //Reset current keyframes values
            CurrentKeyframesValues.Clear();
            CurrentMirrorKeyframesValues.Clear();
            CurrentRootPositionsValues.Clear();
            CurrentRootRotationsValues.Clear();

            for (int i = 0; i < 3; i++)
            {
                CurrentKeyframesValues.Add(new List<float>());
                CurrentMirrorKeyframesValues.Add(new List<float>());
                CurrentRootPositionsValues.Add(new List<float>());
                CurrentRootRotationsValues.Add(new List<float>());
            }
        }

        public static bool IsCurrentClipEmpty()
        {
            if (CurrentClip == null) return false;
            return CurrentClip.empty;
        }

        public static bool IsCurrentBoneLeftHand()
        {
            if (CurrentSelectedBone == null)
            {
                return false;
            }

            return CurrentHumanBone == HumanBodyBones.LeftHand;
        }

        public static bool IsCurrentBoneRightHand()
        {
            if (CurrentSelectedBone == null)
            {
                return false;
            }

            return CurrentHumanBone == HumanBodyBones.RightHand;
        }

        public static void SetCurrentSelectedBone(Transform selectedBone = null)
        {
            CurrentSelectedBone = selectedBone;
            CurrentlyEditingBone = selectedBone;
            CurrentlyEditingMirrorBone = selectedBone;

            CurrentMuscleIndexes.Clear();
            CurrentIsHips = false;

            if (selectedBone != null)
            {
                GetCurrentHumanBone();

                SetCurrentMuscleIndexes();

                if (CurrentHumanBone == HumanBodyBones.Hips)
                {
                    CurrentIsHips = true;
                }
            }

            UpdateGlobalOffsetValues();
        }

        public static void UpdateCurrentBoneInfo()
        {
            GetCurrentHumanBone();
        }

        private static void GetCurrentHumanBone()
        {
            if (CurrentSelectedBone == null)
            {
                return;
            }

            CurrentHumanBone = GetHumanBone(CurrentSelectedBone);
            CurrentHumanBoneIndex = (int)CurrentHumanBone;
            CurrentlyEditingBone = CurrentSelectedBone;
            CurrentlyEditingMirrorBone = CurrentSelectedBone;
            HasMirrorBone = false;

            if (CurrentMirrorMode != 0)
            {
                int boneIndex = CurrentHumanBoneIndex;
                int mirrorBoneIndex = GetMirrorBoneIndex(CurrentHumanBoneIndex);
                if (boneIndex != mirrorBoneIndex)
                {
                    HasMirrorBone = true;
                }

                if (IsForceMirror)
                {
                    CurrentHumanBoneIndex = mirrorBoneIndex;

                    if (HasMirrorBone && currentAnimator != null)
                    {
                        CurrentlyEditingBone = currentAnimator.GetBoneTransform((HumanBodyBones)CurrentHumanBoneIndex);
                    }
                }
                else if (IsForceBothSide)
                {
                    if (HasMirrorBone && currentAnimator != null)
                    {
                        CurrentlyEditingMirrorBone = currentAnimator.GetBoneTransform((HumanBodyBones)mirrorBoneIndex);
                    }
                }
            }

            CurrentBoneRotation = CurrentlyEditingBone.rotation;
            UpdateCurrentRootBonePositionAndRotation();
        }

        private static void SetCurrentMuscleIndexes()
        {
            CurrentMuscleIndexes.Add(GetMuscleIndex(0));
            CurrentMuscleIndexes.Add(GetMuscleIndex(1));
            CurrentMuscleIndexes.Add(GetMuscleIndex(2));
        }

        public static int GetMuscleIndex(int dofIndex)
        {
            int muscleIndex = HumanTrait.MuscleFromBone((int)CurrentHumanBone, dofIndex);
            return muscleIndex;
        }

        public static int GetMuscleIndex(int boneIndex, int dofIndex)
        {
            int muscleIndex = HumanTrait.MuscleFromBone(boneIndex, dofIndex);
            return muscleIndex;
        }

        public static int GetMirrorMuscleIndex(int muscleIndex, int dofIndex)
        {
            int boneIndex = HumanTrait.BoneFromMuscle(muscleIndex);
            int mirrorBoneIndex = GetMirrorBoneIndex(boneIndex);
            int mirrorMuscleIndex = GetMuscleIndex(mirrorBoneIndex, dofIndex);

            return mirrorMuscleIndex;
        }

        private static int GetMirrorBoneIndex(int boneIndex)
        {
            int mirrorBoneIndex = boneIndex;

            if ((boneIndex >= 1 && boneIndex <= 6) || (boneIndex >= 11 && boneIndex <= 22))
            {
                if (boneIndex % 2 == 0) //Even
                {
                    mirrorBoneIndex = boneIndex - 1;
                }
                else if (boneIndex % 2 == 1) //Odd
                {
                    mirrorBoneIndex = boneIndex + 1;
                }
            }
            else if (boneIndex >= 24 && boneIndex <= 38)
            {
                mirrorBoneIndex = boneIndex + 15;
            }
            else if (boneIndex >= 39 && boneIndex <= 53)
            {
                mirrorBoneIndex = boneIndex - 15;
            }

            return mirrorBoneIndex;
        }

        public static string GetMuscleName(int muscleIndex, bool canMirror = false, int dofIndex = -1)
        {
            if (canMirror && dofIndex != -1 && CurrentMirrorMode == 1)
            {
                muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
            }

            string muscleName = HumanTrait.MuscleName[muscleIndex];

            //Fix fingers names
            if (muscleName.EndsWith("Stretched"))
            {
                var splits = muscleName.Split(' ');
                muscleName = string.Format("{0}Hand.{1}.{2} {3}", splits[0], splits[1], splits[2], splits[3]);
            }
            else if (muscleName.EndsWith("Spread"))
            {
                var splits = muscleName.Split(' ');
                muscleName = string.Format("{0}Hand.{1}.{2}", splits[0], splits[1], splits[2]);
            }

            return muscleName;
        }

        public static void MapHumanBones(Animator animator)
        {
            currentAnimator = animator;
            currentAvatar = animator.avatar;
            CurrentRootBone = GetRootBone();
            if (CurrentRootBone != null)
            {
                CurrentRootBonePosition = CurrentRootBone.position;
                CurrentRootBoneRotation = CurrentRootBone.rotation;
            }

            HumanBones.Clear();

            foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone != HumanBodyBones.LastBone)
                {
                    int boneIndex = (int)bone;
                    Transform joint = animator.GetBoneTransform(bone);
                    if (joint != null)
                    {
                        HumanBones.Add(joint, bone);
                    }
                }
            }
        }

        private static Transform GetRootBone()
        {
            if (currentAnimator == null)
            {
                return null;
            }

            return currentAnimator.GetBoneTransform(0);
        }

        public static void GetEditGameObjectHumanPose(ref HumanPose humanPose)
        {
            if (currentAnimator == null || !currentAnimator.isHuman || currentAvatar == null || CurrentRootBone == null)
            {
                return;
            }

            HumanPoseHandler humanPoseHandler = new HumanPoseHandler(currentAvatar, currentAnimator.transform);
            humanPoseHandler.GetHumanPose(ref humanPose);
        }

        public static HumanBodyBones GetHumanBone(Transform bone)
        {
            HumanBodyBones humanBone = HumanBodyBones.LastBone;
            HumanBones.TryGetValue(bone, out humanBone);
            return humanBone;
        }

        public static List<int> GetLeftHandMusclesIndexes(int dofIndex)
        {
            List<int> leftHandMusclesIndexes = new List<int>();

            for (int boneIndex = (int)HumanBodyBones.LeftThumbProximal; boneIndex <= (int)HumanBodyBones.LeftLittleDistal; boneIndex++)
            {
                int muscleIndex = GetMuscleIndex(boneIndex, dofIndex);
                if (muscleIndex != -1)
                {
                    leftHandMusclesIndexes.Add(muscleIndex);
                }
                
            }
            
            return leftHandMusclesIndexes;
        }

        public static int GetFirstLeftHandMuscleIndex(int dofIndex)
        {
            for (int boneIndex = (int)HumanBodyBones.RightThumbProximal; boneIndex <= (int)HumanBodyBones.RightLittleDistal; boneIndex++)
            {
                int muscleIndex = GetMuscleIndex(boneIndex, dofIndex);
                if (muscleIndex != -1)
                {
                    return muscleIndex;
                }
            }

            return -1;
        }

        public static List<int> GetRightHandMusclesIndexes(int dofIndex)
        {
            List<int> rightHandMusclesIndexes = new List<int>();

            for (int boneIndex = (int)HumanBodyBones.RightThumbProximal; boneIndex <= (int)HumanBodyBones.RightLittleDistal; boneIndex++)
            {
                int muscleIndex = GetMuscleIndex(boneIndex, dofIndex);
                if (muscleIndex != -1)
                {
                    rightHandMusclesIndexes.Add(muscleIndex);
                }
            }

            return rightHandMusclesIndexes;
        }

        public static int GetFirstRightHandMuscleIndex(int dofIndex)
        {
            for (int boneIndex = (int)HumanBodyBones.RightThumbProximal; boneIndex <= (int)HumanBodyBones.RightLittleDistal; boneIndex++)
            {
                int muscleIndex = GetMuscleIndex(boneIndex, dofIndex);
                if (muscleIndex != -1)
                {
                    return muscleIndex;
                }
            }

            return -1;
        }

        public static void ClearHumanBones()
        {
            HumanBones.Clear();
            currentAnimator = null;
            currentAvatar = null;
            CurrentRootBone = null;
            CurrentRootBonePosition = Vector3.zero;
            CurrentRootBoneRotation = Quaternion.identity;
        }

        public static bool IsBone(this Transform transform)
        {
            foreach (Transform t in HumanBones.Keys)
            {
                if (transform == t)
                {
                    return true;
                }
            }
            return false;
        }

        public static EditorWindow GetEditorWindow(string windowName)
        {
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.titleContent.text == windowName)
                {
                    return w;
                }
            }

            return null;
        }

        public static T FindComponentInParent<T>(this Transform child)
        {
            Transform t = child;
            while (t.parent != null)
            {
                if (t.parent.TryGetComponent(out T component))
                {
                    return component;
                }
                t = t.parent;
            }

            return default(T);
        }

        public static void RepaintAnimationWindow()
        {
            AnimationWindow.recording = false;
            AnimationWindow.recording = true;
        }

        public static void UpdateCurrentSliderValues()
        {
            if (CurrentMuscleIndexes.Count > 0)
            {
                for (int i = 0; i < CurrentMuscleIndexes.Count; i++)
                {
                    int currentMuscleIndex = CurrentMuscleIndexes[i];
                    if (currentMuscleIndex >= 0)
                    {
                        float baseValue = MuscleValues[i];
                        MuscleValues[i] = GetKeyframeValue(currentMuscleIndex, baseValue, i);
                    }
                    else
                    {
                        MuscleValues[i] = 0f;
                    }

                }
            }

            //Left hand fingers
            int leftHandFingerMuscleIndex1 = GetMuscleIndex((int)HumanBodyBones.LeftRingProximal, 1);
            int leftHandFingerMuscleIndex2 = GetMuscleIndex((int)HumanBodyBones.LeftRingProximal, 2);
            float leftBaseValue1 = LeftHandFingersValues[0];
            float leftBaseValue2 = LeftHandFingersValues[1];
            LeftHandFingersValues[0] = GetKeyframeValue(leftHandFingerMuscleIndex1, leftBaseValue1, 1);
            LeftHandFingersValues[1] = GetKeyframeValue(leftHandFingerMuscleIndex2, leftBaseValue2, 2);

            //Right hand fingers
            int rightHandFingerMuscleIndex1 = GetMuscleIndex((int)HumanBodyBones.RightRingProximal, 1);
            int rightHandFingerMuscleIndex2 = GetMuscleIndex((int)HumanBodyBones.RightRingProximal, 2);
            float rightBaseValue1 = RightHandFingersValues[0];
            float rightBaseValue2 = RightHandFingersValues[1];
            RightHandFingersValues[0] = GetKeyframeValue(rightHandFingerMuscleIndex1, rightBaseValue1, 1);
            RightHandFingersValues[1] = GetKeyframeValue(rightHandFingerMuscleIndex2, rightBaseValue2, 2);

            //Workaround to repaint overlay
            if (IsHumanoidAnimationWindow)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        public static void UpdateCurrentRootBonePositionAndRotation()
        {
            if (CurrentRootBone != null)
            {
                CurrentRootBonePosition = CurrentRootBone.position;
                CurrentRootBoneRotation = CurrentRootBone.rotation;
            }
        }

        public static void UpdateRootValues(bool values = true)
        {
            //Root position and rotation
            for (int i = 0; i < 3; i++)
            {
                string posPropertyName = "RootT." + "xyz"[i];
                string rotPropertyName = "RootQ." + "xyz"[i];

                float rootPositionBaseValue = RootPositionValues[i];
                float rootRotationBaseValue = RootRotationValues[i];
                RootPositionValues[i] = GetKeyframeValue(0, rootPositionBaseValue, -1, posPropertyName);
                RootRotationValues[i] = GetKeyframeValue(0, rootRotationBaseValue, -1, rotPropertyName);

                if (values)
                {
                    List<float> posBaseValues = CurrentRootPositionsValues[i];
                    List<float> rotBaseValues = CurrentRootRotationsValues[i];
                    CurrentRootPositionsValues[i] = GetAllKeyframesValues(0, posBaseValues, -1, posPropertyName);
                    CurrentRootRotationsValues[i] = GetAllKeyframesValues(0, rotBaseValues, -1, rotPropertyName);
                }
            }
        }

        public static void UndoOffsetValues()
        {
            for (int i = 0; i < CurrentMuscleIndexes.Count; i++)
            {
                int currentMuscleIndex = CurrentMuscleIndexes[i];
                if (currentMuscleIndex >= 0)
                {
                    if (OffsetValues[i] != 0)
                    {
                        if (CurrentKeyframesValues[i].Count > 0)
                        {
                            float offsetValue = GetFirstPropertyKeframeValue(currentMuscleIndex, i) - CurrentKeyframesValues[i][0];
                            OffsetValues[i] = offsetValue;
                        }
                    }
                }
            }

            if (CurrentIsHips)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int a = 0; a < 3; a++)
                    {
                        string forcePropertyName = "Root" + "TQ"[i] + "." + "xyz"[a];

                        switch (i)
                        {
                            case 0:
                                if (OffsetValues[a] != 0)
                                {
                                    float currentValue = 0f;
                                    if (CurrentKeyframesValues[a].Count > 0)
                                    {
                                        currentValue = CurrentKeyframesValues[a][0];
                                    }

                                    float offsetValue = GetFirstPropertyKeframeValue(0, -1, forcePropertyName) - currentValue;
                                    OffsetValues[a] = offsetValue;
                                }
                                break;
                            case 1:
                                if (OffsetSecondValues[a] != 0)
                                {
                                    float currentValue = 0f;
                                    if (CurrentMirrorKeyframesValues[a].Count > 0)
                                    {
                                        currentValue = CurrentMirrorKeyframesValues[a][0];
                                    }

                                    float offsetSecondValue = GetFirstPropertyKeframeValue(0, -1, forcePropertyName) - currentValue;
                                    OffsetSecondValues[a] = offsetSecondValue;
                                }
                                break;
                        }
                    }
                }
            }
            else if (IsCurrentBoneLeftHand())
            {
                for (int i = 0; i < 2; i++)
                {
                    if (OffsetSecondValues[i] != 0)
                    {
                        int dofIndex = i + 1;
                        int muscleIndex = GetFirstLeftHandMuscleIndex(dofIndex);
                        float offsetSecondValue = GetFirstPropertyKeframeValue(muscleIndex, dofIndex) - CurrentLeftHandFingersValues[i * 5][0];
                        OffsetSecondValues[i] = offsetSecondValue;
                    }
                }
            }
            else if (IsCurrentBoneRightHand())
            {
                for (int i = 0; i < 2; i++)
                {
                    if (OffsetSecondValues[i] != 0)
                    {
                        int dofIndex = i + 1;
                        int muscleIndex = GetFirstRightHandMuscleIndex(dofIndex);
                        float offsetSecondValue = GetFirstPropertyKeframeValue(muscleIndex, dofIndex) - CurrentRightHandFingersValues[i * 5][0];
                        OffsetSecondValues[i] = offsetSecondValue;
                    }
                }
            }
        }

        public static void UpdateGlobalOffsetValues()
        {
            if (CurrentMuscleIndexes.Count > 0)
            {
                for (int i = 0; i < CurrentMuscleIndexes.Count; i++)
                {
                    int currentMuscleIndex = CurrentMuscleIndexes[i];
                    if (currentMuscleIndex >= 0)
                    {
                        List<float> baseValues = CurrentKeyframesValues[i];
                        CurrentKeyframesValues[i] = GetAllKeyframesValues(currentMuscleIndex, baseValues, i);

                        if (IsForceBothSide)
                        {
                            List<float> baseMirrorValues = CurrentMirrorKeyframesValues[i];
                            int currentMirrorMuscleIndex = GetMirrorMuscleIndex(currentMuscleIndex, i);
                            CurrentMirrorKeyframesValues[i] = GetAllKeyframesValues(currentMirrorMuscleIndex, baseMirrorValues, i);
                        }
                    }
                    else
                    {
                        CurrentKeyframesValues[i] = new List<float>();
                        if (IsForceBothSide)
                        {
                            CurrentMirrorKeyframesValues[i] = new List<float>();
                        }
                    }
                }
                
                if (IsCurrentBoneLeftHand() || (IsCurrentBoneRightHand() && CurrentMirrorMode != 0))
                {
                    CurrentLeftHandFingersValues.Clear();

                    List<int> leftMuscles = GetLeftHandMusclesIndexes(1);
                    for (int i = 0; i < leftMuscles.Count; i++)
                    {
                        int muscleIndex = leftMuscles[i];
                        CurrentLeftHandFingersValues.Add(GetAllKeyframesValues(muscleIndex, new List<float>(), 1));
                    }

                    List<int> leftSecondMuscles = GetLeftHandMusclesIndexes(2);
                    for (int i = 0; i < leftSecondMuscles.Count; i++)
                    {
                        int muscleIndex = leftSecondMuscles[i];
                        CurrentLeftHandFingersValues.Add(GetAllKeyframesValues(muscleIndex, new List<float>(), 2));
                    }
                }

                if (IsCurrentBoneRightHand() || (IsCurrentBoneLeftHand() && CurrentMirrorMode != 0))
                {
                    CurrentRightHandFingersValues.Clear();

                    List<int> rightMuscles = GetRightHandMusclesIndexes(1);
                    for (int i = 0; i < rightMuscles.Count; i++)
                    {
                        int muscleIndex = rightMuscles[i];
                        CurrentRightHandFingersValues.Add(GetAllKeyframesValues(muscleIndex, new List<float>(), 1));
                    }

                    List<int> rightSecondMuscles = GetRightHandMusclesIndexes(2);
                    for (int i = 0; i < rightSecondMuscles.Count; i++)
                    {
                        int muscleIndex = rightSecondMuscles[i];
                        CurrentRightHandFingersValues.Add(GetAllKeyframesValues(muscleIndex, new List<float>(), 2));
                    }
                }
            }

            if (CurrentIsHips)
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int a = 0; a < 3; a++)
                    {
                        string forcePropertyName = "Root" + "TQ"[i] + "." + "xyz"[a];

                        switch (i)
                        {
                            case 0:
                                List<float> baseValues = CurrentKeyframesValues[a];
                                CurrentKeyframesValues[a] = GetAllKeyframesValues(0, baseValues, -1, forcePropertyName);
                                break;
                            case 1:
                                List<float> baseMirrorValues = CurrentMirrorKeyframesValues[a];
                                CurrentMirrorKeyframesValues[a] = GetAllKeyframesValues(0, baseMirrorValues, -1, forcePropertyName);
                                break;
                        }
                    }
                }
            }

            UpdateRootValues();

            OffsetValues = Vector3.zero;
            OffsetSecondValues = Vector3.zero;

            if (CurrentlyEditingBone != null)
            {
                CurrentBoneRotation = CurrentlyEditingBone.rotation;
            }

            //Workaround to repaint overlay
            if (IsHumanoidAnimationWindow)
            {
                SceneView.lastActiveSceneView.Repaint();
            }
        }

        public static float GetKeyframeValue(int muscleIndex, float baseValue, int dofIndex, string forcePropertyName = "")
        {
            if (AnimationWindow == null)
            {
                return baseValue;
            }

            float time = AnimationWindow.time;

            if (dofIndex != -1)
            {
                bool doubleMirror = CurrentClipIsMirror && IsForceMirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || IsForceMirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return baseValue;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            if (curve != null)
            {
                int keyIndex = FindKeyFrame(curve, time);
                if (keyIndex < 0)
                {
                    return baseValue;
                }

                Keyframe keyframe = curve[keyIndex];
                //Debug.Log(propertyName + " keyframe value: " + keyframe.value);
                return keyframe.value;
            }

            return 0f;
        }

        public static List<float> GetAllKeyframesValues(int muscleIndex, List<float> baseValues, int dofIndex, string forcePropertyName = "")
        {
            if (AnimationWindow == null)
            {
                return baseValues;
            }

            List<float> keyframesValues = new List<float>();

            if (dofIndex != -1 && muscleIndex != 0)
            {
                bool doubleMirror = CurrentClipIsMirror && IsForceMirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || IsForceMirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return baseValues;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            if (curve != null && curve.length > 0)
            {
                for (int i = 0; i < curve.length; i++)
                {
                    Keyframe keyframe = curve[i];
                    keyframesValues.Add(keyframe.value);
                    //Debug.Log("all - " + propertyName + " keyframe value: " + keyframe.value);
                }
            }

            return keyframesValues;
        }

        public static float GetFirstPropertyKeframeValue(int muscleIndex, int dofIndex, string forcePropertyName = "")
        {
            if (AnimationWindow == null || muscleIndex == -1)
            {
                return 0f;
            }

            if (dofIndex != -1)
            {
                bool doubleMirror = CurrentClipIsMirror && IsForceMirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || IsForceMirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return 0f;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            if (curve != null && curve.length > 0)
            {
                Keyframe keyframe = curve[0];
                return keyframe.value;
            }

            return 0f;
        }

        public static void SetKeyframe(int muscleIndex, float value, int dofIndex, string forcePropertyName = "", bool forceBothSide = false, bool forceNoMirror = false)
        {
            bool mirror = IsForceMirror;
            if (forceBothSide)
            {
                mirror = true;
            }

            float time = AnimationWindow.time;

            if (!forceNoMirror && dofIndex != -1 && muscleIndex != 0)
            {
                bool doubleMirror = CurrentClipIsMirror && mirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || mirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            if (curve != null)
            {
                int keyIndex = FindKeyFrame(curve, time);
                if (keyIndex < 0)
                {
                    curve.AddKey(time, value);
                }
                else
                {
                    Keyframe keyframe = curve[keyIndex];
                    keyframe.value = value;
                    curve.MoveKey(keyIndex, keyframe);
                }
            }
            else
            {
                curve = new AnimationCurve();
                curve.AddKey(time, value);
            }

            Undo.RecordObject(CurrentClip, "Edit Curve");
            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);

            //Workaround to update animation window
            RepaintAnimationWindow();

            //Force both side
            if (IsForceBothSide && !forceBothSide)
            {
                SetKeyframe(muscleIndex, value, dofIndex, forcePropertyName, true);
            }
        }

        public static void OffsetKeyframe(int muscleIndex, float additiveValue, int dofIndex, string forcePropertyName = "", bool leftFingers = false, bool rightFingers = false, bool spread = true, int fingersMuscleIndex = 0, bool forceBothSide = false)
        {
            bool mirror = IsForceMirror;
            if (forceBothSide)
            {
                mirror = true;
            }

            float time = AnimationWindow.time;

            if (dofIndex != -1 && muscleIndex != 0)
            {
                bool doubleMirror = CurrentClipIsMirror && mirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || mirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            bool curveCreated = false;
            if (curve != null)
            {
                int keyIndex = FindKeyFrame(curve, time);
                if (keyIndex < 0)
                {
                    curve.AddKey(time, additiveValue);
                }
                else
                {
                    Keyframe keyframe = curve[keyIndex];

                    float currentValue;
                    int index;

                    if (muscleIndex == 0)
                    {
                        if (dofIndex > 2)
                        {
                            dofIndex -= 3;
                            currentValue = CurrentRootRotationsValues[dofIndex][keyIndex];
                        }
                        else
                        {
                            currentValue = CurrentRootPositionsValues[dofIndex][keyIndex];
                        }
                    }
                    else
                    {
                        if (forceBothSide)
                        {
                            currentValue = CurrentMirrorKeyframesValues[dofIndex][keyIndex];
                        }
                        else
                        {
                            currentValue = CurrentKeyframesValues[dofIndex][keyIndex];
                        }
                    }


                    if (spread)
                    {
                        index = fingersMuscleIndex;
                    }
                    else
                    {
                        index = fingersMuscleIndex + 5;
                    }

                    if (leftFingers)
                    {
                        if (mirror)
                        {
                            currentValue = CurrentRightHandFingersValues[index][keyIndex];
                        }
                        else
                        {
                            currentValue = CurrentLeftHandFingersValues[index][keyIndex];
                        }

                    }
                    else if (rightFingers)
                    {
                        if (mirror)
                        {
                            currentValue = CurrentLeftHandFingersValues[index][keyIndex];
                        }
                        else
                        {
                            currentValue = CurrentRightHandFingersValues[index][keyIndex];
                        }
                    }

                    float value = currentValue + additiveValue;

                    bool isRootPosition = forcePropertyName.StartsWith("RootT");
                    bool isRootRotation = forcePropertyName.StartsWith("RootQ");
                    float minValue = -1f;
                    float maxValue = 1f;                
                    
                    if (isRootPosition)
                    {
                        value = additiveValue;
                    }

                    if (isRootPosition || isRootRotation)
                    {
                        minValue = -2f;
                        maxValue = 2f;
                    }

                    value = Mathf.Clamp(value, minValue, maxValue);
                    keyframe.value = value;
                    
                    if (isRootPosition) //Update root position values
                    {
                        RootPositionValues[dofIndex] = value;
                    }
                    else if (isRootRotation) //Update root rotation values
                    {
                        RootRotationValues[dofIndex] = value;
                    }
                    else //Updpate muscle values
                    {
                        MuscleValues[dofIndex] = value;
                    }

                    curve.MoveKey(keyIndex, keyframe);
                }
            }
            else
            {
                curve = new AnimationCurve();
                curve.AddKey(time, additiveValue);

                curveCreated = true;
            }

            Undo.RecordObject(CurrentClip, "Edit Curve");
            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);

            //If curve is created, update values
            if (curveCreated)
            {
                UpdateGlobalOffsetValues();
            }

            //Workaround to update animation window
            RepaintAnimationWindow();

            //Force both side
            if (IsForceBothSide && !forceBothSide)
            {
                OffsetKeyframe(muscleIndex, additiveValue, dofIndex, forcePropertyName, leftFingers, rightFingers, spread, fingersMuscleIndex, true);
            }
        }

        public static void OffsetAllKeyframes(int muscleIndex, float additiveValue, int dofIndex, string forcePropertyName = "", bool leftFingers = false, bool rightFingers = false, bool spread = true, int fingersMuscleIndex = 0, bool forceBothSide = false, bool setOffsetValue = false, bool forceNoMirror = false, bool isRootCorrection = false)
        {
            bool mirror = IsForceMirror;
            if (forceBothSide)
            {
                mirror = true;
            }

            if (!forceNoMirror && dofIndex != -1 && muscleIndex != 0)
            {
                bool doubleMirror = CurrentClipIsMirror && mirror;
                if (!doubleMirror)
                {
                    if (CurrentClipIsMirror || mirror)
                    {
                        muscleIndex = GetMirrorMuscleIndex(muscleIndex, dofIndex);
                    }
                }

                if (muscleIndex == -1)
                {
                    return;
                }
            }

            string propertyName = GetMuscleName(muscleIndex);
            if (forcePropertyName != "")
            {
                propertyName = forcePropertyName;
            }
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
            AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

            bool createdCurve = false;
            if (curve != null)
            {
                for (int i = 0; i < curve.length; i++)
                {
                    Keyframe keyframe = curve[i];

                    float currentValue = 0f;
                    int index;

                    if (isRootCorrection)
                    {
                        if (dofIndex > 2)
                        {
                            dofIndex -= 3;
                            currentValue = CurrentRootRotationsValues[dofIndex][i];
                        }
                        else
                        {
                            currentValue = CurrentRootPositionsValues[dofIndex][i];
                        }
                    }
                    else
                    {
                        if (muscleIndex == 0)
                        {
                            if (dofIndex > 2)
                            {
                                dofIndex -= 3;
                                currentValue = CurrentMirrorKeyframesValues[dofIndex][i];
                            }
                            else
                            {
                                currentValue = CurrentKeyframesValues[dofIndex][i];
                            }
                        }
                        else if (!leftFingers && !rightFingers)
                        {
                            if (forceBothSide)
                            {
                                currentValue = CurrentMirrorKeyframesValues[dofIndex][i];
                            }
                            else
                            {
                                currentValue = CurrentKeyframesValues[dofIndex][i];
                            }
                        }
                    }

                    if (spread)
                    {
                        index = fingersMuscleIndex;
                    }
                    else
                    {
                        index = fingersMuscleIndex + 5;
                    }

                    if (leftFingers)
                    {
                        if (mirror)
                        {
                            currentValue = CurrentRightHandFingersValues[index][i];
                        }
                        else
                        {
                            currentValue = CurrentLeftHandFingersValues[index][i];
                        }
                        
                    }
                    else if (rightFingers)
                    {
                        if (mirror)
                        {
                            currentValue = CurrentLeftHandFingersValues[index][i];
                        }
                        else
                        {
                            currentValue = CurrentRightHandFingersValues[index][i];
                        }
                    }

                    float value = currentValue + additiveValue;

                    bool isRootPosition = forcePropertyName.StartsWith("RootT");
                    bool isRootRotation = forcePropertyName.StartsWith("RootQ");
                    float minValue = -1f;
                    float maxValue = 1f;

                    if (isRootPosition && !isRootCorrection)
                    {
                        value = additiveValue;
                    }

                    if (isRootPosition || isRootRotation)
                    {
                        minValue = -2f;
                        maxValue = 2f;
                    }

                    value = Mathf.Clamp(value, minValue, maxValue);

                    keyframe.value = value;

                    if (setOffsetValue)
                    {
                        if (forceBothSide)
                        {
                            OffsetSecondValues[dofIndex] = additiveValue;
                        }
                        else
                        {
                            OffsetValues[dofIndex] = additiveValue;
                        }
                    }

                    curve.MoveKey(i, keyframe);
                }
            }
            else
            {
                float time = AnimationWindow.time;

                curve = new AnimationCurve();
                curve.AddKey(time, additiveValue);

                createdCurve = true;
            }

            Undo.RecordObject(CurrentClip, "Edit Curve");
            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);

            //If curve is created, update values
            if (createdCurve)
            {
                UpdateGlobalOffsetValues();
            }

            //Workaround to update animation window
            RepaintAnimationWindow();

            //Force both side
            if (IsForceBothSide && !forceBothSide)
            {
                OffsetAllKeyframes(muscleIndex, additiveValue, dofIndex, forcePropertyName, leftFingers, rightFingers, spread, fingersMuscleIndex, true, setOffsetValue);
            }
        }

        public static void CorrectRootPositionAndRotation()
        {
            if (CurrentRootBone == null || CurrentAnimator == null || !CurrentAnimator.isHuman)
            {
                return;
            }

            //Root rot correction
            Quaternion hipRot = CurrentRootBone.rotation;
            Quaternion hipPrevRot = CurrentRootBoneRotation;
            Quaternion hipRotOffset = hipPrevRot * Quaternion.Inverse(hipRot);

            for (int i = 0; i < 3; i++)
            {
                string propertyName = "RootQ." + "xyz"[i];
                float value = RootRotationValues[i] + hipRotOffset[i];

                if (CurrentKeyframeMode == 0)
                {
                    SetKeyframe(0, value, -1, propertyName);

                    //Set new value
                    RootRotationValues[i] = value;
                }
                else
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

                    if (curve != null)
                    {
                        for (int k = 0; k < curve.length; k++)
                        {
                            Keyframe keyframe = curve[k];

                            value = CurrentRootRotationsValues[i][k] + hipRotOffset[i];

                            float minValue = -2f;
                            float maxValue = 2f;

                            value = Mathf.Clamp(value, minValue, maxValue);

                            keyframe.value = value;
                            curve.MoveKey(k, keyframe);

                            Undo.RecordObject(CurrentClip, "Edit Curve");
                            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);

                            //Set new value
                            CurrentRootRotationsValues[i][k] = value;

                            //Workaround to update animation window
                            RepaintAnimationWindow();
                        }
                    }
                }
            }

            //Root pos correction
            Vector3 hipPos = CurrentRootBone.position;
            Vector3 hipPrevPos = CurrentRootBonePosition;
            Vector3 hipPosOffset = (hipPos - hipPrevPos) * (1f / CurrentAnimator.humanScale);
            for (int i = 0; i < 3; i++)
            {
                string propertyName = "RootT." + "xyz"[i];
                float value = RootPositionValues[i] - hipPosOffset[i];

                if (CurrentKeyframeMode == 0)
                {
                    SetKeyframe(0, value, -1, propertyName);

                    //Set new value
                    RootPositionValues[i] = value;
                }
                else
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve("", typeof(Animator), propertyName);
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(CurrentClip, binding);

                    if (curve != null)
                    {
                        for (int k = 0; k < curve.length; k++)
                        {
                            Keyframe keyframe = curve[k];

                            value = CurrentRootPositionsValues[i][k] - hipPosOffset[i];

                            float minValue = -2f;
                            float maxValue = 2f;

                            value = Mathf.Clamp(value, minValue, maxValue);

                            keyframe.value = value;
                            curve.MoveKey(k, keyframe);

                            Undo.RecordObject(CurrentClip, "Edit Curve");
                            AnimationUtility.SetEditorCurve(CurrentClip, binding, curve);

                            //Set new value
                            CurrentRootPositionsValues[i][k] = value;

                            //Workaround to update animation window
                            RepaintAnimationWindow();
                        }
                    }
                }
            }
        }

        private static int FindKeyFrame(AnimationCurve curve, float time)
        {
            int keyIndex = -1;

            if (curve.length > 0)
            {
                int begin = 0, end = curve.length - 1;

                while (end - begin > 1)
                {
                    var index = begin + Mathf.FloorToInt((end - begin) / 2f);
                    if (time < curve[index].time)
                    {
                        if (end == index) break;
                        end = index;
                    }
                    else
                    {
                        if (begin == index) break;
                        begin = index;
                    }
                }

                if (Mathf.Abs(curve[begin].time - time) < 0.0001f)
                {
                    keyIndex = begin;
                }

                if (Mathf.Abs(curve[end].time - time) < 0.0001f)
                {
                    keyIndex = end;
                }
            }

            return keyIndex;
        }

        public static void SetHumanoidPose(bool prefabPose = false)
        {
            if (currentAnimator == null || !currentAnimator.isHuman || currentAnimator.avatar == null || CurrentRootBone == null)
            {
                Debug.Log("<b>[Bones Assistant]</b> Cannot set Prefab Pose.");
                return;
            }

            AnimationWindow.previewing = false;
            AnimationWindow.recording = false;

            Transform rootBone = currentAnimator.transform;
            if (!prefabPose)
            {
                rootBone = CurrentRootBone;
            }

            HumanPoseHandler humanPoseHandler = new HumanPoseHandler(currentAvatar, rootBone);
            HumanPose humanPose = new HumanPose();
            humanPoseHandler.GetHumanPose(ref humanPose);

            //Create all keyframes in animation
            for (int i = 0; i < humanPose.muscles.Length; i++)
            {
                SetKeyframe(i, humanPose.muscles[i], -1);
            }

            //Fix root position
            SetKeyframe(0, 1, -1, "RootT.y");

            humanPoseHandler.Dispose();

            UpdateCurrentSliderValues();
            UpdateGlobalOffsetValues();
            UpdateCurrentRootBonePositionAndRotation();

            if (!prefabPose)
            {
                CorrectRootPositionAndRotation();

                //Correct position and rotation
                for (int i = 0; i < 4; i++)
                {
                    if (i < 3)
                    {
                        string posPropertyName = "RootT." + "xyz"[i];
                        float posValue = Vector3.up[i];
                        SetKeyframe(0, posValue, -1, posPropertyName);
                    }

                    string rotPropertyName = "RootQ." + "xyzw"[i];
                    float rotValue = Quaternion.identity[i];
                    SetKeyframe(0, rotValue, -1, rotPropertyName);
                }
            }

            AnimationWindow.previewing = true;
            AnimationWindow.recording = true;
        }

        public static void SaveCurrentPose()
        {
            if (currentAnimator == null || !currentAnimator.isHuman)
            {
                Debug.Log("<b>[Bones Assistant]</b> Cannot save current pose.");
                return;
            }

            HumanPose humanPose = new HumanPose();
            GetEditGameObjectHumanPose(ref humanPose);

            float[] rootValues = new float[6];
            for (int i = 0; i < 3; i++)
            {
                rootValues[i] = RootPositionValues[i];
                rootValues[i + 3] = RootRotationValues[i];
            }

            BonesAssistantResources.Instance.SavePose(humanPose.muscles, rootValues);
        }

        public static void OverridePose(int index)
        {
            if (currentAnimator == null || !currentAnimator.isHuman)
            {
                Debug.Log("<b>[Bones Assistant]</b> Cannot save current pose.");
                return;
            }

            HumanPose humanPose = new HumanPose();
            GetEditGameObjectHumanPose(ref humanPose);

            float[] rootValues = new float[6];
            for (int i = 0; i < 3; i++)
            {
                rootValues[i] = RootPositionValues[i];
                rootValues[i + 3] = RootRotationValues[i];
            }

            BonesAssistantResources.Instance.OverridePose(index, humanPose.muscles, rootValues);
        }

        public static void LoadPose(int index)
        {
            if (currentAnimator == null || !currentAnimator.isHuman)
            {
                Debug.Log("<b>[Bones Assistant]</b> Cannot save current pose.");
                return;
            }

            BonesAssistantSavedPose pose = BonesAssistantResources.Instance.GetPose(index);

            //Create all keyframes in animation
            for (int i = 0; i < pose.Pose.Length; i++)
            {
                SetKeyframe(i, pose.Pose[i], -1);
            }

            //Set root values
            for (int i = 0; i < 3; i++)
            {
                string posPropertyName = "RootT." + "xyz"[i];
                SetKeyframe(0, pose.RootValues[i], -1, posPropertyName);
                
                string rotPropertyName = "RootQ." + "xyz"[i];
                SetKeyframe(0, pose.RootValues[i + 3], -1, rotPropertyName);
            }

            UpdateCurrentSliderValues();
            UpdateGlobalOffsetValues();
            UpdateCurrentRootBonePositionAndRotation();
        }

        public static AnimatorController CreateAnimatorController()
        {
            string path = EditorUtility.OpenFolderPanel("Where do you want to save your animator controller?", Application.dataPath, "Controller");
            path += "/New Animator Controller.controller";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            path = path.GetAssetsRelativePath();

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            return controller;
        }

        public static AnimationClip CreateAnimationClip()
        {
            string path = EditorUtility.OpenFolderPanel("Where do you want to save your animation clip?", Application.dataPath, "Animation");
            path += "/New Animation.anim";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            path = path.GetAssetsRelativePath();

            AnimationClip clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);

            return clip;
        }

        public static void AddClipToController(AnimatorController controller, AnimationClip clip, AnimatorState state = null)
        {
            //Add clip to controller
            if (state != null)
            {
                controller.SetStateEffectiveMotion(state, clip);
            }
            else
            {
                controller.AddMotion(clip);
            }
        }

        public static void AddClipToController(Animator animator, AnimationClip clip)
        {
            //Get controller
            string controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            controller.AddMotion(clip);
        }

        public static AnimatorState GetStateFromClip(AnimationClip clip, AnimatorController controller)
        {
            AnimatorControllerLayer[] controllerlayers = controller.layers;
            foreach (AnimatorControllerLayer controllerLayer in controllerlayers)
            {
                ChildAnimatorState[] controllerStates = controllerLayer.stateMachine.states;
                foreach (ChildAnimatorState controllerState in controllerStates)
                {
                    if (controllerState.state.motion == clip)
                    {
                        return controllerState.state;
                    }
                }
            }

            //Null
            return null;
        }

        public static AnimationClip DuplicateClipAndAddToController(AnimationClip sourceClip, Animator animator)
        {
            //Get Controller
            string controllerPath = AssetDatabase.GetAssetPath(animator.runtimeAnimatorController);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            //Get current clip animator state
            AnimatorState state = GetStateFromClip(sourceClip, controller);

            //Get new clip path
            string clipPath = AssetDatabase.GetAssetPath(sourceClip);
            //string newClipPath = Path.GetDirectoryName(clipPath);
            string newClipPath = AssetDatabase.GenerateUniqueAssetPath(clipPath);
            newClipPath = Path.ChangeExtension(newClipPath, ".anim");

            //Duplicate clip
            AnimationClip newClip = UnityEngine.Object.Instantiate(sourceClip);

            //Create new clip
            AssetDatabase.CreateAsset(newClip, newClipPath);
            AssetDatabase.Refresh();

            //Load asset
            newClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(newClipPath);

            //Replace new clip to controller
            AddClipToController(controller, newClip, state);

            //Add source clip to controller
            AddClipToController(controller, sourceClip);

            //Debug log
            Debug.Log("<b>[Bones Assistant]</b> Just created a '" + sourceClip.name + "' animation clip copy in: " + newClipPath, newClip);

            //Return newly created clip
            return newClip;
        }

        public static string GetAssetsRelativePath(this string absolutePath)
        {
            if (absolutePath.StartsWith(Application.dataPath))
            {
                return "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }
            else
            {
                throw new ArgumentException("Full path does not contain the current project's Assets folder", "absolutePath");
            }
        }

        public static void DrawBoneSlidersSection(Color lineColor, int lineWidth = 0, bool inline = true)
        {
            Color initialGUIColor = GUI.color;

            using (new GUILayout.HorizontalScope())
            {
                float boneLabelWidth = 80;
                if (lineWidth != 0)
                {
                    boneLabelWidth = lineWidth * 0.25f;
                }
                EditorGUILayout.LabelField("Bone: ", GUIUtils.LabelBold, GUILayout.Width(boneLabelWidth));

                string boneName = CurrentlyEditingBone.name;
                if (lineWidth != 0)
                {
                    EditorGUILayout.LabelField(boneName, GUIUtils.LabelRight, GUILayout.Width(lineWidth * 0.75f));
                }
                else
                {
                    EditorGUILayout.LabelField(boneName, GUIUtils.LabelRight);
                }
            }

            GUIUtils.DrawUILine(lineColor, 1, 2, 0, lineWidth);

            if (CurrentMuscleIndexes.Count > 0)
            {
                bool hasDrawMuscleTitle = false;

                for (int i = 0; i < CurrentMuscleIndexes.Count; i++)
                {
                    int currentMuscleIndex = CurrentMuscleIndexes[i];

                    if (currentMuscleIndex >= 0)
                    {
                        if (!hasDrawMuscleTitle)
                        {
                            hasDrawMuscleTitle = true;
                            EditorGUILayout.LabelField("Muscle Rotation", GUIUtils.LabelBold);
                        }

                        //Min max
                        float minValue = -1f;
                        float maxValue = 1f;

                        //Color
                        if (i == 0) GUI.color = GUIUtils.RedLight;
                        else if (i == 1) GUI.color = GUIUtils.GreenLight;
                        else if (i == 2) GUI.color = GUIUtils.BlueLight;

                        GUIContent muscleContent = new GUIContent(GetMuscleName(currentMuscleIndex, true, i));
                        if (!inline)
                        {
                            EditorGUILayout.LabelField(muscleContent, GUILayout.Width(150));
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            if (inline)
                            {
                                EditorGUILayout.LabelField(muscleContent, GUILayout.Width(150));
                            }

                            EditorGUI.BeginChangeCheck();
                            
                            switch (CurrentKeyframeMode)
                            {
                                case 0:
                                    MuscleValues[i] = EditorGUILayout.Slider(MuscleValues[i], minValue, maxValue);
                                    break;

                                case 1:
                                    OffsetValues[i] = EditorGUILayout.Slider(OffsetValues[i], minValue, maxValue);
                                    break;
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                mustRootCorrection = true;

                                switch (CurrentKeyframeMode)
                                {
                                    case 0:
                                        SetKeyframe(currentMuscleIndex, MuscleValues[i], i);
                                        break;

                                    case 1:
                                        OffsetAllKeyframes(currentMuscleIndex, OffsetValues[i], i);
                                        break;
                                }

                                AnimationHelperWindow.RepaintCurrentInstance();
                            }
                        }
                    }
                }

                if (mustRootCorrection && !CurrentIsHips && CurrentRootCorrectionMode == 1)
                {
                    mustRootCorrection = false;
                    UpdateRootValues();
                    CorrectRootPositionAndRotation();
                }

                GUI.color = initialGUIColor;
            }

            //If is hips
            if (CurrentIsHips)
            {
                float axisLabelWidth = 40;
                if (lineWidth != 0)
                {
                    axisLabelWidth = 20;
                }

                //Root position
                EditorGUILayout.LabelField("Root Position", GUIUtils.LabelBold);

                for (int i = 0; i < 3; i++)
                {
                    if (!CurrentAnimatorIsRootMotion && (i == 0 || i == 2))
                    {
                        continue;
                    }

                    float minValue = -2f;
                    float maxValue = 2f;

                    //Axis
                    string labelName = "X";
                    string axisPropertyName = "RootT.x";
                    if (i == 0)
                    {
                        GUI.color = GUIUtils.RedLight;
                    }
                    else if (i == 1)
                    {
                        GUI.color = GUIUtils.GreenLight;
                        labelName = "Y";
                        axisPropertyName = "RootT.y";
                    }
                    else if (i == 2)
                    {
                        GUI.color = GUIUtils.BlueLight;
                        labelName = "Z";
                        axisPropertyName = "RootT.z";
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(labelName, GUILayout.Width(axisLabelWidth));

                        EditorGUI.BeginChangeCheck();

                        switch (CurrentKeyframeMode)
                        {
                            case 0:
                                RootPositionValues[i] = EditorGUILayout.Slider(RootPositionValues[i], minValue, maxValue);
                                break;

                            case 1:
                                OffsetValues[i] = EditorGUILayout.Slider(OffsetValues[i], minValue, maxValue);
                                break;
                        }

                        if (EditorGUI.EndChangeCheck())
                        {
                            switch (CurrentKeyframeMode)
                            {
                                case 0:
                                    SetKeyframe(0, RootPositionValues[i], -1, axisPropertyName);
                                    break;

                                case 1:
                                    OffsetAllKeyframes(0, OffsetValues[i], i, axisPropertyName);
                                    break;
                            }
                        }
                    }
                }

                GUI.color = initialGUIColor;

                if (CurrentAnimatorIsRootMotion)
                {
                    GUIUtils.DrawUILine(lineColor, 1, 2, 0, lineWidth);

                    //Root rotation
                    EditorGUILayout.LabelField("Root Rotation", GUIUtils.LabelBold);

                    for (int i = 0; i < 3; i++)
                    {
                        float minValue = -2f;
                        float maxValue = 2f;

                        //Axis
                        string labelName = "X";
                        string axisPropertyName = "RootQ.x";
                        if (i == 0)
                        {
                            GUI.color = GUIUtils.RedLight;
                        }
                        else if (i == 1)
                        {
                            GUI.color = GUIUtils.GreenLight;
                            labelName = "Y";
                            axisPropertyName = "RootQ.y";
                        }
                        else if (i == 2)
                        {
                            GUI.color = GUIUtils.BlueLight;
                            labelName = "Z";
                            axisPropertyName = "RootQ.z";
                        }

                        using (new GUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(labelName, GUILayout.Width(axisLabelWidth));

                            EditorGUI.BeginChangeCheck();

                            switch (CurrentKeyframeMode)
                            {
                                case 0:
                                    RootRotationValues[i] = EditorGUILayout.Slider(RootRotationValues[i], minValue, maxValue);
                                    break;

                                case 1:
                                    OffsetSecondValues[i] = EditorGUILayout.Slider(OffsetSecondValues[i], minValue, maxValue);
                                    break;
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                switch (CurrentKeyframeMode)
                                {
                                    case 0:
                                        SetKeyframe(0, RootRotationValues[i], -1, axisPropertyName);
                                        break;

                                    case 1:
                                        OffsetAllKeyframes(0, OffsetSecondValues[i], i + 3, axisPropertyName);
                                        break;
                                }
                            }
                        }
                    }

                    GUI.color = initialGUIColor;
                }
            }

            //Hand sliders
            if (IsCurrentBoneLeftHand())
            {
                GUILayout.Space(5);
                GUIUtils.DrawUILine(lineColor, 1, -1, 0, lineWidth);
                GUILayout.Space(5);
                string leftHandLabel = "Left Hand Fingers";
                if (IsForceMirror)
                {
                    leftHandLabel = "Right Hand Fingers";
                }
                EditorGUILayout.LabelField(leftHandLabel, GUIUtils.LabelBold);

                float minValue = -1f;
                float maxValue = 1f;

                if (!inline)
                {
                    EditorGUILayout.LabelField("Spread", GUILayout.Width(150));
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (inline)
                    {
                        EditorGUILayout.LabelField("Spread", GUILayout.Width(150));
                    }

                    EditorGUI.BeginChangeCheck();

                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            LeftHandFingersValues[0] = EditorGUILayout.Slider(LeftHandFingersValues[0], minValue, maxValue);
                            break;

                        case 1:
                            OffsetSecondValues[0] = EditorGUILayout.Slider(OffsetSecondValues[0], minValue, maxValue);
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SetLeftHandValues(1);
                    }
                }

                if (!inline)
                {
                    EditorGUILayout.LabelField("Stretch", GUILayout.Width(150));
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (inline)
                    {
                        EditorGUILayout.LabelField("Stretch", GUILayout.Width(150));
                    }

                    EditorGUI.BeginChangeCheck();

                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            LeftHandFingersValues[1] = EditorGUILayout.Slider(LeftHandFingersValues[1], minValue, maxValue);
                            break;

                        case 1:
                            OffsetSecondValues[1] = EditorGUILayout.Slider(OffsetSecondValues[1], minValue, maxValue);
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SetLeftHandValues(2);
                    }
                }
            }
            else if (IsCurrentBoneRightHand())
            {
                GUILayout.Space(5);
                GUIUtils.DrawUILine(lineColor, 1, -1, 0, lineWidth);
                GUILayout.Space(5);
                string rightHandLabel = "Right Hand Fingers";
                if (IsForceMirror)
                {
                    rightHandLabel = "Left Hand Fingers";
                }
                EditorGUILayout.LabelField(rightHandLabel, GUIUtils.LabelBold);

                float minValue = -1f;
                float maxValue = 1f;

                if (!inline)
                {
                    EditorGUILayout.LabelField("Spread");
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (inline)
                    {
                        EditorGUILayout.LabelField("Spread");
                    }

                    EditorGUI.BeginChangeCheck();

                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            RightHandFingersValues[0] = EditorGUILayout.Slider(RightHandFingersValues[0], minValue, maxValue);
                            break;

                        case 1:
                            OffsetSecondValues[0] = EditorGUILayout.Slider(OffsetSecondValues[0], minValue, maxValue);
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SetRightHandValues(1);
                    }
                }

                if (!inline)
                {
                    EditorGUILayout.LabelField("Stretch");
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (inline)
                    {
                        EditorGUILayout.LabelField("Stretch");
                    }

                    EditorGUI.BeginChangeCheck();

                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            RightHandFingersValues[1] = EditorGUILayout.Slider(RightHandFingersValues[1], minValue, maxValue);
                            break;

                        case 1:
                            OffsetSecondValues[1] = EditorGUILayout.Slider(OffsetSecondValues[1], minValue, maxValue);
                            break;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        SetRightHandValues(2);
                    }
                }
            }

            void SetLeftHandValues(int dofIndex)
            {
                bool spread = dofIndex == 1 ? true : false;

                List<int> leftMuscles = GetLeftHandMusclesIndexes(dofIndex);
                for (int i = 0; i < leftMuscles.Count; i++)
                {
                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            SetKeyframe(leftMuscles[i], LeftHandFingersValues[dofIndex - 1], dofIndex);
                            break;

                        case 1:
                            OffsetAllKeyframes(leftMuscles[i], OffsetSecondValues[dofIndex - 1], dofIndex, "", true, false, spread, i);
                            break;
                    }
                }
            }

            void SetRightHandValues(int dofIndex)
            {
                bool spread = dofIndex == 1 ? true : false;

                List<int> rightMuscles = GetRightHandMusclesIndexes(dofIndex);
                for (int i = 0; i < rightMuscles.Count; i++)
                {
                    switch (CurrentKeyframeMode)
                    {
                        case 0:
                            SetKeyframe(rightMuscles[i], RightHandFingersValues[dofIndex - 1], dofIndex);
                            break;

                        case 1:
                            OffsetAllKeyframes(rightMuscles[i], OffsetSecondValues[dofIndex - 1], dofIndex, "", false, true, spread, i);
                            break;
                    }
                }
            }
        }
    }
}

