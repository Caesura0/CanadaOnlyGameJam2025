using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;
    int currentHealth;
    bool isTranquilized;

    // Changed from static to instance events
    public event EventHandler OnZombieTranquilized;

    // Keep this static if you need to track all zombies spawned
    public static EventHandler OnZombieSpawned;

    private void Start()
    {
        currentHealth = 0;
        OnZombieSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Clamp(currentHealth + damage, 0, maxHealth);
        if (currentHealth >= maxHealth)
        {
            // Use instance event instead of static
            OnZombieTranquilized?.Invoke(this, EventArgs.Empty);
            Destroy(gameObject);
            // zombie is tranquilized
        }
    }

    public void Tranquilize()
    {
        if (isTranquilized) { return; }
        isTranquilized = true;

        // Use instance event instead of static
        OnZombieTranquilized?.Invoke(this, EventArgs.Empty);
    }
}