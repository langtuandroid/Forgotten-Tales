// Bones Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/bones-assistant

using LuceedStudio_Utils;
using UnityEditor;
using UnityEngine;

namespace LuceedStudio_BonesAssistant
{
    public class BonesAssistantWelcome : EditorWindow
    {
        private bool isRequirements = false;
        private bool isEdit = false;
        private bool isPose = false;
        private Vector2 requirementsScroll = Vector2.zero;
        private Vector2 editScroll = Vector2.zero;
        private Vector2 poseScroll = Vector2.zero;

        private string icon_ba_guid = "24efffba92a5000458d431a66d103c8f";
        private string icon_luceed_guid = "900a1e0fde68c9b418a02b1b39e42969";
        private string icon_animationHelper_guid = "95889eeb53bf48e4a84a2bc152730d92";
        private string icon_bonesViewer_guid = "1a66e6f50b32e3146b6d5b99b5c1ac38";
        private string icon_discover_guid = "bc4fb58c0666d4636aef70a1cc7a8752";
        private string icon_discover_hover_guid = "7948458087cb54ba2a47a91dc95b72cd";
        private string icon_doclink_guid = "c205cf50b85724a91bc4a2e91976875b";
        private string icon_doclink_hover_guid = "b35be54bfae96454a9bed35e331c0317";
        private Texture icon_ba;
        private Texture icon_luceed;
        private Texture icon_animationHelper;
        private Texture icon_bonesViewer;
        private Texture icon_discover;
        private Texture icon_discover_hover;
        private Texture icon_doclink;
        private Texture icon_doclink_hover;

        private const float SIDE_MARGIN = 20;
        private const string LINK_LUCEED = "https://luceed.studio/";
        private const string LINK_DOC = "https://luceed.studio/bones-assistant/";
        private const string LINK_CHANGELOG = "https://luceed-studio.notion.site/Changelog-76912a7d2ff4432aa53dcfddbee317e8";

        public static void OpenBonesAssistantWelcomeWindow()
        {
            Vector2 screenCenter = new Vector2(Screen.currentResolution.width, Screen.currentResolution.height) / 2;
            Vector2 windowSize = new Vector2(540, 600);

            BonesAssistantWelcome welcomeWindow = GetWindow<BonesAssistantWelcome>(true);
            welcomeWindow.titleContent = new GUIContent("Bones Assistant Welcome");
            welcomeWindow.position = new Rect(screenCenter - windowSize / 2, windowSize);
            welcomeWindow.minSize = new Vector2(540, 600);
        }

