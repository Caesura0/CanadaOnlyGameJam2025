using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFadeTransition : MonoBehaviour
{

    public static SceneFadeTransition Instance { get; private set; }


    [SerializeField] private float fadeDuration = 1.5f;   // Duration for fade out and in
                    private float loadFadeDuration = 2.1f;   // Duration for fade out and in
    [SerializeField] private CanvasGroup fadeCanvasGroup; // Assign a CanvasGroup to control fading


    //[SerializeField] private float threshold = 5f; // The position threshold to trigger the transition

    [SerializeField] Transform playerTransform;
    private bool isTransitioning = false;
    private void Awake()
    {
        Instance = this;
    }
    private void Start()
    {
        playerTransform = transform;
        fadeCanvasGroup.alpha = 1; // Ensure fade is invisible at the start
        StartCoroutine(FadeInSceneLoad(0));

    }

    private void Update()
    {

    }

    public IEnumerator Transition()
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(Fade(1));

        // Move the player to the new position
        //playerTransform.position = targetPosition.position;

        // Fade in
        yield return StartCoroutine(Fade(0));

        isTransitioning = false;
    }

    public IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        if(targetAlpha == 1)
        {
            yield return new WaitForSeconds(.5f);
        }
        
        fadeCanvasGroup.alpha = targetAlpha;
    }

    public IEnumerator FadeInSceneLoad(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < loadFadeDuration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / loadFadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        if (targetAlpha == 1)
        {
            yield return new WaitForSeconds(.5f);
        }

        fadeCanvasGroup.alpha = targetAlpha;
    
    }

    public IEnumerator FadeInSceneGameEnd(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        if (targetAlpha == 1)
        {
            yield return new WaitForSeconds(.5f);
        }

        fadeCanvasGroup.alpha = targetAlpha;
        SceneManager.LoadScene(2);
    }
}

