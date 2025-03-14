using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NavigationGraph : MonoBehaviour
{
    [Header("Map Settings")]
    public Transform levelRoot; // Kept for backward compatibility
    public LayerMask groundLayer; // Layer for ground/solid platforms
    public float nodeSpacing = 2f; // Distance between nodes (samples)
    public float nodeHeight = 1f; // Vertical distance between nodes
    public float updateDistance = 2f; // Distance goal must move to update paths

    [Header("Continuous Navigation")]
    public bool useContinuousNavigation = true;
    public float coverageRadius = 30f; // Generate nodes within this radius of player

    [Header("Advanced Ground Detection")]
    [Tooltip("How far to cast rays for ground detection")]
    public float groundCheckDistance = 2.0f;
    [Tooltip("Multiple scan heights improve detection of varied geometry")]
    public float[] groundCheckHeights = new float[] { 0.25f, 0.5f, 0.75f, 1.0f, 1.5f, 2.0f };
    [Tooltip("Radius of overlap checks for ground detection")]
    public float groundCheckRadius = 0.3f;
    [Tooltip("Debug visualization for ground detection")]
    public bool visualizeGroundChecks = false;

    [Header("Debug Settings")]
    public bool visualizeGraph = true;
    public bool visualizePaths = true;
    public Color baseNodeColor = Color.green;
    public Color connectorNodeColor = new Color(1f, 0.5f, 0f); // Orange
    public Color pathColor = Color.cyan;

    // The graph representation - just base nodes for now, will add connector and fall nodes when we add jumping
    // Made public to work with ContinuousNavigation
    public Dictionary<Vector2Int, NodeType> nodes = new Dictionary<Vector2Int, NodeType>();

    // Cached paths (goalPosition -> path)
    private Dictionary<Vector2Int, Dictionary<Vector2Int, Vector2>> paths = new Dictionary<Vector2Int, Dictionary<Vector2Int, Vector2>>();

    // Last goal position used for path calculation
    private Vector2Int lastGoalPos = Vector2Int.zero;

    // Reference to player transform (for continuous navigation)
    private Transform playerTransform;

    private void Start()
    {
        // Find player for continuous navigation
        if (useContinuousNavigation)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogWarning("No player found for continuous navigation! Falling back to standard mode.");
                useContinuousNavigation = false;
            }
        }

        if (useContinuousNavigation && playerTransform != null)
        {
            // Start with nodes around the player
            Bounds playerBounds = new Bounds(playerTransform.position, new Vector3(coverageRadius * 2, coverageRadius * 2, 1f));
            BuildGraphForBounds(playerBounds);
        }
        else
        {
            // Traditional build
            BuildGraph();
        }

        if (visualizeGraph)
        {
            StartCoroutine(VisualizeGraphCoroutine());
        }
    }

    private void Update()
    {
        if (useContinuousNavigation && playerTransform != null)
        {
            // Check if we need to generate more nodes around player
            UpdateContinuousNavigation();
        }
    }

    // Continuously update navigation around player
    private void UpdateContinuousNavigation()
    {
        Vector2 playerPos = playerTransform.position;
        Vector2Int playerGridPos = WorldToGrid(playerPos);

        // Check if there are enough nodes around the player
        List<Vector2Int> nearbyNodes = new List<Vector2Int>();

        foreach (var nodePos in nodes.Keys)
        {
            if (Vector2.Distance(GridToWorld(nodePos), playerPos) <= coverageRadius)
            {
                nearbyNodes.Add(nodePos);
            }
        }

        // If we don't have enough nodes nearby, generate more
        if (nearbyNodes.Count < 20)
        {
            GenerateNodesAroundPosition(playerPos, coverageRadius);
        }

        // Clean up distant nodes periodically (every 5 seconds)
        if (Time.frameCount % 300 == 0)
        {
            CleanupDistantNodes(playerPos, coverageRadius * 2);
        }
    }

    private void GenerateNodesAroundPosition(Vector2 position, float radius)
    {
        Bounds bounds = new Bounds(position, new Vector3(radius * 2, radius * 2, 1f));

        // Define grid bounds with generous padding
        int minX = Mathf.FloorToInt((position.x - radius) / nodeSpacing) - 3;
        int maxX = Mathf.CeilToInt((position.x + radius) / nodeSpacing) + 3;
        int minY = Mathf.FloorToInt((position.y - radius) / nodeHeight) - 3;
        int maxY = Mathf.CeilToInt((position.y + radius) / nodeHeight) + 8;

        int nodesCreated = 0;

        // Perform a dense scan of the area
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);

                // Skip if we already have this node
                if (nodes.ContainsKey(gridPos))
                    continue;

                Vector2 worldPos = GridToWorld(gridPos);

                // Skip if too far from center (using squared distance for performance)
                float sqrDist = (worldPos - position).sqrMagnitude;
                if (sqrDist > radius * radius * 1.2f) // 20% extra range
                    continue;

                // Comprehensive check for valid node position
                if (IsValidNodePosition(worldPos))
                {
                    // Add a base node
                    nodes[gridPos] = NodeType.Base;
                    nodesCreated++;
                }
            }
        }

        // Create edge nodes for the new nodes
        if (nodesCreated > 0)
        {
            CreateEdgeNodes();
            Debug.Log($"Generated {nodesCreated} nodes around player");
        }
    }

    // Comprehensive validity check for node positions
    private bool IsValidNodePosition(Vector2 worldPos)
    {
        // 1. Check if the position itself is empty (not inside a collider)
        bool positionClear = !Physics2D.OverlapCircle(worldPos, groundCheckRadius * 0.7f, groundLayer);
        if (!positionClear)
            return false;

        // 2. Multi-height scan for ground below
        bool foundGroundBelow = false;

        // Scan at multiple heights
        foreach (float heightOffset in groundCheckHeights)
        {
            Vector2 scanPos = new Vector2(worldPos.x, worldPos.y - heightOffset);

            // Check with multiple methods
            bool hitGround = Physics2D.OverlapCircle(scanPos, groundCheckRadius, groundLayer);

            // If using debug visualization, draw the checked positions
            if (visualizeGroundChecks && Application.isPlaying)
            {
                Color debugColor = hitGround ? Color.green : Color.red;
                Debug.DrawLine(
                    scanPos + Vector2.left * groundCheckRadius,
                    scanPos + Vector2.right * groundCheckRadius,
                    debugColor,
                    0.1f
                );
                Debug.DrawLine(
                    scanPos + Vector2.up * groundCheckRadius,
                    scanPos + Vector2.down * groundCheckRadius,
                    debugColor,
                    0.1f
                );
            }

            if (hitGround)
            {
                foundGroundBelow = true;
                break;
            }
        }

        // 3. Additional scan with BoxCast for better platform detection
        if (!foundGroundBelow)
        {
            RaycastHit2D hit = Physics2D.BoxCast(
                worldPos,
                new Vector2(groundCheckRadius * 2, 0.1f),
                0f,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            foundGroundBelow = hit.collider != null;

            if (visualizeGroundChecks && Application.isPlaying)
            {
                Color debugColor = foundGroundBelow ? Color.green : Color.red;
                Debug.DrawLine(
                    worldPos,
                    worldPos + Vector2.down * groundCheckDistance,
                    debugColor,
                    0.1f
                );
            }
        }

        // 4. Fan of rays for uneven or thin platforms
        if (!foundGroundBelow)
        {
            float[] angles = new float[] { -30f, -15f, 0f, 15f, 30f };
            foreach (float angle in angles)
            {
                Vector2 direction = Quaternion.Euler(0, 0, angle) * Vector2.down;
                RaycastHit2D hit = Physics2D.Raycast(worldPos, direction, groundCheckDistance, groundLayer);

                if (visualizeGroundChecks && Application.isPlaying)
                {
                    Color debugColor = hit.collider != null ? Color.green : Color.red;
                    Debug.DrawRay(worldPos, direction * groundCheckDistance, debugColor, 0.1f);
                }

                if (hit.collider != null)
                {
                    foundGroundBelow = true;
                    break;
                }
            }
        }

        return positionClear && foundGroundBelow;
    }

    private void CleanupDistantNodes(Vector2 position, float maxDistance)
    {
        List<Vector2Int> nodesToRemove = new List<Vector2Int>();

        foreach (var nodePos in nodes.Keys.ToList())
        {
            Vector2 worldPos = GridToWorld(nodePos);
            if (Vector2.Distance(worldPos, position) > maxDistance)
            {
                nodesToRemove.Add(nodePos);
            }
        }

        foreach (var nodePos in nodesToRemove)
        {
            nodes.Remove(nodePos);
        }

        // Also clean up paths that reference removed nodes
        CleanupPaths();

        if (nodesToRemove.Count > 0)
        {
            Debug.Log($"Removed {nodesToRemove.Count} distant nodes");
        }
    }

    private void CleanupPaths()
    {
        List<Vector2Int> pathsToRemove = new List<Vector2Int>();

        foreach (var kvp in paths)
        {
            Vector2Int goalPos = kvp.Key;

            // Check if goal position node still exists
            if (!nodes.ContainsKey(FindClosestNode(goalPos)))
            {
                pathsToRemove.Add(goalPos);
            }
        }

        foreach (var goalPos in pathsToRemove)
        {
            paths.Remove(goalPos);
        }
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

        if (useContinuousNavigation && playerTransform != null)
        {
            // For continuous navigation, just build around the player
            Bounds playerBounds = new Bounds(playerTransform.position, new Vector3(coverageRadius * 2, coverageRadius * 2, 1f));
            BuildGraphForBounds(playerBounds);
        }
        else
        {
            // Traditional build
            BuildGraph();
        }

        if (visualizeGraph)
        {
            StartCoroutine(VisualizeGraphCoroutine());
        }
    }

    // Build graph for specific bounds
    public void BuildGraphForBounds(Bounds bounds)
    {
        // Define grid range based on bounds with extra padding
        int minX = Mathf.FloorToInt(bounds.min.x / nodeSpacing) - 3;
        int maxX = Mathf.CeilToInt(bounds.max.x / nodeSpacing) + 3;
        int minY = Mathf.FloorToInt(bounds.min.y / nodeHeight) - 3;
        int maxY = Mathf.CeilToInt(bounds.max.y / nodeHeight) + 8;

        int nodesCreated = 0;

        // Scan the grid within bounds
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);

                // Skip if already has a node
                if (nodes.ContainsKey(gridPos))
                    continue;

                Vector2 worldPos = GridToWorld(gridPos);

                // Skip if outside bounds with padding
                if (worldPos.x < bounds.min.x - 3f || worldPos.x > bounds.max.x + 3f ||
                    worldPos.y < bounds.min.y - 3f || worldPos.y > bounds.max.y + 8f)
                {
                    continue;
                }

                // Use the comprehensive check
                if (IsValidNodePosition(worldPos))
                {
                    // Add a base node
                    nodes[gridPos] = NodeType.Base;
                    nodesCreated++;
                }
            }
        }

        // Create edge nodes
        CreateEdgeNodes();

        Debug.Log($"Created {nodesCreated} nodes for bounds {bounds.min} to {bounds.max}");
    }

    // Get node type at a specific grid position
    public NodeType GetNodeTypeAt(Vector2Int gridPos)
    {
        if (nodes.TryGetValue(gridPos, out NodeType nodeType))
        {
            return nodeType;
        }

        // If not found in dictionary, check if it's a valid position
        Vector2 worldPos = GridToWorld(gridPos);

        if (IsValidNodePosition(worldPos))
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
    }

    // Create initial nodes for empty spaces with ground underneath
    private void CreateBaseNodes()
    {
        // Define boundaries of the level
        Bounds levelBounds = CalculateLevelBounds();
        BuildGraphForBounds(levelBounds);
    }

    // Create connector nodes at the edges of platforms for better visualization
    private void CreateEdgeNodes()
    {
        int edgeNodesCreated = 0;

        // Look at each base node
        foreach (var kvp in nodes.ToList())
        {
            Vector2Int pos = kvp.Key;

            // Only check base nodes
            if (kvp.Value != NodeType.Base)
                continue;

            // Enhanced edge detection logic
            bool isEdge = IsEdgeNode(pos);

            if (isEdge)
            {
                // Mark it as an edge/connector node
                nodes[pos] = NodeType.Edge;
                edgeNodesCreated++;
            }
        }

        if (edgeNodesCreated > 0)
        {
            Debug.Log($"Created {edgeNodesCreated} edge nodes");
        }
    }

    // Comprehensive edge detection
    private bool IsEdgeNode(Vector2Int pos)
    {
        // Directions to check (8-way)
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.left,
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(-1, -1), // bottom-left
            new Vector2Int(1, -1),  // bottom-right
            new Vector2Int(-1, 1),  // top-left
            new Vector2Int(1, 1)    // top-right
        };

        int emptyNeighbors = 0;
        int totalChecks = 0;

        // Check each direction
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = pos + dir;

            // Skip vertical/diagonal checks for standard edge detection
            if (dir != Vector2Int.left && dir != Vector2Int.right)
                continue;

            totalChecks++;

            // Check if this position has a node
            bool hasNode = nodes.ContainsKey(neighborPos);

            // If no node exists, check if it could be a valid node position
            if (!hasNode)
            {
                Vector2 neighborWorldPos = GridToWorld(neighborPos);
                bool isValidPosition = Physics2D.OverlapCircle(neighborWorldPos, groundCheckRadius, groundLayer);

                // Count if it's an empty space (not valid for a node)
                if (!isValidPosition)
                {
                    emptyNeighbors++;
                }
            }
        }

        // If any horizontal neighbors are empty, it's an edge
        return emptyNeighbors > 0;
    }

    // Calculate the bounds of the level for graph generation
    private Bounds CalculateLevelBounds()
    {
        if (useContinuousNavigation && playerTransform != null)
        {
            // Use player-centered bounds for continuous navigation
            return new Bounds(playerTransform.position, new Vector3(coverageRadius * 2, coverageRadius * 2, 1f));
        }

        if (levelRoot == null)
        {
            // Try to find ground objects if no level root
            Debug.LogWarning("No level root set, looking for ground objects");
            Collider2D[] groundColliders = Physics2D.OverlapAreaAll(
                new Vector2(-1000, -1000),
                new Vector2(1000, 1000),
                groundLayer
            );

            if (groundColliders.Length > 0)
            {
                Bounds bounds = new Bounds(groundColliders[0].bounds.center, groundColliders[0].bounds.size);

                for (int i = 1; i < groundColliders.Length; i++)
                {
                    bounds.Encapsulate(groundColliders[i].bounds);
                }

                bounds.Expand(10f);
                return bounds;
            }

            return new Bounds(Vector3.zero, Vector3.one * 100); // Default large bounds
        }

        Bounds levelBounds = new Bounds(levelRoot.position, Vector3.zero);
        Collider2D[] colliders = levelRoot.GetComponentsInChildren<Collider2D>();

        if (colliders.Length == 0)
        {
            // Fallback to renderers if no colliders found
            Renderer[] renderers = levelRoot.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                levelBounds.Encapsulate(renderer.bounds);
            }
        }
        else
        {
            foreach (Collider2D collider in colliders)
            {
                levelBounds.Encapsulate(collider.bounds);
            }
        }

        // Expand bounds a bit
        levelBounds.Expand(10f);

        return levelBounds;
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
            // If continuous navigation is active, try to generate nodes at the goal position
            if (useContinuousNavigation && Vector2.Distance(entityWorldPos, goalWorldPos) < coverageRadius * 1.5f)
            {
                GenerateNodesAroundPosition(goalWorldPos, 5f);
                path = GetPathToGoal(goalWorldPos);

                if (path.Count == 0)
                {
                    return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
                }
            }
            else
            {
                return GetFallbackMovementAction(entityWorldPos, goalWorldPos);
            }
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

    // Helper method to manually add nodes for debugging
    public void AddManualNode(Vector2 worldPosition)
    {
        Vector2Int gridPos = WorldToGrid(worldPosition);
        if (!nodes.ContainsKey(gridPos))
        {
            nodes[gridPos] = NodeType.Base;
            Debug.Log($"Manually added node at {worldPosition} (grid: {gridPos})");

            // Update edge nodes
            CreateEdgeNodes();
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

    private void OnDrawGizmos()
    {
        if (playerTransform != null && useContinuousNavigation)
        {
            // Draw the coverage radius
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.1f);
            Gizmos.DrawWireSphere(playerTransform.position, coverageRadius);
        }

        if (!Application.isPlaying || !visualizeGroundChecks)
            return;

        // Visualize ground check heights around player
        if (playerTransform != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 playerPos = playerTransform.position;

            foreach (float height in groundCheckHeights)
            {
                Gizmos.DrawWireSphere(new Vector3(playerPos.x, playerPos.y - height, 0), groundCheckRadius);
            }
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