        private void CreateGUI()
        {
            icon_ba = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_ba_guid), typeof(Texture)) as Texture;
            icon_luceed = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_luceed_guid), typeof(Texture)) as Texture;
            icon_animationHelper = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_animationHelper_guid), typeof(Texture)) as Texture;
            icon_bonesViewer = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_bonesViewer_guid), typeof(Texture)) as Texture;
            icon_discover = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_discover_guid), typeof(Texture)) as Texture;
            icon_discover_hover = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_discover_hover_guid), typeof(Texture)) as Texture;
            icon_doclink = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_doclink_guid), typeof(Texture)) as Texture;
            icon_doclink_hover = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(icon_doclink_hover_guid), typeof(Texture)) as Texture;
        }

        private void OnGUI()
        {
            //Style
            GUIStyle marginLabelStyle = GUIUtils.GetMarginStyle(EditorStyles.boldLabel, new Vector4(25, 25, 0, 0));

            //Header
            Rect headerRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 105);
            Rect titleRect = new Rect(0, 5, EditorGUIUtility.currentViewWidth, 30);
            Rect subTitleRect = new Rect(0, 40, EditorGUIUtility.currentViewWidth, 30);
            Rect versionRect = new Rect((EditorGUIUtility.currentViewWidth - 100) / 2, 77, 100, 20); 

            GUIContent luceedContent = new GUIContent("Luceed Studio");
            if (icon_luceed != null)
            {
                luceedContent = new GUIContent("  Luceed Studio", icon_luceed);
            }

            GUIContent baContent = new GUIContent("Bones Assistant");
            if (icon_ba != null)
            {
                baContent = new GUIContent(icon_ba);
            }

            GUIContent versionContent = new GUIContent("Version " + BonesAssistantVersion.VERSION, "See changelog");

            EditorGUI.LabelField(titleRect, luceedContent, GUIUtils.LabelCenterBold);
            EditorGUI.LabelField(subTitleRect, baContent, GUIUtils.LabelCenterBold);
            if (GUI.Button(versionRect, versionContent))
            {
                Application.OpenURL(LINK_CHANGELOG);
            }

            EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 1f, 0.1f));
            GUILayout.Space(105);
            GUIUtils.DrawUILine(padding: 0);
            GUILayout.Space(10);

            //Open window info
            GUILayout.Label("Open Animation Helper window", marginLabelStyle);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(SIDE_MARGIN);

                if (icon_animationHelper != null)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(15);
                        GUIContent iconAnimationHelperContent = new GUIContent(icon_animationHelper);
                        EditorGUILayout.LabelField(iconAnimationHelperContent, GUILayout.Width(40), GUILayout.Height(40));
                    }
                }

                using (new GUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Width(440)))
                {
                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Navigate to: ", GUILayout.Width(260));
                        EditorGUILayout.LabelField("Window > Bones Assistant > Animation Helper", GUIUtils.HelpBold, GUILayout.Width(260));
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(10);

                        if (GUILayout.Button("or Click here to open\nAnimation Helper window!", GUILayout.Width(180), GUILayout.Height(50)))
                        {
                            AnimationHelperWindow.OpenHumanoidAnimationWindow();
                            Close();
                        }
                    }
                }

                GUILayout.Space(SIDE_MARGIN);
            }

            GUILayout.Space(10);
            GUIUtils.DrawUILine(padding: 5);
            
            //Open bones viewer overlay
            GUILayout.Label("Open Bones Viewer overlay", marginLabelStyle);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(SIDE_MARGIN);

                if (icon_bonesViewer != null)
                {
                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(15);
                        GUIContent iconBonesViewerContent = new GUIContent(icon_bonesViewer);
                        EditorGUILayout.LabelField(iconBonesViewerContent, GUILayout.Width(40), GUILayout.Height(40));
                    }
                }

                using (new GUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Width(440)))
                {
                    using (new GUILayout.VerticalScope())
                    {
                        EditorGUILayout.LabelField("Navigate to: ", GUILayout.Width(260));
                        EditorGUILayout.LabelField("'Overlays' menu in your scene tab settings.\nDisplay 'Bones Viewer' from there.", GUIUtils.HelpBold, GUILayout.Width(260));
                    }

                    using (new GUILayout.VerticalScope())
                    {
                        GUILayout.Space(10);

                        if (GUILayout.Button("or Click here to display\nBones Viewer overlay!", GUILayout.Width(180), GUILayout.Height(50)))
                        {
                            BonesAssistantMenuItems.InitBonesViewerOverlay();
                            Close();
                        }
                    }
                }

                GUILayout.Space(SIDE_MARGIN);
            }

            GUILayout.Space(10);
            GUIUtils.DrawUILine(padding: 5);

            //Humanoid animation info
            GUILayout.Label("Animation Helper Window Documentation", marginLabelStyle);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(25);

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    isRequirements = EditorGUILayout.Foldout(isRequirements, "Requirements");
                    if (isRequirements)
                    {
                        isEdit = false;
                        isPose = false;

                        string requirementsInfo = "Follow window indications to fix all errors and to be able to edit the animation.\n" +
                        "You must select an object with an animator and an animation clip inside.\n" + 
                        "You must have an animation editor window.";
                        requirementsScroll = GUILayout.BeginScrollView(requirementsScroll, "ProgressBarBack", GUILayout.Height(50));
                        GUILayout.Label(requirementsInfo, "WordWrappedMiniLabel");
                        GUILayout.EndScrollView();
                    }

                    GUILayout.Space(5);

                    isEdit = EditorGUILayout.Foldout(isEdit, "Edit animation");
                    if (isEdit)
                    {
                        isRequirements = false;
                        isPose = false;

                        string editInfo = "Once you click on the edit button, you can select any bones to see its corresponding sliders and to animate it.\n" +
                        "When you move a slider, it creates (or override) a keyframe on your timeline position.";
                        editScroll = GUILayout.BeginScrollView(editScroll, "ProgressBarBack", GUILayout.Height(50));
                        GUILayout.Label(editInfo, "WordWrappedMiniLabel");
                        GUILayout.EndScrollView();
                    }

                    GUILayout.Space(5);

                    isPose = EditorGUILayout.Foldout(isPose, "Pose");
                    if (isPose)
                    {
                        isRequirements = false;
                        isEdit = false;

                        string poseInfo = "If it is humanoid, you can directly set a t-pose. It will create (or override) a bunch of keyframes to create the t-pose on your timeline position.\n" +
                        "If you start a completely new animation, I suggest you to start with a t-pose.";
                        poseScroll = GUILayout.BeginScrollView(poseScroll, "ProgressBarBack", GUILayout.Height(50));
                        GUILayout.Label(poseInfo, "WordWrappedMiniLabel");
                        GUILayout.EndScrollView();
                    }
                }

                GUILayout.Space(25);
            }

            GUILayout.FlexibleSpace();

            GUIUtils.DrawUILine(padding: 5);
            GUILayout.Space(120);

            Rect luceedDiscoverRect = GUILayoutUtility.GetLastRect();
            Rect docLinkRect = GUILayoutUtility.GetLastRect();

            //DOC LINK
            docLinkRect.x += 50;
            docLinkRect.y += 10;
            docLinkRect.height = 100;
            docLinkRect.width = 200;

            Texture docLinkTexture = icon_doclink;
            if (docLinkRect.Contains(Event.current.mousePosition))
            {
                docLinkTexture = icon_doclink_hover;
            }

            GUIContent docLinkContent = new GUIContent(docLinkTexture);

            if (GUI.Button(docLinkRect, docLinkContent))
            {
                Application.OpenURL(LINK_DOC);
            }

            //LUCEED DISCOVER
            luceedDiscoverRect.x += 275;
            luceedDiscoverRect.y += 10;
            luceedDiscoverRect.height = 100;
            luceedDiscoverRect.width = 200;

            Texture luceedDiscoverTexture = icon_discover;
            if (luceedDiscoverRect.Contains(Event.current.mousePosition))
            {
                luceedDiscoverTexture = icon_discover_hover;
            }

            GUIContent luceedDiscoverContent = new GUIContent(luceedDiscoverTexture);

            if (GUI.Button(luceedDiscoverRect, luceedDiscoverContent))
            {
                Application.OpenURL(LINK_LUCEED);
            }
        }
    }
}

