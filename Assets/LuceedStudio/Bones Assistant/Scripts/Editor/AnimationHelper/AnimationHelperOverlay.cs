// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using LuceedStudio_Utils;

namespace LuceedStudio_BonesAssistant
{
    [Overlay(typeof(SceneView), "Humanoid Animation")]
    public class AnimationHelperOverlay : IMGUIOverlay, ITransientOverlay
    {
        public bool visible => AnimationHelperInfo.IsHumanoidAnimationWindow;

        private const int WIDTH = 200;

        private Color lineColor = Color.white;

        public override void OnCreated()
        {
            base.OnCreated();

            lineColor.a = 0.2f;
        }

        public override void OnGUI()
        {
            GUIUtils.DrawUILine(lineColor, padding: 5, width: WIDTH);

            if (AnimationHelperInfo.CurrentClip != null)
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Clip: ", GUIUtils.LabelBold, GUILayout.Width(WIDTH * 0.25f));

                    string clipName = AnimationHelperInfo.CurrentClip.name;
                    EditorGUILayout.LabelField(clipName, GUIUtils.LabelRight, GUILayout.Width(WIDTH * 0.75f));
                }

                using (new GUILayout.VerticalScope(GUILayout.Width(WIDTH)))
                {
                    GUIUtils.DrawUILine(lineColor, padding: 5, width: WIDTH);

                    if (!AnimationHelperInfo.CurrentAnimator.isHuman)
                    {
                        string infoString = "This is not a humanoid, you don't have to use this window to animate it.";
                        EditorGUILayout.HelpBox(infoString, MessageType.Info);
                        GUIUtils.DrawUILine(lineColor, padding: 5, width: WIDTH);
                    }
                    else
                    {
                        if (AnimationHelperInfo.CurrentSelectedBone == null)
                        {
                            string infoString = "Select any bones to show related animation sliders.\nUse Bones Viewer overlay for easier bones selection.";
                            EditorGUILayout.HelpBox(infoString, MessageType.Info);
                            GUIUtils.DrawUILine(lineColor, padding: 5, width: WIDTH);

                            if (AnimationHelperInfo.IsCurrentClipEmpty())
                            {
                                string tPoseString = "I suggest you to start with a classic T-Pose by clicking on this button below.";
                                EditorGUILayout.HelpBox(tPoseString, MessageType.Info);

                                if (GUILayout.Button("Set T-Pose Pose"))
                                {
                                    AnimationHelperInfo.SetHumanoidPose();
                                }

                                GUIUtils.DrawUILine(lineColor, padding: 5, width: WIDTH);
                            }
                        }
                        else
                        {
                            AnimationHelperInfo.DrawBoneSlidersSection(lineColor, WIDTH, false);
                        }
                    }
                }
            }
            else
            {
                string noClipString = "No clip, please retry to edit animation using Humanoid Animation window.";
                EditorGUILayout.HelpBox(noClipString, MessageType.Error);
            }
        }
    }
}

