using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationManager : MonoBehaviour
{
    [Header("References")]
    public NavigationGraph navigationGraph;
    public Transform playerTransform;
    public ChunkManager chunkManager;

    [Header("Navigation Settings")]
    public LayerMask groundLayer;  // Define this on NavigationManager and pass to graph
    public string levelRootName = "LevelRoot";  // Name of level root object to find
    public bool useChunkBasedNavigation = true; // Whether to use chunk-based navigation

    [Header("Enemy Spawning")]
    public GameObject[] groundEnemyPrefabs;  // Changed from enemyPrefabs
    public GameObject[] flyingEnemyPrefabs;  // New array for flying enemies
    public Transform enemyContainer;
    public bool spawnEnemiesOnStart = true;
    public int initialGroundEnemyCount = 3;
    public int initialFlyingEnemyCount = 2;  // New count for flying enemies

    // Define enum outside the class to avoid Unity serialization issues
    public enum SpawnAreaType { Radius, Cone }

    [Header("Spawn Area Settings")]
    public SpawnAreaType spawnAreaType = SpawnAreaType.Cone;

    [Tooltip("Maximum distance from player to spawn enemies")]
    public float spawnRadius = 10f;

    [Tooltip("Minimum distance from player to spawn enemies")]
    public float minSpawnDistance = 6f;

    [Header("Cone Settings")]
    [Tooltip("Direction the cone points (in degrees, 0 = right, 90 = up)")]
    [Range(0f, 360f)]
    public float coneDirection = 0f;

    [Tooltip("Total angle of the cone (in degrees)")]
    [Range(0f, 360f)]
    public float coneAngle = 90f;

    [Header("Flying Enemy Settings")]  // New section
    [Tooltip("Minimum height for spawning flying enemies")]
    public float minFlyingHeight = 3f;

    [Tooltip("Maximum height for spawning flying enemies")]
    public float maxFlyingHeight = 8f;

    [Tooltip("Whether to ensure flying node network exists before spawning")]
    public bool ensureFlightNodesExist = true;

    private void Start()
    {
        InitializeComponents();

        if (navigationGraph != null)
        {
            navigationGraph.ClearAndRebuild();
            Debug.Log("Navigation graph built successfully");
        }
        else
        {
            Debug.LogError("Navigation graph is null after initialization!");
            return;
        }

        // Spawn initial enemies after a delay to ensure graph is fully built
        if (spawnEnemiesOnStart)
        {
            StartCoroutine(SpawnInitialEnemies());
        }
    }

    private void InitializeComponents()
    {
        // Find chunk manager if not set
        if (chunkManager == null && useChunkBasedNavigation)
        {
            chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager == null)
            {
                Debug.LogWarning("No ChunkManager found but useChunkBasedNavigation is enabled! Disabling chunk navigation.");
                useChunkBasedNavigation = false;
            }
        }

        // Find navigation graph if not set
        if (navigationGraph == null)
        {
            navigationGraph = FindObjectOfType<NavigationGraph>();
            if (navigationGraph == null)
            {
                Debug.LogError("No NavigationGraph component found! Creating one now.");

                // Create a navigation graph if none exists
                GameObject navGraphObj = new GameObject("NavigationGraph");
                navigationGraph = navGraphObj.AddComponent<NavigationGraph>();

                // Set up the NavigationGraph configuration
                navigationGraph.groundLayer = groundLayer;
                navigationGraph.nodeSpacing = 2f;
                navigationGraph.nodeHeight = 1f;
            }
        }

        // Find player if not set
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Debug.LogError("No player found in scene with tag 'Player'!");
            }
        }

        // Create enemy container if needed
        if (enemyContainer == null)
        {
            GameObject container = new GameObject("Enemies");
            enemyContainer = container.transform;
        }
    }

    // Spawn the initial set of enemies
    private IEnumerator SpawnInitialEnemies()
    {
        // Wait for navigation graph to be fully built
        yield return new WaitForSeconds(0.5f);

        Debug.Log("Starting to spawn initial enemies");

        // Track successful spawns
        int groundEnemiesSpawned = 0;
        int flyingEnemiesSpawned = 0;

        // Ensure flight nodes exist if we're spawning flying enemies
        if (ensureFlightNodesExist && flyingEnemyPrefabs.Length > 0 && initialFlyingEnemyCount > 0)
        {
            FlightNodeGenerator flightNodeGen = FindObjectOfType<FlightNodeGenerator>();
            if (flightNodeGen == null)
            {
                Debug.Log("Creating FlightNodeGenerator for flying enemies");
                GameObject nodeGenObj = new GameObject("FlightNodeGenerator");
                flightNodeGen = nodeGenObj.AddComponent<FlightNodeGenerator>();
                flightNodeGen.navigationGraph = navigationGraph;
                flightNodeGen.useNavigationGraphBounds = true;
                flightNodeGen.groundLayer = navigationGraph.groundLayer;
                flightNodeGen.obstacleLayer = navigationGraph.groundLayer;
            }

            // Generate flight nodes if needed
            if (flightNodeGen.GetGeneratedNodes().Count == 0)
            {
                Debug.Log("Generating flight nodes for flying enemies");
                flightNodeGen.GenerateFlightNodes();
            }

            // Wait a moment for nodes to be generated
            yield return new WaitForSeconds(0.3f);
        }

        // Try to spawn ground enemies
        for (int i = 0; i < initialGroundEnemyCount; i++)
        {
            if (groundEnemyPrefabs.Length > 0)
            {
                GameObject enemy = SpawnGroundEnemy();

                if (enemy != null)
                {
                    groundEnemiesSpawned++;
                    yield return new WaitForSeconds(0.2f); // Stagger spawns
                }
            }
        }

        // Try to spawn flying enemies
        for (int i = 0; i < initialFlyingEnemyCount; i++)
        {
            if (flyingEnemyPrefabs.Length > 0)
            {
                GameObject enemy = SpawnFlyingEnemy();

                if (enemy != null)
                {
                    flyingEnemiesSpawned++;
                    yield return new WaitForSeconds(0.2f); // Stagger spawns
                }
            }
        }

        Debug.Log($"Spawned {groundEnemiesSpawned} ground enemies and {flyingEnemiesSpawned} flying enemies");
    }

    // Spawn a single ground enemy at a valid position (renamed from SpawnEnemy)
    public GameObject SpawnGroundEnemy()
    {
        if (groundEnemyPrefabs.Length == 0 || navigationGraph == null)
            return null;

        // Choose a random enemy prefab
        GameObject prefab = groundEnemyPrefabs[Random.Range(0, groundEnemyPrefabs.Length)];

        // Try to find a valid spawn position
        Vector3 spawnPos = FindValidGroundSpawnPosition();

        // Instantiate the enemy
        GameObject enemyObject = Instantiate(prefab, spawnPos, Quaternion.identity, enemyContainer);

        // Set up the enemy controller
        EnemyBaseController controller = enemyObject.GetComponent<EnemyBaseController>();
        if (controller != null)
        {
            controller.navigationGraph = navigationGraph;
            controller.target = playerTransform;
            Debug.Log($"Set up controller for ground enemy {enemyObject.name}");
        }
        else
        {
            Debug.LogWarning($"No SimpleEnemyController found on spawned enemy {enemyObject.name}");
        }

        return enemyObject;
    }

    // Spawn a single flying enemy at a valid position (new method)
    public GameObject SpawnFlyingEnemy()
    {
        if (flyingEnemyPrefabs.Length == 0)
            return null;

        // Choose a random enemy prefab
        GameObject prefab = flyingEnemyPrefabs[Random.Range(0, flyingEnemyPrefabs.Length)];

        // Try to find a valid spawn position
        Vector3 spawnPos = FindValidFlyingSpawnPosition();

        // Instantiate the enemy
        GameObject enemyObject = Instantiate(prefab, spawnPos, Quaternion.identity, enemyContainer);

        // Set up the goose controller (if it has one)
        GooseController gooseController = enemyObject.GetComponent<GooseController>();
        if (gooseController != null)
        {
            // Reference setup is handled in the GooseController's Start method
            Debug.Log($"Spawned flying enemy {enemyObject.name}");
        }
        else
        {
            Debug.LogWarning($"No GooseController found on spawned flying enemy {enemyObject.name}");
        }

        return enemyObject;
    }

    // Find a valid position to spawn a ground enemy (renamed from FindValidSpawnPosition)
    private Vector3 FindValidGroundSpawnPosition()
    {
        // Define center point for spawning
        Vector3 centerPoint = playerTransform != null ? playerTransform.position : Vector3.zero;

        // List of candidate positions
        List<Vector3> candidatePositions = new List<Vector3>();

        // Try to find valid positions
        for (int i = 0; i < 30; i++)
        {
            // Get a random position based on spawn area type
            Vector2 offset;

            if (spawnAreaType == SpawnAreaType.Cone)
            {
                // Calculate a random position within the cone
                float halfConeAngle = coneAngle * 0.5f;
                float randomAngle = Random.Range(-halfConeAngle, halfConeAngle) + coneDirection;
                randomAngle *= Mathf.Deg2Rad; // Convert to radians

                float distance = Random.Range(minSpawnDistance, spawnRadius);

                offset = new Vector2(
                    Mathf.Cos(randomAngle) * distance,
                    Mathf.Sin(randomAngle) * distance
                );
            }
            else // Radius
            {
                // Get a random position within spawn radius
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnDistance, spawnRadius);

                offset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
            }

            Vector3 testPos = centerPoint + new Vector3(offset.x, offset.y, 0);

            // Check if position is valid (has ground below)
            RaycastHit2D hit = Physics2D.Raycast(
                testPos,
                Vector2.down,
                5f,
                navigationGraph.groundLayer
            );

            if (hit.collider != null)
            {
                // Adjust position to be just above the ground
                Vector3 candidatePos = new Vector3(testPos.x, hit.point.y + 0.5f, 0);

                // Verify this position is not too close to existing enemies
                bool tooCloseToOtherEnemy = false;

                if (enemyContainer != null)
                {
                    foreach (Transform child in enemyContainer)
                    {
                        if (Vector3.Distance(child.position, candidatePos) < 2f)
                        {
                            tooCloseToOtherEnemy = true;
                            break;
                        }
                    }
                }

                // Add to candidates if not too close to other enemies
                if (!tooCloseToOtherEnemy)
                {
                    candidatePositions.Add(candidatePos);

                    // If we have enough candidates, pick a random one and return
                    if (candidatePositions.Count >= 3)
                    {
                        return candidatePositions[Random.Range(0, candidatePositions.Count)];
                    }
                }
            }
        }

        // If we found any valid positions, use one
        if (candidatePositions.Count > 0)
        {
            return candidatePositions[Random.Range(0, candidatePositions.Count)];
        }

        // Fallback - return a position near the center but off to the side
        Debug.LogWarning("Could not find valid ground spawn position, using fallback");
        return centerPoint + new Vector3(Random.Range(-3f, 3f), 5f, 0);
    }

    // Find a valid position to spawn a flying enemy (new method)
    private Vector3 FindValidFlyingSpawnPosition()
    {
        // Define center point for spawning
        Vector3 centerPoint = playerTransform != null ? playerTransform.position : Vector3.zero;

        // List of candidate positions
        List<Vector3> candidatePositions = new List<Vector3>();

        // Try to find valid positions
        for (int i = 0; i < 30; i++)
        {
            // Get a random position based on spawn area type
            Vector2 offset;

            if (spawnAreaType == SpawnAreaType.Cone)
            {
                // Calculate a random position within the cone
                float halfConeAngle = coneAngle * 0.5f;
                float randomAngle = Random.Range(-halfConeAngle, halfConeAngle) + coneDirection;
                randomAngle *= Mathf.Deg2Rad; // Convert to radians

                float distance = Random.Range(minSpawnDistance, spawnRadius);

                offset = new Vector2(
                    Mathf.Cos(randomAngle) * distance,
                    Mathf.Sin(randomAngle) * distance
                );
            }
            else // Radius
            {
                // Get a random position within spawn radius
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(minSpawnDistance, spawnRadius);

                offset = new Vector2(
                    Mathf.Cos(angle) * distance,
                    Mathf.Sin(angle) * distance
                );
            }

            // Get a random height between min and max
            float height = Random.Range(minFlyingHeight, maxFlyingHeight);

            // Get ground height at this position
            Vector3 testPos = centerPoint + new Vector3(offset.x, offset.y, 0);
            RaycastHit2D hit = Physics2D.Raycast(
                testPos,
                Vector2.down,
                100f,
                navigationGraph.groundLayer
            );

            float groundHeight = 0;
            if (hit.collider != null)
            {
                groundHeight = hit.point.y;
            }

            // Set position at proper height above ground
            Vector3 candidatePos = new Vector3(testPos.x, groundHeight + height, 0);

            // Check if position is clear of obstacles
            bool positionClear = !Physics2D.OverlapCircle(
                new Vector2(candidatePos.x, candidatePos.y),
                1.0f,
                navigationGraph.groundLayer
            );

            if (positionClear)
            {
                // Verify this position is not too close to existing enemies
                bool tooCloseToOtherEnemy = false;

                if (enemyContainer != null)
                {
                    foreach (Transform child in enemyContainer)
                    {
                        if (Vector3.Distance(child.position, candidatePos) < 3f)
                        {
                            tooCloseToOtherEnemy = true;
                            break;
                        }
                    }
                }

                // Add to candidates if not too close to other enemies
                if (!tooCloseToOtherEnemy)
                {
                    candidatePositions.Add(candidatePos);

                    // If we have enough candidates, pick a random one and return
                    if (candidatePositions.Count >= 3)
                    {
                        return candidatePositions[Random.Range(0, candidatePositions.Count)];
                    }
                }
            }
        }

        // If we found any valid positions, use one
        if (candidatePositions.Count > 0)
        {
            return candidatePositions[Random.Range(0, candidatePositions.Count)];
        }

        // Fallback - return a position above the player
        Debug.LogWarning("Could not find valid flying spawn position, using fallback");
        return centerPoint + new Vector3(Random.Range(-5f, 5f), 8f, 0);
    }

    // Update the navigation graph (can be called after level changes)
    public void RebuildNavigationGraph()
    {
        if (navigationGraph == null)
        {
            Debug.LogError("Cannot rebuild navigation graph - reference is null!");
            return;
        }

        Debug.Log("Rebuilding navigation graph...");

        // Clear and rebuild from scratch
        navigationGraph.ClearAndRebuild();

        // Also rebuild flight nodes if they exist
        FlightNodeGenerator flightNodeGen = FindObjectOfType<FlightNodeGenerator>();
        if (flightNodeGen != null)
        {
            Debug.Log("Rebuilding flight node network...");
            flightNodeGen.GenerateFlightNodes();
        }
    }

    // New method to handle chunk updates
    public void OnChunkAdded(GameObject chunk)
    {
        if (navigationGraph != null && useChunkBasedNavigation)
        {
            // The ProcessNewChunks method in NavigationGraph will handle this automatically
            // This method is here for explicit notification if needed
            Debug.Log($"Navigation Manager notified of new chunk: {chunk.name}");
        }
    }

    // Draw editor gizmos
    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Vector3 centerPoint = playerTransform.position;

            if (spawnAreaType == SpawnAreaType.Cone)
            {
                // Draw min spawn radius
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
                Gizmos.DrawWireSphere(centerPoint, minSpawnDistance);

                // Draw max spawn radius
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireSphere(centerPoint, spawnRadius);

                // Draw cone
                DrawCone(centerPoint, coneDirection, coneAngle, minSpawnDistance, spawnRadius);
            }
            else
            {
                // Draw spawn radius
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(centerPoint, spawnRadius);

                // Draw min spawn distance
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
                Gizmos.DrawWireSphere(centerPoint, minSpawnDistance);
            }
        }
    }

    // Helper method to draw a cone in the editor
    private void DrawCone(Vector3 origin, float direction, float angle, float minDistance, float maxDistance)
    {
        float halfAngle = angle * 0.5f;
        float startAngle = (direction - halfAngle) * Mathf.Deg2Rad;
        float endAngle = (direction + halfAngle) * Mathf.Deg2Rad;

        // Number of segments to use for the cone arcs
        int segments = 20;

        // Draw the cone as a series of lines
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);

        // Draw the inner arc
        Vector3 prevPoint = origin + new Vector3(Mathf.Cos(startAngle) * minDistance, Mathf.Sin(startAngle) * minDistance, 0);
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            Vector3 newPoint = origin + new Vector3(Mathf.Cos(currentAngle) * minDistance, Mathf.Sin(currentAngle) * minDistance, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }

        // Draw the outer arc
        prevPoint = origin + new Vector3(Mathf.Cos(startAngle) * maxDistance, Mathf.Sin(startAngle) * maxDistance, 0);
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);

            Vector3 newPoint = origin + new Vector3(Mathf.Cos(currentAngle) * maxDistance, Mathf.Sin(currentAngle) * maxDistance, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }

        // Draw radial lines
        Vector3 startMin = origin + new Vector3(Mathf.Cos(startAngle) * minDistance, Mathf.Sin(startAngle) * minDistance, 0);
        Vector3 startMax = origin + new Vector3(Mathf.Cos(startAngle) * maxDistance, Mathf.Sin(startAngle) * maxDistance, 0);
        Gizmos.DrawLine(startMin, startMax);

        Vector3 endMin = origin + new Vector3(Mathf.Cos(endAngle) * minDistance, Mathf.Sin(endAngle) * minDistance, 0);
        Vector3 endMax = origin + new Vector3(Mathf.Cos(endAngle) * maxDistance, Mathf.Sin(endAngle) * maxDistance, 0);
        Gizmos.DrawLine(endMin, endMax);
    }
}