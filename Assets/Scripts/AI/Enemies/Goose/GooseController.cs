using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GooseEnemyController : EnemyBaseController
{
    [Header("Goose Movement Settings")]
    [SerializeField] private float flyingHeight = 5f;
    [SerializeField] private float flyingSpeed = 4f;
    [SerializeField] private float diveBombSpeed = 8f;
    [SerializeField] private float heightVariation = 1.5f;
    [SerializeField] private float preferredPlayerDistance = 6f;
    [SerializeField] private float avoidanceDistance = 3f;

    [Header("Attack Settings")]
    [SerializeField] private float diveBombCooldown = 4f;
    [SerializeField] private float diveBombRange = 8f;
    [SerializeField] private float poopCooldown = 7f;
    [SerializeField] private float poopProbability = 0.3f;
    [SerializeField] private int diveBombDamage = 1;
    [SerializeField] private int poopDamage = 1;
    [SerializeField] private GameObject poopPrefab;

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem diveSwooshEffect;
    [SerializeField] private AudioClip honkSound;
    [SerializeField] private AudioClip diveBombSound;
    [SerializeField] private AudioClip poopSound;

    // State tracking
    private enum GooseAction
    {
        Flying,
        DiveBombing,
        Retreating
    }

    private GooseAction currentGooseAction = GooseAction.Flying;
    private float diveBombTimer = 0f;
    private float poopTimer = 0f;
    private float heightTimer = 0f;
    private float targetFlyHeight;
    private Vector3 diveBombStartPosition;
    private Vector3 diveBombTargetPosition;
    private bool isPerformingAction = false;
    private float actionStartTime = 0f;
    private float actionDuration = 0f;
    private AudioSource audioSource;
    private bool hasRetreatedAfterDive = false;

    protected override void Start()
    {
        // Initialize base controller settings
        detectionRange = 15f;
        loseTrackRange = 20f;
        moveSpeed = flyingSpeed;
        damageOnCollision = true;
        collisionDamage = diveBombDamage;

        // Override ground-based navigation
        respectEdges = false;

        // Call parent initialization
        base.Start();

        // Setup for flying
        rb.gravityScale = 0f;
        rb.drag = 1f;

        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (honkSound != null || diveBombSound != null || poopSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.7f;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // Set initial values
        targetFlyHeight = flyingHeight;
        diveBombTimer = diveBombCooldown * Random.Range(0.5f, 1f); // Random initial cooldown
        poopTimer = poopCooldown * Random.Range(0.5f, 1f); // Random initial cooldown
    }

    protected override void Update()
    {
        base.Update();

        // Update timers
        if (diveBombTimer > 0) diveBombTimer -= Time.deltaTime;
        if (poopTimer > 0) poopTimer -= Time.deltaTime;

        // Handle height variation
        if (currentGooseAction == GooseAction.Flying && !isPerformingAction)
        {
            UpdateFlyingHeight();
        }

        // Handle attack logic in Chase state
        if (currentState == EnemyState.Chase && !isPerformingAction)
        {
            TryGooseAttacks();
        }

        // Update action state
        if (isPerformingAction)
        {
            float actionProgress = (Time.time - actionStartTime) / actionDuration;

            if (actionProgress >= 1.0f)
            {
                isPerformingAction = false;

                // Action-specific cleanup
                if (currentGooseAction == GooseAction.DiveBombing)
                {
                    EndDiveBomb();
                }
                else if (currentGooseAction == GooseAction.Retreating)
                {
                    currentGooseAction = GooseAction.Flying;
                    hasRetreatedAfterDive = false;
                }
            }
        }

        // Sync animation state
        UpdateAnimatorState();
    }

    private void UpdateFlyingHeight()
    {
        // Occasionally change target height for more natural movement
        heightTimer -= Time.deltaTime;
        if (heightTimer <= 0)
        {
            targetFlyHeight = flyingHeight + Random.Range(-heightVariation, heightVariation);
            heightTimer = Random.Range(2f, 4f);
        }
    }

    protected override void ExecuteMovement()
    {
        // Don't use the standard ground-based movement
        // Instead, handle flying movement based on goose state
        switch (currentGooseAction)
        {
            case GooseAction.Flying:
                ExecuteFlyingMovement();
                break;

            case GooseAction.DiveBombing:
                ExecuteDiveBombMovement();
                break;

            case GooseAction.Retreating:
                ExecuteRetreatMovement();
                break;
        }
    }

    private void ExecuteFlyingMovement()
    {
        Vector2 moveDirection = Vector2.zero;

        if (currentState == EnemyState.Chase && target != null)
        {
            // Get distance to player for positioning logic
            float distToPlayer = Vector2.Distance(transform.position, target.position);
            Vector2 dirToPlayer = (target.position - transform.position).normalized;

            // Decide movement behavior based on distance to player
            if (distToPlayer < avoidanceDistance)
            {
                // Too close - move away slightly but maintain elevation
                moveDirection = -dirToPlayer;
                moveDirection.y *= 0.5f; // Reduce vertical component to prioritize horizontal movement
            }
            else if (distToPlayer > preferredPlayerDistance)
            {
                // Too far - move closer
                moveDirection = dirToPlayer;
                moveDirection.y *= 0.5f; // Reduce vertical component
            }
            else
            {
                // Good distance - circle around player
                moveDirection = new Vector2(-dirToPlayer.y, dirToPlayer.x) * 0.7f; // Perpendicular movement
                moveDirection.x += dirToPlayer.x * 0.3f; // Mix in some towards/away movement
            }

            // Adjust facing direction based on movement
            if (faceMovementDirection && Mathf.Abs(moveDirection.x) > 0.1f)
            {
                FlipSpriteDirectly(moveDirection.x > 0);
            }
        }
        else if (currentState == EnemyState.Patrol)
        {
            // Use patrol targets from base class
            Vector2 targetPos = DetermineCurrentTarget();
            moveDirection = (targetPos - (Vector2)transform.position).normalized;

            // Adjust facing direction
            if (faceMovementDirection && Mathf.Abs(moveDirection.x) > 0.1f)
            {
                FlipSpriteDirectly(moveDirection.x > 0);
            }
        }

        // Adjust vertical movement to maintain desired height
        MaintainFlyingHeight();

        // Apply movement
        if (moveDirection != Vector2.zero)
        {
            rb.velocity = moveDirection.normalized * moveSpeed;
        }
    }

    private void MaintainFlyingHeight()
    {
        // Cast ray downward to find ground height
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 100f, groundLayer);

        if (hit.collider != null)
        {
            float groundHeight = hit.point.y;
            float targetHeight = groundHeight + targetFlyHeight;
            float currentHeight = transform.position.y;

            // Smoothly adjust height
            if (Mathf.Abs(currentHeight - targetHeight) > 0.5f)
            {
                float verticalVelocity = rb.velocity.y;
                if (currentHeight < targetHeight)
                {
                    // Move up
                    verticalVelocity = Mathf.Lerp(verticalVelocity, moveSpeed, 0.1f);
                }
                else
                {
                    // Move down
                    verticalVelocity = Mathf.Lerp(verticalVelocity, -moveSpeed * 0.7f, 0.1f);
                }

                // Apply vertical velocity
                rb.velocity = new Vector2(rb.velocity.x, verticalVelocity);
            }
        }
    }

    private void TryGooseAttacks()
    {
        if (target == null) return;

        float distToPlayer = Vector2.Distance(transform.position, target.position);

        // Check if we can dive bomb
        if (diveBombTimer <= 0 && distToPlayer <= diveBombRange && !isPerformingAction)
        {
            // Prioritize dive bomb if player is below
            if (target.position.y < transform.position.y - 1f)
            {
                StartDiveBomb();
                return;
            }
            else if (Random.value < 0.3f) // Less likely to dive if player isn't below
            {
                StartDiveBomb();
                return;
            }
        }

        // Check if we can/should poop
        if (poopTimer <= 0 && !isPerformingAction && Random.value < poopProbability)
        {
            // Must be above player to poop
            if (target.position.y < transform.position.y - 2f && IsDirectlyAbovePlayer())
            {
                DropPoop();
                return;
            }
        }
    }

    private bool IsDirectlyAbovePlayer()
    {
        if (target == null) return false;

        // Check if we're positioned roughly above the player
        float xDiff = Mathf.Abs(transform.position.x - target.position.x);
        return xDiff < 2f;
    }

    private void StartDiveBomb()
    {
        currentGooseAction = GooseAction.DiveBombing;
        isPerformingAction = true;
        actionStartTime = Time.time;
        actionDuration = 1.5f; // Time to complete dive bomb

        // Store positions for interpolation
        diveBombStartPosition = transform.position;
        diveBombTargetPosition = target.position;

        // Make sure the target point is on ground
        RaycastHit2D hit = Physics2D.Raycast(diveBombTargetPosition, Vector2.down, 10f, groundLayer);
        if (hit.collider != null)
        {
            diveBombTargetPosition.y = hit.point.y + 0.5f; // Slightly above ground
        }

        // Play sound & effects
        if (audioSource != null && diveBombSound != null)
        {
            audioSource.PlayOneShot(diveBombSound);
        }

        if (diveSwooshEffect != null)
        {
            diveSwooshEffect.Play();
        }

        // Enable damage collision
        damageOnCollision = true;
        collisionDamage = diveBombDamage;
        knockbackForce = 5f;

        // Reset cooldown
        diveBombTimer = diveBombCooldown;

        // Animation trigger if available
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("DiveBomb");
        }
    }

    private void ExecuteDiveBombMovement()
    {
        if (!isPerformingAction) return;

        // Calculate dive trajectory with curved path
        float progress = (Time.time - actionStartTime) / actionDuration;
        progress = Mathf.Clamp01(progress);

        // Create a curved path: first move slightly up, then dive down
        float heightOffset;
        if (progress < 0.3f)
        {
            // Initial upward movement
            heightOffset = Mathf.Sin(progress * Mathf.PI) * 2f;
        }
        else
        {
            // Diving phase
            heightOffset = 0;
        }

        // Calculate position along curve
        Vector3 straightLinePos = Vector3.Lerp(diveBombStartPosition, diveBombTargetPosition, progress);
        Vector3 curvedPos = straightLinePos + new Vector3(0, heightOffset, 0);

        // Set position directly for more precise control during dive
        transform.position = curvedPos;

        // Set velocity in dive direction for collision detection
        Vector3 moveDir = (diveBombTargetPosition - transform.position).normalized;
        rb.velocity = moveDir * diveBombSpeed;

        // Update facing direction
        if (faceMovementDirection && Mathf.Abs(moveDir.x) > 0.1f)
        {
            FlipSpriteDirectly(moveDir.x > 0);
        }
    }

    private void EndDiveBomb()
    {
        // Transition to retreat
        currentGooseAction = GooseAction.Retreating;
        isPerformingAction = true;
        actionStartTime = Time.time;
        actionDuration = 1.0f;
        hasRetreatedAfterDive = true;

        // Disable damage collision after dive
        damageOnCollision = false;

        // Reset velocity
        rb.velocity = Vector2.up * flyingSpeed;
    }

    private void ExecuteRetreatMovement()
    {
        if (target == null) return;

        // Move upward and away from player
        Vector2 awayFromPlayer = (transform.position - target.position).normalized;

        // Emphasize upward movement
        awayFromPlayer.y = Mathf.Abs(awayFromPlayer.y) + 0.5f;

        // Apply movement
        rb.velocity = awayFromPlayer.normalized * flyingSpeed;
    }

    private void DropPoop()
    {
        // Can't poop without a prefab
        if (poopPrefab == null) return;

        // Play sound
        if (audioSource != null && poopSound != null)
        {
            audioSource.PlayOneShot(poopSound);
        }

        // Instantiate poop object
        GameObject poopObject = Instantiate(poopPrefab, transform.position, Quaternion.identity);

        // Configure the poop projectile
        PoopProjectile poopScript = poopObject.GetComponent<PoopProjectile>();
        if (poopScript == null)
        {
            poopScript = poopObject.AddComponent<PoopProjectile>();
        }

        // Set up poop damage and properties
        poopScript.damage = poopDamage;
        poopScript.knockbackForce = 3f;
        poopScript.destroyOnImpact = true;

        // Reset cooldown
        poopTimer = poopCooldown;

        // Animation trigger if available
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("Poop");
        }
    }

    private void UpdateAnimatorState()
    {
        Animator animator = GetComponent<Animator>();
        if (animator == null) return;

        // Update animation state based on current action
        animator.SetBool("IsFlying", currentGooseAction == GooseAction.Flying);
        animator.SetBool("IsDiving", currentGooseAction == GooseAction.DiveBombing);
        animator.SetFloat("MoveSpeed", rb.velocity.magnitude);
    }

    // Override UpdateGroundedState to always be "flying"
    protected override void UpdateGroundedState()
    {
        // Goose is never grounded when active
        isGrounded = false;
    }

    // Override to handle collision with player during dive bomb
    protected override void OnPlayerDamaged(Collision2D collision)
    {
       base.OnPlayerDamaged(collision);

        // If we hit the player while dive bombing, end it
        if (currentGooseAction == GooseAction.DiveBombing)
        {
            isPerformingAction = false;
            EndDiveBomb();
        }
    }

    // Override to add more visualization options
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw dive bomb range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, diveBombRange);

        // Draw preferred distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, preferredPlayerDistance);

        // Draw avoidance distance
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, avoidanceDistance);

        // Show flying height visualization
        if (Application.isPlaying)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 10f, groundLayer);
            if (hit.collider != null)
            {
                float groundHeight = hit.point.y;
                float targetHeight = groundHeight + targetFlyHeight;

                // Draw line from ground to target height
                Gizmos.color = Color.green;
                Gizmos.DrawLine(new Vector3(transform.position.x, groundHeight, 0),
                               new Vector3(transform.position.x, targetHeight, 0));

                // Draw sphere at target height
                Gizmos.DrawWireSphere(new Vector3(transform.position.x, targetHeight, 0), 0.5f);
            }
        }

        // If diving, show trajectory
        if (Application.isPlaying && currentGooseAction == GooseAction.DiveBombing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, diveBombTargetPosition);
            Gizmos.DrawWireSphere(diveBombTargetPosition, 0.5f);
        }
    }

    // Add destruction tracking methods
    private void OnDestroy()
    {
        Debug.LogError($"GooseEnemyController: Being destroyed! GameObject active: {gameObject.activeInHierarchy}, Time: {Time.time}, Position: {transform.position}");
    }

    private void OnDisable()
    {
        // Only log if this happens during gameplay, not scene changes
        if (Time.time > 1f)
        {
            Debug.LogWarning($"GooseEnemyController: Being disabled! Time: {Time.time}, Position: {transform.position}");
        }
    }
}