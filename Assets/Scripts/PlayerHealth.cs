using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    int health;
    int maxHealth = 3;

    public static EventHandler OnPlayerDeath;



    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private GameObject heartHolder;

    private void Start()
    {
        health = maxHealth;
        SpawnHearts();
    }
    public void TakeDamage(int damage)
    {
        health = Mathf.Clamp(health - damage, 0, maxHealth);
        SpawnHearts();
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        //trigger death
        OnPlayerDeath?.Invoke(this, EventArgs.Empty);
        //stop player movement
        //turn off player collider
        //spawn chopper
        //wait for chopper to get to player, deactive player visual
        //chopper flies away
        //restart game menu
    }

    void SpawnHearts()
    {
        foreach (Transform child in heartHolder.transform)
        {
            Destroy(child.gameObject);
        }
        for (int i = 0; i < health; i++)
        {
            Instantiate(heartPrefab, heartHolder.transform);
        }
    }
}
