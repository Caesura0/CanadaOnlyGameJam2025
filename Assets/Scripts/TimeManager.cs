using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;

    float time = 0f;

    private void Update()
    {
        time += Time.deltaTime;
        timeText.text = "Time: " + time.ToString("F2");
    }

}
