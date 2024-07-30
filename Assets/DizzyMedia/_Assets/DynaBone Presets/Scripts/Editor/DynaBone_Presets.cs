using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DynaBone_Presets : EditorWindow {
    
    
//////////////////////////////////////
///
///     CUSTOM CLASSES
///
///////////////////////////////////////
    
    
    [System.Serializable]
    public class BodyParts {
        
        [Space]
        
        public Transform Pelvis;
        
        [Space]
        
        public Transform LeftHips;
        public Transform LeftKnee;
        public Transform LeftFoot;
        
        [Space]
        
        public Transform RightHips;
        public Transform RightKnee;
        public Transform RightFoot;
        
        [Space]
        
        public Transform LeftArm;
        public Transform LeftElbow;
        public Transform LeftHand;
        
        [Space]
        
        public Transform RightArm;
        public Transform RightElbow;
        public Transform RightHand;
        
        [Space]
        
        public Transform Chest;
        
        [Space]
        
        public Transform Neck;
        public Transform Head;
        
    }//BodyParts
    
    [System.Serializable]
    public class BodyParts_Anim {
        
        [Space]
        
        public Transform Pelvis;
        public Transform Neck;
        public Transform Head;
        
    }//BodyParts    

    
//////////////////////////////////////
///
///     CUSTOM ENUMS
///
///////////////////////////////////////
    
    
    public enum CreateType {
        
        RootTransform = 0,
        OrigTransform = 1,
        
    }//CreateType
    
    public enum ObjectType {
        
        Human = 0,
        Animal = 1,
        
    }//ObjectType
    
    public enum CollisionPoints {
        
        Upper = 0,
        Lower = 1,
        FullBody = 2,
        
    }//CollisionPoints
    
    public enum ArmsPoints {
        
        None = 0,
        UpperArms = 1,
        LowerArms = 2,
        Both = 3,
        
    }//ArmsPoints
    
    public enum LegsPoints {
        
        None = 0,
        UpperLegs = 1,
        LowerLegs = 2,
        Both = 3,
        
    }//LegsPoints
    
    
//////////////////////////////////////
///
///     GUI VALUES
///
///////////////////////////////////////
    
    
    private static string verNumb = " v0.2.1";
    
    public bool useDebug = true;
    public int debugInt = 1;
    
    public bool presetOpts;
    public bool colPresetOpts;
    public bool bonePointsOpts;
    public bool boneColPointsOpts;
    
    int bonePointsTabs;
    
    int wizardTabCount;
    int configTypeInt;
    Vector2 scrollPos;
    
    public BodyParts bodyParts = new BodyParts();
    public BodyParts_Anim bodyPartsAnim = new BodyParts_Anim();
    public List<Transform> bodyPartsTemp = new List<Transform>();
    public List<Transform> anim_BodyPartsTemp = new List<Transform>();
    public Transform rootTrans;
    public Animator charAnim;
    
    public CreateType createType;
    public ObjectType objectType;
    public CollisionPoints colPoints;
    public ArmsPoints armsPoints;
    public LegsPoints legsPoints;
    
    public List<Transform> clothPoints = new List<Transform>();
    public List<Transform> tailPoints = new List<Transform>();
    public List<Transform> earsPoints = new List<Transform>();
    
    public List<DynamicBoneColliderBase> dynamBoneCols = new List<DynamicBoneColliderBase>();
    public List<DynamicBone> dynamBone = new List<DynamicBone>();

    public DynaBone_Template clothPreset;
    public DynaBone_Template tailPreset;
    public DynaBone_Template earsPreset;
    
    public DynaBone_ColTemplate presetHead;
    
    public DynaBone_ColTemplate presetNeck;
    public DynaBone_ColTemplate presetHips;
    
    public DynaBone_ColTemplate preset_LeftUpperArm;
    public DynaBone_ColTemplate preset_LeftLowerArm;
        
    public DynaBone_ColTemplate preset_RightUpperArm;
    public DynaBone_ColTemplate preset_RightLowerArm;
        
    public DynaBone_ColTemplate preset_LeftUpperLeg;
    public DynaBone_ColTemplate preset_LeftLowerLeg;
        
    public DynaBone_ColTemplate preset_RightUpperLeg;
    public DynaBone_ColTemplate preset_RightLowerLeg;
    
    
//////////////////////////////////////
///
///     EDITOR GUI
///
///////////////////////////////////////
    
    
    [MenuItem("Dizzy Media/DynaBone Presets/Presets/New DynaBone Preset", false , 0)]
    public static void Preset_Create() {
        
        DynaBone_Template asset = ScriptableObject.CreateInstance<DynaBone_Template>();

        AssetDatabase.CreateAsset(asset, "Assets/New DynaBone Preset.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
        
    }//Preset_Create
    
    [MenuItem("Dizzy Media/DynaBone Presets/Presets/New DynaBone Collider Preset", false , 1)]
    public static void Preset_CreateCol() {
    
        DynaBone_ColTemplate asset = ScriptableObject.CreateInstance<DynaBone_ColTemplate>();

        AssetDatabase.CreateAsset(asset, "Assets/New DynaBone Collider Preset.asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    
    }//Preset_CreateCol
    
    [MenuItem("Dizzy Media/DynaBone Presets/Review Asset", false , 13)]
    public static void OpenReview() {
            
        Application.OpenURL("http://u3d.as/25dd#reviews");
        
    }//OpenReview
    
    [MenuItem("Dizzy Media/DynaBone Presets/System", false , 0)]
    public static void OpenWizard() {
        
        GetWindow<DynaBone_Presets>(false, "DynaBone Presets" + verNumb, true);
        
    }//OpenWizard

    private void OnGUI() {
            
        GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            
        Texture t0 = (Texture)Resources.Load("EditorContent/DynaBone-Logo");
        
        var style = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleCenter};
            
        GUILayout.Box(t0, style, GUILayout.ExpandWidth(true), GUILayout.Height(200));
        
        EditorGUILayout.Space();
        
        DrawPropertyTabs();
            
    }//OnGUI
    
    void DrawPropertyTabs() {
        
        wizardTabCount = GUILayout.Toolbar(wizardTabCount, new string[] { "Presets", "Debug" });

        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        switch (wizardTabCount) {
                
            case 0:
                SetupDisplay();
                break;

            case 01:
                DebugDisplay();
                break;

        }//wizardTabCount

    }//DrawPropertyTabs
    
    
/////////////////////////////////////////////////////////////////////////////
///
///     EDITOR GUI CALLS
///
//////////////////////////////////////////////////////////////////////////////
    
    
    public void SetupDisplay(){
        
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        
        EditorGUILayout.HelpBox("Use the presets below to create Dynamic Bone components.", MessageType.Info);
        
        if(charAnim == null){
            
            EditorGUILayout.HelpBox("Assign an Animator to get started.", MessageType.Info);
            
            if(rootTrans != null){
             
                ClearParts();
                
            }//rootTrans != null
            
        //charAnim == null
        } else {
            
            if(rootTrans == null){
                
                GrabBodyParts();
                
            }//rootTrans == null
            
            if(rootTrans != null){
                
                if(rootTrans != charAnim.transform){
                    
                    ClearParts();
                    
                }//rootTrans != charAnim.transform
                
            }//rootTrans != null
            
            if(charAnim.isHuman){
                
                EditorGUILayout.HelpBox("Humanoid Detected.", MessageType.Info);
                
            //isHuman
            } else {
                
                EditorGUILayout.HelpBox("Generic Detected.", MessageType.Info);
                
            }//isHuman
            
        }//charAnim == null
        
        ScriptableObject target = this;
        SerializedObject soTar = new SerializedObject(target);
        SerializedProperty charAnimRef = soTar.FindProperty("charAnim");
        SerializedProperty clothPreset = soTar.FindProperty("clothPreset");
        SerializedProperty tailPreset = soTar.FindProperty("tailPreset");
        SerializedProperty earsPreset = soTar.FindProperty("earsPreset");
        SerializedProperty objectTypeRef = soTar.FindProperty("objectType");
        SerializedProperty createTypeRef = soTar.FindProperty("createType");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Character", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(charAnimRef, new GUIContent("Character Animator"), true);
        
        EditorGUILayout.Space();
        
        if(charAnim != null){
            
            EditorGUILayout.PropertyField(objectTypeRef, true);
            
            EditorGUILayout.PropertyField(createTypeRef, true);

            EditorGUILayout.Space();
            
            presetOpts = GUILayout.Toggle(presetOpts, "Presets", GUI.skin.button);

            if(presetOpts){
            
                EditorGUILayout.Space();

                EditorGUILayout.PropertyField(clothPreset, true);

                if((int)objectType == 1){

                    EditorGUILayout.PropertyField(tailPreset, true);
                    EditorGUILayout.PropertyField(earsPreset, true);

                }//objectType == Animal
                
            }//presetOpts
        
        }//charAnim != null
        
        soTar.ApplyModifiedProperties();
        
        if(charAnim != null){
            
            GUILayout.Space(5);

            ScriptableObject target2 = this;
            SerializedObject soTar2 = new SerializedObject(target2);

            SerializedProperty colPointsRef = soTar2.FindProperty("colPoints");
            SerializedProperty armsPointsRef = soTar2.FindProperty("armsPoints");
            SerializedProperty legsPointsRef = soTar2.FindProperty("legsPoints");
            
            SerializedProperty presetHead = soTar2.FindProperty("presetHead");
            SerializedProperty presetNeck = soTar2.FindProperty("presetNeck");
            SerializedProperty presetHips = soTar2.FindProperty("presetHips");
            
            SerializedProperty preset_LeftUpperArm = soTar2.FindProperty("preset_LeftUpperArm");
            SerializedProperty preset_RightUpperArm = soTar2.FindProperty("preset_RightUpperArm");
            
            SerializedProperty preset_LeftLowerArm = soTar2.FindProperty("preset_LeftLowerArm");
            SerializedProperty preset_RightLowerArm = soTar2.FindProperty("preset_RightLowerArm");
            
            SerializedProperty preset_LeftUpperLeg = soTar2.FindProperty("preset_LeftUpperLeg");
            SerializedProperty preset_LeftLowerLeg = soTar2.FindProperty("preset_LeftLowerLeg");
            
            SerializedProperty preset_RightUpperLeg = soTar2.FindProperty("preset_RightUpperLeg");
            SerializedProperty preset_RightLowerLeg = soTar2.FindProperty("preset_RightLowerLeg");
            
            SerializedProperty clothPoints = soTar2.FindProperty("clothPoints");
            SerializedProperty tailPoints = soTar2.FindProperty("tailPoints");
            SerializedProperty earsPoints = soTar2.FindProperty("earsPoints");
            
            SerializedProperty bodyParts = soTar2.FindProperty("bodyParts");
            SerializedProperty bodyPartsAnim = soTar2.FindProperty("bodyPartsAnim");
            
            GUILayout.Space(5);
            
            colPresetOpts = GUILayout.Toggle(colPresetOpts, "Collider Presets", GUI.skin.button);

            if(colPresetOpts){
            
                EditorGUILayout.Space();

                if((int)objectType == 0){

                    EditorGUILayout.PropertyField(colPointsRef, new GUIContent("Collision Points"), true);

                    if((int)colPoints == 0){

                        GUILayout.Space(5);

                        EditorGUILayout.PropertyField(armsPointsRef, true);

                    }//colPoints == upper

                    if((int)colPoints == 1){

                        GUILayout.Space(5);

                        EditorGUILayout.PropertyField(legsPointsRef, true);

                    }//colPoints == lower

                }//objectType = human

                if((int)objectType == 0){

                    EditorGUILayout.Space();

                    if((int)colPoints == 0 | (int)colPoints == 2){

                        EditorGUILayout.PropertyField(presetHead, new GUIContent("Head"), true);
                        EditorGUILayout.PropertyField(presetNeck, new GUIContent("Neck"), true);

                    }//colPoints == upper | (int)colPoints == both

                    EditorGUILayout.PropertyField(presetHips, new GUIContent("Hips"), true);

                    GUILayout.Space(5);

                    if((int)colPoints == 0){

                        if((int)armsPoints == 1){

                            EditorGUILayout.PropertyField(preset_LeftUpperArm, new GUIContent("Left Upper Arm"), true);
                            EditorGUILayout.PropertyField(preset_RightUpperArm, new GUIContent("Right Upper Arm"), true);

                        }//armsPoints == upper

                        if((int)armsPoints == 2){

                            EditorGUILayout.PropertyField(preset_LeftLowerArm, new GUIContent("Left Lower Arm"), true);
                            EditorGUILayout.PropertyField(preset_RightLowerArm, new GUIContent("Right Lower Arm"), true);

                        }//armsPoints == lower

                        if((int)armsPoints == 3){

                            EditorGUILayout.PropertyField(preset_LeftUpperArm, new GUIContent("Left Upper Arm"), true);
                            EditorGUILayout.PropertyField(preset_RightUpperArm, new GUIContent("Right Upper Arm"), true);

                            EditorGUILayout.Space();

                            EditorGUILayout.PropertyField(preset_LeftLowerArm, new GUIContent("Left Lower Arm"), true);
                            EditorGUILayout.PropertyField(preset_RightLowerArm, new GUIContent("Right Lower Arm"), true);

                        }//armsPoints == both

                    }//colPoints == upper

                    if((int)colPoints == 1){

                        if((int)legsPoints == 1){

                            EditorGUILayout.PropertyField(preset_LeftUpperLeg, new GUIContent("Left Upper Leg"), true);
                            EditorGUILayout.PropertyField(preset_RightUpperLeg, new GUIContent("Right Upper Leg"), true);

                        }//legsPoints == upper

                        if((int)legsPoints == 2){

                            EditorGUILayout.PropertyField(preset_LeftLowerLeg, new GUIContent("Left Lower Leg"), true);
                            EditorGUILayout.PropertyField(preset_RightLowerLeg, new GUIContent("Right Lower Leg"), true);

                        }//legsPoints == lower

                        if((int)legsPoints == 3){

                            EditorGUILayout.PropertyField(preset_LeftUpperLeg, new GUIContent("Left Upper Leg"), true);
                            EditorGUILayout.PropertyField(preset_RightUpperLeg, new GUIContent("Right Upper Leg"), true);

                            EditorGUILayout.Space();

                            EditorGUILayout.PropertyField(preset_LeftLowerLeg, new GUIContent("Left Lower Leg"), true);
                            EditorGUILayout.PropertyField(preset_RightLowerLeg, new GUIContent("Right Lower Leg"), true);

                        }//legsPoints == both

                    }//colPoints == lower

                    if((int)colPoints == 2){

                        EditorGUILayout.PropertyField(preset_LeftUpperArm, new GUIContent("Left Upper Arm"), true);
                        EditorGUILayout.PropertyField(preset_RightUpperArm, new GUIContent("Right Upper Arm"), true);

                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(preset_LeftLowerArm, new GUIContent("Left Lower Arm"), true);
                        EditorGUILayout.PropertyField(preset_RightLowerArm, new GUIContent("Right Lower Arm"), true);

                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(preset_LeftUpperLeg, new GUIContent("Left Upper Leg"), true);
                        EditorGUILayout.PropertyField(preset_RightUpperLeg, new GUIContent("Right Upper Leg"), true);

                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(preset_LeftLowerLeg, new GUIContent("Left Lower Leg"), true);
                        EditorGUILayout.PropertyField(preset_RightLowerLeg, new GUIContent("Right Lower Leg"), true);

                    }//colPoints == full body

                }//objectType == human

                if((int)objectType == 1){

                    EditorGUILayout.PropertyField(presetHead, new GUIContent("Head"), true);
                    EditorGUILayout.PropertyField(presetNeck, new GUIContent("Neck"), true);
                    EditorGUILayout.PropertyField(presetHips, new GUIContent("Hips"), true);

                }//objectType == animal
                
            }//colPresetOpts

            int iSpaceButWidth = 412;
            int iSpaceButWidth_Center = 200;
            int iButtonWidth = 200;

            EditorGUILayout.Space();
            
            bonePointsOpts = GUILayout.Toggle(bonePointsOpts, "Bone Points", GUI.skin.button);
            
            if(bonePointsOpts){

                EditorGUILayout.Space();
                
                if((int)objectType == 0){
                
                    bonePointsTabs = GUILayout.SelectionGrid(bonePointsTabs, new string[] { "Cloth"}, 1);
                
                }//objectType == human
                
                if((int)objectType == 1){
                
                    bonePointsTabs = GUILayout.SelectionGrid(bonePointsTabs, new string[] { "Cloth", "Tail", "Ears"}, 3);
                
                }//objectType == animal
                
                EditorGUILayout.Space();
                
                if(bonePointsTabs == 0){
                    
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(clothPoints, true);

                    EditorGUILayout.Space();

                    GUILayout.BeginHorizontal();

                    GUILayout.Space(Screen.width/2 - iSpaceButWidth/2);

                    if(GUILayout.Button("Add Selected", GUILayout.Width(iButtonWidth))) {

                        ClothPoints_Add();

                    }//Button

                    if(GUILayout.Button("Clear Cloth Points", GUILayout.Width(iButtonWidth))) {

                        ClothPoints_Clear();

                    }//Button

                    GUILayout.EndHorizontal();
                
                }//bonePointsTabs == cloth
                
            }//bonePointsOpts

            if((int)objectType == 0){

                if(!charAnim.isHuman){
                        
                    boneColPointsOpts = GUILayout.Toggle(boneColPointsOpts, "Bone Collider Points", GUI.skin.button);

                    if(boneColPointsOpts){
                        
                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(bodyParts, true);

                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();

                        GUILayout.Space(Screen.width/2 - iSpaceButWidth_Center/2);

                        if(GUILayout.Button("Clear Body Parts", GUILayout.Width(iButtonWidth))) {

                            ClearParts_Generic();

                        }//Button

                        GUILayout.EndHorizontal();
                            
                    }//boneColPointsOpts

                }//!isHuman

            }//objectType == human

            if((int)objectType == 1){
                    
                if(bonePointsOpts){
                    
                    if(bonePointsTabs == 1){
                    
                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(tailPoints, true);

                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();

                        GUILayout.Space(Screen.width/2 - iSpaceButWidth/2);

                        if(GUILayout.Button("Add Selected", GUILayout.Width(iButtonWidth))) {

                            TailPoints_Add();

                        }//Button

                        if(GUILayout.Button("Clear Tail Points", GUILayout.Width(iButtonWidth))) {

                            TailPoints_Clear();

                        }//Button

                        GUILayout.EndHorizontal();
                        
                    }//bonePointsTabs == 1
                    
                    if(bonePointsTabs == 2){
                        
                        EditorGUILayout.Space();

                        EditorGUILayout.PropertyField(earsPoints, true);

                        EditorGUILayout.Space();

                        GUILayout.BeginHorizontal();

                        GUILayout.Space(Screen.width/2 - iSpaceButWidth/2);

                        if(GUILayout.Button("Add Selected", GUILayout.Width(iButtonWidth))) {

                            EarsPoints_Add();

                        }//Button

                        if(GUILayout.Button("Clear Ears Points", GUILayout.Width(iButtonWidth))) {

                            EarsPoints_Clear();

                        }//Button

                        GUILayout.EndHorizontal();
                        
                    }//bonePointsTabs == ears
                        
                }//bonePointsOpts

                EditorGUILayout.Space();
                    
                boneColPointsOpts = GUILayout.Toggle(boneColPointsOpts, "Bone Collider Points", GUI.skin.button);

                if(boneColPointsOpts){
                    
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(bodyPartsAnim, new GUIContent("Animal Body Parts"), true);

                    EditorGUILayout.Space();

                    GUILayout.BeginHorizontal();

                    GUILayout.Space(Screen.width/2 - iSpaceButWidth_Center/2);

                    if(GUILayout.Button("Clear Body Parts", GUILayout.Width(iButtonWidth))) {

                        ClearParts_Generic();

                    }//Button

                    GUILayout.EndHorizontal();
                        
                }//boneColPointsOpts

            }//objectType == animal
            
            soTar2.ApplyModifiedProperties();
            
        }//charAnim != null
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        GUILayout.Space(3);
        
        if(charAnim != null){
        
            GUI.enabled = true;
            
        //charAnim != null
        } else {
            
            GUI.enabled = false;
            
        }//charAnim != null
            
        GUILayout.BeginHorizontal();
            
        if(GUILayout.Button("Add Default Presets")) {

            Presets_DefaultSet();

        }//Button
            
        if(GUILayout.Button("Clear Presets")) {

            Presets_Clear();

        }//Button
            
        GUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        
        if(GUILayout.Button("Setup")) {

            DynaBone_Setup();

        }//Button
            
        if(GUILayout.Button("Clear")) {

            DynaBone_Clear();

        }//Button
        
        EditorGUILayout.Space();
        
    }//SetupDisplay
    
    void DebugDisplay(){
        
        scrollPos = GUILayout.BeginScrollView(scrollPos, false, true, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            
        EditorGUILayout.HelpBox("Use the options below to utilize debug.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Debug Notifications", EditorStyles.centeredGreyMiniLabel);
        
        EditorGUILayout.Space();
        
        debugInt = GUILayout.Toolbar(debugInt, new string[] { "OFF", "ON" });
        
        if(debugInt == 0){
            
            useDebug = false;
            
        }//debugInt == 0
        
        if(debugInt == 1){
            
            useDebug = true;
            
        }//debugInt == 1
        
        EditorGUILayout.Space();
        
        ScriptableObject target = this;
        SerializedObject soTar = new SerializedObject(target);
        
        SerializedProperty bodyParts = soTar.FindProperty("bodyParts");
        SerializedProperty rootTrans = soTar.FindProperty("rootTrans");
        
        SerializedProperty dynamBoneCols = soTar.FindProperty("dynamBoneCols");
        SerializedProperty dynamBone = soTar.FindProperty("dynamBone");

        EditorGUILayout.PropertyField(rootTrans, new GUIContent("Root Transform"), true);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.PropertyField(dynamBoneCols, new GUIContent("Dynamic Bone Colliders"), true);
        EditorGUILayout.PropertyField(dynamBone, new GUIContent("Dynamic Bones"), true);
        
        EditorGUILayout.Space();
        
        if((int)objectType == 0){
        
            if(charAnim != null && charAnim.isHuman){

                EditorGUILayout.PropertyField(bodyParts, true);

            }//charAnim != null & isHuman
            
        }//objectType == human
        
        soTar.ApplyModifiedProperties();
        
        EditorGUILayout.EndScrollView();
            
        EditorGUILayout.Space();
        
    }//DebugDisplay
    
    
/////////////////////////////////////////////////////////////////////////////
///
///     EDITOR ACTIONS
///
//////////////////////////////////////////////////////////////////////////////
    
    
//////////////////////////////////////
///
///     BODY PARTS ACTIONS
///
//////////////////////////////////////
    
    
    void GrabBodyParts(){
        
        if(rootTrans == null){
            
            rootTrans = charAnim.transform;
        
        }//rootTrans = null
        
        if(charAnim.isHuman){
        
            bodyParts.Pelvis = charAnim.GetBoneTransform(HumanBodyBones.Hips);

            bodyParts.LeftHips = charAnim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            bodyParts.RightHips = charAnim.GetBoneTransform(HumanBodyBones.RightUpperLeg);

            bodyParts.LeftKnee = charAnim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            bodyParts.RightKnee = charAnim.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            bodyParts.RightFoot = charAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
            bodyParts.LeftFoot = charAnim.GetBoneTransform(HumanBodyBones.RightFoot);

            bodyParts.LeftArm = charAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            bodyParts.RightArm = charAnim.GetBoneTransform(HumanBodyBones.RightUpperArm);

            bodyParts.LeftElbow = charAnim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            bodyParts.RightElbow = charAnim.GetBoneTransform(HumanBodyBones.RightLowerArm);

            bodyParts.LeftHand = charAnim.GetBoneTransform(HumanBodyBones.LeftHand);
            bodyParts.RightHand = charAnim.GetBoneTransform(HumanBodyBones.RightHand);

            bodyParts.Chest = charAnim.GetBoneTransform(HumanBodyBones.Chest);

            bodyParts.Neck = charAnim.GetBoneTransform(HumanBodyBones.Neck);
            bodyParts.Head = charAnim.GetBoneTransform(HumanBodyBones.Head);

            bodyPartsTemp = new List<Transform>();

            if(bodyParts.Pelvis != null){

                bodyPartsTemp.Add(bodyParts.Pelvis);

            //Pelvis != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Pelvis");

                }//useDebug

            }//Pelvis != null

            if(bodyParts.LeftHips != null){

                bodyPartsTemp.Add(bodyParts.LeftHips);

            //LeftHips != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Hips");

                }//useDebug

            }//LeftHips != null

            if(bodyParts.LeftKnee != null){

                bodyPartsTemp.Add(bodyParts.LeftKnee);

            //LeftKnee != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Knee");

                }//useDebug

            }//LeftKnee != null

            if(bodyParts.LeftFoot != null){

                bodyPartsTemp.Add(bodyParts.LeftFoot);

            //LeftFoot != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Foot");

                }//useDebug

            }//LeftFoot != null

            if(bodyParts.RightHips != null){

                bodyPartsTemp.Add(bodyParts.RightHips);

            //RightHips != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Hips");

                }//useDebug

            }//RightHips != null

            if(bodyParts.RightKnee != null){

                bodyPartsTemp.Add(bodyParts.RightKnee);

                    //RightKnee != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Knee");

                }//useDebug

            }//RightKnee != null

            if(bodyParts.RightFoot != null){

                bodyPartsTemp.Add(bodyParts.RightFoot);

            //RightFoot != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Foot");

                }//useDebug

            }//RightFoot != null

            if(bodyParts.LeftArm != null){

                bodyPartsTemp.Add(bodyParts.LeftArm);

            //LeftArm != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Arm");

                }//useDebug

            }//LeftArm != null

            if(bodyParts.LeftElbow != null){

                bodyPartsTemp.Add(bodyParts.LeftElbow);

            //LeftElbow != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Elbow");

                }//useDebug

            }//LeftElbow != null

            if(bodyParts.LeftHand != null){

                bodyPartsTemp.Add(bodyParts.LeftHand);

            //LeftHand != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Left Hand");

                }//useDebug

            }//LeftHand != null

            if(bodyParts.RightArm != null){

                bodyPartsTemp.Add(bodyParts.RightArm);

            //RightArm != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Arm");

                }//useDebug

            }//RightArm != null

            if(bodyParts.RightElbow != null){

                bodyPartsTemp.Add(bodyParts.RightElbow);

            //RightElbow != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Elbow");

                }//useDebug

            }//RightElbow != null

            if(bodyParts.RightHand != null){

                bodyPartsTemp.Add(bodyParts.RightHand);

            //RightHand != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Right Hand");

                }//useDebug

            }//RightHand != null

            if(bodyParts.Chest != null){

                bodyPartsTemp.Add(bodyParts.Chest);

            //Chest != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Middle Spine");

                }//useDebug

            }//Chest != null

            if(bodyParts.Head != null){

                bodyPartsTemp.Add(bodyParts.Head);

            //Head != null
            } else {

                if(useDebug){

                    Debug.Log("Transform Missing - Head");

                }//useDebug

            }//Head != null

            if(useDebug){

                Debug.Log("Body Parts Grabbed");

            }//useDebug
            
        //isHuman
        } else {
            
            anim_BodyPartsTemp = new List<Transform>();
            
        }//isHuman
        
    }//GrabBodyParts
    
    void ClearParts(){
        
        if(rootTrans != null){
            
            rootTrans = null;
        
        }//rootTrans = null
        
        if((int)objectType == 0){
        
            if(bodyPartsTemp.Count > 0){

                bodyPartsTemp.Clear();

                if(useDebug){

                    Debug.Log("Body Parts Cleared");

                }//useDebug

            //bodyPartsTemp.Count > 0
            } else {

                if(useDebug){

                    Debug.Log("No Body Parts To Clear");

                }//useDebug

            }//bodyPartsTemp.Count > 0

            BodyParts tempBodyParts = new BodyParts();
            bodyParts = tempBodyParts;
            
        }//objectType == human
        
        if((int)objectType == 1){
            
            BodyParts_Anim tempAnimBodyParts = new BodyParts_Anim();
            bodyPartsAnim = tempAnimBodyParts;
            
        }//objectType == animal
        
        dynamBoneCols = new List<DynamicBoneColliderBase>();
        dynamBone = new List<DynamicBone>();
        
    }//ClearParts
    
    public void ClearParts_Generic(){
        
        if((int)objectType == 0){
            
            BodyParts tempBodyParts = new BodyParts();
            bodyParts = tempBodyParts;
            
        }//objectType == human
        
        if((int)objectType == 1){
            
            BodyParts_Anim tempAnimBodyParts = new BodyParts_Anim();
            bodyPartsAnim = tempAnimBodyParts;
        
        }//objectType == animal
        
    }//ClearParts_Generic
    
    
