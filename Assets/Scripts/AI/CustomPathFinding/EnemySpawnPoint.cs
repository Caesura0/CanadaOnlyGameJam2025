using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// New component that defines a spawn point
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

    void Start()
    {
        // Find the navigation manager
        navigationManager = FindObjectOfType<NavigationManager>();
        if (navigationManager == null)
        {
            Debug.LogError("No NavigationManager found in scene!");
            return;
        }

        // Find the player
        if (navigationManager.playerTransform != null)
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
        }
    }

    void Update()
    {
        // Check if we need to respawn
        if (spawnedEnemy == null && !isRespawning && respawnAfterDeath)
        {
            StartCoroutine(RespawnAfterDelay());
        }

        // Check activation distance
        if (playerTransform != null && spawnedEnemy == null && !isRespawning)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= activationDistance)
            {
                SpawnEnemy();
            }
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
            // If using the flying enemy controllers like GooseController
            GooseController gooseController = spawnedEnemy.GetComponent<GooseController>();
            if (gooseController != null)
            {
                // GooseController references are set in its Start() method
                Debug.Log($"Spawned flying enemy {spawnedEnemy.name} at spawn point");
            }
        }
        else
        {
            // For ground-based enemies
            EnemyBaseController controller = spawnedEnemy.GetComponent<EnemyBaseController>();
            if (controller != null)
            {
                controller.navigationGraph = navigationManager.navigationGraph;
                controller.target = playerTransform;
                Debug.Log($"Spawned ground enemy {spawnedEnemy.name} at spawn point");
            }
        }

        // Subscribe to enemy destruction to handle respawning
        SpawnPointDeathTracker deathTracker = spawnedEnemy.AddComponent<SpawnPointDeathTracker>();
        deathTracker.spawnPoint = this;

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

    // Visualize the spawn point in the editor
    private void OnDrawGizmos()
    {
        // Draw spawn point
        Gizmos.color = isFlyingEnemy ? Color.blue : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw activation radius
        Gizmos.color = new Color(1f, 1f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, activationDistance);

        // For flying enemies, visualize the flight height
        if (isFlyingEnemy)
        {
            // Raycast to find ground height
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                Vector2.down,
                100f,
                Physics.DefaultRaycastLayers
            );

            if (hit.collider != null)
            {
                Vector3 groundPos = hit.point;
                Vector3 flyPos = new Vector3(transform.position.x, groundPos.y + flyHeight, transform.position.z);

                // Draw line from ground to flying height
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(groundPos, flyPos);

                // Draw sphere at flying height
                Gizmos.DrawWireSphere(flyPos, 0.3f);
            }
        }
    }
}