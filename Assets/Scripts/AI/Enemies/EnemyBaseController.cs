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
        else
        {
            // Fallback if no collider found
            Vector2 checkPosition = new Vector2(transform.position.x, transform.position.y - 0.5f);
            isGrounded = Physics2D.Raycast(checkPosition, Vector2.down, groundCheckDistance, groundLayer);
            Debug.DrawRay(checkPosition, Vector2.down * groundCheckDistance, isGrounded ? Color.green : Color.red);
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

    protected virtual void ExecuteMovement()
    {
        if (!isGrounded)
            return;

        // Calculate target velocity based on current action
        float targetVelocityX = 0f;

        switch (currentAction)
        {
            case MovementAction.MoveRight:
                targetVelocityX = moveSpeed;
                if (faceMovementDirection && !facingRight)
                    FlipSprite();
                break;

            case MovementAction.MoveLeft:
                targetVelocityX = -moveSpeed;
                if (faceMovementDirection && facingRight)
                    FlipSprite();
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
                        if (dirToTarget > 0 && !facingRight || dirToTarget < 0 && facingRight)
                            FlipSprite();
                    }
                }
                break;
        }

        // Apply velocity
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);
    }

    // Flag to prevent multiple flips while turning
    protected bool isFlipping = false;
    // Stored direction for pending flip
    protected bool flipToFaceRight;

    protected virtual void FlipSprite()
    {
        // Prevent multiple flips while already flipping
        if (isFlipping)
            return;

        // Store the intended flip direction
        flipToFaceRight = !facingRight;

        // Get animator if we have one
        Animator animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator != null)
        {
            try
            {
                isFlipping = true;
                animator.SetTrigger("Turn");
                // Safety fallback in case animation event doesn't fire
                StartCoroutine(FlipSafetyFallback());
                return;
            }
            catch (System.Exception)
            {
                // If there's an error, just do the immediate flip
                isFlipping = false;
            }
        }

        // If no animator or error, just do immediate flip
        DoFlip(flipToFaceRight);
    }

    // The actual flip logic
    protected virtual void DoFlip(bool toFaceRight)
    {
        facingRight = toFaceRight;

        // Option 1: If using spriteRenderer.flipX
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
    }

    public void OnTurnAnimationFlip()
    {
        DoFlip(flipToFaceRight);
    }

    public void OnTurnAnimationComplete()
    {
        isFlipping = false;
    }

    protected IEnumerator FlipSafetyFallback()
    {
        // Wait for anim to complete
        yield return new WaitForSeconds(0.2f);

        // If still flipping, force completion
        if (isFlipping)
        {
            DoFlip(flipToFaceRight);
            isFlipping = false;
            Debug.LogWarning("Turn animation did not trigger flip event - used fallback");
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
            if (Vector2.Distance(transform.position, patrolTarget) < 1f)
            {
                float direction = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f));
                patrolTarget = startPosition + new Vector2(direction * patrolDistance, 0);

                // Check if the patrol target is over ground
                RaycastHit2D hit = Physics2D.Raycast(patrolTarget, Vector2.down, 3f, groundLayer);
                if (hit.collider != null)
                {
                    patrolTarget.y = hit.point.y + 0.5f;
                }
                else
                {
                    patrolTarget = startPosition;
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