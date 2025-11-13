using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PhysicalObject : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 15f;
    
    [SerializeField] private float jumpCooldown;
    [SerializeField] float shellRadius = 0.01f;
    [SerializeField] private PhysicsMaterial2D noFrictionMaterial;
    [SerializeField] private PhysicsMaterial2D fullFrictionMaterial;
    private float lastJumpTime = 0;

    private Vector2 moveInput;
    private bool jumpPressed;
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    [SerializeField] private SpriteRenderer wrapSprite;
    private CapsuleCollider2D collider;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isSlope;
    [SerializeField] private Vector2 velocity = Vector2.zero;
    [SerializeField] private Vector2 groundNormal = Vector2.up;
    [SerializeField] private float slopeNormal = 0.4f;

    [SerializeField] private ContactFilter2D contactFilter;
    private RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
    private RaycastHit2D[] hitBuffer2 = new RaycastHit2D[8];
    [SerializeField] private CharacterStatus charStatus;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        collider = GetComponent<CapsuleCollider2D>();
        lastJumpTime = Time.time;
    }

    void Start()
    {
        
        
        
    }

    void FixedUpdate()
    {
        CheckGrounded();
        DynamicMove();
    }

    void DynamicMove()
    {
        // Slope tangent = perpendicular to normal
        float targetSpeed = moveInput.x * moveSpeed;
        float accelRate = Mathf.Abs(targetSpeed) > 0.1f ? acceleration : deceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
        // Project velocity along slope
        Vector2 moveDirection = new Vector2(groundNormal.y, -groundNormal.x);

        if (jumpPressed && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isGrounded = false;
            rb.sharedMaterial = noFrictionMaterial;
        }

        // In the air
        if (!isGrounded)
        {
            rb.velocity = new Vector2(velocity.x, rb.velocity.y);
        }

        // Grounded slope
        if (isGrounded && moveDirection != Vector2.right)
        {
            Vector2 projectedVelocity = velocity.x * moveDirection.normalized;
            rb.velocity = new Vector2(projectedVelocity.x, projectedVelocity.y);
        }

        // Grounded
        if (isGrounded && moveDirection == Vector2.right)
        {
            rb.velocity = new Vector2(velocity.x, rb.velocity.y);
        }
    }

    void CheckGrounded()
    {
        int hits = collider.Cast(Vector2.down, contactFilter, hitBuffer, shellRadius);
        int hits2 = collider.Cast(new Vector2(moveInput.x, 0).normalized, contactFilter, hitBuffer2, shellRadius);
        if (hits > 0)
        {
            for (int i = 0; i < hits; i++)
            {
                RaycastHit2D hit = hitBuffer[i];
                if (hit.normal.y > slopeNormal)
                {
                    if (!isGrounded)
                    {
                        lastJumpTime = Time.time;
                    }

                    isGrounded = true;
                    groundNormal = hit.normal;
                    rb.sharedMaterial = fullFrictionMaterial;
                }
            }
            if (hits2 > 0 && isGrounded)
            {
                for (int i = 0; i < hits2; i++)
                {
                    RaycastHit2D hit = hitBuffer2[i];
                    if (hit.normal.y > slopeNormal)
                    {
                        groundNormal = hit.normal;
                    }
                }
            }
                
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector2.up;
            rb.sharedMaterial = noFrictionMaterial;
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Projectile"))
        {
            
        }
    }

    public void SetInput(Vector2 moveInput, bool jumpPressed)
    {
        this.moveInput = moveInput;
        this.jumpPressed = jumpPressed;
    }

    public void Reset()
    {
        rb.velocity = Vector2.zero;
        moveInput = Vector2.zero;
    }

    public void FlipSprite(float angle)
    {
        bool flip = angle < 90f && angle > -90f;
        sprite.flipX = flip;
        wrapSprite.flipX = flip;
    }

    public void SetWrapColor(Color color)
    {
        wrapSprite.color = color;
    }

    private void OnDrawGizmos()
    {
        if (rb == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(rb.position, rb.position + groundNormal.normalized);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(rb.position, rb.position + rb.velocity);

        Gizmos.color = isGrounded ? Color.yellow : Color.red;
        Vector2 groundCheckPos = (Vector2)collider.bounds.center + Vector2.down * collider.bounds.extents.y;
        Gizmos.DrawRay(groundCheckPos, Vector2.down * shellRadius);
    }
}