//////////////////////////////////////
///
///     CLOTH POINTS ACTIONS
///
//////////////////////////////////////
    
    
    public void ClothPoints_Add(){
        
        if(Selection.gameObjects.Length > 0){
            
            if(clothPoints.Count > 0){
                
                foreach(GameObject tempObjs in Selection.gameObjects){

                    if(!clothPoints.Contains(tempObjs.transform)){
                        
                        clothPoints.Add(tempObjs.transform);
                        
                        if(useDebug){
                         
                            Debug.Log(tempObjs.name + " - Transform added");
                            
                        }//useDebug
                        
                    //!Contains
                    } else {
                        
                        if(useDebug){
                            
                            Debug.Log(tempObjs.name + " - Transform already present.");
                            
                        }//useDebug
                        
                    }//!Contains
                    
                }//foreach tempObjs
                
            //clothPoints.Count > 0
            } else {
                
                foreach(GameObject tempObjs in Selection.gameObjects){
                    
                    clothPoints.Add(tempObjs.transform);
                    
                }//foreach tempObjs in selection
                
                if(clothPoints.Count > 0){
                    
                    if(useDebug){
                    
                        Debug.Log("Cloth points added to list.");
                    
                    }//useDebug
                    
                }//clothPoints.Count > 0
                
            }//clothPoints.Count > 0
            
        }//Selection.Length > 0
        
    }//ClothPoints_Add
    
    public void ClothPoints_Clear(){
        
        if(clothPoints.Count > 0){
            
            clothPoints.Clear();

            if(useDebug){

                Debug.Log("Cloth points cleared.");

            }//useDebug
        
        //clothPoints.Count > 0
        } else {
            
            if(useDebug){

                Debug.Log("No cloth points!");

            }//useDebug
            
        }//clothPoints.Count > 0
            
    }//ClothPoints_Clear
    
    
