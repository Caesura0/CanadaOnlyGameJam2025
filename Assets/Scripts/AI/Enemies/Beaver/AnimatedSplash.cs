using UnityEngine;
using System.Collections;

public class AnimatedSplash : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private AnimationCurve scaleCurve;
    [SerializeField] private AnimationCurve alphaCurve;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private bool randomizeRotation = true;
    [SerializeField] private bool spawnDroplets = true;

    [Header("Droplet Settings")]
    [SerializeField] private int dropletCount = 3;
    [SerializeField] private float dropletSpeed = 3f;
    [SerializeField] private float dropletSize = 0.3f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalScale = transform.localScale;

            // Default curves if not set in inspector
            if (scaleCurve.length == 0)
            {
                // Start small, quickly grow, then slowly shrink
                scaleCurve = new AnimationCurve(
                    new Keyframe(0f, 0.1f),
                    new Keyframe(0.2f, maxScale),
                    new Keyframe(1f, maxScale * 0.7f)
                );
            }

            if (alphaCurve.length == 0)
            {
                // Start fully visible, then fade out toward the end
                alphaCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.7f, 0.9f),
                    new Keyframe(1f, 0f)
                );
            }

            // Randomize rotation
            if (randomizeRotation)
            {
                float randomAngle = Random.Range(-15f, 15f);
                transform.rotation = Quaternion.Euler(0, 0, randomAngle);
            }

            // Spawn droplets
            if (spawnDroplets)
            {
                SpawnDroplets();
            }

            // Start animation
            StartCoroutine(AnimateSplash());
        }
    }

    private IEnumerator AnimateSplash()
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime = Time.time - startTime;
            float t = elapsedTime / duration;

            // Apply scale
            float scaleMultiplier = scaleCurve.Evaluate(t);
            transform.localScale = originalScale * scaleMultiplier;

            // Apply alpha
            Color newColor = originalColor;
            newColor.a = alphaCurve.Evaluate(t);
            spriteRenderer.color = newColor;

            yield return null;
        }

        // Destroy after animation completes
        Destroy(gameObject);
    }

    private void SpawnDroplets()
    {
        // Only spawn droplets if we have a sprite
        if (spriteRenderer.sprite == null)
            return;

        for (int i = 0; i < dropletCount; i++)
        {
            // Create a small copy of the sprite as a droplet
            GameObject droplet = new GameObject("Droplet_" + i);
            droplet.transform.position = transform.position;

            SpriteRenderer dropletRenderer = droplet.AddComponent<SpriteRenderer>();
            dropletRenderer.sprite = spriteRenderer.sprite;
            dropletRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

            // Make it smaller
            droplet.transform.localScale = originalScale * dropletSize;

            // Give it random upward velocity
            Rigidbody2D rb = droplet.AddComponent<Rigidbody2D>();
            float randomAngle = Random.Range(-30f, 30f);
            Vector2 randomDirection = Quaternion.Euler(0, 0, randomAngle) * Vector2.up;
            float randomSpeed = Random.Range(dropletSpeed * 0.8f, dropletSpeed * 1.2f);

            rb.velocity = randomDirection * randomSpeed;
            rb.gravityScale = 1.5f;

            // Add a script to fade it out
            FadingDroplet fadingScript = droplet.AddComponent<FadingDroplet>();
            fadingScript.Initialize(1.0f, dropletRenderer);
        }
    }
}

// Helper class for droplets
public class FadingDroplet : MonoBehaviour
{
    private float lifetime;
    private SpriteRenderer renderer;
    private float startTime;

    public void Initialize(float life, SpriteRenderer spriteRenderer)
    {
        lifetime = life;
        renderer = spriteRenderer;
        startTime = Time.time;
    }

    private void Update()
    {
        float elapsed = Time.time - startTime;
        float normalizedTime = elapsed / lifetime;

        if (normalizedTime >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        if (renderer != null)
        {
            Color color = renderer.color;
            // Start fading out at halfway point
            if (normalizedTime > 0.5f)
            {
                color.a = 1f - ((normalizedTime - 0.5f) * 2f);
            }
            renderer.color = color;
        }
    }
}