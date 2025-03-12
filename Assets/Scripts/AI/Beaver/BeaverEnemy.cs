using UnityEngine;

public class SimpleBeaverEnemy : BaseEnemy
{
    [Header("Beaver Settings")]
    [SerializeField] private float jumpForce = 10f;
    [SerializeField] private float minJumpInterval = 2f;
    [SerializeField] private float maxJumpInterval = 5f;

    [Header("Effects")]
    [SerializeField] private GameObject jumpSplashEffectPrefab;  // Splash when jumping out
    [SerializeField] private GameObject diveSplashEffectPrefab;  // Splash when going back under
    [SerializeField] private GameObject bubbleEffectPrefab;
    [SerializeField] private Transform[] jumpPoints;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip splashSound;

    private bool isUnderwater = true;
    private float nextJumpTime = 0f;
    private int currentJumpPointIndex = 0;
    private SpriteRenderer spriteRenderer;
    private GameObject activeBubbleEffect;
    private AudioSource audioSource;

    private Transform currentJumpPoint =>
        jumpPoints != null && jumpPoints.Length > 0 ?
        jumpPoints[currentJumpPointIndex] : transform;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (jumpSound != null || splashSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    protected override void Start()
    {
        base.Start();

        // Make sure we have jump points
        if (jumpPoints == null || jumpPoints.Length == 0)
        {
            Debug.LogError("Beaver needs at least one jump point!");
            return;
        }

        // Start at first jump point
        MoveToCurrentJumpPoint();

        // Hide beaver sprite
        HideBeaver();

        // Create bubble effect
        CreateBubbleEffect();

        // Set next jump time
        SetNextJumpTime();

        // Initialize animator if available
        if (animator != null)
        {
            // Set initial underwater state
            animator.SetBool("IsUnderwater", true);
        }
    }

    protected override void UpdateBehavior()
    {
        // Check if we should go back underwater
        if (!isUnderwater && rb.velocity.y < 0 && transform.position.y <= currentJumpPoint.position.y)
        {
            ReturnToWater();
        }

        // Check if it's time to jump
        if (isUnderwater && Time.time >= nextJumpTime)
        {
            Jump();
        }
    }

    // Make jumps more frequent when player is detected
    protected override void OnPlayerDetected()
    {
        // If we're underwater and not about to jump already
        if (isUnderwater && Time.time < nextJumpTime - 0.5f)
        {
            // Schedule a jump sooner
            nextJumpTime = Time.time + Random.Range(0.2f, 0.7f);
        }
    }

    private void Jump()
    {
        // Don't jump if not underwater
        if (!isUnderwater) return;
        PerformJump();
    }

    // Called by animation event when the pre-jump animation completes
    public void OnJumpAnimationComplete()
    {
        PerformJump();
    }

    private void PerformJump()
    {
        // Create jump splash effect
        CreateSplashEffect(true);

        // Hide bubble effect during jump
        if (activeBubbleEffect != null)
        {
            Destroy(activeBubbleEffect);
            activeBubbleEffect = null;
        }

        // Simple vertical jump
        rb.velocity = new Vector2(0, jumpForce);

        // Set as not underwater
        isUnderwater = false;
        rb.gravityScale = 2f; // Normal gravity above water

        // Show beaver sprite
        ShowBeaver();

        // Schedule next jump
        SetNextJumpTime();

        // Trigger the actual jump animation
        if (animator != null)
            animator.SetTrigger("Jump");

        // Play jump sound
        PlaySound(jumpSound);
    }

    // Helper to check if we have a prepare jump animation
    private bool HasJumpPrepareAnimation()
    {
        if (animator == null) return false;

        // Check animator runtimes for a state with PrepareJump transition
        // This is a simple check - you may need to customize based on your animator setup
        return animator.HasState(0, Animator.StringToHash("PrepareJump"));
    }

    private void ReturnToWater()
    {
        isUnderwater = true;
        rb.gravityScale = 0.1f; // Minimal gravity underwater for physics stability

        // Create dive splash effect
        CreateSplashEffect(false);

        // Stop movement
        rb.velocity = Vector2.zero;

        // Hide beaver
        HideBeaver();

        // Move to next jump point if we have multiple
        if (jumpPoints.Length > 1)
        {
            MoveToNextJumpPoint();
        }

        // Create new bubble effect
        CreateBubbleEffect();

        // Play splash sound
        PlaySound(splashSound);
    }

    private void ShowBeaver()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Set animator state for being visible/above water
        if (animator != null)
        {
            animator.SetBool("IsUnderwater", false);
        }
    }

