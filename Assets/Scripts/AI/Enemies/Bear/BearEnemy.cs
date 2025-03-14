using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BearEnemyController : EnemyBaseController
{
    [Header("Bear Settings")]
    [SerializeField] private float chargeSpeed = 7f;
    [SerializeField] private float chargeCooldown = 3f;
    [SerializeField] private float swipeRange = 1.5f;
    [SerializeField] private float swipeCooldown = 1f;
    [SerializeField] private float maxChargeDistance = 12f;

    [Header("Distance Thresholds")]
    [SerializeField] private float chargeMinDistance = 3f;
    [SerializeField] private float chargeMaxDistance = 8f;

    [Header("Attack Hitboxes")]
    [SerializeField] private GameObject swipeHitbox;
    [SerializeField] private int swipeDamage = 1;
    [SerializeField] private int chargeDamage = 1;

    // Simple state tracking for bear attacks
    private enum BearAction
    {
        None,
        Charging,
        Swiping
    }

    private BearAction currentBearAction = BearAction.None;
    private float swipeTimer = 0f;
    private float chargeTimer = 0f;
    private float actionTimer = 0f;

    // Position tracking for charge
    private Vector3 chargeStartPosition;
    private Vector3 chargeDirection;
    private int facingDirection = 1;

    // Reference to animator
    private Animator animator;
    private bool isFacingRight = true;

    protected override void Start()
    {
        // Increase detection range for the bear
        detectionRange = 15f;
        loseTrackRange = 20f;

        base.Start();
        animator = GetComponent<Animator>();

        // Start with abilities ready
        swipeTimer = 0f;
        chargeTimer = 0f;
    }

    protected override void Update()
    {
        base.Update();

        // Update timers
        if (chargeTimer > 0) chargeTimer -= Time.deltaTime;
        if (swipeTimer > 0) swipeTimer -= Time.deltaTime;
        if (actionTimer > 0)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0)
            {
                // Action completed
                currentBearAction = BearAction.None;

                // Reset animator states
                if (animator != null)
                {
                    animator.SetBool("IsCharging", false);
                    animator.ResetTrigger("Swipe");
                }

                Debug.Log("Bear action completed");
            }
        }

        // Update animator states
        UpdateAnimatorState();

        // If in chase state, check for attacks
        if (currentState == EnemyState.Chase && currentBearAction == BearAction.None)
        {
            TryBearAttacks();
        }
    }

    private void UpdateAnimatorState()
    {
        if (animator == null) return;

        // Update base animation states
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("MoveSpeed", Mathf.Abs(rb.velocity.x));
    }

    private void TryBearAttacks()
    {
        if (target == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, target.position);

        // Debug distance information
        Debug.Log($"Bear state: {currentState}, Distance to player: {distanceToPlayer}, swipeRange: {swipeRange}, " +
                  $"chargeRange: {chargeMinDistance}-{chargeMaxDistance}, " +
                  $"swipeTimer: {swipeTimer}, chargeTimer: {chargeTimer}");

        // First priority - swipe if close enough
        if (distanceToPlayer <= swipeRange && swipeTimer <= 0)
        {
            StartSwipeAttack();
            return;
        }

        // Second priority - charge if in range
        bool isInChargeRange = distanceToPlayer >= chargeMinDistance &&
                              distanceToPlayer <= chargeMaxDistance;

        if (isInChargeRange && chargeTimer <= 0)
        {
            StartCharge();
            return;
        }
    }

    // Override ExecuteMovement to handle the bear's special states
    protected override void ExecuteMovement()
    {
        // Handle special bear actions
        if (currentBearAction == BearAction.Charging)
        {
            HandleChargeMovement();
            return;
        }
        else if (currentBearAction == BearAction.Swiping)
        {
            // Don't move during swipe
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // For all other states, use the base movement
        base.ExecuteMovement();
    }

    // Override the CanSeeTarget to make the bear more aggressive
    protected override bool CanSeeTarget()
    {
        if (target == null)
            return false;

        // If the player is very close, always "see" them regardless of obstacles
        float distanceToTarget = Vector2.Distance(transform.position, target.position);
        if (distanceToTarget < swipeRange * 2)
        {
            return true;
        }

        // Otherwise use normal bubble detection
        return base.CanSeeTarget();
    }

    private void StartCharge()
    {
        Debug.Log("Starting bear charge attack");
        currentBearAction = BearAction.Charging;

        // Store charge start position
        chargeStartPosition = transform.position;

        // Set and store charge direction based on player position
        facingDirection = target.position.x > transform.position.x ? 1 : -1;
        chargeDirection = new Vector3(facingDirection, 0, 0);

        // Flip sprite to face target
        FlipSpriteDirectly(facingDirection > 0);

        // Play charge animation
        if (animator != null)
        {
            animator.SetBool("IsCharging", true);
        }

        // Set charge cooldown
        chargeTimer = chargeCooldown;

        // Enable collision damage during charge
        damageOnCollision = true;
        collisionDamage = chargeDamage;
        knockbackForce = 10f; // Stronger knockback for charge
        minDamageVelocity = chargeSpeed * 0.5f; // Only damage if moving fast enough
    }

    private void HandleChargeMovement()
    {
        // Apply charge force in fixed direction
        float chargeVelocity = facingDirection * chargeSpeed;
        rb.velocity = new Vector2(chargeVelocity, rb.velocity.y);

        // Check if we've traveled too far
        float chargeDistance = Vector3.Distance(
            new Vector3(transform.position.x, 0, 0),
            new Vector3(chargeStartPosition.x, 0, 0)
        );

        // For debugging
        Debug.Log($"Charging: distance={chargeDistance}/{maxChargeDistance}, wall={DetectWall()}");

        if (chargeDistance > maxChargeDistance || DetectWall())
        {
            EndCharge();
        }
    }

    private void EndCharge()
    {
        Debug.Log("Ending bear charge attack");
        // Slow down the bear when ending charge
        rb.velocity = new Vector2(rb.velocity.x * 0.3f, rb.velocity.y);

        // Reset action state
        currentBearAction = BearAction.None;

        // Reset animation state
        if (animator != null)
        {
            animator.SetBool("IsCharging", false);
        }

        // Disable collision damage after charge
        damageOnCollision = false;
    }

    private bool DetectWall()
    {
        // Cast a ray in charge direction
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + new Vector3(0, 0.5f, 0),
            chargeDirection,
            1f,
            groundLayer
        );

        return hit.collider != null;
    }

    private void StartSwipeAttack()
    {
        Debug.Log("Starting bear swipe attack");
        currentBearAction = BearAction.Swiping;

        // Stop movement during swipe
        rb.velocity = Vector2.zero;

        // Set timers
        actionTimer = 0.5f; // Time to complete swipe action
        swipeTimer = swipeCooldown; // Cooldown before next swipe

        // Play swipe animation
        if (animator != null)
        {
            animator.SetTrigger("Swipe");
        }

        // Activate Hitbox
        CheckSwipeHit();
    }

    private void ActivateSwipeHitbox()
    {
        if (swipeHitbox != null)
        {
            // Position correctly based on facing direction
            swipeHitbox.transform.localPosition = new Vector3(
                Mathf.Abs(swipeHitbox.transform.localPosition.x) * (isFacingRight ? 1 : -1),
                swipeHitbox.transform.localPosition.y,
                swipeHitbox.transform.localPosition.z
            );

            swipeHitbox.SetActive(true);

            // Set a timer to deactivate it
            StartCoroutine(DeactivateSwipeHitbox(0.3f));
        }
        else
        {
            // Fallback for direct collision check
            CheckSwipeHit();
        }
    }

    // Direct check for swipe hit if no hitbox is available
    private void CheckSwipeHit()
    {
        if (target == null) return;

        // Check distance to player
        float distToPlayer = Vector2.Distance(transform.position, target.position);

        if (distToPlayer <= swipeRange)
        {
            // Check if player is in front of bear
            Vector2 dirToPlayer = (target.position - transform.position).normalized;
            bool isInFront = (isFacingRight && dirToPlayer.x > 0) || (!isFacingRight && dirToPlayer.x < 0);

            if (isInFront)
            {
                // Get player component and apply damage
                PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    Debug.Log("Bear swipe hit player!");
                    playerHealth.TakeDamage(swipeDamage);
                }
            }
        }
    }

    // Coroutine to deactivate the swipe hitbox after a delay
    private IEnumerator DeactivateSwipeHitbox(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (swipeHitbox != null)
        {
            swipeHitbox.SetActive(false);
        }
    }

    // Override the OnPlayerDamaged callback to handle charge-specific behavior
    protected override void OnPlayerDamaged(Collision2D collision)
    {
        base.OnPlayerDamaged(collision);

        // If we hit the player while charging, end the charge
        if (currentBearAction == BearAction.Charging)
        {
            EndCharge();
        }
    }

    // Override DoFlip to update our facing direction
    protected override void FlipSpriteDirectly(bool toFaceRight)
    {
        // Call base DoFlip method
        base.FlipSpriteDirectly(toFaceRight);

        // Update our local tracking variable
        isFacingRight = facingRight;
    }

    // Override OnSwitchState to add debugging
    protected override void OnSwitchState(EnemyState newState)
    {
        Debug.Log($"Bear switching from {currentState} to {newState}");

        // Reset bear action when switching to patrol state
        if (newState == EnemyState.Patrol && currentBearAction != BearAction.None)
        {
            currentBearAction = BearAction.None;

            // Reset animation state
            if (animator != null)
            {
                animator.SetBool("IsCharging", false);
                animator.ResetTrigger("Swipe");
            }

            // Make sure collision damage is disabled
            damageOnCollision = false;
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw swipe range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, swipeRange);

        // Draw charge min range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeMinDistance);

        // Draw charge max range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, chargeMaxDistance);

        // Draw charge direction if charging
        if (Application.isPlaying && currentBearAction == BearAction.Charging)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, chargeDirection * 3f);

            // Draw max charge distance
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(chargeStartPosition, maxChargeDistance);
        }
    }
}