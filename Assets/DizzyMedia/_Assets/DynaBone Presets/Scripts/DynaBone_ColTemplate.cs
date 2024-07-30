using UnityEngine;

[CreateAssetMenu(fileName = "New DynaBone Collider Preset", menuName = "Dizzy Media/DynaBone Presets/New DynaBone Collider Preset", order = 2)]
public class DynaBone_ColTemplate : ScriptableObject {
    
    public string partName = "";
    public DynamicBoneColliderBase.Direction direction = DynamicBoneColliderBase.Direction.X;
    public Vector3 center = new Vector3(0, 0, 0);
    public float radius = 0;
    public float height = 0;

}
