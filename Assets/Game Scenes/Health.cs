using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    // The initial health of the object
    public int maxHealth = 100;

    // The current health of the object
    private int currentHealth;

    void Start()
    {
        // Initialize current health to max health at the start
        currentHealth = maxHealth;
    }

    // Function to apply damage to the object
    public void TakeDamage(int damage)
    {
        // Reduce current health by the damage amount
        currentHealth -= damage;

        // Check if health has dropped to zero or below
        if (currentHealth <= 0)
        {
            // Destroy the object if health is zero
            Destroy(gameObject);
        }
    }
}