//////////////////////////////////////
///
///     TAIL POINTS ACTIONS
///
//////////////////////////////////////
    
    
    public void TailPoints_Add(){
        
        if(Selection.gameObjects.Length > 0){
            
            if(tailPoints.Count > 0){
                
                foreach(GameObject tempObjs in Selection.gameObjects){

                    if(!tailPoints.Contains(tempObjs.transform)){
                        
                        tailPoints.Add(tempObjs.transform);
                        
                        if(useDebug){
                         
                            Debug.Log(tempObjs.name + " - Transform added");
                            
                        }//useDebug
                        
                    //!Contains
                    } else {
                        
                        if(useDebug){
                            
                            Debug.Log(tempObjs.name + " - Transform already present.");
                            
                        }//useDebug
                        
                    }//!Contains
                    
                }//foreach tempObjs
                
            //tailPoints.Count > 0
            } else {
                
                foreach(GameObject tempObjs in Selection.gameObjects){
                    
                    tailPoints.Add(tempObjs.transform);
                    
                }//foreach tempObjs in selection
                
                if(tailPoints.Count > 0){
                    
                    if(useDebug){
                    
                        Debug.Log("Tail points added to list.");
                    
                    }//useDebug
                    
                }//tailPoints.Count > 0
                
            }//tailPoints.Count > 0
            
        }//Selection.Length > 0
        
    }//TailPoints_Add
    
    public void TailPoints_Clear(){
        
        if(tailPoints.Count > 0){
            
            tailPoints.Clear();

            if(useDebug){

                Debug.Log("Tail points cleared.");

            }//useDebug
        
        //tailPoints.Count > 0
        } else {
            
            if(useDebug){

                Debug.Log("No tail points!");

            }//useDebug
            
        }//tailPoints.Count > 0
            
    }//TailPoints_Clear
    
    
