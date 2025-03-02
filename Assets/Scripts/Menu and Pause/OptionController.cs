using UnityEngine;
using UnityEngine.UI;





public class OptionController : MonoBehaviour
{
    [SerializeField] Slider musicVolumeSlider;
    [SerializeField] Slider soundEffectVolumeSlider;

    const string MUSICFLOATNAME = "musicVolume";
    const string SOUNDEFFECTFLOATNAME = "soundEffectVolume";

    AudioManager audioManager;
    private void Awake()
    {
        if (!PlayerPrefs.HasKey(MUSICFLOATNAME))
        {
            PlayerPrefs.SetFloat(MUSICFLOATNAME, 1f);
        }
        if (!PlayerPrefs.HasKey(SOUNDEFFECTFLOATNAME))
        {
            PlayerPrefs.SetFloat(SOUNDEFFECTFLOATNAME, 1f);
        }
        Debug.Log(PlayerPrefs.GetFloat(SOUNDEFFECTFLOATNAME) + "start");


    }
    private void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSICFLOATNAME);
        soundEffectVolumeSlider.value = PlayerPrefs.GetFloat(SOUNDEFFECTFLOATNAME);
        ChangeMusicVolume();
        ChangeSoundEffectVolume();
        //CloseWindow();

    }
    public void ChangeMusicVolume()
    {
        audioManager.SetMusicVolume(MUSICFLOATNAME, musicVolumeSlider.value);
        //need to wait a certain amount of time? maybe no sound
        //AudioManager.Instance.ChangeVolumeClick();
    }
    public void ChangeSoundEffectVolume()
    {
        audioManager.SetSoundEffectVolume(SOUNDEFFECTFLOATNAME, soundEffectVolumeSlider.value);
        PlayerPrefs.SetFloat(SOUNDEFFECTFLOATNAME, soundEffectVolumeSlider.value);
        //need to wait a certain amount of time? maybe no sound
        //AudioManager.Instance.ChangeVolumeClick();
        PlayerPrefs.Save();
        
    }

    public void CloseWindow()
    {
        ChangeMusicVolume();
        ChangeSoundEffectVolume();
        PlayerPrefs.Save();
        gameObject.SetActive(false);
        AudioManager.Instance.PlayCloseClick();
    }

}
