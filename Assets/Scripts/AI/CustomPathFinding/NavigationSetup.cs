using UnityEngine;

// Add this component to your game scene to set up player-centered navigation
// It will automatically create and configure the navigation system for your chunked level
public class NavigationSetup : MonoBehaviour
{
    [Header("References")]
    public Transform playerTransform;
    public ChunkManager chunkManager;
    public LayerMask groundLayer;

    [Header("Navigation Settings")]
    public float nodeSpacing = 2f;
    public float nodeHeight = 1f;
    public float coverageRadius = 30f;

    [Header("Visualization")]
    public bool visualizeGraph = true;
    public bool visualizePaths = true;

    // Created components
    private NavigationGraph navGraph;
    private NavigationManager navManager;

    private void Awake()
    {
        // Find player if not set
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("No player found! Navigation setup failed.");
                enabled = false;
                return;
            }
        }

        // Find ChunkManager if not set
        if (chunkManager == null)
        {
            chunkManager = FindObjectOfType<ChunkManager>();
            if (chunkManager == null)
            {
                Debug.LogWarning("No ChunkManager found. Will use player-centered navigation without chunk awareness.");
            }
        }

        // Create the navigation graph
        GameObject navGraphObj = new GameObject("NavigationGraph");
        navGraph = navGraphObj.AddComponent<NavigationGraph>();

        // Configure the graph
        navGraph.groundLayer = groundLayer;
        navGraph.nodeSpacing = nodeSpacing;
        navGraph.nodeHeight = nodeHeight;
        navGraph.useContinuousNavigation = true;
        navGraph.coverageRadius = coverageRadius;
        navGraph.visualizeGraph = visualizeGraph;
        navGraph.visualizePaths = visualizePaths;

        // Create the navigation manager
        GameObject navManagerObj = new GameObject("NavigationManager");
        navManager = navManagerObj.AddComponent<NavigationManager>();

        // Configure the manager
        navManager.navigationGraph = navGraph;
        navManager.playerTransform = playerTransform;
        navManager.chunkManager = chunkManager;
        navManager.groundLayer = groundLayer;
        navManager.useChunkBasedNavigation = chunkManager != null;

        Debug.Log("Navigation system set up successfully!");
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            // Draw the coverage radius
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.1f);
            Gizmos.DrawWireSphere(playerTransform.position, coverageRadius);

            // Draw node spacing grid (just a few samples)
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Vector3 playerPos = playerTransform.position;
            int gridSize = 5;

            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int y = -gridSize; y <= gridSize; y++)
                {
                    Vector3 nodePos = new Vector3(
                        Mathf.Round(playerPos.x / nodeSpacing) * nodeSpacing + x * nodeSpacing,
                        Mathf.Round(playerPos.y / nodeHeight) * nodeHeight + y * nodeHeight,
                        0
                    );

                    Gizmos.DrawWireSphere(nodePos, 0.1f);
                }
            }
        }
    }
}