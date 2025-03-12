using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlightNodeGenerator : MonoBehaviour
{
    [Header("Node Generation")]
    public Transform nodeContainer;
    public GameObject nodeTemplate;
    public float nodeSpacing = 2f;
    public float verticalSpacing = 2f;
    public int verticalLayers = 4;
    public float minHeightAboveGround = 4f;

    [Header("Bounds")]
    public bool useNavigationGraphBounds = true;
    public NavigationGraph navigationGraph;
    public Bounds customLevelBounds = new Bounds(Vector3.zero, new Vector3(30, 20, 0));

    [Header("Connection Settings")]
    public float maxConnectionDistance = 5f;
    public float maxHeightDifference = 5f;
    [Tooltip("Maximum number of connections per node")]
    public int maxConnectionsPerNode = 4;
    [Tooltip("Preference for horizontal connections (higher = more horizontal paths)")]
    [Range(0.1f, 2f)]
    public float horizontalPreference = 1.2f;

    [Header("Obstacle Avoidance")]
    public LayerMask obstacleLayer;
    public float obstacleCheckRadius = 1f;
    public LayerMask groundLayer;

    [Header("Debug")]
    public bool visualizeNodes = true;
    public bool visualizeConnections = true;
    public Color nodeColor = Color.cyan;
    public Color connectionColor = Color.blue;

    private List<Node> generatedNodes = new List<Node>();
    private Bounds levelBounds;

    void Start()
    {
        // Find NavigationGraph if not assigned and using its bounds
        if (useNavigationGraphBounds && navigationGraph == null)
        {
            navigationGraph = FindObjectOfType<NavigationGraph>();
        }

        // Create node container if needed
        if (nodeContainer == null)
        {
            GameObject container = new GameObject("FlightNodes");
            nodeContainer = container.transform;
        }

        // Generate the flight node network
        GenerateFlightNodes();
    }

    public void GenerateFlightNodes()
    {
        // Clear any existing nodes
        ClearNodes();

        // Determine level bounds
        CalculateLevelBounds();

        // Create nodes
        CreateNodes();

        // Connect nodes
        ConnectNodes();

        Debug.Log($"Generated {generatedNodes.Count} flight nodes with connections");
    }

    private void CalculateLevelBounds()
    {
        if (useNavigationGraphBounds && navigationGraph != null)
        {
            // Get reference to the level bounds from NavigationGraph
            Bounds navBounds = CalculateBoundsFromNavigationGraph();
            levelBounds = navBounds;
        }
        else
        {
            // Use custom bounds
            levelBounds = customLevelBounds;
        }

        // Expand bounds a bit to ensure coverage
        levelBounds.Expand(2f);
    }

    private Bounds CalculateBoundsFromNavigationGraph()
    {
        if (navigationGraph.levelRoot == null)
            return new Bounds(Vector3.zero, Vector3.one * 100);

        Bounds bounds = new Bounds(navigationGraph.levelRoot.position, Vector3.zero);
        Collider2D[] colliders = navigationGraph.levelRoot.GetComponentsInChildren<Collider2D>();

        if (colliders.Length == 0)
        {
            Renderer[] renderers = navigationGraph.levelRoot.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        else
        {
            foreach (Collider2D collider in colliders)
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        // Expand bounds
        bounds.Expand(5f);
        return bounds;
    }

    private void CreateNodes()
    {
        float minX = levelBounds.min.x + nodeSpacing / 2;
        float maxX = levelBounds.max.x - nodeSpacing / 2;
        float minZ = 0;
        float maxZ = 0;

        // Calculate ground height at different positions
        for (float x = minX; x <= maxX; x += nodeSpacing)
        {
            for (float z = minZ; z <= maxZ; z += nodeSpacing)
            {
                // Get ground height at this position
                float groundHeight = GetGroundHeight(new Vector3(x, levelBounds.max.y, z));

                // Generate nodes at different heights
                for (int layer = 0; layer < verticalLayers; layer++)
                {
                    float height = groundHeight + minHeightAboveGround + (layer * verticalSpacing);

                    // Don't create nodes above level ceiling
                    if (height > levelBounds.max.y)
                        continue;

                    Vector3 nodePosition = new Vector3(x, height, z);

                    // Check if position is clear of obstacles
                    if (!IsPositionClear(nodePosition))
                        continue;

                    // Create node
                    CreateNodeAt(nodePosition);
                }
            }
        }
    }

    private float GetGroundHeight(Vector3 position)
    {
        RaycastHit2D hit = Physics2D.Raycast(
            new Vector2(position.x, position.y),
            Vector2.down,
            Mathf.Infinity,
            groundLayer
        );

        if (hit.collider != null)
        {
            return hit.point.y;
        }

        return levelBounds.min.y;
    }

    private bool IsPositionClear(Vector3 position)
    {
        return !Physics2D.OverlapCircle(new Vector2(position.x, position.y), obstacleCheckRadius, obstacleLayer);
    }

    private Node CreateNodeAt(Vector3 position)
    {
        if (nodeTemplate == null)
        {
            GameObject nodeObj = new GameObject("FlightNode");
            nodeObj.transform.position = position;
            nodeObj.transform.SetParent(nodeContainer);

            Node node = nodeObj.AddComponent<Node>();
            node.connections = new List<Node>();

            generatedNodes.Add(node);
            return node;
        }
        else
        {
            GameObject nodeObj = Instantiate(nodeTemplate, position, Quaternion.identity, nodeContainer);
            nodeObj.name = "FlightNode";

            Node node = nodeObj.GetComponent<Node>();
            if (node == null)
                node = nodeObj.AddComponent<Node>();

            node.connections = new List<Node>();

            generatedNodes.Add(node);
            return node;
        }
    }

    // Helper class for sorting nodes by connection priority
    private class NodeConnection
    {
        public Node node;
        public float priority;
    }

    private void ConnectNodes()
    {
        // Connect each node to nearby nodes with smarter prioritization
        foreach (Node node in generatedNodes)
        {
            // Collect potential connections with priority scores
            List<NodeConnection> potentialConnections = new List<NodeConnection>();

            foreach (Node otherNode in generatedNodes)
            {
                if (node == otherNode)
                    continue;

                float distance = Vector3.Distance(node.transform.position, otherNode.transform.position);
                float heightDifference = Mathf.Abs(node.transform.position.y - otherNode.transform.position.y);

                // Check if within connection distance and height difference limits
                if (distance <= maxConnectionDistance && heightDifference <= maxHeightDifference)
                {
                    // Check if line of sight is clear
                    if (IsClearPath(node.transform.position, otherNode.transform.position))
                    {
                        // Calculate connection priority
                        // Lower = higher priority
                        float horizontalDistance = Vector2.Distance(
                            new Vector2(node.transform.position.x, 0),
                            new Vector2(otherNode.transform.position.x, 0)
                        );

                        // Prioritize horizontal connections over vertical ones
                        float priority = distance;
                        if (horizontalPreference > 1f)
                        {
                            // Apply horizontal preference (reduce priority for horizontal connections)
                            priority = distance - (horizontalDistance * (horizontalPreference - 1f));
                        }

                        // Additional priority for nodes in the same vertical layer
                        if (heightDifference < 0.1f)
                        {
                            priority *= 0.8f;
                        }

                        potentialConnections.Add(new NodeConnection
                        {
                            node = otherNode,
                            priority = priority
                        });
                    }
                }
            }

            // Sort by priority (lowest first)
            potentialConnections.Sort((a, b) => a.priority.CompareTo(b.priority));

            // Connect to the best nodes (limited by maxConnectionsPerNode)
            int connectionsToMake = Mathf.Min(maxConnectionsPerNode, potentialConnections.Count);
            for (int i = 0; i < connectionsToMake; i++)
            {
                Node targetNode = potentialConnections[i].node;
                if (!node.connections.Contains(targetNode))
                {
                    node.connections.Add(targetNode);
                }
            }
        }
    }

    private bool IsClearPath(Vector3 start, Vector3 end)
    {
        Vector2 start2D = new Vector2(start.x, start.y);
        Vector2 end2D = new Vector2(end.x, end.y);
        Vector2 direction = end2D - start2D;
        float distance = direction.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(start2D, direction.normalized, distance, obstacleLayer);
        return hit.collider == null;
    }

    private void ClearNodes()
    {
        generatedNodes.Clear();

        if (nodeContainer != null)
        {
            // Remove all children
            while (nodeContainer.childCount > 0)
            {
                DestroyImmediate(nodeContainer.GetChild(0).gameObject);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!visualizeNodes && !visualizeConnections)
            return;

        // Visualize generated nodes
        if (Application.isPlaying && generatedNodes.Count > 0)
        {
            if (visualizeNodes)
            {
                Gizmos.color = nodeColor;
                foreach (Node node in generatedNodes)
                {
                    if (node != null)
                    {
                        Gizmos.DrawSphere(node.transform.position, 0.3f);
                    }
                }
            }

            if (visualizeConnections)
            {
                Gizmos.color = connectionColor;
                foreach (Node node in generatedNodes)
                {
                    if (node != null && node.connections != null)
                    {
                        foreach (Node connectedNode in node.connections)
                        {
                            if (connectedNode != null)
                            {
                                Gizmos.DrawLine(node.transform.position, connectedNode.transform.position);
                            }
                        }
                    }
                }
            }
        }

        // Visualize level bounds
        if (visualizeNodes && !Application.isPlaying)
        {
            // Calculate bounds if possible
            if (useNavigationGraphBounds && navigationGraph != null)
            {
                Bounds navBounds = CalculateBoundsFromNavigationGraph();
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireCube(navBounds.center, navBounds.size);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawWireCube(customLevelBounds.center, customLevelBounds.size);
            }
        }
    }

    // Public method to get all generated nodes
    public List<Node> GetGeneratedNodes()
    {
        return generatedNodes;
    }
}