using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;

    float time = 0f;
    bool isGameOver = false;
    private void Start()
    {
        PlayerHealth.OnPlayerDeath += PlayerHealth_OnPlayerDeath;
    }

    private void PlayerHealth_OnPlayerDeath(object sender, EventArgs e)
    {
        isGameOver = true;
    }

    private void Update()
    {
        if (isGameOver) return;
        time += Time.deltaTime;
        timeText.text = "Time: " + time.ToString("F2");
    }

}