//////////////////////////////////////
///
///     EARS POINTS ACTIONS
///
//////////////////////////////////////
    
    
    public void EarsPoints_Add(){
        
        if(Selection.gameObjects.Length > 0){
            
            if(earsPoints.Count > 0){
                
                foreach(GameObject tempObjs in Selection.gameObjects){

                    if(!earsPoints.Contains(tempObjs.transform)){
                        
                        earsPoints.Add(tempObjs.transform);
                        
                        if(useDebug){
                         
                            Debug.Log(tempObjs.name + " - Transform added");
                            
                        }//useDebug
                        
                    //!Contains
                    } else {
                        
                        if(useDebug){
                            
                            Debug.Log(tempObjs.name + " - Transform already present.");
                            
                        }//useDebug
                        
                    }//!Contains
                    
                }//foreach tempObjs
                
            //earsPoints.Count > 0
            } else {
                
                foreach(GameObject tempObjs in Selection.gameObjects){
                    
                    earsPoints.Add(tempObjs.transform);
                    
                }//foreach tempObjs in selection
                
                if(earsPoints.Count > 0){
                    
                    if(useDebug){
                    
                        Debug.Log("Ears points added to list.");
                    
                    }//useDebug
                    
                }//earsPoints.Count > 0
                
            }//earsPoints.Count > 0
            
        }//Selection.Length > 0
        
    }//EarsPoints_Add
    
    public void EarsPoints_Clear(){
        
        if(earsPoints.Count > 0){
            
            earsPoints.Clear();

            if(useDebug){

                Debug.Log("Ears points cleared.");

            }//useDebug
        
        //earsPoints.Count > 0
        } else {
            
            if(useDebug){

                Debug.Log("No ears points!");

            }//useDebug
            
        }//earsPoints.Count > 0
        
    }//EarsPoints_Clear
    
    
