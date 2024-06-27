using UnityEngine;
using System.ComponentModel;


namespace InTerra
{
    [AddComponentMenu("InTerra/InTerra Tracks")]
    public class InTerra_Track : MonoBehaviour
    {
        [SerializeField] public Material trackMaterial;

        [SerializeField] public float quadWidth = 0.45f;
        [SerializeField] public float quadLenght = 1.0f;
        [SerializeField] public float quadOffsetX = 0.0f;
        [SerializeField] public float quadOffsetZ = 0.0f;

        [SerializeField] float stepSize = 0.2f;
        [SerializeField] float lenghtUV = 3f;

        [SerializeField] [Min(0)] public float groundedCheckDistance = 0.6f;
        [SerializeField] public float startCheckDistance = 0.0f;
        [SerializeField] [Min(0)] float time = 0.1f;
        [SerializeField] [Min(25)] public float ereaseDistance = 75.0f;

        [SerializeField] public bool delete;

        private Vector3 lastPosition;
        private Vector3 lastVertexUp;
        private Vector3 lastVertexDown;

        private float lastUV0_X;
        private float lastUV1_X;

        bool grounded;

        public float targetTime = 0;
        float groupSize = 0.5f;
              
        Vector3 groupLastPosition;
        int trackType;

        MaterialPropertyBlock materialBlock;
        bool initTrack;       
        
        int c = 0;
        [SerializeField, HideInInspector] GameObject trackFadeOut;
        [SerializeField, HideInInspector] GameObject tracks;

        GameObject TrackMesh;

