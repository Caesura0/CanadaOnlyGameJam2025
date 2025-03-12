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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    protected virtual void Update()
    {
        if (playerTransform != null)
        {
            bool wasPlayerDetected = playerDetected;
            playerDetected = Vector2.Distance(transform.position, playerTransform.position) < detectionRange;

            if (playerDetected && !wasPlayerDetected)
            {
                OnPlayerDetected();
            }
            else if (!playerDetected && wasPlayerDetected)
            {
                OnPlayerLost();
            }
        }

        UpdateBehavior();
    }

    // Overrides
    protected virtual void OnPlayerDetected() { }
    protected virtual void OnPlayerLost() { }
    protected abstract void UpdateBehavior();

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

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}