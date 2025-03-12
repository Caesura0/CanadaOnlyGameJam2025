using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class BearDebugOverlay : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    public Color textColor = Color.white;
    public Vector2 offset = new Vector2(0, 2f);

    private BearEnemyController bearController;
    private Dictionary<string, string> debugValues = new Dictionary<string, string>();
    private Camera mainCamera;

    private void Awake()
    {
        bearController = GetComponent<BearEnemyController>();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!showDebugInfo || bearController == null) return;

        // Get component values using reflection to access protected/private fields
        System.Type type = bearController.GetType();

        // State info
        UpdateValue("Main State", bearController.currentState.ToString());

        // Get bear substate
        var bearStateField = type.GetField("bearState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bearStateField != null)
        {
            UpdateValue("Bear State", bearStateField.GetValue(bearController).ToString());
        }

        // Get current action
        UpdateValue("Action", GetFieldValue<object>(bearController, "currentAction").ToString());

        // Get is grounded
        UpdateValue("Grounded", GetFieldValue<bool>(bearController, "isGrounded").ToString());

        // Get velocity
        Rigidbody2D rb = bearController.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            UpdateValue("Velocity", rb.velocity.ToString("F2"));
        }

        // Get target info
        var targetField = type.GetField("target", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (targetField != null)
        {
            Transform target = targetField.GetValue(bearController) as Transform;
            if (target != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);
                UpdateValue("Target Distance", distance.ToString("F2"));
            }
            else
            {
                UpdateValue("Target", "null");
            }
        }

        // Get timers
        UpdateValue("Swipe Timer", GetFieldValue<float>(bearController, "swipeTimer").ToString("F2"));
        UpdateValue("Charge Timer", GetFieldValue<float>(bearController, "chargeTimer").ToString("F2"));
        UpdateValue("State Change Timer", GetFieldValue<float>(bearController, "stateChangeTimer").ToString("F2"));

        // Animator info
        Animator animator = bearController.GetComponent<Animator>();
        if (animator != null)
        {
            UpdateValue("Animator State", animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Swipe") ? "Swipe" :
                                        animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Charge") ? "Charge" :
                                        animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Warning") ? "Warning" :
                                        animator.GetCurrentAnimatorStateInfo(0).fullPathHash.ToString());

            UpdateValue("IsCharging", animator.GetBool("IsCharging").ToString());
            UpdateValue("IsWarning", animator.GetBool("IsWarning").ToString());
        }
        else
        {
            UpdateValue("Animator", "null");
        }
    }

    private T GetFieldValue<T>(object obj, string fieldName)
    {
        System.Type type = obj.GetType();
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public |
                                            System.Reflection.BindingFlags.NonPublic |
                                            System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (T)field.GetValue(obj);
        }
        return default(T);
    }

    private void UpdateValue(string key, string value)
    {
        if (debugValues.ContainsKey(key))
        {
            debugValues[key] = value;
        }
        else
        {
            debugValues.Add(key, value);
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || bearController == null || mainCamera == null) return;

        // Calculate screen position
        Vector3 worldPos = transform.position + (Vector3)offset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        screenPos.y = Screen.height - screenPos.y; // Flip y for GUI coordinates

        // If behind camera, don't draw
        if (screenPos.z < 0) return;

        // Setup style
        GUIStyle style = new GUIStyle();
        style.normal.textColor = textColor;
        style.alignment = TextAnchor.UpperCenter;
        style.fontSize = 12;

        // Draw background
        float width = 160;
        float lineHeight = 18;
        float height = debugValues.Count * lineHeight + 10;
        Rect bgRect = new Rect(screenPos.x - width / 2, screenPos.y - 5, width, height);
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.Box(bgRect, "");
        GUI.color = Color.white;

        // Draw values
        float yPos = screenPos.y;
        foreach (var pair in debugValues)
        {
            GUI.Label(new Rect(screenPos.x - width / 2, yPos, width, 20), $"{pair.Key}: {pair.Value}", style);
            yPos += lineHeight;
        }
    }
}