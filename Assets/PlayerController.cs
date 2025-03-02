using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    //InputControls
    float moveX;

    //parameters
    [SerializeField] float moveSpeed = 10f;
    [SerializeField] float jumpStrength = 7f;
    [SerializeField] float extraGravity = 700f;
    [SerializeField] float gravityDelay = .2f;
    [SerializeField] float coyoteTime = .4f;
    [SerializeField] float holdThreshold = 0.8f;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] LayerMask platformLayer;




    //References
    [SerializeField] private Transform feetTransform;
    [SerializeField] private Vector2 groundCheck;
    CapsuleCollider2D playerCollider;
    Rigidbody2D rb;
    Gun gun;




    //State
    float timeInAir;
    float timeInCoyoteTime;
    bool doubleJumpAvailable;
    public bool isGrounded;
    bool isHoldingDown = false;
    float holdTime = 0f;


    private void Start()
    {
        playerCollider = GetComponent<CapsuleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        gun = GetComponentInChildren<Gun>();
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

        if (Input.GetKey(KeyCode.S))
        {
            holdTime += Time.deltaTime;
            if (!isHoldingDown && holdTime >= holdThreshold)
            {
                isHoldingDown = true;
                StartCoroutine(DisableCollision());
            }
        }
        else
        {
            holdTime = 0f;
            isHoldingDown = false;
        }
    

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

    IEnumerator DisableCollision()
    {
        Collider2D platform = Physics2D.OverlapCircle( feetTransform.position, 1f, platformLayer);
        if (platform != null)
        {
            Physics2D.IgnoreCollision(playerCollider, platform, true);
            yield return new WaitForSeconds(1f);
            Physics2D.IgnoreCollision(playerCollider, platform, false);
        }
    }



}
