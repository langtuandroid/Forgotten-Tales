using UnityEngine;
using UnityEditor;
using System.ComponentModel;


namespace InTerra
{
    [CustomEditor(typeof(InTerra_Track))]
    [CanEditMultipleObjects]
    public class InTerra_TracksGUI : Editor
    {
        SerializedProperty trackMaterial;

        SerializedProperty quadWidth;
        SerializedProperty quadLenght;
        SerializedProperty quadOffsetX;
        SerializedProperty quadOffsetZ;

        SerializedProperty stepSize;
        SerializedProperty lenghtUV;

        SerializedProperty groundedCheckDistance;
        SerializedProperty startCheckDistance;
        SerializedProperty time;

        SerializedProperty ereaseDistance;
        MaterialEditor matEditor;

        bool minmax;

        void OnEnable()
        {
            quadWidth = serializedObject.FindProperty("quadWidth");
            stepSize = serializedObject.FindProperty("stepSize");
            lenghtUV = serializedObject.FindProperty("lenghtUV");
            trackMaterial = serializedObject.FindProperty("trackMaterial");
            ereaseDistance = serializedObject.FindProperty("ereaseDistance");

            startCheckDistance = serializedObject.FindProperty("startCheckDistance");
            groundedCheckDistance = serializedObject.FindProperty("groundedCheckDistance");
            time = serializedObject.FindProperty("time");
            quadLenght = serializedObject.FindProperty("quadLenght");

            quadOffsetX = serializedObject.FindProperty("quadOffsetX");
            quadOffsetZ = serializedObject.FindProperty("quadOffsetZ");
        }

        public override void OnInspectorGUI()
        {
            var styleBold = new GUIStyle(EditorStyles.boldLabel);        
            serializedObject.Update();

            var t = (target as InTerra_Track);

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.PropertyField(trackMaterial);               
            }

            Material mat = t.trackMaterial;

            if (mat == null)
            {
                GUI.enabled = false;
            }
            else
            {
                if (mat.shader && mat.shader.name != null && (mat.shader.name != "InTerra/Tracks Material"))
                {
                    EditorGUILayout.HelpBox("Track Material need to have InTerra/Track Material shader!", MessageType.Warning);
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        if (GUILayout.Button("Assign Track Shader to Selected Material"))
                        {
                            mat.shader = Shader.Find("InTerra/Tracks Material");
                        }
                    }
                    GUI.enabled = false;
                }
                else
                {
                    if(matEditor == null)
                    {
                        matEditor = (MaterialEditor)CreateEditor(mat);
                    }
                    else
                    {
                        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                        {

                        if (trackMaterial.hasMultipleDifferentValues)
                        {
                                EditorGUILayout.HelpBox("Materials on selected objects are not the same, editing Material is not alloved.", MessageType.Info);
                                GUI.enabled = false;
                        }
                            InTerra_GUI.TrackMaterialEditor(mat, matEditor, ref minmax);
                            GUI.enabled = true;
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Ground Check Ray Setting", styleBold);

                EditorGUILayout.Space();
                
                PropertyLine(groundedCheckDistance, "Checking Distance", "The length of checking ray, the ray can be seen drawn with red color if gizmos are enabled.");
                PropertyLine(startCheckDistance, "Ray Start Offset", " Offset of start position of checking ray.");
                PropertyLine(time, "Time Delay", "The ray has to be detecting ground for this amount of time (in seconds) for the object to be considered grounded.");
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Stamps Size and Position", styleBold);
                EditorGUILayout.Space();

                if (mat && mat.IsKeywordEnabled("_TRACKS"))
                {
                    PropertyLine(lenghtUV, "UV Length", "Adjust UV tiling of X axis of Heightmap.");
                }

                PropertyLine(quadWidth, "Width");
                PropertyLine(quadLenght, "Length");        

                PropertyLine(quadOffsetX, "Position Offset X");
                PropertyLine(quadOffsetZ, "Position Offset Z");
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                PropertyLine(stepSize, "Distance Threshold", "The stamps for drawing track will be created only if the object reach this distance.");
                PropertyLine(ereaseDistance, "Erase at Distance", "Threshold distance after which the tracks stamps will be erased.");
            }

            serializedObject.ApplyModifiedProperties();

            void PropertyLine(SerializedProperty property, string label, string tooltip = null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent() { text = label, tooltip = tooltip });
            }
        }

        private void OnSceneGUI()
        {
            if(!Application.isPlaying)
            { 
                var t = (target as InTerra_Track);
                Debug.DrawLine(t.transform.position - new Vector3(0, -t.startCheckDistance, 0), t.transform.position - new Vector3(0, -t.startCheckDistance + t.groundedCheckDistance, 0), Color.red);

                Vector3 forwardVector = t.GetForwardVector();

                Debug.DrawLine(t.VertexDebugPositions()[0] - forwardVector * (t.quadLenght / 2), t.VertexDebugPositions()[1] - forwardVector * (t.quadLenght / 2), Color.blue);
                Debug.DrawLine(t.VertexDebugPositions()[0] + forwardVector * (t.quadLenght / 2), t.VertexDebugPositions()[1] + forwardVector * (t.quadLenght / 2), Color.blue);

                Debug.DrawLine(t.VertexDebugPositions()[0] - forwardVector * (t.quadLenght / 2), t.VertexDebugPositions()[0] + forwardVector * (t.quadLenght / 2), Color.blue);
                Debug.DrawLine(t.VertexDebugPositions()[1] - forwardVector * (t.quadLenght / 2), t.VertexDebugPositions()[1] + forwardVector * (t.quadLenght / 2), Color.blue);
            }           
        }

        private void OnDisable()
        {
            GameObject.DestroyImmediate(matEditor);
        }

    }
}
