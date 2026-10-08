using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem movementDust;
    [SerializeField] private ParticleSystem landingDust;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Camera Follow")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float cameraFollowOffset;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerInputActions inputActions;

    private float moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool wasGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = animator != null ? animator : GetComponent<Animator>();
        playerCamera = playerCamera != null ? playerCamera : Camera.main;
        inputActions = new PlayerInputActions();
        UpdateGroundedState();
        wasGrounded = isGrounded;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();
    private void OnDestroy() => inputActions.Dispose();

    private void Update()
    {
        HandleInput();
        HandleRotation();
        UpdateGroundedState();
        HandleJump();
        HandleAnimation();
        HandleParticles();
    }

    private void FixedUpdate()
    {
        HandleMovement();
        HandleCameraFollow();
    }

    private void HandleInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>().x;
    }

    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void HandleCameraFollow()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 targetPosition = playerCamera.transform.position;
        targetPosition.x = transform.position.x + cameraFollowOffset;
        targetPosition.y = playerCamera.transform.position.y;
        targetPosition.z = playerCamera.transform.position.z;

        playerCamera.transform.position = Vector3.Lerp(
            playerCamera.transform.position,
            targetPosition,
            cameraFollowSpeed * Time.deltaTime
        );
    }

    private void HandleRotation()
    {
        if (Mathf.Approximately(moveInput, 0f))
        {
            return;
        }

        if ((moveInput > 0f && isFacingRight) || (moveInput < 0f && !isFacingRight))
        {
            return;
        }

        transform.Rotate(0f, 180f, 0f);
        isFacingRight = moveInput > 0f;
    }

    private void HandleJump()
    {
        if (!inputActions.Player.Jump.WasPressedThisFrame() || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void HandleAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("Grounded", isGrounded);
    }

    private void HandleParticles()
    {
        HandleMovementDust();
        HandleLandingDust();

        wasGrounded = isGrounded;
    }

    private void HandleMovementDust()
    {
        bool isMoving = !Mathf.Approximately(moveInput, 0f);

        if (movementDust == null)
        {
            return;
        }

        if (isGrounded && isMoving)
        {
            if (!movementDust.isPlaying)
            {
                movementDust.Play();
            }

            return;
        }

        if (movementDust.isPlaying)
        {
            movementDust.Stop(
                withChildren: true,
                stopBehavior: ParticleSystemStopBehavior.StopEmitting
            );
        }
    }

    private void HandleLandingDust()
    {
        if (landingDust != null && !wasGrounded && isGrounded)
        {
            landingDust.Play();
        }
    }

    private void UpdateGroundedState()
    {
        isGrounded = IsGrounded();
    }

    private bool IsGrounded()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);

        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }
}