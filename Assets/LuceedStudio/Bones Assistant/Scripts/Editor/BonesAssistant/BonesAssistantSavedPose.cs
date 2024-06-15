// Stylized Character Creator from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/stylized-character-creator

using System;
using UnityEngine;

namespace LuceedStudio_BonesAssistant
{
    [Serializable]
    public class BonesAssistantSavedPose
    {
        public string Name;
        public float[] Pose;
        public float[] RootValues;

        public BonesAssistantSavedPose(float[] pose, float[] rootValues, string name = "Pose")
        {
            this.Name = name;
            this.Pose = pose;
            this.RootValues = rootValues;
        }
    }
}