        private void Update()
        {
            if (trackMaterial != null)
            {
                if (trackMaterial.IsKeywordEnabled("_TRACKS"))
                {
                    trackType = 1;
                }
                else if (trackMaterial.IsKeywordEnabled("_FOOTPRINTS"))
                {
                    trackType = 2;
                }
                else
                {
                    trackType = 0;
                }
            }

            RaycastHit hit;                 

            Vector3 forwardVector = GetForwardVector();
            if(Physics.Raycast(transform.position - new Vector3(0, -startCheckDistance,0), Vector3.down, out hit, groundedCheckDistance))
            {
                grounded = true;
            }
            else
            {
                grounded = false;
            }

          
            if (!initTrack)
            {
                Vector3 normal2D = new Vector3(0, 1f, 0);
                lastPosition = new Vector3(transform.position.x, 0, transform.position.z) - forwardVector * quadLenght;

                lastVertexUp = VertexPositions()[0] - forwardVector * quadLenght;
                lastVertexDown = VertexPositions()[1] - forwardVector * quadLenght;

                CreateTrackMesh(0);
                lastPosition = new Vector3(transform.position.x, 0, transform.position.z);
                groupLastPosition = new Vector3(transform.position.x, 0, transform.position.z);

                if (trackType == 1)
                {            
                    CreateTrackMesh(2);
                     
                }

                initTrack = true;
            }
            else
            {
                float distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), lastPosition);
                float distance2 = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), lastPosition);
                if (trackType == 1)
                {
                    if (distance > stepSize)
                    {
                        CreateTrackMesh(1);
                        
                        CreateTrackMesh(2);
                        
                        lastPosition = new Vector3(transform.position.x, 0, transform.position.z);
                    }
                }
                else
                { 
                    if (distance > stepSize && grounded && (targetTime <= 0.0f))
                    {
                        CreateTrackMesh(0);

                        lastPosition = new Vector3(transform.position.x, 0, transform.position.z);
                        targetTime = time;
                    }
                }

                if (grounded)
                {
                    targetTime -= Time.deltaTime;  
                }
                else
                {
                    targetTime = time;
                }
            }        
        }

        public void CreateTrackMesh(int positionIndex)
        {
            var dataScript = InTerra_Data.GetUpdaterScript();
            bool newObject = (groupSize < Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), groupLastPosition));           

            if (tracks == null)
            {
                tracks = new GameObject("InTerra_Tracks_" + this.name);
            }

            Mesh tMesh;

            if (positionIndex == 2)
            {
                DestroyImmediate(trackFadeOut);
                trackFadeOut = new GameObject("Track Fade out Stamp");
                trackFadeOut.AddComponent<MeshFilter>();
                trackFadeOut.AddComponent<MeshRenderer>();
                trackFadeOut.transform.parent = tracks.transform;
                tMesh = trackFadeOut.GetComponent<MeshFilter>().mesh;
            }
            else
            {
                if (TrackMesh == null || newObject)
                {
                    c += 1;
                    TrackMesh = new GameObject("Track Stamp " + c);
                    TrackMesh.AddComponent<MeshFilter>();
                    TrackMesh.AddComponent<MeshRenderer>();                   
                }
                tMesh = TrackMesh.GetComponent<MeshFilter>().mesh;
            }

            TrackMesh.transform.parent = tracks.transform;

            int vertLenght;
            int trianglesLenght;

            if (!newObject)
            {            
                vertLenght = tMesh.vertices.Length + 4;
                trianglesLenght = tMesh.triangles.Length + 6;
            }
            else
            {
                tMesh = new Mesh();
                
                vertLenght = 4;
                trianglesLenght = 6;
                groupLastPosition = new Vector3(transform.position.x, 0, transform.position.z);

            }

            tMesh.name = "Track Mesh " + c;
            Vector3[] vertices = new Vector3[vertLenght];
            Vector2[] uv = new Vector2[vertLenght];
            Vector2[] uv2 = new Vector2[vertLenght];
            Vector2[] uv3 = new Vector2[vertLenght];
            int[] triangles = new int[trianglesLenght];

            Vector3 forwardVector = GetForwardVector();

            float distance;
            Vector3 newVertexUp;
            Vector3 newVertexDown;

            if (!newObject)
            {
                tMesh.vertices.CopyTo(vertices, 0);
                tMesh.uv.CopyTo(uv, 0);
                tMesh.uv2.CopyTo(uv2, 0);
                tMesh.uv3.CopyTo(uv3, 0);
                tMesh.triangles.CopyTo(triangles, 0);
            }
     
            int vIndex = vertices.Length - 4;

            int vIndex0 = vIndex + 0;
            int vIndex1 = vIndex + 1;
            int vIndex2 = vIndex + 2;
            int vIndex3 = vIndex + 3;

            if (positionIndex == 0)
            {
                if (trackType == 1)
                {
                    newVertexUp = VertexPositions()[0];
                    newVertexDown = VertexPositions()[1];
                    SetFadingIn(vIndex, ref uv3);
                }
                else
                {
                    forwardVector = GetForwardVector();
                    lastVertexUp = VertexPositions()[0] - forwardVector * (quadLenght / 2);
                    lastVertexDown = VertexPositions()[1] - forwardVector * (quadLenght / 2);
                    newVertexUp = VertexPositions()[0] + forwardVector * (quadLenght / 2);
                    newVertexDown = VertexPositions()[1] + forwardVector * (quadLenght / 2);
                    SetNoFading(vIndex, ref uv3);
                }
                distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), lastPosition);
            }
            else
            {
                newVertexUp = VertexPositions()[0];
                newVertexDown = VertexPositions()[1];
                distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), lastPosition);
                SetNoFading(vIndex, ref uv3);

            }
            if (positionIndex == 2)
            {
                newVertexUp = VertexPositions()[0] + forwardVector * quadLenght;
                newVertexDown = VertexPositions()[1] + forwardVector * quadLenght;
                distance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z) + forwardVector * quadLenght, lastPosition);
                SetFadingOut(vIndex, ref uv3);
            }
            else
            {
                dataScript.TrackDepthIndex += 0.0001f;
            }

            vertices[vIndex0] = lastVertexDown;
            vertices[vIndex1] = lastVertexUp;

            lastUV0_X = lastUV0_X % 1;
            lastUV1_X = lastUV1_X % 1;

            vertices[vIndex2] = newVertexUp;
            vertices[vIndex3] = newVertexDown;

            if (trackType == 1)
            {
                uv[vIndex0] = new Vector2(lastUV0_X, 1);
                uv[vIndex1] = new Vector2(lastUV1_X, 0);
                uv[vIndex2] = new Vector2((lastUV0_X + (1 / lenghtUV * distance)), 0);
                uv[vIndex3] = new Vector2((lastUV1_X + (1 / lenghtUV * distance)), 1);
            }
            else
            {
                uv[vIndex0] = new Vector2(1, 1);
                uv[vIndex1] = new Vector2(1, 0);
                uv[vIndex2] = new Vector2(0, 0);
                uv[vIndex3] = new Vector2(0, 1);
            }

            int tIndex = triangles.Length - 6;
            triangles[tIndex + 2] = vIndex0;
            triangles[tIndex + 1] = vIndex1;
            triangles[tIndex + 0] = vIndex2;

            triangles[tIndex + 5] = vIndex0;
            triangles[tIndex + 4] = vIndex2;
            triangles[tIndex + 3] = vIndex3;

            tMesh.vertices = vertices;
            tMesh.uv = uv;
            tMesh.uv2 = uv;
            tMesh.uv3 = uv3;

            tMesh.triangles = triangles;

            if (positionIndex != 2)
            {
                lastUV0_X = uv[vIndex2].x;
                lastUV1_X = uv[vIndex3].x;
                lastVertexUp = newVertexUp;
                lastVertexDown = newVertexDown;
            }


            if (positionIndex == 2)
            {
                trackFadeOut.GetComponent<MeshFilter>().mesh = tMesh;
                trackFadeOut.GetComponent<MeshRenderer>().sharedMaterial = trackMaterial;
                trackFadeOut.layer = dataScript.TrackLayer;
            }
            else
            {
                if (!TrackMesh.TryGetComponent<MeshFilter>(out MeshFilter mr))
                {
                    TrackMesh.AddComponent<MeshRenderer>();
                    TrackMesh.AddComponent<MeshFilter>();
                }

                TrackMesh.layer = dataScript.TrackLayer;
               
                TrackMesh.GetComponent<MeshFilter>().mesh = tMesh;

                if (newObject)
                {
                    TrackMesh.transform.position = new Vector3(TrackMesh.transform.position.x, TrackMesh.transform.position.x - dataScript.TrackDepthIndex, TrackMesh.transform.position.z);
                    TrackMesh.GetComponent<MeshRenderer>().sharedMaterial = trackMaterial;

                    if(dataScript.TracksFading)
                    { 
                        if(materialBlock == null)
                        {
                            materialBlock = new MaterialPropertyBlock();
                        }
                        trackMaterial.EnableKeyword("_FADE");
                        trackMaterial.SetFloat("_TrackFadeTime", dataScript.TracksFadingTime);

                        materialBlock.SetFloat("_FadingTimeStart", Time.timeSinceLevelLoad);                   
                        TrackMesh.GetComponent<MeshRenderer>().SetPropertyBlock(materialBlock);
                    }
                    else
                    {
                        trackMaterial.DisableKeyword("_FADE");
                    }

                    TrackMesh.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off ;
                    TrackMesh.GetComponent<MeshRenderer>().lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    TrackMesh.GetComponent<MeshRenderer>().receiveShadows = false;
                }
            }

            for (int i = 0; i < tracks.transform.childCount; i++)
            {
                Mesh m = tracks.transform.GetChild(i).GetComponent<MeshFilter>().mesh;

                if (Vector2.Distance(new Vector2(m.vertices[0].x, m.vertices[0].z), new Vector2(transform.position.x, transform.position.z)) > ereaseDistance)
                {
                    Destroy(tracks.transform.GetChild(i).gameObject);
                }
                else
                {
                    break;
                }
            }
        }

        public Vector3 GetForwardVector()
        {
              if(initTrack && trackType == 0)
              {
                  return (new Vector3(transform.position.x, 0, transform.position.z) - lastPosition).normalized;
              }
              else
              {
                return Vector3.Cross((transform.right).normalized, new Vector3(0, 1f, 0));
              }
        }

        Vector3[] VertexPositions()
        {
            Vector3[] vecPos = new Vector3[2];
            Vector3 normal2D = new Vector3(0, 1f, 0);
            Vector3 pos = new Vector3(transform.position.x + quadOffsetX, Terrain.activeTerrain.GetPosition().y - 1.0f - InTerra_Data.GetUpdaterScript().TrackDepthIndex, transform.position.z + quadOffsetZ);
            vecPos[0] = pos + Vector3.Cross(GetForwardVector(), normal2D) * (quadWidth / 2);
            vecPos[1] = pos + Vector3.Cross(GetForwardVector(), normal2D * -1f) * (quadWidth / 2);

            return vecPos;
        }

        public Vector3[] VertexDebugPositions()
        {
            Vector3[] vecPos = new Vector3[2];
            Vector3 normal2D = new Vector3(0, 1f, 0);
            Vector3 pos = new Vector3(transform.position.x + quadOffsetX, transform.position.y, transform.position.z + quadOffsetZ);
            vecPos[0] = pos + Vector3.Cross(GetForwardVector(), normal2D) * (quadWidth / 2);
            vecPos[1] = pos + Vector3.Cross(GetForwardVector(), normal2D * -1f) * (quadWidth / 2);

            return vecPos;
        }

        void SetFadingIn(int vertLenght, ref Vector2[] uv3)
        {
            uv3[vertLenght + 0] = new Vector2(0, 0);
            uv3[vertLenght + 1] = new Vector2(0, 0);
            uv3[vertLenght + 2] = new Vector2(1, 0);
            uv3[vertLenght + 3] = new Vector2(1, 0);
        }

        void SetFadingOut(int vertLenght, ref Vector2[] uv3)
        {
            uv3[vertLenght + 0] = new Vector2(1, 0);
            uv3[vertLenght + 1] = new Vector2(1, 0);
            uv3[vertLenght + 2] = new Vector2(0, 0);
            uv3[vertLenght + 3] = new Vector2(0, 0);
        }

        void SetNoFading(int vertLenght, ref Vector2[] uv3)
        {
            uv3[vertLenght + 0] = new Vector2(1, 0);
            uv3[vertLenght + 1] = new Vector2(1, 0);
            uv3[vertLenght + 2] = new Vector2(1, 0);
            uv3[vertLenght + 3] = new Vector2(1, 0);
        }

    }
}
