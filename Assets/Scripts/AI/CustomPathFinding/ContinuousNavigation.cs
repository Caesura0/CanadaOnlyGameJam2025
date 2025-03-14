using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this to your NavigationGraph component or use it as a separate component
public class ContinuousNavigation : MonoBehaviour
{
    [Header("Navigation Coverage")]
    public Transform playerTransform;
    public float navWindowWidth = 40f;    // How wide the navigation window is
    public float navWindowHeight = 30f;   // How tall the navigation window is
    public float updateDistance = 10f;    // Update when player moves this far from last update position
    public float cleanupDistance = 50f;   // Remove nodes beyond this distance from player

    [Header("Performance")]
    public float updateInterval = 0.5f;   // How often to check for updates (seconds)
    public int maxNodesPerUpdate = 100;   // Maximum nodes to process per update

    // References
    private NavigationGraph navigationGraph;
    private Vector2 lastUpdatePosition;
    private float nextUpdateTime = 0f;

    private void Start()
    {
        navigationGraph = GetComponent<NavigationGraph>();
        if (navigationGraph == null)
        {
            navigationGraph = FindObjectOfType<NavigationGraph>();
            if (navigationGraph == null)
            {
                Debug.LogError("No NavigationGraph found! ContinuousNavigation requires a NavigationGraph.");
                enabled = false;
                return;
            }
        }

        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("No player transform assigned and couldn't find one with tag 'Player'!");
                enabled = false;
                return;
            }
        }

        // Perform initial navigation setup
        lastUpdatePosition = playerTransform.position;
        UpdateNavigationZone(lastUpdatePosition, true);
    }

    private void Update()
    {
        if (Time.time < nextUpdateTime || playerTransform == null)
            return;

        // Check if player has moved far enough to update
        float distanceMoved = Vector2.Distance(playerTransform.position, lastUpdatePosition);
        if (distanceMoved >= updateDistance)
        {
            UpdateNavigationZone(playerTransform.position, false);
            lastUpdatePosition = playerTransform.position;
        }

        nextUpdateTime = Time.time + updateInterval;
    }

    // Update the navigation graph around the given position
    public void UpdateNavigationZone(Vector2 centerPosition, bool fullRebuild)
    {
        // Calculate the bounds of the navigation window
        Bounds navBounds = new Bounds(centerPosition, new Vector3(navWindowWidth, navWindowHeight, 1f));

        // Direction of movement (will have more nodes in this direction)
        Vector2 moveDirection = (centerPosition - (Vector2)lastUpdatePosition).normalized;

        // Extend bounds in the movement direction
        if (!fullRebuild && moveDirection.magnitude > 0.1f)
        {
            Vector3 extension = new Vector3(moveDirection.x, moveDirection.y, 0) * updateDistance * 2;
            navBounds.Expand(extension);
        }

        if (fullRebuild)
        {
            // Full rebuild - clear everything and start fresh
            navigationGraph.ClearAndRebuild();
        }
        else
        {
            // Incremental update
            // 1. Generate new nodes in the expanded area
            GenerateNodesInBounds(navBounds);

            // 2. Clean up distant nodes
            CleanupDistantNodes(centerPosition);
        }
    }

    private void GenerateNodesInBounds(Bounds bounds)
    {
        // Calculate grid positions
        int minX = Mathf.FloorToInt(bounds.min.x / navigationGraph.nodeSpacing) - 1;
        int maxX = Mathf.CeilToInt(bounds.max.x / navigationGraph.nodeSpacing) + 1;
        int minY = Mathf.FloorToInt(bounds.min.y / navigationGraph.nodeHeight) - 1;
        int maxY = Mathf.CeilToInt(bounds.max.y / navigationGraph.nodeHeight) + 5; // More vertical space

        int nodesProcessed = 0;

        // Create a list of positions to check
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }

        // Shuffle the list to distribute node creation evenly
        ShuffleList(positions);

        // Process up to maxNodesPerUpdate
        for (int i = 0; i < Mathf.Min(positions.Count, maxNodesPerUpdate); i++)
        {
            Vector2Int gridPos = positions[i];

            // Skip if we already have a node here
            if (navigationGraph.GetNodeTypeAt(gridPos) != NodeType.None)
                continue;

            // Check if this position is valid for a node
            Vector2 worldPos = navigationGraph.GridToWorld(gridPos);
            Vector2 belowPos = worldPos + Vector2.down * navigationGraph.nodeHeight * 0.6f;

            bool isEmpty = !Physics2D.OverlapCircle(worldPos, 0.1f, navigationGraph.groundLayer);
            bool hasGroundBelow = Physics2D.OverlapCircle(belowPos, 0.1f, navigationGraph.groundLayer);

            if (isEmpty && hasGroundBelow)
            {
                // This is a valid node position - add it through a public method
                AddNodeAtPosition(gridPos, NodeType.Base);
                nodesProcessed++;
            }
        }

        // After adding nodes, update edge nodes
        UpdateEdgeNodes();

        Debug.Log($"Generated {nodesProcessed} new navigation nodes");
    }

    private void CleanupDistantNodes(Vector2 centerPosition)
    {
        // Convert center to grid position
        Vector2Int centerGrid = navigationGraph.WorldToGrid(centerPosition);

        // Create a list of nodes to remove
        List<Vector2Int> nodesToRemove = new List<Vector2Int>();

        // Check all nodes
        foreach (var nodePos in GetAllNodePositions())
        {
            float distance = Vector2Int.Distance(nodePos, centerGrid);
            if (distance > cleanupDistance / navigationGraph.nodeSpacing)
            {
                nodesToRemove.Add(nodePos);
            }
        }

        // Remove nodes beyond cleanup distance
        foreach (var nodePos in nodesToRemove)
        {
            RemoveNodeAtPosition(nodePos);
        }

        if (nodesToRemove.Count > 0)
        {
            Debug.Log($"Removed {nodesToRemove.Count} distant navigation nodes");
        }
    }

    private void UpdateEdgeNodes()
    {
        // This method is a simplified version of CreateEdgeNodes from NavigationGraph
        List<Vector2Int> baseNodes = new List<Vector2Int>();
        List<Vector2Int> potentialEdges = new List<Vector2Int>();

        // Get all base nodes
        foreach (var nodePos in GetAllNodePositions())
        {
            if (GetNodeTypeAtPosition(nodePos) == NodeType.Base)
            {
                baseNodes.Add(nodePos);

                // Check if this could be an edge
                Vector2Int rightPos = nodePos + Vector2Int.right;
                Vector2Int leftPos = nodePos + Vector2Int.left;

                bool hasRightNeighbor = NodeExistsAtPosition(rightPos) ||
                                      Physics2D.OverlapCircle(navigationGraph.GridToWorld(rightPos), 0.1f, navigationGraph.groundLayer);

                bool hasLeftNeighbor = NodeExistsAtPosition(leftPos) ||
                                     Physics2D.OverlapCircle(navigationGraph.GridToWorld(leftPos), 0.1f, navigationGraph.groundLayer);

                // If missing neighbors on either side, it's a potential edge
                if (!hasRightNeighbor || !hasLeftNeighbor)
                {
                    potentialEdges.Add(nodePos);
                }
            }
        }

        // Update node types for edges
        foreach (var edgePos in potentialEdges)
        {
            UpdateNodeTypeAtPosition(edgePos, NodeType.Edge);
        }
    }

    // These methods would call into the NavigationGraph to manipulate nodes
    // You would need to add these public methods to NavigationGraph

    private List<Vector2Int> GetAllNodePositions()
    {
        // This should be implemented in NavigationGraph to return all node positions
        // For now, this is a placeholder that returns an empty list
        return new List<Vector2Int>();
    }

    private void AddNodeAtPosition(Vector2Int position, NodeType type)
    {
        // This should be implemented in NavigationGraph
        // For example: navigationGraph.nodes[position] = type;
    }

    private void RemoveNodeAtPosition(Vector2Int position)
    {
        // This should be implemented in NavigationGraph
        // For example: navigationGraph.nodes.Remove(position);
    }

    private bool NodeExistsAtPosition(Vector2Int position)
    {
        // This should be implemented in NavigationGraph
        // For example: return navigationGraph.nodes.ContainsKey(position);
        return navigationGraph.GetNodeTypeAt(position) != NodeType.None;
    }

    private NodeType GetNodeTypeAtPosition(Vector2Int position)
    {
        // This should use the existing method
        return navigationGraph.GetNodeTypeAt(position);
    }

    private void UpdateNodeTypeAtPosition(Vector2Int position, NodeType type)
    {
        // This should be implemented in NavigationGraph
        // For example: if (navigationGraph.nodes.ContainsKey(position)) navigationGraph.nodes[position] = type;
    }

    // Helper method to shuffle a list
    private void ShuffleList<T>(List<T> list)
    {
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            int r = i + Random.Range(0, n - i);
            T temp = list[r];
            list[r] = list[i];
            list[i] = temp;
        }
    }

    // Draw the navigation window in the editor
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null)
            return;

        // Draw the navigation window
        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.2f);
        Gizmos.DrawWireCube(playerTransform.position, new Vector3(navWindowWidth, navWindowHeight, 1f));

        // Draw the cleanup distance
        Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.1f);
        Gizmos.DrawWireSphere(playerTransform.position, cleanupDistance);

        // Draw the update trigger distance
        Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(lastUpdatePosition, updateDistance);
    }
}