//////////////////////////////////////
///
///     PRESET ACTIONS
///
//////////////////////////////////////
    
    
    public void Presets_DefaultSet(){
        
        if((int)objectType == 0){

            presetHead = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_Head")));
            presetNeck = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_Neck")));
            presetHips = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_Hips")));
            
            preset_LeftUpperArm = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_LeftUpperArm")));
            preset_LeftLowerArm = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_LeftLowerArm")));

            preset_RightUpperArm = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_RightUpperArm")));
            preset_RightLowerArm = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_RightLowerArm")));

            preset_LeftUpperLeg = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_LeftUpperLeg")));
            preset_LeftLowerLeg = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_LeftLowerLeg")));

            preset_RightUpperLeg = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_RightUpperLeg")));
            preset_RightLowerLeg = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Hum_RightLowerLeg")));
            
        }//objectType == human
        
        if((int)objectType == 1){
            
            presetHead = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Animal_Head")));
            presetNeck = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Animal_Neck")));
            presetHips = AssetDatabase.LoadAssetAtPath<DynaBone_ColTemplate>(AssetDatabase.GUIDToAssetPath(Asset_Find("Animal_Hips")));
            
            tailPreset = AssetDatabase.LoadAssetAtPath<DynaBone_Template>(AssetDatabase.GUIDToAssetPath(Asset_Find("Animal_Tail")));
            earsPreset = AssetDatabase.LoadAssetAtPath<DynaBone_Template>(AssetDatabase.GUIDToAssetPath(Asset_Find("Animal_EarsBasic")));
            
        }//objectType == animal
        
        if(presetHips != null){
            
            if(useDebug){
            
                Debug.Log("Default Presets Set");
            
            }//useDebug
        
        //presetHips != null
        } else {
            
            if(useDebug){
                
                Debug.Log("Default Presets not found.");
                
            }//useDebug
            
        }//presetHips != null
        
    }//Presets_DefaultSet
    
    public void Presets_Clear(){
        
        presetHead = null;
        presetNeck = null;
        presetHips = null;
            
        preset_LeftUpperArm = null;
        preset_LeftLowerArm = null;
            
        preset_RightUpperArm = null;
        preset_RightLowerArm = null;
            
        preset_LeftUpperLeg = null;
        preset_LeftLowerLeg = null;
            
        preset_RightUpperLeg = null;
        preset_RightLowerLeg = null;
        
        clothPreset = null;
        tailPreset = null;
        earsPreset = null;
        
        if(presetHips == null){
            
            if(useDebug){
            
                Debug.Log("Presets Cleared");
            
            }//useDebug
        
        //presetHips != null
        } else {
            
            if(useDebug){
                
                Debug.Log("Presets not cleared.");
                
            }//useDebug
            
        }//presetHips != null
        
    }//Presets_Clear
    
    
//////////////////////////////////////
///
///     DYNABONE ACTIONS
///
//////////////////////////////////////
    
    
    public void DynaBone_Setup(){
        
        dynamBoneCols = new List<DynamicBoneColliderBase>();

        //objecttype = humanoid
        if((int)objectType == 0){
                
            //colpoints = upper
            if((int)colPoints == 0){
                    
                DynaBoneCreate_Head();
                DynaBoneCreate_Neck();
                    
                //armsPoints = upper or both lower/upper
                if((int)armsPoints == 1 | (int)armsPoints == 3){

                    DynaBoneCreate_LeftUpArm();
                    DynaBoneCreate_RightUpArm();

                }//armsPoints == upper | armsPoints == both

                //armsPoints = lower or both lower/upper
                if((int)armsPoints == 2 | (int)armsPoints == 3){
                            
                    DynaBoneCreate_LeftLowArm();
                    DynaBoneCreate_RightLowArm();

                }//armsPoints == lower | armsPoints == both
                    
            }//colPoints == upper
                
            //colpoints = lower
            if((int)colPoints == 1){

                DynaBoneCreate_Hips();

                //legsPoints = upper or both lower/upper
                if((int)legsPoints == 1 | (int)legsPoints == 3){

                    DynaBoneCreate_LeftUpLeg();
                    DynaBoneCreate_RightUpLeg();

                }//legsPoints == upper | legsPoints == both

                //legsPoints = lower or both lower/upper
                if((int)legsPoints == 2 | (int)legsPoints == 3){
                            
                    DynaBoneCreate_LeftLowLeg();
                    DynaBoneCreate_RightLowLeg();

                }//legsPoints == lower | legsPoints == both

            }//colPoints = lower
                
            //colpoints = full body
            if((int)colPoints == 2){
                    
                DynaBoneCreate_Head();
                DynaBoneCreate_Neck();
                DynaBoneCreate_Hips();
                    
                DynaBoneCreate_LeftUpArm();
                DynaBoneCreate_RightUpArm();
                    
                DynaBoneCreate_LeftLowArm();
                DynaBoneCreate_RightLowArm();
                    
                DynaBoneCreate_LeftUpLeg();
                DynaBoneCreate_RightUpLeg();
                    
                DynaBoneCreate_LeftLowLeg();
                DynaBoneCreate_RightLowLeg();
                    
            }//colPoints = both
                
        }//objectType = human
            
        //objecttype = animal
        if((int)objectType == 1){
                
            DynaBoneCreate_Head();
            DynaBoneCreate_Neck();
            DynaBoneCreate_Hips();
                
        }//objectType = animal
            
        DynaBonesCreate();
        
    }//DynaBone_Setup
    
    public void DynaBone_Clear(){
        
        Component[] newComps = rootTrans.GetComponentsInChildren<DynamicBoneCollider>();
        
        if(newComps.Length > 0){
            
            foreach(Component tempComps in newComps) {

                DestroyImmediate(tempComps);

            }//foreach tempComps
        
        }//newComps.Length > 0
        
        if((int)createType == 0){
            
            foreach(Transform child in rootTrans) {

                if(child.name == "Dynamic Bones"){

                    DestroyImmediate(child.gameObject);

                }//child.name = "Dynamic Bones"

            }//foreach child in rootTrans
            
        }//createType = RootTransform
        
        if((int)createType == 1){
            
            Component[] newCompsBone = rootTrans.GetComponentsInChildren<DynamicBone>();
            
            if(newCompsBone.Length > 0){
                
                foreach(Component tempCompsBone in newCompsBone) {

                    DestroyImmediate(tempCompsBone);

                }//foreach tempComps
            
            }//newCompsBone.Length > 0
            
        }//createType = origTransform
        
        dynamBoneCols = new List<DynamicBoneColliderBase>();
        dynamBone = new List<DynamicBone>();
        
        if(useDebug){
            
            Debug.Log("Dynamic Bones Cleared");
            
        }//useDebug
        
    }//DynaBone_Clear
    
    
