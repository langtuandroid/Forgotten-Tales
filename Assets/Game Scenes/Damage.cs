using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damage : MonoBehaviour
{
    // The amount of damage to deal
    public int damageAmount = 10;

    // This function is called when the collider on this object enters a collision with another collider
    private void OnTriggerEnter(Collider other)
    {
        // Try to get the HealthScript component from the collided object
        Health health = other.gameObject.GetComponent<Health>();

        // If the collided object has a HealthScript component
        if (health != null)
        {
            // Apply damage to the object
            health.TakeDamage(damageAmount);
        }
    }
}
