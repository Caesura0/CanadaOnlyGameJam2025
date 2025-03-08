using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    [SerializeField] Collider2D boundaryCollider;

    private void Awake()
    {
        SetBoundaryActive(false);
    }
    public void SetBoundaryActive(bool isActive)
    {
        if (boundaryCollider != null)
            boundaryCollider.gameObject.SetActive(isActive);
    }

    public Collider2D GetBoundaryCollider()
    {
        return boundaryCollider;
    }
}
