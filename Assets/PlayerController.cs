using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] private Transform feetTransform;
    [SerializeField] private Vector2 groundCheck;

    [SerializeField] private LayerMask groundLayer;


    [SerializeField] private float jumpStrength = 7f;
    [SerializeField] private float extraGravity = 700f;
    [SerializeField] private float gravityDelay = .2f;
    [SerializeField] private float coyoteTime = .4f;

    Rigidbody2D rb;
    float moveX;


    float timeInAir;
    float timeInCoyoteTime;
    bool doubleJumpAvailable;
    public bool isGrounded;

    private void Start()
    {
        rb = this.GetComponent<Rigidbody2D>();
    }


    private void Update()
    {
        GatherInput();
        HandleJump();
        HandleSpriteFlip();
        Move();
        GravityDelay();
        CoyoteTimer();
        isGrounded = IsGrounded();

    }

    private void GatherInput()
    {
        moveX = Input.GetAxis("Horizontal");
    }

    private void FixedUpdate()
    {
        ExtraGravity();
    }


    void Move()
    {
        Vector2 movement = new Vector2(moveX * moveSpeed, rb.velocity.y);
        rb.velocity = movement;


    }

    void ApplyJumpForce()
    {
        timeInAir = 0;
        timeInCoyoteTime = 0f;
        rb.velocity = Vector3.zero;
        rb.AddForce(Vector2.up * jumpStrength, ForceMode2D.Impulse);
    }

    private void HandleSpriteFlip()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (mousePosition.x < transform.position.x)
        {
            transform.eulerAngles = new Vector3(0f, -180f, 0f);
        }
        else
        {
            transform.eulerAngles = new Vector3(0f, 0f, 0f);
        }
    }

    public bool IsFacingRight()
    {
        return transform.eulerAngles.y == 0;
    }


    void ExtraGravity() 
    { 
    
        if (timeInAir > gravityDelay)
        {
            rb.AddForce(new Vector2(0f, -extraGravity * Time.deltaTime));
        }
    }


    void GravityDelay()
    {
        if (!IsGrounded())
        {
            timeInAir += Time.deltaTime;
        }
        else
        {
            timeInAir = 0;
        }
    }


    public bool IsGrounded()
    {
        
        Collider2D isGrounded = Physics2D.OverlapBox(feetTransform.position, groundCheck, 0f, groundLayer);
        return isGrounded;
    }


    private void HandleJump()
    {
        //if (!frameInput.Jump)
        //    return;
        if(!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }  
        if (IsGrounded())
        {
            ApplyJumpForce();
        }
        else if (timeInCoyoteTime > 0f)
        {
            doubleJumpAvailable = true;
            ApplyJumpForce();

        }
        else if (doubleJumpAvailable)
        {
            doubleJumpAvailable = false;
             ApplyJumpForce();
        }



    }

    void CoyoteTimer()
    {
        if (IsGrounded())
        {
            doubleJumpAvailable = true;
            timeInCoyoteTime = coyoteTime;
        }
        else
        {
            timeInCoyoteTime -= Time.deltaTime;
        }
    }



}
