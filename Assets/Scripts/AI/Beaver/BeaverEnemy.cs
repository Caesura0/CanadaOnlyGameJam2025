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

        if (jumpPoints == null || jumpPoints.Length == 0)
        {
            Debug.LogError("Beaver needs at least one jump point!");
            return;
        }

        MoveToCurrentJumpPoint();

        HideBeaver();

        CreateBubbleEffect();

        SetNextJumpTime();

        if (animator != null)
        {
            animator.SetBool("IsUnderwater", true);
        }
    }

    protected override void UpdateBehavior()
    {
        if (!isUnderwater && rb.velocity.y < 0 && transform.position.y <= currentJumpPoint.position.y)
        {
            ReturnToWater();
        }
        if (isUnderwater && Time.time >= nextJumpTime)
        {
            Jump();
        }
    }

    // Make jumps more frequent when player is detected
    protected override void OnPlayerDetected()
    {
        if (isUnderwater && Time.time < nextJumpTime - 0.5f)
        {
            nextJumpTime = Time.time + Random.Range(0.2f, 0.7f);
        }
    }

    private void Jump()
    {
        if (!isUnderwater) return;
        PerformJump();
    }

    private void PerformJump()
    {
        CreateSplashEffect(true);

        if (activeBubbleEffect != null)
        {
            Destroy(activeBubbleEffect);
            activeBubbleEffect = null;
        }

        // Simple vertical jump
        rb.velocity = new Vector2(0, jumpForce);

        isUnderwater = false;
        rb.gravityScale = 2f; // Normal gravity above water

        ShowBeaver();

        SetNextJumpTime();

        if (animator != null)
            animator.SetTrigger("Jump");

        // Play jump sound?
        PlaySound(jumpSound);
    }

    private void ReturnToWater()
    {
        isUnderwater = true;
        rb.gravityScale = 0.1f;

        CreateSplashEffect(false);

        rb.velocity = Vector2.zero;

        HideBeaver();
        if (jumpPoints.Length > 1)
        {
            MoveToNextJumpPoint();
        }

        CreateBubbleEffect();
        PlaySound(splashSound);
    }

    private void ShowBeaver()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

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
        GameObject prefab = isJumping ? jumpSplashEffectPrefab : diveSplashEffectPrefab;

        if (prefab != null)
        {
            GameObject splash = Instantiate(
                prefab,
                transform.position,
                Quaternion.identity
            );

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

        transform.position = jumpPoints[currentJumpPointIndex].position;
    }

    private void MoveToNextJumpPoint()
    {
        if (jumpPoints == null || jumpPoints.Length <= 1)
            return;

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
            // Make sure we don't pick the same point we're currently at (unelss we want that?)
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
                // With only 2 points, alternate for now
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
            interval = Random.Range(minJumpInterval * 0.5f, maxJumpInterval * 0.5f);
        }
        else
        {
            interval = Random.Range(minJumpInterval, maxJumpInterval);
        }

        nextJumpTime = Time.time + interval;

        if (Debug.isDebugBuild)
        {
            Debug.Log($"Beaver scheduled next jump in {interval:F1} seconds" +
                      (playerDetected ? " (player detected)" : ""));
        }
    }

    // Override the Die method to show a death animation/effect?
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