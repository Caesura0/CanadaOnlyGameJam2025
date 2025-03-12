using UnityEngine;

public class SplashEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f; // Default duration if not using animation events
    [SerializeField] private bool useAnimationEvents = false;

    private Animator animator;
    private float timer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        timer = duration;
    }

    private void Update()
    {
        // If not using animation events, destroy after duration
        if (!useAnimationEvents)
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    // Call this from an animation event at the end of the splash animation if desired
    public void OnAnimationComplete()
    {
        Destroy(gameObject);
    }
}