using UnityEngine;

// Very minimal base class with only essential functionality
public abstract class BaseEnemy : MonoBehaviour
{
    [Header("Base Settings")]
    [SerializeField] protected float detectionRange = 5f;
    [SerializeField] protected int health = 3;

    protected Transform playerTransform;
    protected Rigidbody2D rb;
    protected Animator animator;
    protected bool playerDetected = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        // Find the player in the scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    protected virtual void Update()
    {
        // Check if player is within detection range
        if (playerTransform != null)
        {
            bool wasPlayerDetected = playerDetected;
            playerDetected = Vector2.Distance(transform.position, playerTransform.position) < detectionRange;

            // Only call these if detection state changed
            if (playerDetected && !wasPlayerDetected)
            {
                OnPlayerDetected();
            }
            else if (!playerDetected && wasPlayerDetected)
            {
                OnPlayerLost();
            }
        }

        // Update behavior
        UpdateBehavior();
    }

    // Override these in subclasses
    protected virtual void OnPlayerDetected() { }
    protected virtual void OnPlayerLost() { }
    protected abstract void UpdateBehavior();

    // Health functionality
    public virtual void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    // Debug visualization
    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}