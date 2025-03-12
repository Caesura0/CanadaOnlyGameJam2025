using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GooseController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the AStarManager")]
    public AStarManager aStarManager;

    [Tooltip("Transform of the player to chase")]
    public Transform playerTransform;

    [Header("Detection Settings")]
    [Tooltip("Distance at which goose can detect player")]
    public float detectionRadius = 10f;

    [Tooltip("Distance at which goose can attack player")]
    public float attackRadius = 8f;

    [Tooltip("Cooldown between attacks (seconds)")]
    public float attackCooldown = 2f;

    [Header("Movement Settings")]
    [Tooltip("Normal flying speed")]
    public float flyingSpeed = 5f;

    [Tooltip("Pursuit flying speed")]
    public float pursuitSpeed = 5f;

    [Tooltip("Attack diving speed")]
    public float attackSpeed = 7f;

    [Tooltip("Maximum random wander distance")]
    public float maxWanderDistance = 8f;

    [Tooltip("Height above ground to maintain while flying")]
    public float preferredHeight = 5f;

    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer;

    [Header("Map Bounds")]
    [Tooltip("Distance from edge that's considered 'near bounds'")]
    public float mapBoundsMargin = 5f;

    [Tooltip("Custom map center (leave at zero to calculate automatically)")]
    public Vector2 customMapCenter = Vector2.zero;

    [Tooltip("Map width (X) for bounds checking")]
    public float mapWidth = 100f;

    [Tooltip("Map height (Y) for bounds checking")]
    public float mapHeight = 60f;

    [Header("Animation")]
    [Tooltip("Optional animator component")]
    public Animator animator;

    [Tooltip("Animation trigger for attack")]
    public string attackTrigger = "Attack";

    [Tooltip("Boolean parameter for flying")]
    public string flyingBool = "IsFlying";

    [Header("Debug")]
    public bool showDebugInfo = true;

    // State machine
    public enum GooseState { Idle, Wander, Pursue, Attack, Retreat }
    private GooseState currentState = GooseState.Idle;

    // Pathfinding
    private List<Node> currentPath;
    private int currentPathIndex = 0;
    private Node currentNode;
    private float nextPathUpdateTime = 0f;
    private float pathUpdateInterval = 0.5f;

    // Attack
    private float lastAttackTime = 0f;
    private bool isAttacking = false;
    private Vector3 attackStartPosition;
    private Vector3 attackDirection;

    // Wander
    private Vector3 wanderTarget;
    private float wanderUpdateTime = 0f;

    // References
    private SpriteRenderer spriteRenderer;

    // Direction tracking
    private bool isFacingRight = true;

    // Stuck detection
    private Vector3 lastPosition;
    private float stuckCheckTime = 0f;
    private int stuckCount = 0;

    // Movement tracking
    private Vector3 prevPosition;
    private float updateTimer = 0f;

    void Start()
    {
        // Find references at runtime
        if (aStarManager == null)
            aStarManager = AStarManager.instance;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        prevPosition = transform.position;

        // Initialize with wander state
        SetState(GooseState.Wander);
    }

    void Update()
    {
        // Update timer for consistent movement tracking
        updateTimer += Time.deltaTime;

        // Update current node
        currentNode = aStarManager.FindNearestNode(transform.position);

        // Check for stuck behavior
        CheckIfStuck();

        // Update state behavior
        UpdateStateBehavior();

        // Update detection
        UpdateDetection();

        // Update animations
        if (animator != null)
            animator.SetBool(flyingBool, true);

        // Update facing direction based on movement at a consistent rate
        if (updateTimer >= 0.05f) // 20 times per second
        {
            UpdateDirection();
            prevPosition = transform.position; // Track position for next velocity calculation
            updateTimer = 0f;
        }
    }

    private void UpdateStateBehavior()
    {
        switch (currentState)
        {
            case GooseState.Wander:
                // Check if we need a new wander target
                if (Time.time > wanderUpdateTime || Vector3.Distance(transform.position, wanderTarget) < 0.5f)
                {
                    SetNewWanderTarget();
                }

                // Move toward target
                MoveToward(wanderTarget, flyingSpeed);
                MaintainHeight();
                break;

            case GooseState.Pursue:
                // Update path to player
                if (Time.time > nextPathUpdateTime)
                {
                    UpdatePathToPlayer();
                    nextPathUpdateTime = Time.time + pathUpdateInterval;
                }

                // Follow path
                FollowPath(pursuitSpeed);
                break;

            case GooseState.Attack:
                if (!isAttacking && CanAttackPlayer())
                {
                    StartAttack();
                }
                else if (isAttacking)
                {
                    ExecuteAttack();
                }
                else
                {
                    SetState(GooseState.Pursue);
                }
                break;

            case GooseState.Retreat:
                if (playerTransform != null)
                {
                    // Get retreat direction (away from player)
                    Vector3 awayFromPlayer = transform.position - playerTransform.position;

                    // If we're near the map bounds, adjust to retreat toward the center
                    bool nearBounds = IsNearMapBounds();

                    if (nearBounds)
                    {
                        // Get direction toward map center
                        Vector3 mapCenter = GetMapCenter();
                        Vector3 towardCenter = (mapCenter - transform.position).normalized;

                        // Blend retreat direction - 70% toward center, 30% away from player
                        awayFromPlayer = (towardCenter * 0.7f + awayFromPlayer.normalized * 0.3f).normalized;
                    }

                    // Set retreat target with height gain
                    Vector3 retreatTarget = transform.position + awayFromPlayer.normalized * 5f + Vector3.up;

                    // Move toward retreat target
                    MoveToward(retreatTarget, flyingSpeed);
                }

                // After retreating for a bit, go back to wander
                if (Time.time > wanderUpdateTime)
                {
                    SetState(GooseState.Wander);
                }
                break;

            case GooseState.Idle:
            default:
                // Just hover and maintain height
                MaintainHeight();

                if (Time.time > wanderUpdateTime)
                {
                    SetState(GooseState.Wander);
                }
                break;
        }
    }

    private void UpdateDetection()
    {
        switch (currentState)
        {
            case GooseState.Wander:
            case GooseState.Idle:
                // If we're wandering or idle, check if we can see player
                if (CanDetectPlayer())
                {
                    SetState(GooseState.Pursue);
                }
                break;

            case GooseState.Pursue:
                // If we're pursuing, check if we've lost sight or can attack
                if (!CanDetectPlayer())
                {
                    SetState(GooseState.Wander);
                }
                else if (CanAttackPlayer())
                {
                    SetState(GooseState.Attack);
                }
                break;
        }
    }

    private void CheckIfStuck()
    {
        if (Time.time > stuckCheckTime)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            if (distanceMoved < 0.1f && currentState == GooseState.Wander)
            {
                stuckCount++;
                if (stuckCount >= 3)
                {
                    SetNewWanderTarget();
                    stuckCount = 0;
                }
            }
            else
            {
                stuckCount = 0;
            }

            lastPosition = transform.position;
            stuckCheckTime = Time.time + 0.5f;
        }
    }

    private void UpdateDirection()
    {
        // Determine direction based on current movement or target
        float dirX = 0;

        if (isAttacking && attackDirection != Vector3.zero)
        {
            dirX = attackDirection.x;
        }
        else if (currentPath != null && currentPathIndex < currentPath.Count)
        {
            dirX = currentPath[currentPathIndex].transform.position.x - transform.position.x;
        }
        else if (currentState == GooseState.Wander)
        {
            dirX = wanderTarget.x - transform.position.x;
        }
        else if (playerTransform != null && (currentState == GooseState.Pursue || currentState == GooseState.Attack))
        {
            dirX = playerTransform.position.x - transform.position.x;
        }

        // Only update if we have a clear direction
        if (Mathf.Abs(dirX) > 0.1f)
        {
            bool newFacingRight = dirX > 0;

            // Only flip if direction changed
            if (newFacingRight != isFacingRight)
            {
                isFacingRight = newFacingRight;

                if (spriteRenderer != null)
                    spriteRenderer.flipX = !isFacingRight;
            }
        }
    }

    #region Movement

    private void MoveToward(Vector3 target, float speed)
    {
        Vector3 direction = (target - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
    }

    private void FollowPath(float speed)
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        if (currentPathIndex < currentPath.Count)
        {
            Node targetNode = currentPath[currentPathIndex];
            float distanceToTarget = Vector3.Distance(transform.position, targetNode.transform.position);

            if (distanceToTarget < 0.5f)
            {
                currentPathIndex++;

                if (currentPathIndex >= currentPath.Count)
                {
                    if (CanAttackPlayer())
                    {
                        SetState(GooseState.Attack);
                        return;
                    }
                    else
                    {
                        UpdatePathToPlayer();
                        currentPathIndex = 0;
                    }
                }
            }

            if (currentPathIndex < currentPath.Count)
            {
                MoveToward(currentPath[currentPathIndex].transform.position, speed);
            }
        }
        else
        {
            UpdatePathToPlayer();
            currentPathIndex = 0;
        }
    }

    private void MaintainHeight()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            100f,
            groundLayer
        );

        if (hit.collider != null)
        {
            float currentHeight = transform.position.y - hit.point.y;

            if (currentHeight < preferredHeight)
            {
                transform.position += Vector3.up * Mathf.Min(flyingSpeed * Time.deltaTime, preferredHeight - currentHeight);
            }
            else if (currentHeight > preferredHeight + 2f)
            {
                transform.position += Vector3.down * Mathf.Min(flyingSpeed * 0.5f * Time.deltaTime, currentHeight - preferredHeight);
            }
        }
    }

    #endregion

    #region Pathfinding

    private void UpdatePathToPlayer()
    {
        if (playerTransform == null || aStarManager == null || currentNode == null)
            return;

        Node playerNode = aStarManager.FindNearestNode(playerTransform.position);

        if (playerNode != null && currentNode != null)
        {
            List<Node> newPath = aStarManager.GeneratePath(currentNode, playerNode);

            if (newPath != null && newPath.Count > 0)
            {
                currentPath = newPath;
                currentPathIndex = 0;
            }
        }
    }

    #endregion

    #region Wander

    private void SetNewWanderTarget()
    {
        // First check if we're near map bounds
        if (IsNearMapBounds())
        {
            // If near bounds, target toward map center
            Vector3 mapCenter = GetMapCenter();
            Vector3 towardCenter = (mapCenter - transform.position).normalized;
            wanderTarget = transform.position + towardCenter * maxWanderDistance * 0.8f;
            wanderUpdateTime = Time.time + Random.Range(3f, 5f);
            return;
        }

        Node[] allNodes = aStarManager.AllNodes();

        if (allNodes.Length == 0)
        {
            // Fallback to random position
            wanderTarget = transform.position + new Vector3(
                Random.Range(-maxWanderDistance, maxWanderDistance),
                Random.Range(0, maxWanderDistance / 2),
                0
            );

            // Ensure we don't wander toward map edges
            Vector3 mapCenter = GetMapCenter();
            Vector3 currentToTarget = wanderTarget - transform.position;
            Vector3 currentToCenter = mapCenter - transform.position;

            // If target is away from center but we're already near edge, flip direction
            if (Vector3.Dot(currentToTarget, currentToCenter) < 0 && IsNearMapBounds())
            {
                wanderTarget = transform.position - currentToTarget;
            }

            wanderUpdateTime = Time.time + Random.Range(2f, 5f);
            return;
        }

        // Try to find nodes within reasonable distance
        List<Node> validNodes = new List<Node>();

        foreach (Node node in allNodes)
        {
            if (node == null) continue;

            float distance = Vector3.Distance(transform.position, node.transform.position);

            if (distance <= maxWanderDistance && distance > 2f)
            {
                // Check if we can reach this node
                RaycastHit2D hit = Physics2D.Raycast(
                    transform.position,
                    (node.transform.position - transform.position).normalized,
                    distance,
                    groundLayer
                );

                if (hit.collider == null)
                {
                    validNodes.Add(node);
                }
            }
        }

        if (validNodes.Count > 0)
        {
            Node targetNode = validNodes[Random.Range(0, validNodes.Count)];
            wanderTarget = targetNode.transform.position;
        }
        else
        {
            // Fallback to random direction
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.2f, 0.5f),
                0
            ).normalized;

            wanderTarget = transform.position + randomDir * Random.Range(3f, maxWanderDistance * 0.8f);

            // Check if this direction takes us toward map edge
            Vector3 mapCenter = GetMapCenter();
            Vector3 currentToTarget = wanderTarget - transform.position;
            Vector3 currentToCenter = mapCenter - transform.position;

            // If target is away from center but we're already near edge, go toward center instead
            if (Vector3.Dot(currentToTarget, currentToCenter) < 0 && IsNearMapBounds())
            {
                wanderTarget = transform.position + currentToCenter.normalized * Random.Range(3f, maxWanderDistance * 0.8f);
            }
        }

        wanderUpdateTime = Time.time + Random.Range(3f, 8f);
    }

    #endregion

    #region Attack

    private void StartAttack()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        attackStartPosition = transform.position;

        if (playerTransform != null)
        {
            attackDirection = (playerTransform.position - transform.position).normalized;
        }
        else
        {
            attackDirection = Vector3.down;
        }

        if (animator != null)
        {
            animator.SetTrigger(attackTrigger);
        }
    }

    private void ExecuteAttack()
    {
        transform.position += attackDirection * attackSpeed * Time.deltaTime;

        if (playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer < 0.8f)
            {
                // Hit player - implement damage here
                Debug.Log("Goose hit player!");

                isAttacking = false;
                SetState(GooseState.Retreat);
                return;
            }
        }

        float attackDistance = Vector3.Distance(transform.position, attackStartPosition);

        RaycastHit2D groundHit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            0.5f,
            groundLayer
        );

        if (groundHit.collider != null || attackDistance > 5f)
        {
            isAttacking = false;
            SetState(GooseState.Retreat);
        }
    }

    #endregion

    #region Detection

    private bool CanDetectPlayer()
    {
        if (playerTransform == null)
            return false;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer > detectionRadius)
            return false;

        // Line of sight check
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            (playerTransform.position - transform.position).normalized,
            distToPlayer,
            groundLayer
        );

        return hit.collider == null;
    }

    private bool CanAttackPlayer()
    {
        if (playerTransform == null)
            return false;

        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        return distToPlayer <= attackRadius && Time.time > lastAttackTime + attackCooldown;
    }

    #endregion

    #region State Management

    private void SetState(GooseState newState)
    {
        // Handle exit actions
        switch (currentState)
        {
            case GooseState.Attack:
                isAttacking = false;
                break;

            case GooseState.Retreat:
                // If we were retreating and going to wander, check if we're out of bounds
                if (newState == GooseState.Wander && IsNearMapBounds())
                {
                    // If we're near bounds, force retreat toward center instead
                    Vector3 toCenter = GetMapCenter() - transform.position;
                    wanderTarget = transform.position + toCenter.normalized * 10f;
                    wanderUpdateTime = Time.time + 5f;
                }
                break;
        }

        // Set new state
        currentState = newState;
        stuckCount = 0;

        // Handle entry actions
        switch (newState)
        {
            case GooseState.Idle:
                currentPath = null;
                wanderUpdateTime = Time.time + Random.Range(1f, 3f);
                break;

            case GooseState.Wander:
                SetNewWanderTarget();
                break;

            case GooseState.Pursue:
                UpdatePathToPlayer();
                break;

            case GooseState.Retreat:
                wanderUpdateTime = Time.time + 2f; // Shortened retreat time
                currentPath = null;
                break;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Goose state: {newState}");
        }
    }

    #endregion

    #region Map Bounds Handling

    private Vector3 GetMapCenter()
    {
        // If custom center is set, use it
        if (customMapCenter != Vector2.zero)
        {
            return new Vector3(customMapCenter.x, customMapCenter.y, 0);
        }

        // Otherwise, try to calculate from level data
        if (aStarManager != null)
        {
            // Get all nodes as the most reliable way to determine level bounds
            Node[] allNodes = aStarManager.AllNodes();

            if (allNodes.Length > 0)
            {
                Vector3 sum = Vector3.zero;
                foreach (Node node in allNodes)
                {
                    if (node != null)
                        sum += node.transform.position;
                }

                // Return average position of all nodes
                return sum / allNodes.Length;
            }
        }

        // Fallback: use the player position
        if (playerTransform != null)
        {
            return playerTransform.position;
        }

        // Last resort: return this object's position (not ideal)
        return transform.position;
    }

    private bool IsNearMapBounds()
    {
        Vector3 mapCenter = GetMapCenter();
        Vector3 position = transform.position;

        // Calculate relative position from center
        float relativeX = Mathf.Abs(position.x - mapCenter.x);
        float relativeY = Mathf.Abs(position.y - mapCenter.y);

        // Check if we're near the map edge
        bool nearHorizontalBound = relativeX > (mapWidth / 2f - mapBoundsMargin);
        bool nearVerticalBound = relativeY > (mapHeight / 2f - mapBoundsMargin);

        // Also do a simple node check - if there are no nodes around us, we're probably out of bounds
        bool noNodesNearby = true;

        if (aStarManager != null)
        {
            Node nearestNode = aStarManager.FindNearestNode(position);
            if (nearestNode != null)
            {
                float distanceToNode = Vector3.Distance(position, nearestNode.transform.position);
                noNodesNearby = distanceToNode > 15f; // If nearest node is very far, we're likely out of bounds
            }
        }

        return nearHorizontalBound || nearVerticalBound || noNodesNearby;
    }

    #endregion

    void OnDrawGizmos()
    {
        if (!showDebugInfo)
            return;

        // Draw detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Draw attack radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);

        // Draw facing direction
        Gizmos.color = Color.blue;
        Vector3 facingDir = isFacingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(transform.position, facingDir * 2f);

        // Draw current path
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;

            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                if (currentPath[i] != null && currentPath[i + 1] != null)
                {
                    Gizmos.DrawLine(
                        currentPath[i].transform.position,
                        currentPath[i + 1].transform.position
                    );
                }
            }

            // Show current target node
            if (currentPathIndex < currentPath.Count && currentPath[currentPathIndex] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(currentPath[currentPathIndex].transform.position, 0.3f);
            }
        }

        // Draw wander target
        if (currentState == GooseState.Wander)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(wanderTarget, 0.2f);
        }

        // Draw map bounds if using bounds checking
        if (mapWidth > 0 && mapHeight > 0)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            Vector3 center = GetMapCenter();
            Vector3 size = new Vector3(mapWidth, mapHeight, 0);
            Gizmos.DrawWireCube(center, size);

            // Inner bounds with margin
            Gizmos.color = new Color(1f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(mapWidth - mapBoundsMargin * 2, mapHeight - mapBoundsMargin * 2, 0));
        }

        // Show state info
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, currentState.ToString());
#endif
    }
}