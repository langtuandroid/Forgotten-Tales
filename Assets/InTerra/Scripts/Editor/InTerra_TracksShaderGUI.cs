using System.ComponentModel;
using UnityEngine;
using UnityEditor;


namespace InTerra
{
    public class InTerra_TracksShaderGUI : ShaderGUI
    {
        MaterialProperty[] properties;
        bool minmax = false;      

        public enum TrackType
        {
            [Description("Wheel Tracks")] WheelTracks,
            [Description("Footprints")] Footprints,
            [Description("Default")] Default
        }


        [ExecuteInEditMode]
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            this.properties = properties;
            Material targetMat = materialEditor.target as Material;

            InTerra_GUI.TrackMaterialEditor(targetMat, materialEditor, ref minmax);

        }

    }
}
