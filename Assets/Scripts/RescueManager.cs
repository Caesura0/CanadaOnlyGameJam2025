using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RescueManager : MonoBehaviour
{

    public class OnRescueArgs : EventArgs
    {
        public int rescued;
        public int totalToRescue;
    }


    int rescued = 0;

    int totalToRescue = 10;

    public static EventHandler<OnRescueArgs> onRescue;


    private void OnEnable()
    {
        EnemyHealth.OnZombieTranqulized += AddRescued;
    }

    private void AddRescued(object sender, EventArgs e)
    {
        rescued++;
        onRescue?.Invoke(this, new OnRescueArgs { rescued = this.rescued, totalToRescue = this.totalToRescue });
        if (rescued >= totalToRescue)
        {
            
            Debug.Log("All rescued!");
        }
    }





}
