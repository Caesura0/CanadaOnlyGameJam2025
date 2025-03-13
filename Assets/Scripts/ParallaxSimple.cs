using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxSimple : MonoBehaviour
{
    [SerializeField] private float parallaxOffset = -0.1f;

    private Vector2 startPos;
    private Camera mainCamera;
    private Vector2 travel => (Vector2)mainCamera.transform.position - startPos;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        startPos = transform.position;
    }

    private void FixedUpdate()
    {

        //Vector2 newPosition = startPos + new Vector2(travel.x * parallaxOffset, 0f);
        //transform.position = new Vector2(newPosition.x, transform.position.y);
    }


}
