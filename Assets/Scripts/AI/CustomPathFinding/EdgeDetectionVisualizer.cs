using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// This script helps visualize edge detection in the Unity Editor
[RequireComponent(typeof(EnemyBaseController))]
public class EdgeDetectionVisualizer : MonoBehaviour
{
    public bool showEdgeDetection = true;
    public Color edgeDetectionColor = Color.red;
    public Color groundDetectionColor = Color.green;
    public float detectDistance = 0.5f;
    public float rayLength = 1.5f;

    private EnemyBaseController controller;

    private void Awake()
    {
        controller = GetComponent<EnemyBaseController>();
    }

    private void OnDrawGizmos()
    {
        if (!showEdgeDetection || controller == null)
            return;

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
            return;

        // Check both left and right
        DrawEdgeDetection(collider, 1); // Right
        DrawEdgeDetection(collider, -1); // Left
    }

    private void DrawEdgeDetection(Collider2D collider, float direction)
    {
        // Calculate edge check position
        Vector2 bottomPosition = new Vector2(
            collider.bounds.center.x + (collider.bounds.extents.x * direction) + (detectDistance * direction),
            collider.bounds.min.y + 0.1f
        );

        // Cast a ray downward to check for ground
        RaycastHit2D hit = Physics2D.Raycast(bottomPosition, Vector2.down, rayLength, controller.groundLayer);

        // Draw the ray
        Gizmos.color = hit.collider != null ? groundDetectionColor : edgeDetectionColor;
        Gizmos.DrawLine(bottomPosition, bottomPosition + Vector2.down * rayLength);

        // Draw a sphere at the end of the ray
        Gizmos.DrawWireSphere(bottomPosition + Vector2.down * rayLength, 0.1f);
    }
}

// Custom editor to make it look nicer in the inspector
[CustomEditor(typeof(EdgeDetectionVisualizer))]
public class EdgeDetectionVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EdgeDetectionVisualizer visualizer = (EdgeDetectionVisualizer)target;

        EditorGUILayout.LabelField("Edge Detection Visualization", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        visualizer.showEdgeDetection = EditorGUILayout.Toggle("Show Edge Detection", visualizer.showEdgeDetection);
        visualizer.edgeDetectionColor = EditorGUILayout.ColorField("Edge Detection Color", visualizer.edgeDetectionColor);
        visualizer.groundDetectionColor = EditorGUILayout.ColorField("Ground Detection Color", visualizer.groundDetectionColor);
        visualizer.detectDistance = EditorGUILayout.FloatField("Detection Distance", visualizer.detectDistance);
        visualizer.rayLength = EditorGUILayout.FloatField("Ray Length", visualizer.rayLength);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(visualizer);
        }
    }
}
#endif