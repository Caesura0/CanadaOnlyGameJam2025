using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] int maxHealth = 3;

    int currentHealth;
    bool isTranquilized;

    public static EventHandler OnZombieTranquilized;
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
            OnZombieTranquilized?.Invoke(this, EventArgs.Empty);
            //Destroy(gameObject);
            // zombie is tranqulized
        }
    }

    public void Tranquilize()
    {
        if (isTranquilized) { return; }
        isTranquilized = true;
        OnZombieTranquilized?.Invoke(this, EventArgs.Empty);
    }
}
