using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOnHits : MonoBehaviour
{
    // Number of hits required to destroy the object
    public int requiredHits = 3;

    // Tag of objects that will count towards the hit
    public string targetTag = "Enemy";

    // Track the current number of hits
    private int currentHits = 0;

    // This function will be called when the object collides with another object
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object has the correct tag
        if (collision.gameObject.CompareTag(targetTag))
        {
            // Increase the hit count
            currentHits++;

            // Check if the number of hits has reached the required number
            if (currentHits >= requiredHits)
            {
                // Destroy the object
                Destroy(gameObject);
            }
        }
    }
}
