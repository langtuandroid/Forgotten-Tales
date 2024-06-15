// Collaborate Assistant from Luceed Studio - https://luceed.studio
// Documentation - https://luceed.studio/collaborate-assistant

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LuceedStudio_BonesAssistant
{
    public class BonesAssistantResources : ScriptableObject
    {
        [Header("Saved poses")]
        public List<BonesAssistantSavedPose> SavedPoses = new List<BonesAssistantSavedPose>();

        static BonesAssistantResources inst;
        public static BonesAssistantResources Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = Resources.Load<BonesAssistantResources>("BA_Resources");
                }

                return inst;
            }
        }

        private void MakeDirty()
        {
            EditorUtility.SetDirty(inst);
        }

        public void SavePose(float[] pose, float[] rootValues)
        {
            string name = "Pose " + (SavedPoses.Count + 1);
            BonesAssistantSavedPose newPose = new BonesAssistantSavedPose(pose, rootValues, name);
            SavedPoses.Add(newPose);

            MakeDirty();
        }

        public BonesAssistantSavedPose GetPose(int index)
        {
            return SavedPoses[index];
        }

        public void RenamePose(int index, string newName)
        {
            SavedPoses[index].Name = newName;

            MakeDirty();
        }

        public void OverridePose(int index, float[] pose, float[] rootValues)
        {
            SavedPoses[index].Pose = pose;
            SavedPoses[index].RootValues = rootValues;

            MakeDirty();
        }

        public void DeletePose(int index)
        {
            SavedPoses.RemoveAt(index);

            MakeDirty();
        }
    }
}
