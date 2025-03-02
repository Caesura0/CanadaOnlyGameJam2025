using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundManager", menuName = "Audio/SoundManager")]

public class SoundManagerSO :  ScriptableObject
{
   

    public AudioClip boulderSound;
    public AudioClip footstepSound;
    public AudioClip hitSound;
    public AudioClip pickupPointsSound;
    public AudioClip pickupBigPointsSound;
    public AudioClip pickupSpeedSound;
    public AudioClip jumpSound;

    [Space]
    [Space]
    [Space]

    public AudioClip buttonClick;
    public AudioClip switchButtonClick;
    public AudioClip closeClick;
    public AudioClip invalidClick;
    public AudioClip pauseClick;
    public AudioClip resumeClick;
    public AudioClip changeVolumeClick;



    [Space]
    [Space]
    [Space]

    //win lose sounds
    public AudioClip loseSound;
    public AudioClip winSound;
    public AudioClip lowTimeAlertSound;
    public AudioClip addTimeSound;

    [Space]
    [Space]
    [Space]

    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    public AudioClip gameEndMusic;
    public AudioClip scoreScreenMusic;




}
