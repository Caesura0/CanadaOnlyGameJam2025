using System.Collections;
using UnityEngine;

public static class EdgeDetectionExtensions
{
    /// <summary>
    /// Checks if there's an edge in front of the entity based on its movement direction
    /// </summary>
    /// <param name="controller">The enemy controller</param>
    /// <param name="movementDirection">Current movement direction (1 for right, -1 for left)</param>
    /// <param name="groundLayer">Layer mask for ground detection</param>
    /// <returns>True if an edge is detected, false otherwise</returns>
    public static bool IsEdgeAhead(this EnemyBaseController controller, float movementDirection, LayerMask groundLayer)
    {
        if (Mathf.Abs(movementDirection) < 0.1f)
            return false;

        // Get the collider for proper positioning
        Collider2D collider = controller.GetComponent<Collider2D>();
        if (collider == null)
            return false;

        // Calculate edge check position (slightly in front of the collider in movement direction)
        float edgeCheckDistance = 0.5f; // Distance ahead to check
        float rayLength = 1.5f; // How far down to check for ground

        // Calculate the position to check for an edge
        // We want this to be just beyond the front edge of the collider
        Vector2 bottomPosition = new Vector2(
            collider.bounds.center.x + (collider.bounds.extents.x * Mathf.Sign(movementDirection)) + (edgeCheckDistance * Mathf.Sign(movementDirection)),
            collider.bounds.min.y + 0.1f // Slightly above the bottom to avoid false positives
        );

        // Cast a ray downward to check for ground
        RaycastHit2D hit = Physics2D.Raycast(bottomPosition, Vector2.down, rayLength, groundLayer);

        // Visualize the raycast
        Debug.DrawRay(bottomPosition, Vector2.down * rayLength, hit.collider != null ? Color.green : Color.red, 0.1f);

        // Additional debug visualization - larger cross at the edge check position
        float debugSize = 0.2f;
        Debug.DrawLine(
            new Vector3(bottomPosition.x - debugSize, bottomPosition.y, 0),
            new Vector3(bottomPosition.x + debugSize, bottomPosition.y, 0),
            hit.collider != null ? Color.green : Color.red,
            0.1f
        );
        Debug.DrawLine(
            new Vector3(bottomPosition.x, bottomPosition.y - debugSize, 0),
            new Vector3(bottomPosition.x, bottomPosition.y + debugSize, 0),
            hit.collider != null ? Color.green : Color.red,
            0.1f
        );

        // Output debug info
        if (hit.collider == null)
        {
            Debug.Log($"Edge detected for {controller.gameObject.name} at {bottomPosition}, moving direction: {movementDirection}");
        }

        // If no ground detected, we're at an edge
        return hit.collider == null;
    }
}