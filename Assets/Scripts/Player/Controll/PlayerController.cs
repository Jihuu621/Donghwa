using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("이동")]
    public float moveSpeed = 8f;                // 최대 속도
    public float acceleration = 60f;
    public float deceleration = 60f;
    public float velPower = 0.9f;

    [Header("점프")]
    public float jumpForce = 18f;
    public float jumpCutMultiplier = 0.5f;
    public int extraJumps = 0;

    [Header("점프 보정")]
    public float gravityScale = 3f;
    public float fallGravityMultiplier = 2.5f;

    [Header("땅 & 벽 체크")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.08f;
    public LayerMask groundLayer;

    public Transform wallCheck;
    public float wallCheckDistance = 0.2f;

    [Header("코요티 & 버퍼")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;

    [Header("벽 미끄러짐/점프")]
    public float wallSlideSpeed = 2.5f;
    public Vector2 wallJumpVelocity = new Vector2(12f, 18f);
    public float wallJumpDuration = 0.18f;

    [Header("대시 (옵션)")]
    public bool allowDash = false;
    public float dashSpeed = 20f;
    public float dashDuration = 0.14f;
    public float dashCooldown = 0.6f;

    // 내부 변수들
    Rigidbody2D rb;
    float horizontal;
    bool facingRight = true;

    // 땅/벽 상태
    bool isGrounded;
    bool isTouchingWall;
    int wallDir; // -1 = 왼쪽, 1 = 오른쪽

    // 점프 상태
    float coyoteTimer;
    float jumpBufferTimer;
    int jumpsRemaining;

    // 벽 점프 상태
    bool isWallSliding;
    float wallJumpTimer;

    // 대시 상태
    bool isDashing;
    float dashTimer;
    float dashCooldownTimer;

    // 넉백 상태
    float knockbackTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        jumpsRemaining = extraJumps;
    }

    void Update()
    {
        // --- 입력 (구 Input System) ---
        horizontal = Input.GetAxisRaw("Horizontal"); // -1,0,1

        // 넉백 중이면 입력 무시 (Update에서도 막아두기)
        if (knockbackTimer > 0f)
        {
            // 입력 차단
            horizontal = 0f;
            knockbackTimer -= Time.deltaTime;
        }

        // 점프 입력
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (Input.GetButtonUp("Jump"))
        {
            // 가변 점프: 점프 중간에 떼면 속도 줄이기
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }
        }

        // 대시 입력
        if (allowDash && Input.GetButtonDown("Fire3") && dashCooldownTimer <= 0f && !isDashing)
        {
            StartDash();
        }

        // 타이머
        if (isGrounded) coyoteTimer = coyoteTime; else coyoteTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        PlayerParry parry = GetComponent<PlayerParry>();
        if (parry != null && parry.IsStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        CheckSurroundings();

        if (isDashing)
        {
            HandleDash();
            return;
        }

        if (wallJumpTimer > 0f)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
        }

        // 넉백 중이면 horizontal을 0으로 만들어 컨트롤 잠금
        if (knockbackTimer > 0f)
        {
            horizontal = 0f;
        }

        HandleMovement();
        HandleJumping();
        HandleWallSlide();

        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.fixedDeltaTime;
    }

    void CheckSurroundings()
    {
        // 땅 체크
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 벽 체크 (좌우 레이캐스트)
        RaycastHit2D leftHit = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, groundLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, groundLayer);
        isTouchingWall = leftHit.collider != null || rightHit.collider != null;
        if (leftHit.collider != null) wallDir = -1; else if (rightHit.collider != null) wallDir = 1; else wallDir = 0;
    }

    void HandleMovement()
    {
        // 벽 점프 후 잠시 동안 좌우 입력 무시
        if (wallJumpTimer > 0f) horizontal = 0f;

        float targetSpeed = horizontal * moveSpeed;
        float speedDiff = targetSpeed - rb.linearVelocity.x;

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velPower) * Mathf.Sign(speedDiff);

        rb.AddForce(new Vector2(movement, 0f));

        // 속도 제한
        if (Mathf.Abs(rb.linearVelocity.x) > moveSpeed && Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(targetSpeed))
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(rb.linearVelocity.x) * moveSpeed, rb.linearVelocity.y);
        }

        if (knockbackTimer <= 0f)
        {
            if (horizontal > 0.1f && !facingRight) Flip();
            else if (horizontal < -0.1f && facingRight) Flip();
        }
    }

    void HandleJumping()
    {
        // 점프 버퍼 & 코요티 타임
        if (jumpBufferTimer > 0f)
        {
            if (coyoteTimer > 0f || jumpsRemaining > 0 || isWallSliding)
            {
                if (isWallSliding)
                {
                    // 벽 점프: 벽 반대 방향으로 튀기기
                    rb.linearVelocity = new Vector2(-wallDir * wallJumpVelocity.x, wallJumpVelocity.y);
                    wallJumpTimer = wallJumpDuration;
                    isWallSliding = false;
                }
                else
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                    if (!isGrounded && jumpsRemaining > 0)
                    {
                        jumpsRemaining--;
                    }
                }

                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }
        }

        // 착지 시 추가 점프 초기화
        if (isGrounded && !Input.GetButton("Jump"))
        {
            jumpsRemaining = extraJumps;
        }

        // 낙하 중 중력 증가
        if (rb.linearVelocity.y < 0f)
        {
            rb.gravityScale = gravityScale * fallGravityMultiplier;
        }
        else
        {
            rb.gravityScale = gravityScale;
        }
    }

    void HandleWallSlide()
    {
        isWallSliding = false;

        if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f)
        {
            isWallSliding = true;
            // 낙하 속도 제한
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // 대시 중에는 중력 제거
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2((facingRight ? 1f : -1f) * dashSpeed, 0f);
    }

    void HandleDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
        {
            isDashing = false;
            rb.gravityScale = gravityScale;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x *= -1f;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.left * wallCheckDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * wallCheckDistance);
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = force;
        knockbackTimer = Mathf.Max(knockbackTimer, duration);
    }
    public bool FacingRight => facingRight;
}
