using UnityEngine;
using DG.Tweening;
using System;

public class GetToTheChawper : MonoBehaviour
{
    [SerializeField] GameObject helicopterPrefab; // Assign in Inspector
    [SerializeField] float aboveCameraOffset = 5f; // Offset above camera
    [SerializeField] float descendSpeed = 2f;
    [SerializeField] float ascendSpeed = 2f;
    [SerializeField] float hoverDuration = 1f; // Time before ascending
    [SerializeField] Transform ladderGO;

    private Transform player;
    private Camera mainCamera;

    public static event Action OnPlayerRescue;

    void Start()
    {
        mainCamera = Camera.main;
        PlayerHealth.OnPlayerDeath += HandlePlayerDeath;
    }

    private void HandlePlayerDeath(object sender, EventArgs e)
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 spawnPos = new Vector3(player.position.x, mainCamera.transform.position.y + aboveCameraOffset, 0);

        helicopterPrefab.transform.position = spawnPos;
        helicopterPrefab.SetActive(true);

        // Move helicopter down to the player
        helicopterPrefab.transform.DOMoveY(player.position.y + 1f, descendSpeed).SetEase(Ease.Linear).OnComplete(() =>
        {
            // Attach player to the helicopter
            player.SetParent(ladderGO);
            player.transform.position = ladderGO.position;
            player.gameObject.GetComponent<Collider2D>().enabled = false;
            player.gameObject.GetComponent<Rigidbody2D>().isKinematic = true;



            // Wait a bit, then ascend
            DOVirtual.DelayedCall(hoverDuration, () =>
            {
                helicopterPrefab.transform.DOMoveY(mainCamera.transform.position.y + aboveCameraOffset + 5f, ascendSpeed)
                    .SetEase(Ease.Linear)
                    .OnComplete(() =>
                    {
                        helicopterPrefab.SetActive(false);
                        OnPlayerRescue?.Invoke();
                    });
            });
        });

    }

    void OnDestroy()
    {
        PlayerHealth.OnPlayerDeath -= HandlePlayerDeath;
    }


}