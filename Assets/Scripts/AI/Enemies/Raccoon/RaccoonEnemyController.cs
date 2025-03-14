using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RaccoonEnemyController : EnemyBaseController
{
    [Header("Raccoon Settings")]
    [SerializeField] private float aggressiveSpeed = 4.5f;
    [SerializeField] private float normalSpeed = 3f;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float jumpCooldown = 2f;
    [SerializeField] private float jumpProbability = 0.3f;

    // Jump state tracking
    private float jumpTimer = 0f;
    private bool isJumping = false;
    private float lastJumpTime = 0f;

    // Reference to animator
    private Animator animator;

    protected override void Start()
    {
        // Set collision damage to always be active for raccoon
        damageOnCollision = true;
        collisionDamage = contactDamage;
        knockbackForce = 5f;

        // Basic setup
        base.Start();

        // Get animator
        animator = GetComponent<Animator>();

        // Set speeds
        moveSpeed = normalSpeed;
    }

    protected override void Update()
    {
        base.Update();

        // Update jump timer
        if (jumpTimer > 0f)
            jumpTimer -= Time.deltaTime;

        // Update animator states
        UpdateAnimatorState();

        // Change speed based on state
        if (currentState == EnemyState.Chase)
        {
            moveSpeed = aggressiveSpeed;

            // Consider jumping when chasing
            TryJump();
        }
        else
        {
            moveSpeed = normalSpeed;
        }
    }

    private void UpdateAnimatorState()
    {
        if (animator == null) return;

        // Update animation parameters
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetFloat("MoveSpeed", Mathf.Abs(rb.velocity.x));
        animator.SetBool("IsJumping", isJumping);
    }

    private void TryJump()
    {
        // Only attempt to jump if grounded and cooldown has passed
        if (!isGrounded || jumpTimer > 0f || !target)
            return;

        // Check if player is higher than raccoon - increases jump probability
        bool playerIsHigher = target.position.y > transform.position.y + 1f;
        float adjustedProbability = playerIsHigher ? jumpProbability * 2f : jumpProbability;

        // Random chance to jump (higher if player is above)
        if (Random.value < adjustedProbability)
        {
            // Jump!
            PerformJump();
        }

        // Set cooldown regardless to prevent constant jump checks
        jumpTimer = 0.5f;
    }

    private void PerformJump()
    {
        // Apply jump force
        rb.velocity = new Vector2(rb.velocity.x, 0f); // Reset Y velocity
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Update state
        isJumping = true;
        lastJumpTime = Time.time;
        jumpTimer = jumpCooldown;

        // Play jump animation
        if (animator != null)
        {
            animator.SetTrigger("Jump");
        }

        StartCoroutine(ResetJumpState());
    }

    private IEnumerator ResetJumpState()
    {
        // Wait until we start falling
        yield return new WaitUntil(() => rb.velocity.y <= 0);

        // Reset jump state once we've started falling
        isJumping = false;
    }

    // Override to add jump behavior for traversing terrain
    protected override void HandleEdgeDetection(float movementDir)
    {
        // If we're chasing the player and they're across a gap, try to jump it
        if (currentState == EnemyState.Chase && jumpTimer <= 0 && IsTargetAcrossGap())
        {
            // Jump to try to cross gap
            PerformJump();
            return;
        }

        // Otherwise use base behavior (stop at edge)
        base.HandleEdgeDetection(movementDir);
    }

    private bool IsTargetAcrossGap()
    {
        if (target == null) return false;

        // Direction to target
        Vector2 direction = target.position - transform.position;
        direction.Normalize();

        // Check if there's a gap between us and the target
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.up * 0.1f,
            direction,
            Mathf.Min(direction.magnitude, 5f),
            groundLayer
        );

        // Check if there's ground on the other side near the target
        RaycastHit2D targetGroundHit = Physics2D.Raycast(
            target.position,
            Vector2.down,
            2f,
            groundLayer
        );

        // If no obstacles in path and target is on solid ground, it's likely across a gap
        return hit.collider == null && targetGroundHit.collider != null;
    }

    // Override to add some visual feedback when the raccoon damages the player
    protected override void OnPlayerDamaged(Collision2D collision)
    {
        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Optional - play a sound effect
        // AudioSource.PlayClipAtPoint(attackSound, transform.position);

        // Add a small backward hop after attacking
        Vector2 bounceDirection = (transform.position - collision.transform.position).normalized;
        rb.AddForce(bounceDirection * 3f, ForceMode2D.Impulse);
    }
}