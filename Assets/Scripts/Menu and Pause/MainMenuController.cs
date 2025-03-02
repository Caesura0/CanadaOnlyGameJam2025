using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] GameObject optionsWindow;


    private void Start()
    {
        AudioManager.Instance.PlayMainMenuMusic();
    }
    public void StartGame()
    {
        // Load the "Gameplay" scene
        Loader.Load(Loader.Scene.Gameplay);
        //AudioManager.Instance.PlayButtonClick();
    }



    public void QuitGame()
    {
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        //AudioManager.Instance.PlayButtonClick();
        Application.Quit();
    }


    public void OpenOptionsWindow()
    {
        //AudioManager.Instance.PlayButtonClick();
        optionsWindow.SetActive(true);
    }


}
