using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI; // Reference to the pause menu UI GameObject
    private bool isPaused = false; // Flag to track whether the game is paused

    [SerializeField] GameObject gameoverPanel;


    public static EventHandler OnRestart;

    private void OnEnable()
    {
        GetToTheChawper.OnPlayerRescue += OpenGameOverPanel;
    }

    private void Start()
    {
        //pauseMenuUI.transform.DOScale(0, .01f);
        pauseMenuUI.transform.localScale = Vector3.zero;
        pauseMenuUI.SetActive(false);
        isPaused = false;
    }
    void Update()
    {


        if (Input.GetKeyDown(KeyCode.Escape) /*&& !GameManager.Instance.GameOver*/)
        {
            if (isPaused)
            {
                Resume(); // If the game is already paused, resume it
            }
            else
            {
                Pause(); // If the game is not paused, pause it
            }
        }
    }

    public void Pause()
    {

        Time.timeScale = 0f; // Pause the game by setting time scale to 0
        isPaused = true;
        pauseMenuUI.SetActive(true); // Activate the pause menu UI
        
        AnimateTextBoxOpen();
        //AudioManager.Instance.PlayPauseClick();
    }

    public void Resume()
    {
        Time.timeScale = 1f; // Resume the game by setting time scale to 1
        isPaused = false;
        AnimateTextBoxClose();
         // Deactivate the pause menu UI
        //AudioManager.Instance.PlayResumeClick();
    }

    public void RestartScene()
    {
        CloseHighScoreWindow();
        CloseGameOverPanel();
        //GameManager.Instance.ResetGame();
        Time.timeScale = 1f; // Ensure time scale is set to 1
        OnRestart?.Invoke(this, EventArgs.Empty);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); // Restart the current scene


    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Load the MainMenu scene
        Time.timeScale = 1f; // Ensure time scale is set to 1
    }

    public void OpenHighScoreWindow()
    {
        //HighscoreTable.Instance.RefreshAndLoadHighscoreList();
    }

    public void CloseHighScoreWindow()
    {
        //HighscoreTable.Instance.CloseVisual();
    }

    public void OpenGameOverPanel()
    {
        AnimateTextBoxOpen();
        gameoverPanel.SetActive(true);
    }
    public void CloseGameOverPanel()
    {
        gameoverPanel.SetActive(false);
    }


    void AnimateTextBoxOpen()
    {

        
        Debug.Log("open");
        //pauseMenuUI.transform.localScale = new Vector3(1,1,1);

        pauseMenuUI.transform.DOScale(1, .25f).SetUpdate(true);




    }

    void AnimateTextBoxClose()
    {
        Debug.Log("close");
        pauseMenuUI.transform.DOScale(0, .23f).OnComplete(() => {
            pauseMenuUI.SetActive(false);
        });
    }
}