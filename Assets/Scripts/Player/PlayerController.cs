using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public partial class PlayerController : MonoBehaviour
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
    [SerializeField] private float attackMovementThreshold = 0.01f;

    [Header("---Block---")]
    [SerializeField] private Vector3 blockShieldBlockingOffset = new Vector3(-0.4f, 0f, 0f);
    [SerializeField, Min(0.01f)] private float blockShieldMoveDuration = 0.22f;

    [Header("---Damage Flash---")]
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("---Camera Follow---")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float cameraFollowOffset;
    [SerializeField] private float damageShakeDuration = 0.15f;
    [SerializeField] private float damageShakeMagnitude = 0.12f;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerInputActions inputActions;
    private CharacterAudio characterAudio;

    private float moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasDied;
    private SpriteRenderer[] spriteRenderers;
    private SpriteRenderer blockingShieldRenderer;
    private Vector3 blockingShieldRestPosition;
    private Vector3 blockingShieldMoveVelocity;
    private bool blockingPoseActive;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private Coroutine damageShakeCoroutine;
    private Coroutine attackDamageCoroutine;
    private float attackCooldownRemaining;
    private IDamageable ownDamageable;
    private Vector3 damageShakeOffset;
    private CombatController combatController;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    public bool CanStartTurnCombat => !hasDied;
    public int AttackDamage => attackDamage;
    public float AttackHitDelay => attackHitDelay;
    public float AttackCooldown => attackCooldown;
    public bool IsFacingRight => isFacingRight;

    public void RestoreSaveTransform(PlayerSaveData data)
    {
        transform.SetPositionAndRotation(data.position, data.rotation);
        isFacingRight = data.facingRight;
        rb.position = data.position;
        rb.linearVelocity = data.velocity;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        characterAudio = GetComponent<CharacterAudio>();
        col = GetComponent<Collider2D>();
        ownDamageable = GetComponentInParent<IDamageable>();
        combatController = GetComponent<CombatController>();
        animator = animator != null ? animator : GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        CacheBlockingShield();
        SetupDamageFlashMaterials();
        playerCamera = playerCamera != null ? playerCamera : Camera.main;
        UpdateGroundedState();
        wasGrounded = isGrounded;
    }

    private void OnEnable()
    {
        inputActions ??= new PlayerInputActions();
        inputActions.Player.Enable();
    }
    private void OnDisable()
    {
        inputActions?.Player.Disable();

        if (damageShakeCoroutine != null)
        {
            StopCoroutine(damageShakeCoroutine);
            damageShakeCoroutine = null;
        }

        damageShakeOffset = Vector3.zero;
    }
    private void OnDestroy()
    {
        inputActions?.Dispose();

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
        if (Time.timeScale <= 0f || hasDied || (combatController != null && combatController.IsInCombat))
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

    private void LateUpdate()
    {
        if (blockingShieldRenderer != null)
        {
            Vector3 targetPosition = blockingShieldRestPosition
                + (blockingPoseActive ? blockShieldBlockingOffset : Vector3.zero);
            blockingShieldRenderer.transform.localPosition = Vector3.SmoothDamp(
                blockingShieldRenderer.transform.localPosition,
                targetPosition,
                ref blockingShieldMoveVelocity,
                blockShieldMoveDuration,
                Mathf.Infinity,
                Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        if (hasDied)
        {
            return;
        }

        if (combatController == null || !combatController.IsInCombat)
        {
            HandleMovement();
        }

        HandleCameraFollow();
    }

    public void Die()
    {
        if (hasDied)
        {
            return;
        }

        hasDied = true;
        characterAudio?.Death();
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        movementDust?.Stop(
            withChildren: true,
            stopBehavior: ParticleSystemStopBehavior.StopEmitting
        );
        animator.SetBool("hasDied", true);
        StartCoroutine(QuitGame());
    }

    public void PrepareForTurnCombat()
    {
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Grounded", isGrounded);
        movementDust?.Stop(withChildren: true, stopBehavior: ParticleSystemStopBehavior.StopEmitting);
    }

    public void SetBlockingPose(bool blocking)
    {
        blockingPoseActive = blocking && blockingShieldRenderer != null;
    }

    private void CacheBlockingShield()
    {
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            if (renderer == null || renderer.sprite == null)
            {
                continue;
            }

            if (renderer.sprite.name.Contains("Shield"))
            {
                blockingShieldRenderer = renderer;
                blockingShieldRestPosition = renderer.transform.localPosition;
                return;
            }
        }
    }

    public void PlayDamageAnimation()
    {
        if (hasDied)
        {
            return;
        }

        characterAudio?.Hurt();
        animator.ResetTrigger("TakeDamage");
        animator.SetTrigger("TakeDamage");
    }

    public void PlayAttackAnimation()
    {
        if (hasDied || attackCooldownRemaining > 0f)
        {
            return;
        }

        characterAudio?.Attack();
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
        attackCooldownRemaining = attackCooldown;
        attackDamageCoroutine = StartCoroutine(ApplyAttackDamage());
    }

    public void PlayAttackAnimationVisualOnly()
    {
        if (hasDied)
        {
            return;
        }

        characterAudio?.Attack();
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");
    }

    private IEnumerator QuitGame()
    {
        Debug.Log("Quitting game...");
        yield return new WaitForSeconds(3f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
