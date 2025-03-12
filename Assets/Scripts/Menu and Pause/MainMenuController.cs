using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject optionsWindow;
    [SerializeField] private Image fadePanel; // Assign this in the Inspector

    private void Start()
    {
        AudioManager.Instance.PlayMainMenuMusic();
        fadePanel.gameObject.SetActive(true);
        fadePanel.color = new Color(0, 0, 0, 1); // Ensure it's fully black at the start

        // Fade in when the game starts
        fadePanel.DOFade(0, 1f).SetEase(Ease.InOutQuad);
    }

    public void StartGame()
    {
        // Disable interaction during transition
        fadePanel.raycastTarget = true;

        // Play fade-out animation
        fadePanel.DOFade(1, 1f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            Loader.Load(Loader.Scene.Gameplay);
        });

        //AudioManager.Instance.PlayButtonClick();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }

    public void OpenOptionsWindow()
    {
        optionsWindow.SetActive(true);
    }
}