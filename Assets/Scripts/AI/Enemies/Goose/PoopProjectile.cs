using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PoopProjectile : MonoBehaviour
{
    [Header("Properties")]
    public float fallSpeed = 5f;
    public int damage = 1;
    public float knockbackForce = 3f;
    public float maxLifetime = 5f;
    public bool destroyOnImpact = true;
    public LayerMask collisionLayers;

    [Header("Effects")]
    public GameObject impactEffect;
    public AudioClip impactSound;
    public float splatRadius = 1f;

    private Rigidbody2D rb;
    private float creationTime;
    private bool hasHit = false;

    private void Awake()
    {
        // Ensure this object is not a child of the goose when instantiated
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        rb = GetComponent<Rigidbody2D>();

        // Configure rigidbody for falling
        rb.gravityScale = 1.5f;
        rb.drag = 0.5f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Set random initial rotation
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        // Add small random horizontal velocity
        rb.velocity = new Vector2(Random.Range(-1f, 1f), 0);

        // Store creation time
        creationTime = Time.time;
    }

    private void Update()
    {
        if (!hasHit)
        {
            // Add downward force
            rb.AddForce(Vector2.down * fallSpeed, ForceMode2D.Force);

            // Rotate slightly as it falls for more natural motion
            transform.Rotate(0, 0, Time.deltaTime * 50f);

            // Check lifetime
            if (Time.time > creationTime + maxLifetime)
            {
                // Make sure we're not child of anything before self-destructing
                if (transform.parent != null)
                {
                    transform.SetParent(null);
                }
                //Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // Check if we hit the player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Get player health component
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Deal damage
                playerHealth.TakeDamage(damage);

                // Apply knockback to player
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDirection = Vector2.up; // Mainly upward
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }

        // Handle impact
        OnHit(collision.contacts[0].point);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Alternative version for trigger colliders
        if (other.CompareTag("Player"))
        {
            // Deal damage
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);

                // Apply knockback
                Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    Vector2 knockbackDirection = Vector2.up; // Mainly upward
                    playerRb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
                }
            }
        }

        // Handle impact
        OnHit(transform.position);
    }

    private void OnHit(Vector2 hitPosition)
    {
        hasHit = true;

        // Make sure we're not a child of any other object to prevent parent destruction
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        // Stop movement
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        // Disable collider
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }

        // Spawn impact effect
        if (impactEffect != null)
        {
            Instantiate(impactEffect, hitPosition, Quaternion.identity);
        }

        // Play impact sound
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, hitPosition);
        }

        // Create splat effect (flattening the poop)
        transform.localScale = new Vector3(splatRadius, 0.2f, 1f);

        // Either destroy or leave a splat
        if (destroyOnImpact)
        {
            Destroy(gameObject, 0.2f); // Small delay to show impact
        }
        else
        {
            // Leave the splat for some time then destroy
            Destroy(gameObject, 5f);
        }
    }
}