//////////////////////////////////////
///
///     DYNAMIC BONE BODY PARTS CREATION
///
//////////////////////////////////////
    
    
/////////////////////
///
///     UPPER
///
/////////////////////
    
    
    public void DynaBoneCreate_Head(){
        
        if(presetHead != null){
            
            if((int)objectType == 0){
            
                if(bodyParts.Head != null){

                    DynamicBoneCollider headCol = bodyParts.Head.gameObject.AddComponent<DynamicBoneCollider>();

                    headCol.m_Direction = presetHead.direction;
                    headCol.m_Center = presetHead.center;
                    headCol.m_Radius = presetHead.radius;
                    headCol.m_Height = presetHead.height;

                    dynamBoneCols.Add(headCol);

                    if(useDebug){

                        Debug.Log("Head BoneCollider Created");

                    }//useDebug

                //bodyParts.Head != null
                } else {

                    if(useDebug){

                        Debug.Log("Head Transform = null");

                    }//useDebug

                }//bodyParts.Head != null

            }//objectType == human
        
            if((int)objectType == 1){

                if(bodyPartsAnim.Head != null){

                    DynamicBoneCollider headCol = bodyPartsAnim.Head.gameObject.AddComponent<DynamicBoneCollider>();

                    headCol.m_Direction = presetHead.direction;
                    headCol.m_Center = presetHead.center;
                    headCol.m_Radius = presetHead.radius;
                    headCol.m_Height = presetHead.height;

                    dynamBoneCols.Add(headCol);

                    if(useDebug){

                        Debug.Log("Head BoneCollider Created");

                    }//useDebug

                //bodyParts.Head != null
                } else {

                    if(useDebug){

                        Debug.Log("Head Transform = null");

                    }//useDebug

                }//bodyParts.Head != null

            }//objectType == animal
        
        //presetHead != null
        } else {

            if(useDebug){

                Debug.Log("No Head Preset Set.");

            }//useDebug

        }//presetHead != null
    
    }//DynaBoneCreate_Head
    
    public void DynaBoneCreate_Neck(){
        
        if(presetNeck != null){
            
            if((int)objectType == 0){
            
                if(bodyParts.Neck != null){

                    DynamicBoneCollider neckCol = bodyParts.Neck.gameObject.AddComponent<DynamicBoneCollider>();

                    neckCol.m_Direction = presetNeck.direction;
                    neckCol.m_Center = presetNeck.center;
                    neckCol.m_Radius = presetNeck.radius;
                    neckCol.m_Height = presetNeck.height;

                    dynamBoneCols.Add(neckCol);

                    if(useDebug){

                        Debug.Log("Neck BoneCollider Created");

                    }//useDebug

                //bodyParts.Neck != null
                } else {

                    if(useDebug){

                        Debug.Log("Neck Transform = null");

                    }//useDebug

                }//bodyParts.Neck != null

            }//objectType == human
        
            if((int)objectType == 1){

                if(bodyPartsAnim.Neck != null){

                    DynamicBoneCollider neckCol = bodyPartsAnim.Neck.gameObject.AddComponent<DynamicBoneCollider>();

                    neckCol.m_Direction = presetNeck.direction;
                    neckCol.m_Center = presetNeck.center;
                    neckCol.m_Radius = presetNeck.radius;
                    neckCol.m_Height = presetNeck.height;

                    dynamBoneCols.Add(neckCol);

                    if(useDebug){

                        Debug.Log("Neck BoneCollider Created");

                    }//useDebug

                //bodyParts.Neck != null
                } else {

                    if(useDebug){

                        Debug.Log("Neck Transform = null");

                    }//useDebug

                }//bodyParts.Neck != null

            }//objectType == animal
        
        //presetNeck != null
        } else {

            if(useDebug){

                Debug.Log("No Neck Preset Set.");

            }//useDebug

        }//presetNeck != null
    
    }//DynaBoneCreate_Neck
    
    public void DynaBoneCreate_Hips(){
        
        if(presetHips != null){
            
            if((int)objectType == 0){
            
                if(bodyParts.Pelvis != null){

                    DynamicBoneCollider pelvisCol = bodyParts.Pelvis.gameObject.AddComponent<DynamicBoneCollider>();

                    pelvisCol.m_Direction = presetHips.direction;
                    pelvisCol.m_Center = presetHips.center;
                    pelvisCol.m_Radius = presetHips.radius;
                    pelvisCol.m_Height = presetHips.height;

                    dynamBoneCols.Add(pelvisCol);

                    if(useDebug){

                        Debug.Log("Hips BoneCollider Created");

                    }//useDebug

                //bodyParts.Pelvis != null
                } else {

                    if(useDebug){

                        Debug.Log("Pelvis Transform = null");

                    }//useDebug

                }//bodyParts.Pelvis != null
                
            }//objectType == human
            
            if((int)objectType == 1){

                if(bodyPartsAnim.Pelvis != null){

                    DynamicBoneCollider pelvisCol = bodyPartsAnim.Pelvis.gameObject.AddComponent<DynamicBoneCollider>();

                    pelvisCol.m_Direction = presetHips.direction;
                    pelvisCol.m_Center = presetHips.center;
                    pelvisCol.m_Radius = presetHips.radius;
                    pelvisCol.m_Height = presetHips.height;

                    dynamBoneCols.Add(pelvisCol);

                    if(useDebug){

                        Debug.Log("Hips BoneCollider Created");

                    }//useDebug

                //bodyParts.Neck != null
                } else {

                    if(useDebug){

                        Debug.Log("Pelvis Transform = null");

                    }//useDebug

                }//bodyParts.Neck != null

            }//objectType == animal
                            
        //presetHips != null
        } else {
                            
            if(useDebug){
                                
                Debug.Log("No Hips Preset Set.");
                            
            }//useDebug
                            
        }//presetHips != null
        
    }//DynaBoneCreate_Hips
    
    
/////////////////////
///
///     ARMS UPPER
///
/////////////////////
    
    
    public void DynaBoneCreate_LeftUpArm(){
        
        if(preset_LeftUpperArm != null){
            
            if(bodyParts.LeftArm != null){
                            
                DynamicBoneCollider leftUpArmCol = bodyParts.LeftArm.gameObject.AddComponent<DynamicBoneCollider>();

                leftUpArmCol.m_Direction = preset_LeftUpperArm.direction;
                leftUpArmCol.m_Center = preset_LeftUpperArm.center;
                leftUpArmCol.m_Radius = preset_LeftUpperArm.radius;
                leftUpArmCol.m_Height = preset_LeftUpperArm.height;

                dynamBoneCols.Add(leftUpArmCol);

                if(useDebug){

                    Debug.Log("LeftUpperArm BoneCollider Created");

                }//useDebug
                
            //bodyParts.LeftArm != null
            } else {
                
                if(useDebug){

                    Debug.Log("LeftArm Transform = null");

                }//useDebug
                
            }//bodyParts.LeftArm != null
                                
        //preset_LeftUpperArm != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Left Upper Arm Preset Set.");

            }//useDebug
                                
        }//preset_LeftUpperArm != null
    
    }//DynaBoneCreate_LeftUpArm
    
    public void DynaBoneCreate_RightUpArm(){
        
        if(preset_RightUpperArm != null){
            
            if(bodyParts.RightArm != null){
                            
                DynamicBoneCollider rightUpArmCol = bodyParts.RightArm.gameObject.AddComponent<DynamicBoneCollider>();

                rightUpArmCol.m_Direction = preset_RightUpperArm.direction;
                rightUpArmCol.m_Center = preset_RightUpperArm.center;
                rightUpArmCol.m_Radius = preset_RightUpperArm.radius;
                rightUpArmCol.m_Height = preset_RightUpperArm.height;

                dynamBoneCols.Add(rightUpArmCol);

                if(useDebug){

                    Debug.Log("RightUpperArm BoneCollider Created");

                }//useDebug
                
            //bodyParts.RightArm != null
            } else {
                
                if(useDebug){

                    Debug.Log("RightArm Transform = null");

                }//useDebug
                
            }//bodyParts.RightArm != null
                              
        //preset_RightUpperArm != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Right Upper Arm Preset Set.");

            }//useDebug
                                
        }//preset_RightUpperArm != null
        
    }//DynaBoneCreate_RightUpArm
    
    
/////////////////////
///
///     ARMS LOWER
///
/////////////////////
    
    
    public void DynaBoneCreate_LeftLowArm(){
        
        if(preset_LeftLowerArm != null){
            
            if(bodyParts.LeftElbow != null){

                DynamicBoneCollider leftLowerArmCol = bodyParts.LeftElbow.gameObject.AddComponent<DynamicBoneCollider>();

                leftLowerArmCol.m_Direction = preset_LeftLowerArm.direction;
                leftLowerArmCol.m_Center = preset_LeftLowerArm.center;
                leftLowerArmCol.m_Radius = preset_LeftLowerArm.radius;
                leftLowerArmCol.m_Height = preset_LeftLowerArm.height;

                dynamBoneCols.Add(leftLowerArmCol);

                if(useDebug){

                    Debug.Log("LeftLowerArm BoneCollider Created");

                }//useDebug
                
            //bodyParts.LeftElbow != null
            } else {
                
                if(useDebug){

                    Debug.Log("LeftElbow Transform = null");

                }//useDebug
                
            }//bodyParts.LeftElbow != null
                                
        //preset_LeftLowerArm != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Left Lower Arm Preset Set.");

            }//useDebug
                                
        }//preset_LeftLowerArm != null
        
    }//DynaBoneCreate_LeftLowArm
    
    public void DynaBoneCreate_RightLowArm(){
        
        if(preset_RightLowerArm != null){
            
            if(bodyParts.RightElbow != null){
                                
                DynamicBoneCollider rightLowerArmCol = bodyParts.RightElbow.gameObject.AddComponent<DynamicBoneCollider>();

                rightLowerArmCol.m_Direction = preset_RightLowerArm.direction;
                rightLowerArmCol.m_Center = preset_RightLowerArm.center;
                rightLowerArmCol.m_Radius = preset_RightLowerArm.radius;
                rightLowerArmCol.m_Height = preset_RightLowerArm.height;

                dynamBoneCols.Add(rightLowerArmCol);

                if(useDebug){

                    Debug.Log("RightLowerArm BoneCollider Created");

                }//useDebug
                
            //bodyParts.RightElbow != null
            } else {
                
                if(useDebug){

                    Debug.Log("RightElbow Transform = null");

                }//useDebug
                
            }//bodyParts.RightElbow != null
                                
        //preset_RightLowerArm != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Right Lower Arm Preset Set.");

            }//useDebug
                                
        }//preset_RightLowerArm != null
        
    }//DynaBoneCreate_RightLowArm
    
    
