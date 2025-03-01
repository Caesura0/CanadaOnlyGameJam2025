using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{

    Vector2 mousePosition;

    PlayerController playerController;

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        RotateGun();
    }

    private void RotateGun()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Vector2 direction = _mousePosition - (Vector2)PlayerController.Instance.transform.position;
        Vector2 direction = playerController.transform.InverseTransformPoint(mousePosition);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
