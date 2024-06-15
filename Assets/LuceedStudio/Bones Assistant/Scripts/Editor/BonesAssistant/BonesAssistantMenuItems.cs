// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace LuceedStudio_BonesAssistant
{
    public static class BonesAssistantMenuItems
    {
        [MenuItem("Window/Bones Assistant/Animation Helper", priority = 1011)]
        public static void OpenAnimationHelperWindow()
        {
            AnimationHelperWindow.OpenHumanoidAnimationWindow();
        }

        [MenuItem("Window/Bones Assistant/Bones Viewer", priority = 1012)]
        public static void InitBonesViewerOverlay()
        {
            Overlay overlay;
            SceneView.lastActiveSceneView.TryGetOverlay("bonesviewer", out overlay);

            if (overlay.displayed)
            {
                Debug.Log("<b>[Bones Assistant]</b> Bones Viewer overlay is already displayed in your scene view.");
            }
            else
            {
                Debug.Log("<b>[Bones Assistant]</b> Displaying Bones Viewer overlay in your scene view.");
            }

            overlay.displayed = true;
        }

        [MenuItem("Window/Bones Assistant/Welcome", priority = 1023)]
        public static void OpenWelcomeWindow()
        {
            BonesAssistantWelcome.OpenBonesAssistantWelcomeWindow();
        }
    }
}