    private void HideBeaver()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        // Set animator state for being hidden/underwater
        if (animator != null)
        {
            animator.SetBool("IsUnderwater", true);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void CreateSplashEffect(bool isJumping)
    {
        // Choose the correct splash effect based on whether we're jumping or diving
        GameObject prefab = isJumping ? jumpSplashEffectPrefab : diveSplashEffectPrefab;

        if (prefab != null)
        {
            // Create splash at water level
            GameObject splash = Instantiate(
                prefab,
                transform.position,
                Quaternion.identity
            );

            // Clean up splash after a short time
            Destroy(splash, 1.0f);
        }
    }

    private void CreateBubbleEffect()
    {
        if (bubbleEffectPrefab != null && activeBubbleEffect == null)
        {
            activeBubbleEffect = Instantiate(
                bubbleEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            // Parent to the current jump point
            if (currentJumpPoint != null)
            {
                activeBubbleEffect.transform.parent = currentJumpPoint;
            }
        }
    }

    private void MoveToCurrentJumpPoint()
    {
        if (jumpPoints == null || jumpPoints.Length == 0 || currentJumpPointIndex >= jumpPoints.Length)
            return;

        // Move to the current jump point
        transform.position = jumpPoints[currentJumpPointIndex].position;
    }

    private void MoveToNextJumpPoint()
    {
        if (jumpPoints == null || jumpPoints.Length <= 1)
            return;

        // Store the previous point index
        int previousPointIndex = currentJumpPointIndex;

        if (playerDetected && playerTransform != null)
        {
            // When player is detected, find the closest point to the player
            float closestDistance = float.MaxValue;
            int closestPointIndex = 0;

            for (int i = 0; i < jumpPoints.Length; i++)
            {
                if (jumpPoints[i] == null) continue;

                float distance = Vector2.Distance(jumpPoints[i].position, playerTransform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPointIndex = i;
                }
            }

            currentJumpPointIndex = closestPointIndex;
        }
        else
        {
            // When no player is detected, pick a random point
            // Make sure we don't pick the same point we're currently at
            if (jumpPoints.Length > 2)
            {
                int newIndex = currentJumpPointIndex;
                while (newIndex == currentJumpPointIndex)
                {
                    newIndex = Random.Range(0, jumpPoints.Length);
                }
                currentJumpPointIndex = newIndex;
            }
            else
            {
                // With only 2 points, just switch to the other one
                currentJumpPointIndex = (currentJumpPointIndex + 1) % jumpPoints.Length;
            }
        }

        // Only move if we've actually chosen a different point
        if (previousPointIndex != currentJumpPointIndex)
        {
            MoveToCurrentJumpPoint();

            if (Debug.isDebugBuild)
            {
                Debug.Log($"Beaver moved to jump point {currentJumpPointIndex}" +
                          (playerDetected ? " (player detected)" : " (random)"));
            }
        }
    }

    private void SetNextJumpTime()
    {
        // More frequent jumps when player is detected
        float interval;

        if (playerDetected)
        {
            // When player is detected, jump more frequently - use shorter range
            interval = Random.Range(minJumpInterval * 0.5f, maxJumpInterval * 0.5f);
        }
        else
        {
            // Normal behavior - use full range between min and max
            interval = Random.Range(minJumpInterval, maxJumpInterval);
        }

        // Set the next jump time
        nextJumpTime = Time.time + interval;

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Beaver scheduled next jump in {interval:F1} seconds" +
                      (playerDetected ? " (player detected)" : ""));
        }
    }

    // Override the Die method to show a death animation/effect
    protected override void Die()
    {
        // Optional: Add a death effect/animation here

        // Hide the beaver immediately
        HideBeaver();

        // Destroy any effects
        if (activeBubbleEffect != null)
            Destroy(activeBubbleEffect);

        // Destroy after delay to allow for any death effects
        Destroy(gameObject, 0.2f);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        // Draw jump points
        if (jumpPoints != null)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < jumpPoints.Length; i++)
            {
                if (jumpPoints[i] != null)
                {
                    // Draw jump point
                    Gizmos.DrawWireSphere(jumpPoints[i].position, 0.5f);

                    // Draw lines between points
                    if (i < jumpPoints.Length - 1 && jumpPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(jumpPoints[i].position, jumpPoints[i + 1].position);
                    }
                    else if (i == jumpPoints.Length - 1 && jumpPoints[0] != null)
                    {
                        Gizmos.DrawLine(jumpPoints[i].position, jumpPoints[0].position);
                    }
                }
            }
        }
    }
}