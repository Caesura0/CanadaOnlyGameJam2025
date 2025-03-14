using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBaseController : MonoBehaviour
{
    [Header("References")]
    public NavigationGraph navigationGraph;
    public Transform target;

    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;
    public bool faceMovementDirection = true;

    [Header("AI Settings")]
    public float pathfindingUpdateRate = 0.5f;
    public float detectionRange = 10f;       // How far away the enemy can see the player
    public float loseTrackRange = 15f;       // How far the enemy will chase before losing the player
    public bool respectEdges = true;         // Whether the enemy should stop at platform edges
    public float edgeDetectionCooldown = 1f; // Cooldown after detecting an edge to prevent flipping back and forth

    [Header("Edge Detection")]
    public float edgeCheckDistance = 0.5f;   // How far ahead to check for edges
    public float edgeRayLength = 1.5f;       // How far down to check for ground
    public bool debugEdgeDetection = true;   // Whether to show debug information for edge detection

    [Header("Patrol Settings")]
    public bool usePatrolPoints = false;
    public Transform[] patrolPoints;
    public float patrolDistance = 8f;        // How far the enemy will patrol if no points
    public enum EnemyState { Patrol, Chase }

    [Header("State")]
    public EnemyState currentState = EnemyState.Patrol;
    bool isTranquilized;

    // Protected fields accessible to derived classes
    protected Rigidbody2D rb;
    protected MovementAction currentAction = MovementAction.Idle;
    protected float lastPathUpdateTime;
    protected SpriteRenderer spriteRenderer;
    protected Vector2 startPosition;
    protected Vector2 patrolTarget;
    protected int currentPatrolIndex = 0;
    protected float stateChangeTime;
    protected Coroutine stateCoroutine;
    protected bool isGrounded;
    protected bool facingRight = true;

    // Edge detection state
    protected float lastEdgeDetectionTime = 0f;
    protected float lastDirectionChangeTime = 0f;
    protected int consecutiveEdgeDetections = 0;
    protected bool atEdge = false;

    protected virtual void Start()
    {
        // Get the EnemyHealth component and subscribe to its instance event
        EnemyHealth healthComponent = GetComponent<EnemyHealth>();
        if (healthComponent != null)
        {
            healthComponent.OnZombieTranquilized += EnemyHealth_OnZombieTranquilized;
        }

        // Get and configure the rigidbody
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"{gameObject.name} has no Rigidbody2D component! Adding one now.");
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3f;
            rb.mass = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // Find SpriteRenderer component
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        startPosition = transform.position;
        patrolTarget = startPosition;
        stateChangeTime = Time.time;

        // Find navigation graph if not assigned
        if (navigationGraph == null)
        {
            navigationGraph = FindObjectOfType<NavigationGraph>();
            if (navigationGraph == null)
            {
                Debug.LogError("No NavigationGraph found in the scene!");
                enabled = false;
                return;
            }
        }

        // Find player if not assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogWarning("No target assigned and no Player found!");
            }
        }

        StartStateMachine();
    }

    private void EnemyHealth_OnZombieTranquilized(object sender, EventArgs e)
    {
        isTranquilized = true;
        rb.velocity = Vector2.zero;
    }

    protected virtual void Update()
    {
        //if (isTranquilized) return;
        UpdateGroundedState();
        UpdatePathfinding();

        // Check for edges directly in Update to ensure it runs every frame
        if (respectEdges)
        {
            CheckForEdges();
        }
    }

    protected virtual void FixedUpdate()
    {
        ExecuteMovement();
    }

    protected virtual void UpdateGroundedState()
    {
        if (GetComponent<Collider2D>() is Collider2D collider)
        {
            // Use the bottom of the collider for ground check
            Vector2 checkPosition = new Vector2(
                collider.bounds.center.x,
                collider.bounds.min.y  // Bottom of the collider
            );

            // Cast a slightly wider ray to avoid missing the ground
            float rayWidth = collider.bounds.size.x * 0.8f;
            isGrounded = Physics2D.BoxCast(
                checkPosition,
                new Vector2(rayWidth, 0.1f),
                0f,
                Vector2.down,
                groundCheckDistance,
                groundLayer
            );

            // Visualize ground check
            if (debugEdgeDetection)
            {
                Debug.DrawLine(
                    checkPosition - new Vector2(rayWidth / 2, 0),
                    checkPosition + new Vector2(rayWidth / 2, 0),
                    isGrounded ? Color.green : Color.red
                );
                Debug.DrawLine(
                    checkPosition,
                    checkPosition + Vector2.down * groundCheckDistance,
                    isGrounded ? Color.green : Color.red
                );
            }
        }
        else
        {
            // Fallback if no collider found
            Vector2 checkPosition = new Vector2(transform.position.x, transform.position.y - 0.5f);
            isGrounded = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);

            if (debugEdgeDetection)
            {
                Debug.DrawRay(checkPosition, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
            }
        }
    }

    protected virtual void UpdatePathfinding()
    {
        if (Time.time - lastPathUpdateTime <= pathfindingUpdateRate)
            return;

        Vector2 currentTarget = DetermineCurrentTarget();

        // Get movement action from nav graph
        currentAction = navigationGraph.GetMovementAction(transform.position, currentTarget);

        lastPathUpdateTime = Time.time;
    }

    // New method to directly check for edges
    protected virtual void CheckForEdges()
    {
        if (!isGrounded) return;

        // Get the movement direction
        float movementDir = 0f;

        if (currentAction == MovementAction.MoveRight)
        {
            movementDir = 1f;
        }
        else if (currentAction == MovementAction.MoveLeft)
        {
            movementDir = -1f;
        }
        else if (currentState == EnemyState.Chase && target != null)
        {
            movementDir = Mathf.Sign(target.position.x - transform.position.x);
        }

        // Don't check if not moving
        if (Mathf.Abs(movementDir) < 0.1f)
        {
            atEdge = false;
            return;
        }

        // Only check for edges at a certain interval
        if (Time.time < lastEdgeDetectionTime + edgeDetectionCooldown)
        {
            return;
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null) return;

        // Calculate the position to check for an edge
        Vector2 bottomPosition = new Vector2(
            collider.bounds.center.x + (collider.bounds.extents.x * movementDir) + (edgeCheckDistance * movementDir),
            collider.bounds.min.y + 0.1f
        );

        // Cast a ray downward to check for ground
        RaycastHit2D hit = Physics2D.Raycast(bottomPosition, Vector2.down, edgeRayLength, groundLayer);

        // Visualize the raycast
        if (debugEdgeDetection)
        {
            Debug.DrawRay(bottomPosition, Vector2.down * edgeRayLength, hit.collider != null ? Color.green : Color.red);

            // Draw a cross at the check position
            float debugSize = 0.2f;
            Debug.DrawLine(
                new Vector3(bottomPosition.x - debugSize, bottomPosition.y, 0),
                new Vector3(bottomPosition.x + debugSize, bottomPosition.y, 0),
                hit.collider != null ? Color.green : Color.red
            );
            Debug.DrawLine(
                new Vector3(bottomPosition.x, bottomPosition.y - debugSize, 0),
                new Vector3(bottomPosition.x, bottomPosition.y + debugSize, 0),
                hit.collider != null ? Color.green : Color.red
            );
        }

        // If no ground detected, we're at an edge
        atEdge = hit.collider == null;

        if (atEdge && !isTranquilized)
        {
            lastEdgeDetectionTime = Time.time;

            // Handle edge detection
            HandleEdgeDetection(movementDir);
        }
    }

    // New method to handle edge detection response
    protected virtual void HandleEdgeDetection(float movementDir)
    {
        // We've detected an edge
        if (debugEdgeDetection)
        {
            Debug.Log($"{gameObject.name} detected an edge, moving direction: {movementDir}");
        }

        // Immediately stop horizontal movement
        rb.velocity = new Vector2(0, rb.velocity.y);

        // If in chase mode and we hit an edge, check if player is below
        if (currentState == EnemyState.Chase)
        {
            if (IsTargetOnLowerPlatform())
            {
                // For now, just stop at the edge - in future could add jump down behavior
                // This is where you would add jumping logic in the future!
                if (debugEdgeDetection)
                {
                    Debug.DrawLine(transform.position, target.position, Color.yellow, 0.1f);
                    Debug.Log($"{gameObject.name} sees player on lower platform, considering jump");
                }
            }
            else
            {
                // If chasing and hit edge, update patrol target to go the other way
                // This prevents flipping back and forth
                consecutiveEdgeDetections++;

                if (consecutiveEdgeDetections > 2)
                {
                    // If we keep hitting edges, switch to patrol mode temporarily
                    SwitchState(EnemyState.Patrol);
                    consecutiveEdgeDetections = 0;
                }
                else
                {
                    // Flip direction
                    FlipSpriteDirectly(!facingRight);
                }
            }
        }
        else if (currentState == EnemyState.Patrol)
        {
            // For patrol, just update the patrol target to go the other way
            FlipSpriteDirectly(!facingRight);
            UpdatePatrolTarget();

            // Reset this counter when in patrol mode
            consecutiveEdgeDetections = 0;
        }
    }

    protected virtual void ExecuteMovement()
    {
        if (!isGrounded || isTranquilized)
            return;

        // Don't move if at an edge
        if (atEdge && respectEdges)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // Calculate target velocity based on current action
        float targetVelocityX = 0f;

        switch (currentAction)
        {
            case MovementAction.MoveRight:
                targetVelocityX = moveSpeed;
                if (faceMovementDirection)
                    FlipSpriteDirectly(true);
                break;

            case MovementAction.MoveLeft:
                targetVelocityX = -moveSpeed;
                if (faceMovementDirection)
                    FlipSpriteDirectly(false);
                break;

            case MovementAction.Idle:
            case MovementAction.Wait:
                // Additional state-specific handling
                if (currentState == EnemyState.Chase && target != null)
                {
                    float dirToTarget = Mathf.Sign(target.position.x - transform.position.x);
                    targetVelocityX = dirToTarget * moveSpeed;

                    if (faceMovementDirection)
                    {
                        FlipSpriteDirectly(dirToTarget > 0);
                    }
                }
                break;
        }

        // Apply velocity only if we're not at an edge
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);
    }

    // Simple direct sprite flipping 
    protected virtual void FlipSpriteDirectly(bool faceRight)
    {
        // Only flip if we're changing direction
        if (facingRight != faceRight)
        {
            facingRight = faceRight;
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = !facingRight;
            }

            // Record when we last changed direction
            lastDirectionChangeTime = Time.time;
        }
    }

    protected Vector2 DetermineCurrentTarget()
    {
        switch (currentState)
        {
            case EnemyState.Chase:
                return target != null ? (Vector2)target.position : transform.position;
            case EnemyState.Patrol:
                return patrolTarget;
            default:
                return patrolTarget;
        }
    }

    #region State Machine
    protected void StartStateMachine()
    {
        if (stateCoroutine != null)
            StopCoroutine(stateCoroutine);

        stateCoroutine = StartCoroutine(StateMachineCoroutine());
    }

    protected IEnumerator StateMachineCoroutine()
    {
        while (enabled)
        {
            if (target == null)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            float distanceToTarget = Vector2.Distance(transform.position, target.position);

            switch (currentState)
            {
                case EnemyState.Patrol:
                    if (distanceToTarget <= detectionRange)
                    {
                        Debug.Log($"{gameObject.name} detected player at distance {distanceToTarget}");
                        SwitchState(EnemyState.Chase);
                    }
                    UpdatePatrolTarget();
                    break;

                case EnemyState.Chase:
                    if (distanceToTarget > loseTrackRange)
                    {
                        Debug.Log($"{gameObject.name} lost player at distance {distanceToTarget}");
                        SwitchState(EnemyState.Patrol);
                    }
                    break;
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    protected void SwitchState(EnemyState newState)
    {
        if (newState == currentState)
            return;

        currentState = newState;
        stateChangeTime = Time.time;

        if (currentState == EnemyState.Patrol)
        {
            UpdatePatrolTarget();
        }

        OnSwitchState(newState);

        Debug.Log($"{gameObject.name} switched to {newState} state");
    }

    // Overriders
    protected virtual void OnSwitchState(EnemyState newState)
    {
        // Base implementation does nothing
    }

    // Simplify to just a distance check - no raycast
    protected virtual bool CanSeeTarget()
    {
        if (target == null)
            return false;

        // Simple distance check - no line of sight
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        return distanceToTarget <= detectionRange;
    }

    // Helper method to check if the target is on a lower platform
    protected virtual bool IsTargetOnLowerPlatform()
    {
        if (target == null)
            return false;

        // Check if target is significantly below the enemy
        float heightDifference = transform.position.y - target.position.y;

        if (heightDifference < 1.0f)
            return false;

        // Check if there's platform beneath the target (to ensure they're on a platform)
        RaycastHit2D targetGroundHit = Physics2D.Raycast(target.position, Vector2.down, 1.0f, groundLayer);

        // Visualize the ray
        if (debugEdgeDetection)
        {
            Debug.DrawRay(target.position, Vector2.down * 1.0f, targetGroundHit.collider != null ? Color.blue : Color.red);
        }

        return targetGroundHit.collider != null;
    }

    protected virtual void UpdatePatrolTarget()
    {
        if (usePatrolPoints && patrolPoints.Length > 0)
        {
            if (Vector2.Distance(transform.position, patrolPoints[currentPatrolIndex].position) < 1f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            patrolTarget = patrolPoints[currentPatrolIndex].position;
        }
        else
        {
            if (Vector2.Distance(transform.position, patrolTarget) < 1f ||
                Time.time - lastDirectionChangeTime < 2f) // Also update if we recently changed direction
            {
                float direction = facingRight ? 1 : -1; // Use current facing direction
                patrolTarget = startPosition + new Vector2(direction * patrolDistance, 0);

                // Check if the patrol target is over ground
                RaycastHit2D hit = Physics2D.Raycast(patrolTarget, Vector2.down, 3f, groundLayer);
                if (hit.collider != null)
                {
                    patrolTarget.y = hit.point.y + 0.5f;
                }
                else
                {
                    // If no ground found in that direction, try the opposite
                    direction *= -1;
                    patrolTarget = startPosition + new Vector2(direction * patrolDistance, 0);

                    // Check again
                    hit = Physics2D.Raycast(patrolTarget, Vector2.down, 3f, groundLayer);
                    if (hit.collider != null)
                    {
                        patrolTarget.y = hit.point.y + 0.5f;
                    }
                    else
                    {
                        // If still no ground, just stay near start
                        patrolTarget = startPosition;
                    }
                }
            }
        }
    }
    #endregion

    protected virtual void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw lose track range
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, loseTrackRange);

        // Draw patrol area if not using specific points
        if (!usePatrolPoints)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(Application.isPlaying ? (Vector3)startPosition : transform.position, patrolDistance);
        }

        // Draw patrol points if using them
        if (usePatrolPoints && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.3f);
                }
            }
        }

        // Draw edge detection rays
        if (debugEdgeDetection && Application.isPlaying)
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                // Draw for both directions
                Vector2 leftCheckPos = new Vector2(
                    collider.bounds.center.x - collider.bounds.extents.x - edgeCheckDistance,
                    collider.bounds.min.y + 0.1f
                );

                Vector2 rightCheckPos = new Vector2(
                    collider.bounds.center.x + collider.bounds.extents.x + edgeCheckDistance,
                    collider.bounds.min.y + 0.1f
                );

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(leftCheckPos, leftCheckPos + Vector2.down * edgeRayLength);
                Gizmos.DrawLine(rightCheckPos, rightCheckPos + Vector2.down * edgeRayLength);

                Gizmos.DrawWireSphere(leftCheckPos, 0.1f);
                Gizmos.DrawWireSphere(rightCheckPos, 0.1f);
            }
        }
    }

    private void OnDestroy()
    {
        EnemyHealth healthComponent = GetComponent<EnemyHealth>();
        if (healthComponent != null)
        {
            healthComponent.OnZombieTranquilized -= EnemyHealth_OnZombieTranquilized;
        }
    }
}