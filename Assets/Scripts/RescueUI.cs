using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static RescueManager;

public class RescueUI : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI rescueText;
    private void OnEnable()
    {
        RescueManager.onRescue += ShowRescueUI;
    }

    private void ShowRescueUI(object sender, OnRescueArgs e)
    {
        rescueText.text = $"Rescued {e.rescued}/{e.totalToRescue}";
    }
}
