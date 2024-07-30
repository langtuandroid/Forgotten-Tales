using UnityEngine;

[CreateAssetMenu(fileName = "New DynaBone Preset", menuName = "Dizzy Media/DynaBone Presets/New DynaBone Preset", order = 1)]
public class DynaBone_Template : ScriptableObject {

    public float updateRate = 60.0f;
            
    [Range(0, 1)]
    public float damping = 0.1f;
    public AnimationCurve dampingDistrib = null;
            
    [Range(0, 1)]
    public float elasticity = 0.1f;
    public AnimationCurve elasticityDistrib = null;
    
    [Range(0, 1)]
    public float stiffness = 0.1f;
    public AnimationCurve stiffnessDistrib = null;
            
    [Range(0, 1)]
    public float inert = 0.1f;
    public AnimationCurve inertDistrib = null;
            
    public float friction = 0.1f;
    public AnimationCurve frictionDistrib = null;
            
    public float radius = 0.1f;
    public AnimationCurve radiusDistrib = null;
            
    public float endLength = 0;
            
    public Vector3 endOffset = Vector3.zero;
            
    public Vector3 gravity = Vector3.zero;
            
    public Vector3 force = Vector3.zero;
    
}