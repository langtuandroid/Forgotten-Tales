using UnityEngine;
using System;

namespace InTerra
{
	[Serializable] public class DictionaryMaterialTerrain : SerializableDictionary<Material, Terrain> { }

	[AddComponentMenu("")]
	public class InTerra_UpdateAndCheck : MonoBehaviour
	{
		[SerializeField, HideInInspector] public bool FirstInit;
		[SerializeField, HideInInspector] public DictionaryMaterialTerrain MaterialTerrain = new DictionaryMaterialTerrain();

		[SerializeField, HideInInspector] public bool TracksFading;
		[SerializeField, HideInInspector] public float TracksFadingTime = 30.0f;
		[SerializeField, HideInInspector] public int TrackTextureSize = 2048;
		[SerializeField, HideInInspector] public LayerMask TrackLayer;
		[SerializeField, HideInInspector] public float TrackArea = 40;
		[HideInInspector] public RenderTexture TrackTexture;
		[HideInInspector] public float TrackDepthIndex;

		[SerializeField, HideInInspector] public float GlobalSmoothness = 0;

		void Update()
		{
			if (!InTerra_Setting.DisableAllAutoUpdates) InTerra_Data.CheckAndUpdate();

		}
	}
}