/////////////////////
///
///     LEGS UPPER
///
/////////////////////
    
    
    public void DynaBoneCreate_LeftUpLeg(){
        
        if(preset_LeftUpperLeg != null){
            
            if(bodyParts.LeftHips != null){
                            
                DynamicBoneCollider leftUpLegCol = bodyParts.LeftHips.gameObject.AddComponent<DynamicBoneCollider>();

                leftUpLegCol.m_Direction = preset_LeftUpperLeg.direction;
                leftUpLegCol.m_Center = preset_LeftUpperLeg.center;
                leftUpLegCol.m_Radius = preset_LeftUpperLeg.radius;
                leftUpLegCol.m_Height = preset_LeftUpperLeg.height;

                dynamBoneCols.Add(leftUpLegCol);

                if(useDebug){

                    Debug.Log("LeftUpperLeg BoneCollider Created");

                }//useDebug
                
            //bodyParts.LeftHips != null
            } else {
                
                if(useDebug){

                    Debug.Log("LeftHips Transform = null");

                }//useDebug
                
            }//bodyParts.LeftHips != null
                                
        //preset_LeftUpperLeg != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Left Upper Leg Preset Set.");

            }//useDebug
                                
        }//preset_LeftUpperLeg != null
        
    }//DynaBoneCreate_LeftUpLeg
    
    public void DynaBoneCreate_RightUpLeg(){
        
        if(preset_RightUpperLeg != null){
            
            if(bodyParts.RightHips != null){
                            
                DynamicBoneCollider rightUpLegCol = bodyParts.RightHips.gameObject.AddComponent<DynamicBoneCollider>();

                rightUpLegCol.m_Direction = preset_RightUpperLeg.direction;
                rightUpLegCol.m_Center = preset_RightUpperLeg.center;
                rightUpLegCol.m_Radius = preset_RightUpperLeg.radius;
                rightUpLegCol.m_Height = preset_RightUpperLeg.height;

                dynamBoneCols.Add(rightUpLegCol);

                if(useDebug){

                    Debug.Log("RightUpperLeg BoneCollider Created");

                }//useDebug
                
            //bodyParts.RightHips != null
            } else {
                
                if(useDebug){

                    Debug.Log("RightHips Transform = null");

                }//useDebug
                
            }//bodyParts.RightHips != null
                              
        //preset_RightUpperLeg != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Right Upper Leg Preset Set.");

            }//useDebug
                                
        }//preset_RightUpperLeg != null

    }//DynaBoneCreate_RightUpLeg
    
    
/////////////////////
///
///     LEGS LOWER
///
/////////////////////
    
    
    public void DynaBoneCreate_LeftLowLeg(){
        
        if(preset_LeftLowerLeg != null){
            
            if(bodyParts.LeftKnee != null){

                DynamicBoneCollider leftLowerLegCol = bodyParts.LeftKnee.gameObject.AddComponent<DynamicBoneCollider>();

                leftLowerLegCol.m_Direction = preset_LeftLowerLeg.direction;
                leftLowerLegCol.m_Center = preset_LeftLowerLeg.center;
                leftLowerLegCol.m_Radius = preset_LeftLowerLeg.radius;
                leftLowerLegCol.m_Height = preset_LeftLowerLeg.height;

                dynamBoneCols.Add(leftLowerLegCol);

                if(useDebug){

                    Debug.Log("LeftLowerLeg BoneCollider Created");

                }//useDebug
                
            //bodyParts.LeftKnee != null
            } else {
                
                if(useDebug){

                    Debug.Log("LeftKnee Transform = null");

                }//useDebug
                
            }//bodyParts.LeftKnee != null
                                
        //preset_LeftLowerLeg != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Left Lower Leg Preset Set.");

            }//useDebug
                                
        }//preset_LeftLowerLeg != null
        
    }//DynaBoneCreate_LeftLowLeg
    
    public void DynaBoneCreate_RightLowLeg(){
        
        if(preset_RightLowerLeg != null){
            
            if(bodyParts.RightKnee != null){
                                
                DynamicBoneCollider rightLowerLegCol = bodyParts.RightKnee.gameObject.AddComponent<DynamicBoneCollider>();

                rightLowerLegCol.m_Direction = preset_RightLowerLeg.direction;
                rightLowerLegCol.m_Center = preset_RightLowerLeg.center;
                rightLowerLegCol.m_Radius = preset_RightLowerLeg.radius;
                rightLowerLegCol.m_Height = preset_RightLowerLeg.height;

                dynamBoneCols.Add(rightLowerLegCol);

                if(useDebug){

                    Debug.Log("RightLowerLeg BoneCollider Created");

                }//useDebug
                
            //bodyParts.RightKnee != null
            } else {
                
                if(useDebug){

                    Debug.Log("RightKnee Transform = null");

                }//useDebug
                
            }//bodyParts.RightKnee != null
                                
        //preset_RightLowerLeg != null
        } else {
                                
            if(useDebug){

                Debug.Log("No Right Lower Leg Preset Set.");

            }//useDebug
                                
        }//preset_RightLowerLeg != null
        
    }//DynaBoneCreate_RightLowLeg
    
    
