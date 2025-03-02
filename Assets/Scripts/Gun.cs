using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{

    Vector2 mousePosition;

    PlayerController playerController;

    [SerializeField] private GameObject bulletPrefab;


    [SerializeField] private Transform shootPointTransform;

    private void Start()                                                                                {
        playerController = FindFirstObjectByType<PlayerController>();
        
                                                                                                        }


    

    private void Update()
    {
        RotateGun();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, shootPointTransform.position, transform.rotation);
        if(bullet.TryGetComponent(out Bullet bulletComponent))
        {
            bulletComponent.Init();
        }

    }


    private void RotateGun()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = playerController.transform.InverseTransformPoint(mousePosition);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
