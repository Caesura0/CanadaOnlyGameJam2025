using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    [Header("Spawn Points")]
    public bool useSpawnPoints = true;
    public bool disableRandomSpawning = true;

    private NavigationManager navigationManager;
    private List<EnemySpawnPoint> spawnPoints = new List<EnemySpawnPoint>();

    private void Awake()
    {
        navigationManager = GetComponent<NavigationManager>();
        if (navigationManager == null)
        {
            Debug.LogError("SpawnPointManager must be attached to the same GameObject as NavigationManager!");
            enabled = false;
            return;
        }

        // Find all spawn points in the scene
        EnemySpawnPoint[] points = FindObjectsOfType<EnemySpawnPoint>();
        spawnPoints.AddRange(points);

        // Disable random spawning if needed
        if (useSpawnPoints && disableRandomSpawning)
        {
            navigationManager.spawnEnemiesOnStart = false;
        }
    }

    // You can add methods to create spawn points at runtime
    public EnemySpawnPoint CreateSpawnPoint(Vector3 position, GameObject enemyPrefab, bool isFlyingEnemy = false)
    {
        GameObject spawnPointObj = new GameObject($"SpawnPoint_{spawnPoints.Count}");
        spawnPointObj.transform.position = position;

        EnemySpawnPoint spawnPoint = spawnPointObj.AddComponent<EnemySpawnPoint>();
        spawnPoint.enemyPrefab = enemyPrefab;
        spawnPoint.isFlyingEnemy = isFlyingEnemy;

        spawnPoints.Add(spawnPoint);
        return spawnPoint;
    }

    // Get all active spawn points
    public List<EnemySpawnPoint> GetActiveSpawnPoints()
    {
        return spawnPoints;
    }
}
