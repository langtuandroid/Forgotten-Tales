using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private SceneAsset sceneAsset; // Drag and drop the scene here
    [SerializeField] private float delay = 5f;

    private string sceneName;

    private void Start()
    {
        if (sceneAsset != null)
        {
#if UNITY_EDITOR
            sceneName = sceneAsset.name;
#endif
            Invoke("LoadScene", delay);
        }
        else
        {
            Debug.LogError("SceneAsset is not assigned. Please drag and drop a scene in the Inspector.");
        }
    }

    private void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is not set.");
        }
    }
}
