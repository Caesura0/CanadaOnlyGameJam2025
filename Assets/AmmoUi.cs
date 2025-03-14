using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AmmoUi : MonoBehaviour
{
    Gun gun;

    [SerializeField] TextMeshProUGUI ammoText;


    private void Start()
    {
        gun = FindObjectOfType<Gun>();
    }

    // Update is called once per frame
    void Update()
    {

        ammoText.text = $"Ammo: {gun.currentHeldAmmo} Clip: {gun.currentClipAmmo}";
        

    }
}
