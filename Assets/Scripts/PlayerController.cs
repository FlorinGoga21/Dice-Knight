using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private bool isGrounded;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        CheckGround();
        HandleJump();

        animator.SetFloat("Speed", Mathf.Abs(moveInput.x));
        animator.SetBool("Grounded", isGrounded);
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void LateUpdate()
    {
        KeepPlayerOnScreen();
    }

    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(
            moveInput.x * moveSpeed,
            rb.linearVelocity.y
        );

        if (moveInput.x != 0)
        {
            transform.localScale = new Vector3(
                Mathf.Sign(moveInput.x),
                1f,
                1f
            );
        }
    }

    private void HandleJump()
    {
        if (inputActions.Player.Jump.WasPressedThisFrame() && isGrounded)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                jumpForce
            );
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    private void KeepPlayerOnScreen()
    {
    Vector3 position = transform.position;

    float screenLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
    float screenRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

    float playerHalfWidth = GetComponent<Collider2D>().bounds.extents.x;

    position.x = Mathf.Clamp(
        position.x,
        screenLeft + playerHalfWidth,
        screenRight - playerHalfWidth
    );

    transform.position = position;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.DrawWireSphere(
            groundCheck.position,
            groundCheckRadius
        );
    }
}