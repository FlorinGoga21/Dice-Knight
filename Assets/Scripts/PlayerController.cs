using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("---Movement---")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("---Ground Check---")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("---Particles---")]
    [SerializeField] private ParticleSystem movementDust;
    [SerializeField] private ParticleSystem landingDust;

    [Header("---Animation---")]
    [SerializeField] private Animator animator;

    [Header("---Attack---")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackHitDelay = 0.25f;
    [SerializeField] private float attackCooldown = 0.5f;

    [Header("---Damage Flash---")]
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("---Camera Follow---")]
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
    private bool hasDied;
    private SpriteRenderer[] spriteRenderers;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private Coroutine attackDamageCoroutine;
    private float attackCooldownRemaining;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = animator != null ? animator : GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetupDamageFlashMaterials();
        playerCamera = playerCamera != null ? playerCamera : Camera.main;
        inputActions = new PlayerInputActions();
        UpdateGroundedState();
        wasGrounded = isGrounded;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();
    private void OnDestroy()
    {
        inputActions.Dispose();

        if (damageFlashMaterials == null)
        {
            return;
        }

        foreach (Material material in damageFlashMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    private void Update()
    {
        if (hasDied)
        {
            return;
        }

        HandleInput();
        HandleRotation();
        UpdateGroundedState();
        HandleJump();
        UpdateAttackCooldown();
        HandleAttack();
        HandleAnimation();
        HandleParticles();
    }

    private void FixedUpdate()
    {
        if (hasDied)
        {
            return;
        }

        HandleMovement();
        HandleCameraFollow();
    }

    public void Die()
    {
        if (hasDied)
        {
            return;
        }

        hasDied = true;
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        movementDust?.Stop(
            withChildren: true,
            stopBehavior: ParticleSystemStopBehavior.StopEmitting
        );
        animator.SetBool("hasDied", true);
        StartCoroutine(QuitGame());
    }

    public void FlashWhenDamaged()
    {
        if (hasDied)
        {
            return;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(DamageFlash());
    }

    public void PlayDamageAnimation()
    {
        if (hasDied)
        {
            return;
        }

        animator.ResetTrigger("TakeDamage");
        animator.SetTrigger("TakeDamage");
    }

    public void PlayAttackAnimation()
    {
        if (hasDied || attackCooldownRemaining > 0f)
        {
            return;
        }

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
        attackCooldownRemaining = attackCooldown;
        attackDamageCoroutine = StartCoroutine(ApplyAttackDamage());
    }

    private IEnumerator DamageFlash()
    {
        SetFlashAmount(1f);
        yield return new WaitForSeconds(damageFlashDuration);
        SetFlashAmount(0f);
        damageFlashCoroutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        if (damageFlashMaterials == null)
        {
            return;
        }

        foreach (Material material in damageFlashMaterials)
        {
            material.SetFloat(FlashAmount, amount);
        }
    }

    private void SetupDamageFlashMaterials()
    {
        if (damageFlashMaterial == null)
        {
            return;
        }

        damageFlashMaterials = new Material[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            Material material = new Material(damageFlashMaterial);
            spriteRenderers[i].material = material;
            damageFlashMaterials[i] = material;
        }
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

    private void HandleAttack()
    {
        if (!inputActions.Player.Attack.WasPressedThisFrame())
        {
            return;
        }

        PlayAttackAnimation();
    }

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        if (hasDied)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D hitCollider in hitColliders)
        {
            EnemyController enemy = hitCollider.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
                break;
            }
        }

        attackDamageCoroutine = null;
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining -= Time.deltaTime;
        }
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
    private IEnumerator QuitGame()
    {
        Debug.Log("Quitting game...");
        yield return new WaitForSeconds(3f); // Optional: Wait for a moment before quitting
        if (Application.isEditor)
        {
            UnityEditor.EditorApplication.isPlaying = false; // Stop play mode in the editor
        }
        else
        {
            Application.Quit(); // Quit the application
        }
    }
}