using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStarManager : MonoBehaviour
{
    public static AStarManager instance;

    // For caching nodes to avoid FindObjectsOfType calls
    private Node[] cachedNodes;
    private float lastNodeCacheTime;
    private const float NODE_CACHE_TIMEOUT = 5f; // Seconds

    private void Awake()
    {
        instance = this;
    }

    public List<Node> GeneratePath(Node start, Node end)
    {
        if (start == null || end == null)
            return null;

        // Use a priority queue (min heap) for better performance
        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        // Reset all nodes (more efficient than FindObjectsOfType)
        Node[] allNodes = GetAllNodes();
        foreach (Node n in allNodes)
        {
            n.gScore = float.MaxValue;
            n.cameFrom = null;
        }

        start.gScore = 0;
        start.hScore = Vector2.Distance(start.transform.position, end.transform.position);
        openSet.Add(start);

        while (openSet.Count > 0)
        {
            // Find node with lowest fScore in openSet
            int lowestF = 0;
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FScore() < openSet[lowestF].FScore())
                {
                    lowestF = i;
                }
            }

            Node currentNode = openSet[lowestF];

            // Check if we've reached the end
            if (currentNode == end)
            {
                return ReconstructPath(start, end);
            }

            // Move current node from open to closed set
            openSet.RemoveAt(lowestF);
            closedSet.Add(currentNode);

            // Check all connections
            foreach (Node connectedNode in currentNode.connections)
            {
                // Skip if connection is null or already in closed set
                if (connectedNode == null || closedSet.Contains(connectedNode))
                    continue;

                float tentativeGScore = currentNode.gScore +
                    Vector2.Distance(currentNode.transform.position, connectedNode.transform.position);

                bool newPathIsBetter = tentativeGScore < connectedNode.gScore;

                if (newPathIsBetter)
                {
                    // This path is better, record it
                    connectedNode.cameFrom = currentNode;
                    connectedNode.gScore = tentativeGScore;
                    connectedNode.hScore = Vector2.Distance(connectedNode.transform.position, end.transform.position);

                    if (!openSet.Contains(connectedNode))
                    {
                        openSet.Add(connectedNode);
                    }
                }
            }
        }

        // No path found
        return null;
    }

    private List<Node> ReconstructPath(Node start, Node end)
    {
        List<Node> path = new List<Node>();
        Node currentNode = end;

        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = currentNode.cameFrom;

            // Safety check in case of broken path
            if (currentNode == null)
            {
                Debug.LogWarning("Path reconstruction failed - broken path");
                return null;
            }
        }

        // Add the start node
        path.Add(start);

        // Reverse to get start-to-end order
        path.Reverse();
        return path;
    }

    public Node FindNearestNode(Vector2 pos)
    {
        Node foundNode = null;
        float minDistance = float.MaxValue;

        foreach (Node node in GetAllNodes())
        {
            float currentDistance = Vector2.Distance((Vector2)node.transform.position, pos);
            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                foundNode = node;
            }
        }

        return foundNode;
    }

    public Node FindFurthestNode(Vector2 pos)
    {
        Node foundNode = null;
        float maxDistance = 0f;

        foreach (Node node in GetAllNodes())
        {
            float currentDistance = Vector2.Distance((Vector2)node.transform.position, pos);
            if (currentDistance > maxDistance)
            {
                maxDistance = currentDistance;
                foundNode = node;
            }
        }

        return foundNode;
    }

    // Cache nodes to avoid frequent FindObjectsOfType calls
    private Node[] GetAllNodes()
    {
        if (cachedNodes == null || Time.time - lastNodeCacheTime > NODE_CACHE_TIMEOUT)
        {
            cachedNodes = FindObjectsOfType<Node>();
            lastNodeCacheTime = Time.time;
        }

        return cachedNodes;
    }

    // Force refresh the node cache
    public void RefreshNodeCache()
    {
        cachedNodes = FindObjectsOfType<Node>();
        lastNodeCacheTime = Time.time;
    }

    // Public method to access all nodes
    public Node[] AllNodes()
    {
        return GetAllNodes();
    }
}