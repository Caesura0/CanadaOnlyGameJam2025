using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RescueManager : MonoBehaviour
{
    public class OnRescueArgs : EventArgs
    {
        public int rescued;
        public int totalToRescue;
    }

    [SerializeField] private int totalToRescue = 10;
    private int rescued = 0;

    // Keep this as a static event for UI and other systems
    public static EventHandler<OnRescueArgs> onRescue;

    // List to track all enemy health components we're monitoring
    private List<EnemyHealth> trackedEnemies = new List<EnemyHealth>();

    private void OnEnable()
    {
        // Subscribe to enemy spawned event to catch new enemies
        EnemyHealth.OnZombieSpawned += OnZombieSpawned;

        // Find and subscribe to any existing enemies
        FindAndSubscribeToExistingEnemies();
    }

    private void OnDisable()
    {
        // Unsubscribe from the spawn event
        EnemyHealth.OnZombieSpawned -= OnZombieSpawned;

        // Unsubscribe from all tracked enemies
        UnsubscribeFromAllEnemies();
    }

    private void OnZombieSpawned(object sender, EventArgs e)
    {
        // A new enemy was spawned, subscribe to its event
        if (sender is EnemyHealth newEnemy)
        {
            SubscribeToEnemy(newEnemy);
        }
    }

    private void FindAndSubscribeToExistingEnemies()
    {
        // Find all existing EnemyHealth components in the scene
        EnemyHealth[] existingEnemies = FindObjectsOfType<EnemyHealth>();

        foreach (var enemy in existingEnemies)
        {
            SubscribeToEnemy(enemy);
        }

        Debug.Log($"RescueManager: Found and subscribed to {existingEnemies.Length} existing enemies");
    }

    private void SubscribeToEnemy(EnemyHealth enemy)
    {
        // Only subscribe if we're not already tracking this enemy
        if (!trackedEnemies.Contains(enemy))
        {
            // Subscribe to this enemy's tranquilized event
            enemy.OnZombieTranquilized += AddRescued;
            trackedEnemies.Add(enemy);
            Debug.Log($"RescueManager: Subscribed to enemy {enemy.gameObject.name}");
        }
    }

    private void UnsubscribeFromAllEnemies()
    {
        // Unsubscribe from all tracked enemies
        foreach (var enemy in trackedEnemies)
        {
            if (enemy != null) // Check in case the enemy was destroyed
            {
                enemy.OnZombieTranquilized -= AddRescued;
            }
        }

        trackedEnemies.Clear();
    }

    private void AddRescued(object sender, EventArgs e)
    {
        rescued++;

        // If the sender is an EnemyHealth, remove it from our tracked list
        if (sender is EnemyHealth tranquilizedEnemy)
        {
            trackedEnemies.Remove(tranquilizedEnemy);
        }

        // Invoke the rescue event
        onRescue?.Invoke(this, new OnRescueArgs
        {
            rescued = this.rescued,
            totalToRescue = this.totalToRescue
        });

        if (rescued >= totalToRescue)
        {
            Debug.Log("All rescued!");
            // Add any "win condition" code here
        }
    }

    // Public method to get current rescue stats
    public (int current, int total) GetRescueStats()
    {
        return (rescued, totalToRescue);
    }
}