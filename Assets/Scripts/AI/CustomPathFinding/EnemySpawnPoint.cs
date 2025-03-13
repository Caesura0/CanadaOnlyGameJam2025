using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    public bool spawnOnStart = true;
    public bool respawnAfterDeath = true;
    public float respawnDelay = 5f;
    public float activationDistance = 15f; // Only spawn when player is within this distance

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public bool isFlyingEnemy = false;
    public float flyHeight = 3f; // Only used for flying enemies

    // Reference to spawned enemy
    private GameObject spawnedEnemy;
    private bool isRespawning = false;
    private Transform playerTransform;
    private NavigationManager navigationManager;
    private bool hasSpawnedOnce = false; // Track if we've spawned at least once

    void Start()
    {
        // Find the navigation manager
        navigationManager = FindObjectOfType<NavigationManager>();

        // Find the player
        if (navigationManager != null && navigationManager.playerTransform != null)
        {
            playerTransform = navigationManager.playerTransform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        // Spawn on start if enabled
        if (spawnOnStart)
        {
            SpawnEnemy();
            hasSpawnedOnce = true;
        }
    }

    void Update()
    {
        // FIXED VERSION: Only do proximity check if we haven't spawned yet
        if (!hasSpawnedOnce && playerTransform != null && spawnedEnemy == null && !isRespawning)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= activationDistance)
            {
                SpawnEnemy();
                hasSpawnedOnce = true;
            }
        }

        // Only check for respawn if respawnAfterDeath is true
        if (respawnAfterDeath && spawnedEnemy == null && !isRespawning && hasSpawnedOnce)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    public GameObject SpawnEnemy()
    {
        if (enemyPrefab == null || navigationManager == null)
            return null;

        // Don't spawn if already spawned
        if (spawnedEnemy != null)
            return spawnedEnemy;

        // Create the spawn position
        Vector3 spawnPosition = transform.position;

        // For flying enemies, adjust height if needed
        if (isFlyingEnemy)
        {
            // Raycast down to find ground height
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                100f,
                navigationManager.navigationGraph.groundLayer
            );

            if (hit.collider != null)
            {
                // Set position at proper height above ground
                spawnPosition = new Vector3(transform.position.x, hit.point.y + flyHeight, transform.position.z);
            }
        }

        // Instantiate the enemy
        spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, navigationManager.enemyContainer);

        // Set up the enemy based on type
        if (isFlyingEnemy)
        {
            // Flying enemy setup if needed
        }
        else
        {
            // Try for EnemyBaseController
            EnemyBaseController controller = spawnedEnemy.GetComponent<EnemyBaseController>();
            if (controller != null)
            {
                controller.navigationGraph = navigationManager.navigationGraph;
                controller.target = playerTransform;
            }
        }

        // Subscribe to enemy destruction to handle respawning
        SpawnPointDeathTracker deathTracker = spawnedEnemy.AddComponent<SpawnPointDeathTracker>();

        return spawnedEnemy;
    }

    private IEnumerator RespawnAfterDelay()
    {
        isRespawning = true;
        yield return new WaitForSeconds(respawnDelay);
        SpawnEnemy();
        isRespawning = false;
    }

    // Called when enemy is destroyed
    public void OnEnemyDestroyed()
    {
        spawnedEnemy = null;
    }
}