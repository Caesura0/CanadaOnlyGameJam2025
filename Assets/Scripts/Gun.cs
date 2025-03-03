using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    //input variables
    Vector2 mousePosition;


    //References
    PlayerController playerController;
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] Transform shootPointTransform;

    //Parameters
    [SerializeField] float rateOfFire = 2f;
    [SerializeField] int clipSize = 10;

    [SerializeField] float reloadTime = 1f;


    //State
    bool isReloading = false;
    bool isShooting = false;
    int currentClipAmmo;
    int currentHeldAmmo;

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        currentClipAmmo = clipSize;
        currentHeldAmmo = 20;

    }




    private void Update()
    {
        RotateGun();
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
        }
        if (isShooting || isReloading) return;

        if (Input.GetMouseButtonDown(0))
        {
            StartCoroutine(ShootCoroutine());
        }
    }

    IEnumerator ShootCoroutine()
    {
        Shoot();
        yield return new WaitForSeconds(rateOfFire);
        isShooting = false;
    }

    public void Shoot()
    {
        if(!isReloading && currentClipAmmo > 0)
        {
            isShooting = true;
            currentClipAmmo--;
            GameObject bullet = Instantiate(bulletPrefab, shootPointTransform.position, transform.rotation);
            if (bullet.TryGetComponent(out Bullet bulletComponent))
            {
                bulletComponent.Init();
            }
            
        }
        else if (currentClipAmmo <= 0)
        {
            StartCoroutine(Reload());
        }

        //add object pooling


    }


    IEnumerator Reload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        int ammoNeededToReload = clipSize - currentHeldAmmo;
        int fileReloadAmount = Mathf.Clamp(currentHeldAmmo - ammoNeededToReload, 0, currentHeldAmmo);
        currentHeldAmmo -= fileReloadAmount;
        currentClipAmmo += fileReloadAmount;
        isReloading = false;
    }

    private void RotateGun()
    {
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = playerController.transform.InverseTransformPoint(mousePosition);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// This is called from an ammo pickup
    /// </summary>
    /// <param name="amount"></param>
    public void AddAmmo(int amount)
    {
        currentHeldAmmo += amount;
    }
}
