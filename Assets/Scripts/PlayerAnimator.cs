using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    Animator animator;


    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }


    public void SetIsJumping(bool isJumping)
    {
        animator.SetTrigger("jumpTrigger");
    }

    public void SetIsFalling(bool isFalling)
    {
        animator.SetBool("isFalling", isFalling);
    }

    public void SetMovementSpeed(float moveX)
    {
        animator.SetFloat("moveX", moveX);
    }

    public void SetIsGrounded(bool isGrounded)
    {
        animator.SetBool("isGrounded", isGrounded);
    }
}