//////////////////////////////////////
///
///     DYNAMIC BONES CREATION
///
//////////////////////////////////////
    
    
    public void DynaBonesCreate(){
        
        GameObject dynamObj = new GameObject();
        
        if((int)createType == 0){

            dynamObj.transform.parent = rootTrans;

            dynamObj.transform.localPosition = new Vector3(0, 0, 0);
            dynamObj.transform.localEulerAngles = new Vector3(0, 0, 0);

            dynamObj.name = "Dynamic Bones";
            
        //createType = RootTransform
        } else {
            
            DestroyImmediate(dynamObj);
            
        }//createType = RootTransform
            
        if((int)objectType == 0 | (int)objectType == 1){
            
            if(clothPoints.Count > 0){

                if((int)createType == 0){
                
                    GameObject newClothObj = new GameObject();
                    newClothObj.transform.parent = dynamObj.transform;
                    newClothObj.transform.localPosition = new Vector3(0, 0, 0);
                    newClothObj.transform.localEulerAngles = new Vector3(0, 0, 0);

                    newClothObj.name = "Cloth Bones";

                    foreach(Transform tempTrans in clothPoints){

                        GameObject newDynamObj = new GameObject();
                        newDynamObj.transform.parent = newClothObj.transform;
                        newDynamObj.transform.localPosition = new Vector3(0, 0, 0);
                        newDynamObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                        newDynamObj.name = "DynamBone_" + tempTrans.name;

                        DynamicBone newDynamBone = newDynamObj.AddComponent<DynamicBone>();

                        newDynamBone.m_Root = tempTrans;
                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(clothPreset != null){

                            newDynamBone.m_UpdateRate = clothPreset.updateRate;
                            newDynamBone.m_Damping = clothPreset.damping;
                            newDynamBone.m_DampingDistrib = clothPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = clothPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = clothPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = clothPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = clothPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = clothPreset.inert;
                            newDynamBone.m_InertDistrib = clothPreset.inertDistrib;
                            newDynamBone.m_Friction = clothPreset.friction;
                            newDynamBone.m_FrictionDistrib = clothPreset.frictionDistrib;

                            newDynamBone.m_Radius = clothPreset.radius;
                            newDynamBone.m_RadiusDistrib = clothPreset.radiusDistrib;

                            newDynamBone.m_EndLength = clothPreset.endLength;
                            newDynamBone.m_EndOffset = clothPreset.endOffset;
                            newDynamBone.m_Gravity = clothPreset.gravity;
                            newDynamBone.m_Force = clothPreset.force;

                        //clothPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Cloth Preset Set.");

                            }//useDebug

                        }//clothPreset != null

                        dynamBone.Add(newDynamBone);

                    }//foreach tempTrans in clothpoints
                    
                }//createType = RootTransform
                
                if((int)createType == 1){
                    
                    foreach(Transform tempTrans in clothPoints){
                        
                        tempTrans.gameObject.AddComponent<DynamicBone>();
                        
                        DynamicBone newDynamBone = tempTrans.GetComponent<DynamicBone>();
                        
                        newDynamBone.m_Root = tempTrans;
                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(clothPreset != null){

                            newDynamBone.m_UpdateRate = clothPreset.updateRate;
                            newDynamBone.m_Damping = clothPreset.damping;
                            newDynamBone.m_DampingDistrib = clothPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = clothPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = clothPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = clothPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = clothPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = clothPreset.inert;
                            newDynamBone.m_InertDistrib = clothPreset.inertDistrib;
                            newDynamBone.m_Friction = clothPreset.friction;
                            newDynamBone.m_FrictionDistrib = clothPreset.frictionDistrib;

                            newDynamBone.m_Radius = clothPreset.radius;
                            newDynamBone.m_RadiusDistrib = clothPreset.radiusDistrib;

                            newDynamBone.m_EndLength = clothPreset.endLength;
                            newDynamBone.m_EndOffset = clothPreset.endOffset;
                            newDynamBone.m_Gravity = clothPreset.gravity;
                            newDynamBone.m_Force = clothPreset.force;

                        //clothPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Cloth Preset Set.");

                            }//useDebug

                        }//clothPreset != null

                        dynamBone.Add(newDynamBone);
                        
                    }//foreach tempTrans in clothpoints
                    
                }//createType = origTransform

            }//clothPoints.Count > 0
                
        }//objectType == human
            
        if((int)objectType == 1){
                
            if(tailPoints.Count > 0){
                
                if((int)createType == 0){
                    
                    GameObject newTailObj = new GameObject();
                    newTailObj.transform.parent = dynamObj.transform;
                    newTailObj.transform.localPosition = new Vector3(0, 0, 0);
                    newTailObj.transform.localEulerAngles = new Vector3(0, 0, 0);

                    newTailObj.name = "Tail Bones";

                    foreach(Transform tempTailTrans in tailPoints){

                        GameObject newDynamObj = new GameObject();
                        newDynamObj.transform.parent = newTailObj.transform;
                        newDynamObj.transform.localPosition = new Vector3(0, 0, 0);
                        newDynamObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                        newDynamObj.name = "DynamBone_" + tempTailTrans.name;

                        DynamicBone newDynamBone = newDynamObj.AddComponent<DynamicBone>();

                        newDynamBone.m_Root = tempTailTrans;
                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(tailPreset != null){

                            newDynamBone.m_UpdateRate = tailPreset.updateRate;
                            newDynamBone.m_Damping = tailPreset.damping;
                            newDynamBone.m_DampingDistrib = tailPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = tailPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = tailPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = tailPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = tailPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = tailPreset.inert;
                            newDynamBone.m_InertDistrib = tailPreset.inertDistrib;
                            newDynamBone.m_Friction = tailPreset.friction;
                            newDynamBone.m_FrictionDistrib = tailPreset.frictionDistrib;

                            newDynamBone.m_Radius = tailPreset.radius;
                            newDynamBone.m_RadiusDistrib = tailPreset.radiusDistrib;

                            newDynamBone.m_EndLength = tailPreset.endLength;
                            newDynamBone.m_EndOffset = tailPreset.endOffset;
                            newDynamBone.m_Gravity = tailPreset.gravity;
                            newDynamBone.m_Force = tailPreset.force;

                        //tailPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Tail Preset Set.");

                            }//useDebug

                        }//tailPreset != null

                        dynamBone.Add(newDynamBone);

                    }//foreach tempTailTrans in tailPoints
                    
                }//createType = RootTransform
                
                if((int)createType == 1){
                    
                    foreach(Transform tempTailTrans in tailPoints){

                        tempTailTrans.gameObject.AddComponent<DynamicBone>();
                        
                        DynamicBone newDynamBone = tempTailTrans.GetComponent<DynamicBone>();

                        newDynamBone.m_Root = tempTailTrans;
                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(tailPreset != null){

                            newDynamBone.m_UpdateRate = tailPreset.updateRate;
                            newDynamBone.m_Damping = tailPreset.damping;
                            newDynamBone.m_DampingDistrib = tailPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = tailPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = tailPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = tailPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = tailPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = tailPreset.inert;
                            newDynamBone.m_InertDistrib = tailPreset.inertDistrib;
                            newDynamBone.m_Friction = tailPreset.friction;
                            newDynamBone.m_FrictionDistrib = tailPreset.frictionDistrib;

                            newDynamBone.m_Radius = tailPreset.radius;
                            newDynamBone.m_RadiusDistrib = tailPreset.radiusDistrib;

                            newDynamBone.m_EndLength = tailPreset.endLength;
                            newDynamBone.m_EndOffset = tailPreset.endOffset;
                            newDynamBone.m_Gravity = tailPreset.gravity;
                            newDynamBone.m_Force = tailPreset.force;

                        //tailPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Tail Preset Set.");

                            }//useDebug

                        }//tailPreset != null

                        dynamBone.Add(newDynamBone);

                    }//foreach tempTailTrans in tailPoints
                    
                }//createType = origTransform
                    
            }//tailPoints.Count > 0
                
            if(earsPoints.Count > 0){
                
                if((int)createType == 0){
                    
                    GameObject newEarsObj = new GameObject();
                    newEarsObj.transform.parent = dynamObj.transform;
                    newEarsObj.transform.localPosition = new Vector3(0, 0, 0);
                    newEarsObj.transform.localEulerAngles = new Vector3(0, 0, 0);

                    newEarsObj.name = "Ears Bones";

                    foreach(Transform tempEarsTrans in earsPoints){

                        GameObject newDynamObj = new GameObject();
                        newDynamObj.transform.parent = newEarsObj.transform;
                        newDynamObj.transform.localPosition = new Vector3(0, 0, 0);
                        newDynamObj.transform.localEulerAngles = new Vector3(0, 0, 0);
                        newDynamObj.name = "DynamBone_" + tempEarsTrans.name;

                        DynamicBone newDynamBone = newDynamObj.AddComponent<DynamicBone>();

                        newDynamBone.m_Root = tempEarsTrans;

                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(earsPreset != null){

                            newDynamBone.m_UpdateRate = earsPreset.updateRate;
                            newDynamBone.m_Damping = earsPreset.damping;
                            newDynamBone.m_DampingDistrib = earsPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = earsPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = earsPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = earsPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = earsPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = earsPreset.inert;
                            newDynamBone.m_InertDistrib = earsPreset.inertDistrib;
                            newDynamBone.m_Friction = earsPreset.friction;
                            newDynamBone.m_FrictionDistrib = earsPreset.frictionDistrib;

                            newDynamBone.m_Radius = earsPreset.radius;
                            newDynamBone.m_RadiusDistrib = earsPreset.radiusDistrib;

                            newDynamBone.m_EndLength = earsPreset.endLength;
                            newDynamBone.m_EndOffset = earsPreset.endOffset;
                            newDynamBone.m_Gravity = earsPreset.gravity;
                            newDynamBone.m_Force = earsPreset.force;

                        //earsPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Ears Preset Set.");

                            }//useDebug

                        }//earsPreset != null

                        dynamBone.Add(newDynamBone);

                    }//foreach tempEarsTrans in earsPoints
                    
                }//createType = RootTransform
                
                if((int)createType == 1){
                    
                    foreach(Transform tempEarsTrans in earsPoints){

                        tempEarsTrans.gameObject.AddComponent<DynamicBone>();
                        
                        DynamicBone newDynamBone = tempEarsTrans.GetComponent<DynamicBone>();

                        newDynamBone.m_Root = tempEarsTrans;
                        newDynamBone.m_Colliders = dynamBoneCols;

                        if(earsPreset != null){

                            newDynamBone.m_UpdateRate = earsPreset.updateRate;
                            newDynamBone.m_Damping = earsPreset.damping;
                            newDynamBone.m_DampingDistrib = earsPreset.dampingDistrib;
                            newDynamBone.m_Elasticity = earsPreset.elasticity;
                            newDynamBone.m_ElasticityDistrib = earsPreset.elasticityDistrib;
                            newDynamBone.m_Stiffness = earsPreset.stiffness;
                            newDynamBone.m_StiffnessDistrib = earsPreset.stiffnessDistrib;
                            newDynamBone.m_Inert = earsPreset.inert;
                            newDynamBone.m_InertDistrib = earsPreset.inertDistrib;
                            newDynamBone.m_Friction = earsPreset.friction;
                            newDynamBone.m_FrictionDistrib = earsPreset.frictionDistrib;

                            newDynamBone.m_Radius = earsPreset.radius;
                            newDynamBone.m_RadiusDistrib = earsPreset.radiusDistrib;

                            newDynamBone.m_EndLength = earsPreset.endLength;
                            newDynamBone.m_EndOffset = earsPreset.endOffset;
                            newDynamBone.m_Gravity = earsPreset.gravity;
                            newDynamBone.m_Force = earsPreset.force;

                        //earsPreset != null
                        } else {

                            if(useDebug){

                                Debug.Log("No Ears Preset Set.");

                            }//useDebug

                        }//earsPreset != null

                        dynamBone.Add(newDynamBone);

                    }//foreach tempEarsTrans in earsPoints
                    
                }//createType = origTransform
                    
            }//earsPoints.Count > 0
                
        }//objectType == animal

        if(useDebug){

            if(dynamBone.Count > 0){
                
                Debug.Log("Dynamic Bones Created");
            
            //dynamBone.Count > 0
            } else {
                
                Debug.Log("Dynamic Bones Not Created");
                
            }//dynamBone.Count > 0
            
        }//useDebug
        
    }//DynaBonesCreate
    
    
//////////////////////////////////////
///
///     EXTRA FUNCTIONS
///
//////////////////////////////////////
    
    
    public string Asset_Find(string name){
        
        string[] results = new string[0];
            
        results = AssetDatabase.FindAssets(name);
            
        if(results.Length > 0){

            foreach(string newRes in results) {
                        
                return newRes;
                    
            }//foreach newRes
                
        }//results.Length > 0
        
        return "";
        
    }//Asset_Find
    
    
}//DynaBone_Presets