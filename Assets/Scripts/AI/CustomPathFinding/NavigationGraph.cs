using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NavigationGraph : MonoBehaviour
{
    [Header("Map Settings")]
    public Transform levelRoot; // Parent object containing all level platforms
    public LayerMask groundLayer; // Layer for ground/solid platforms
    public float nodeSpacing = 2f; // Distance between nodes (samples)
    public float nodeHeight = 1f; // Vertical distance between nodes
    public float updateDistance = 2f; // Distance goal must move to update paths

    [Header("Debug Settings")]
    public bool visualizeGraph = true;
    public bool visualizePaths = true;
    public Color baseNodeColor = Color.green;
    public Color connectorNodeColor = new Color(1f, 0.5f, 0f); // Orange
    public Color pathColor = Color.cyan;

    // The graph representation - just base nodes for now, will add connector and fall nodes when we add jumping
    private Dictionary<Vector2Int, NodeType> nodes = new Dictionary<Vector2Int, NodeType>();

    // Cached paths (goalPosition -> path)
    private Dictionary<Vector2Int, Dictionary<Vector2Int, Vector2>> paths = new Dictionary<Vector2Int, Dictionary<Vector2Int, Vector2>>();

    // Last goal position used for path calculation
    private Vector2Int lastGoalPos = Vector2Int.zero;

    private void Start()
    {
        BuildGraph();
    }

    // Public method to convert world to grid coordinates
    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / nodeSpacing),
            Mathf.RoundToInt(worldPos.y / nodeHeight)
        );
    }

    // Public method to convert grid to world coordinates
    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            gridPos.x * nodeSpacing,
            gridPos.y * nodeHeight
        );
    }

    // Clear everything and rebuild
    public void ClearAndRebuild()
    {
        StopAllCoroutines();
        nodes.Clear();
        paths.Clear();
        BuildGraph();
    }

    // Get node type at a specific grid position
    public NodeType GetNodeTypeAt(Vector2Int gridPos)
    {
        if (nodes.TryGetValue(gridPos, out NodeType nodeType))
        {
            return nodeType;
        }

        // If not found in dictionary, check if it's over ground - if so, 
        // it's effectively a base node for pathfinding purposes
        Vector2 worldPos = GridToWorld(gridPos);
        Vector2 belowPos = worldPos + Vector2.down * nodeHeight * 0.6f;

        bool isEmpty = !Physics2D.OverlapCircle(worldPos, 0.1f, groundLayer);
        bool hasGroundBelow = Physics2D.OverlapCircle(belowPos, 0.1f, groundLayer);

        if (isEmpty && hasGroundBelow)
        {
            return NodeType.Base;
        }

        return NodeType.None;
    }

    // Build the navigation graph for the current level
    public void BuildGraph()
    {
        nodes.Clear();

        // Step 1: Create base nodes (empty spaces with ground underneath)
        CreateBaseNodes();

        // Step 2: Create connector nodes (nodes at platform edges for better visualization)
        CreateEdgeNodes();

        Debug.Log($"Navigation graph built with {nodes.Count} nodes");

        if (visualizeGraph)
        {
            StartCoroutine(VisualizeGraphCoroutine());
        }

        Debug.Log($"Ground Layer mask: {groundLayer.value}");
    }

    // Create initial nodes for empty spaces with ground underneath
    private void CreateBaseNodes()
    {
        // Define boundaries of the level
        Bounds levelBounds = CalculateLevelBounds();
        int minX = Mathf.FloorToInt(levelBounds.min.x / nodeSpacing) - 2; // Add padding
        int maxX = Mathf.CeilToInt(levelBounds.max.x / nodeSpacing) + 2;
        int minY = Mathf.FloorToInt(levelBounds.min.y / nodeHeight) - 2;
        int maxY = Mathf.CeilToInt(levelBounds.max.y / nodeHeight) + 5; // More vertical padding

        // Track created nodes
        int baseNodesCreated = 0;

        // Increase detection radius
        float detectionRadius = 0.25f;  // Increased from 0.1f

        // Scan the level grid with smaller steps for more precision
        float scanStep = 0.5f;  // Scan at half the normal spacing for better coverage

        for (float x = minX * nodeSpacing; x <= maxX * nodeSpacing; x += nodeSpacing * scanStep)
        {
            for (float y = minY * nodeHeight; y <= maxY * nodeHeight; y += nodeHeight * scanStep)
            {
                Vector2 worldPos = new Vector2(x, y);
                Vector2 belowPos = worldPos + Vector2.down * nodeHeight * 0.6f;

                // If current position is empty and position below has ground
                bool isEmpty = !Physics2D.OverlapCircle(worldPos, detectionRadius, groundLayer);
                bool hasGroundBelow = Physics2D.OverlapCircle(belowPos, detectionRadius, groundLayer);

                if (isEmpty && hasGroundBelow)
                {
                    // Convert to grid position
                    Vector2Int gridPos = WorldToGrid(worldPos);

                    // Only add if not already added
                    if (!nodes.ContainsKey(gridPos))
                    {
                        // Add a base node
                        nodes[gridPos] = NodeType.Base;
                        baseNodesCreated++;
                    }
                }
            }
        }

        Debug.Log($"Created {baseNodesCreated} base nodes");
    }

    // Create connector nodes at the edges of platforms for better visualization
    private void CreateEdgeNodes()
    {
        Dictionary<Vector2Int, NodeType> edgeNodes = new Dictionary<Vector2Int, NodeType>();
        int edgeNodesCreated = 0;

        // Look at each base node
        foreach (var kvp in nodes.ToList())
        {
            Vector2Int pos = kvp.Key;

            // Only check base nodes
            if (kvp.Value != NodeType.Base)
                continue;

            // Check right neighbor
            Vector2Int rightPos = new Vector2Int(pos.x + 1, pos.y);
            bool hasRightNeighbor = nodes.ContainsKey(rightPos) ||
                                    Physics2D.OverlapCircle(GridToWorld(rightPos), 0.1f, groundLayer);

            // Check left neighbor
            Vector2Int leftPos = new Vector2Int(pos.x - 1, pos.y);
            bool hasLeftNeighbor = nodes.ContainsKey(leftPos) ||
                                   Physics2D.OverlapCircle(GridToWorld(leftPos), 0.1f, groundLayer);

            // If this is an edge node (missing either left or right neighbor)
            if (!hasRightNeighbor || !hasLeftNeighbor)
            {
                // Mark it as an edge/connector node
                nodes[pos] = NodeType.Edge;
                edgeNodesCreated++;
            }
        }

        Debug.Log($"Created {edgeNodesCreated} edge nodes");
    }

    private Bounds CalculateLevelBounds()
    {
        if (levelRoot == null)
        {
            // If levelRoot is not set, try to find all objects in the ground layer
            Debug.LogWarning("LevelRoot not set, attempting to find all ground objects");
            Bounds levelBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool foundAny = false;

            // Find all colliders in the ground layer
            Collider2D[] groundColliders = Physics2D.OverlapAreaAll(
                new Vector2(-1000, -1000),
                new Vector2(1000, 1000),
                groundLayer);

            if (groundColliders.Length > 0)
            {
                levelBounds = new Bounds(groundColliders[0].bounds.center, groundColliders[0].bounds.size);
                foundAny = true;

                for (int i = 1; i < groundColliders.Length; i++)
                {
                    levelBounds.Encapsulate(groundColliders[i].bounds);
                }
            }

            if (!foundAny)
            {
                Debug.LogError("Could not find any ground colliders! Using default bounds.");
                return new Bounds(Vector3.zero, Vector3.one * 100);
            }

            // Expand bounds
            //bounds.Expand(10f);
            Debug.Log($"Level bounds: min({levelBounds.min}), max({levelBounds.max}), size({levelBounds.size})");
            return levelBounds;
        }

        // Original implementation for when levelRoot is set
        Bounds bounds = new Bounds(levelRoot.position, Vector3.zero);
        Collider2D[] colliders = levelRoot.GetComponentsInChildren<Collider2D>();

        if (colliders.Length == 0)
        {
            // Fallback to renderers if no colliders found
            Renderer[] renderers = levelRoot.GetComponentsInChildren<Renderer>();
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

        // Expand bounds a bit more
        bounds.Expand(10f);

        return bounds;
    }

    // Generate or retrieve a path to the goal
    public Dictionary<Vector2Int, Vector2> GetPathToGoal(Vector2 goalWorldPos)
    {
        Vector2Int goalGridPos = WorldToGrid(goalWorldPos);

        // Check if we already have a path to this goal
        if (paths.ContainsKey(goalGridPos))
        {
            return paths[goalGridPos];
        }

        // If goal is too far from last calculation point, generate a new path
        if (lastGoalPos != Vector2Int.zero &&
            Vector2Int.Distance(goalGridPos, lastGoalPos) < updateDistance)
        {
            // Use existing path if goal hasn't moved much
            if (paths.ContainsKey(lastGoalPos))
            {
                return paths[lastGoalPos];
            }
        }

        // Generate new path
        Dictionary<Vector2Int, Vector2> newPath = GeneratePathToGoal(goalGridPos);
        paths[goalGridPos] = newPath;
        lastGoalPos = goalGridPos;

        return newPath;
    }

    // Generate a new path to the goal position
    private Dictionary<Vector2Int, Vector2> GeneratePathToGoal(Vector2Int goalGridPos)
    {
        // Find the closest node to the goal
        Vector2Int closestGoalNode = FindClosestNode(goalGridPos);
        if (closestGoalNode == Vector2Int.zero)
            return new Dictionary<Vector2Int, Vector2>();

        // Create the path direction map (node position -> direction vector)
        Dictionary<Vector2Int, Vector2> pathDirections = new Dictionary<Vector2Int, Vector2>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Start with the goal node
        queue.Enqueue(closestGoalNode);
        visited.Add(closestGoalNode);
        pathDirections[closestGoalNode] = Vector2.zero; // No direction at goal

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Get connected nodes
            List<Vector2Int> neighbors = GetConnectedNodes(current);

            foreach (Vector2Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    // Calculate direction from neighbor to current
                    Vector2 directionVec = new Vector2(current.x - neighbor.x, current.y - neighbor.y);
                    Vector2 direction = directionVec.normalized;
                    pathDirections[neighbor] = direction;

                    // Mark as visited and add to queue
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        if (visualizePaths)
        {
            StartCoroutine(VisualizePathCoroutine(pathDirections, goalGridPos));
        }

        return pathDirections;
    }

    // Find the closest node to a world position
    private Vector2Int FindClosestNode(Vector2Int gridPos)
    {
        float closestDistance = float.MaxValue;
        Vector2Int closestNode = Vector2Int.zero;
        Vector2 worldPos = GridToWorld(gridPos);

        // First try: Find exact match
        if (nodes.ContainsKey(gridPos))
        {
            return gridPos;
        }

        // Second try: Find node at same height
        foreach (var nodePos in nodes.Keys)
        {
            if (nodePos.y == gridPos.y)
            {
                float distance = Mathf.Abs(nodePos.x - gridPos.x);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestNode = nodePos;
                }
            }
        }

        // If we found a node at same height within reasonable distance
        if (closestNode != Vector2Int.zero && closestDistance < 5)
        {
            return closestNode;
        }

        // Third try: Find any closest node
        closestDistance = float.MaxValue;
        foreach (var nodePos in nodes.Keys)
        {
            float distance = Vector2Int.Distance(gridPos, nodePos);

            // Prioritize nodes that are on the same horizontal level or above
            if (nodePos.y <= gridPos.y)
            {
                distance *= 0.8f; // Give preference to nodes below or at same level
            }

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNode = nodePos;
            }
        }

        return closestNode;
    }

    // Get all nodes connected to the given node
    private List<Vector2Int> GetConnectedNodes(Vector2Int nodePos)
    {
        List<Vector2Int> connected = new List<Vector2Int>();

        // For base nodes, we only consider horizontal movement for now
        Vector2Int[] directions = new Vector2Int[] {
            Vector2Int.left,
            Vector2Int.right
        };

        // Check all applicable directions
        foreach (var dir in directions)
        {
            Vector2Int checkPos = nodePos + dir;

            // Check if there's a node at this position
            if (nodes.ContainsKey(checkPos))
            {
                connected.Add(checkPos);
            }
        }

        return connected;
    }

    // Get movement action for an entity to move toward a goal
    public MovementAction GetMovementAction(Vector2 entityWorldPos, Vector2 goalWorldPos)
    {
        Vector2Int entityGridPos = WorldToGrid(entityWorldPos);
        Dictionary<Vector2Int, Vector2> path = GetPathToGoal(goalWorldPos);

        if (path.Count == 0)
        {
            return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
        }

        // Find closest node to entity
        Vector2Int closestNode = FindClosestNode(entityGridPos);
        if (closestNode == Vector2Int.zero)
        {
            return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
        }

        // Get direction from the path
        if (!path.TryGetValue(closestNode, out Vector2 direction))
        {
            return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
        }

        // Debug log
        Vector3 entityWorldPosition = new Vector3(entityWorldPos.x, entityWorldPos.y, 0);
        Debug.DrawLine(entityWorldPos, entityWorldPosition + (Vector3)(direction * 2f), Color.magenta, 0.1f);

        // Check for height difference - this allows for future jump implementation
        Vector2Int goalGridPos = WorldToGrid(goalWorldPos);
        float heightDifference = goalGridPos.y - entityGridPos.y;

        // Translate direction to action
        if (direction.x > 0.1f)
        {
            // Moving right
            // In the future, could check if we need to jump right
            if (heightDifference > 1.0f)
            {
                // For now, still move right, but could be Jump in the future
                return MovementAction.MoveRight;
            }
            return MovementAction.MoveRight;
        }
        else if (direction.x < -0.1f)
        {
            // Moving left
            // In the future, could check if we need to jump left
            if (heightDifference > 1.0f)
            {
                // For now, still move left, but could be Jump in the future
                return MovementAction.MoveLeft;
            }
            return MovementAction.MoveLeft;
        }
        else
        {
            // No clear direction, try a fallback
            return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
        }
    }

    // Fallback movement when pathfinding fails
    private MovementAction GetFallbackMovementAction(Vector2 entityPos, Vector2 goalPos)
    {
        Vector2 direction = goalPos - entityPos;

        // If goal is to the right, move right
        if (direction.x > 0.3f)
        {
            return MovementAction.MoveRight;
        }
        // If goal is to the left, move left
        else if (direction.x < -0.3f)
        {
            return MovementAction.MoveLeft;
        }
        // Default to idle
        else
        {
            return MovementAction.Idle;
        }
    }

    // Visualize the graph for debugging
    private IEnumerator VisualizeGraphCoroutine()
    {
        Debug.Log("Starting graph visualization...");
        int baseCount = 0;
        int edgeCount = 0;

        while (visualizeGraph)
        {
            // Track counts for debugging
            baseCount = 0;
            edgeCount = 0;

            // Draw nodes
            foreach (var kvp in nodes)
            {
                Vector2Int gridPos = kvp.Key;
                NodeType nodeType = kvp.Value;
                Vector3 worldPos = GridToWorld(gridPos);

                // Choose color by node type
                Color nodeColor;
                switch (nodeType)
                {
                    case NodeType.Base:
                        nodeColor = baseNodeColor;
                        baseCount++;
                        break;
                    case NodeType.Edge:
                        nodeColor = connectorNodeColor;
                        edgeCount++;
                        break;
                    default:
                        nodeColor = Color.white;
                        break;
                }

                // Draw a larger cross for easier visibility
                float size = 0.3f;
                Debug.DrawLine(
                    worldPos + Vector3.left * size,
                    worldPos + Vector3.right * size,
                    nodeColor,
                    2.0f  // Longer duration to make it persist
                );

                Debug.DrawLine(
                    worldPos + Vector3.up * size,
                    worldPos + Vector3.down * size,
                    nodeColor,
                    2.0f
                );

                // Draw a circle for edge nodes
                if (nodeType == NodeType.Edge)
                {
                    // Draw a "circle" using multiple short lines
                    int segments = 8;
                    for (int i = 0; i < segments; i++)
                    {
                        float angle1 = i * 2 * Mathf.PI / segments;
                        float angle2 = (i + 1) * 2 * Mathf.PI / segments;

                        Vector3 point1 = worldPos + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0) * size;
                        Vector3 point2 = worldPos + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0) * size;

                        Debug.DrawLine(point1, point2, nodeColor, 2.0f);
                    }
                }
            }

            // Log node counts
            Debug.Log($"Visualizing graph: {baseCount} base nodes, {edgeCount} edge nodes");

            yield return new WaitForSeconds(1.5f);  // Refresh visualization every 1.5 seconds
        }
    }

    // Visualize the path for debugging
    private IEnumerator VisualizePathCoroutine(Dictionary<Vector2Int, Vector2> path, Vector2Int goalPos)
    {
        while (visualizePaths && paths.ContainsKey(goalPos) && paths[goalPos] == path)
        {
            // Draw the goal
            Vector3 goalWorldPos = GridToWorld(goalPos);
            Debug.DrawLine(
                goalWorldPos + Vector3.left * 0.3f,
                goalWorldPos + Vector3.right * 0.3f,
                Color.yellow,
                0.2f
            );

            Debug.DrawLine(
                goalWorldPos + Vector3.up * 0.3f,
                goalWorldPos + Vector3.down * 0.3f,
                Color.yellow,
                0.2f
            );

            // Draw the paths
            foreach (var kvp in path)
            {
                Vector2Int nodePos = kvp.Key;
                Vector2 direction = kvp.Value;

                if (direction != Vector2.zero) // Skip the goal node
                {
                    Vector3 nodeWorldPos = GridToWorld(nodePos);
                    Vector3 directionWorldPos = nodeWorldPos + (Vector3)(direction * 0.4f);

                    Debug.DrawLine(
                        nodeWorldPos,
                        directionWorldPos,
                        pathColor,
                        0.2f
                    );
                }
            }

            yield return new WaitForSeconds(0.2f);
        }
    }
}

// Supporting enum
public enum NodeType
{
    None,       // Not a valid node
    Base,       // Standard node on ground
    Edge        // Node at platform edge (for visualization, treated as Base for pathfinding)
    // We'll add Connector and Fall nodes when we add jumping later
}

public enum MovementAction
{
    Idle,
    MoveLeft,
    MoveRight,
    Jump,    // Kept for future implementation
    Wait     // Kept for future implementation
}