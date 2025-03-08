using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;

    int currentHealth;

    public static EventHandler OnZombieTranqulized;
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
            OnZombieTranqulized?.Invoke(this, EventArgs.Empty);
            Destroy(gameObject);
            // zombie is tranqulized
        }
    }
}
