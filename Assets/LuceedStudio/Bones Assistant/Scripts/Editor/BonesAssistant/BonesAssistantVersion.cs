// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using UnityEditor;

namespace LuceedStudio_BonesAssistant
{
    [InitializeOnLoad]
    public static class BonesAssistantVersion
    {
        public static string VERSION = "1.1.0";
        private static bool opened = false;

        static BonesAssistantVersion()
        {
            if (opened)
            {
                return;
            }

            bool showWelcomeWindow = EditorPrefs.GetString("LuceedStudio.BonesAssistant.Version", "") != VERSION;
            if (showWelcomeWindow)
            {
                EditorPrefs.SetString("LuceedStudio.BonesAssistant.Version", VERSION);
                EditorApplication.update += OpenWindowOnUpdate;
            }
        }

        private static void OpenWindowOnUpdate()
        {
            EditorApplication.update -= OpenWindowOnUpdate;
            BonesAssistantWelcome.OpenBonesAssistantWelcomeWindow();
            opened = true;
        }
    }
}

