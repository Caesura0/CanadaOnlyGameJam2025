using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetToTheChawper : MonoBehaviour
{
    PlayerHealth playerHealth;
    void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
    }

    private void Update()
    {
        MoveToPlayer();
    }

    private void MoveToPlayer()
    {

        if (playerHealth != null)
        {
            Vector3 playerPosition = playerHealth.transform.position;
            Vector3 direction = playerPosition - transform.position;
            transform.position += direction.normalized * Time.deltaTime;
        }
    }
}
