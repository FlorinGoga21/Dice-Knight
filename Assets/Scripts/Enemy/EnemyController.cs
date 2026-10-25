using System.Collections;
using UnityEngine;

/// <summary>
/// Controls enemy movement, targeting AI, attack animations, damage flashing,
/// and turn-based combat actions. Works in tandem with EnemyStats for health and rewards.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public partial class EnemyController : MonoBehaviour, IDamageable
{
    [Header("---Stats---")]
    [SerializeField] private EnemyStats stats;

    [Header("---Target---")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionRange = 12f;

    [Header("---Movement---")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("---Attack---")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackHitDelay = 0.25f;

    [Header("---Dice---")]
    [Tooltip("Fallback when no player is available. In combat, enemies match the player's available dice.")]
    [SerializeField, Range(1, 3)] private int diceCount = 1;

    /// <summary>
    /// Matches the player's available dice, including dice gained after this enemy spawned.
    /// </summary>
    public int DiceCount
    {
        get
        {
            if (targetCombat == null) ResolveTarget();
            return targetCombat != null ? targetCombat.PlayerDiceCount : Mathf.Max(1, diceCount);
        }
    }

    [Header("---Ground Check---")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("---Animation & Visuals---")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visuals;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("---Death---")]
    [SerializeField] private float deathAnimationTimeout = 5f;

    [Header("---Damage Flash---")]
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("---Damage Shake---")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float damageShakeDuration = 0.2f;
    [SerializeField] private float damageShakeMagnitude = 0.1f;

    private Rigidbody2D rb;
    private Collider2D col;
    private IDamageable targetDamageable;
    private CombatController targetCombat;
    private EnemyProjectileAttack projectileAttack;
    private SpriteRenderer[] spriteRenderers;
    private CharacterAudio characterAudio;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private Coroutine attackDamageCoroutine;
    private Coroutine damageShakeCoroutine;

    private float attackCooldownRemaining;
    private bool isGrounded;
    private bool hasDied;
    private bool turnBasedCombat;

    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    public bool IsDead => (stats != null && stats.IsDead) || hasDied;
    public EnemyStats Stats => stats;
    public bool IsAttackInProgress => projectileAttack != null && projectileAttack.IsAttacking;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        characterAudio = GetComponent<CharacterAudio>();
        col = GetComponent<Collider2D>();
        projectileAttack = GetComponent<EnemyProjectileAttack>();
        animator = animator != null ? animator : GetComponent<Animator>();
        visuals = visuals != null ? visuals : transform;
        playerCamera = playerCamera != null ? playerCamera : Camera.main;

        if (stats == null)
        {
            stats = GetComponent<EnemyStats>();
            if (stats == null)
            {
                stats = gameObject.AddComponent<EnemyStats>();
            }
        }

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetupDamageFlashMaterials();
    }

    private void OnEnable()
    {
        ResolveTarget();
    }

    private void ResolveTarget()
    {
        if (target == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            target = player != null ? player.transform : null;
        }

        targetDamageable = target != null ? target.GetComponentInParent<IDamageable>() : null;
        targetCombat = target != null ? target.GetComponentInParent<CombatController>() : null;

        // Prevent target damageable from matching this enemy's own damageable
        if (ReferenceEquals(targetDamageable, this) || (stats != null && ReferenceEquals(targetDamageable, stats)))
        {
            target = null;
            targetDamageable = null;
            targetCombat = null;
        }
    }

    private void Update()
    {
        if (hasDied)
        {
            return;
        }

        if (target == null || targetCombat == null) ResolveTarget();
        if (target != null) FaceTarget(target.position.x - transform.position.x);

        if (turnBasedCombat || (targetCombat != null && targetCombat.IsInCombat))
        {
            rb.linearVelocity = Vector2.zero;
            HandleAnimation();
            return;
        }

        UpdateGroundedState();
        UpdateAttackCooldown();
        HandleTarget();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (hasDied || turnBasedCombat || (targetCombat != null && targetCombat.IsInCombat))
        {
            return;
        }

        HandleMovement();
    }

    private void OnDestroy()
    {
        CleanupDamageFlashMaterials();
    }

    /// <summary>
    /// Forwards damage taking to EnemyStats.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (stats != null)
        {
            stats.TakeDamage(damage);
        }
    }

    public void SetTurnBasedCombat(bool enabled, CombatController opponent = null)
    {
        turnBasedCombat = enabled;
        if (enabled)
        {
            if (opponent != null) target = opponent.transform;
            ResolveTarget();
            rb.linearVelocity = Vector2.zero;
            if (target != null) FaceTarget(target.position.x - transform.position.x);
        }
    }

    public void PerformTurnAttack(int damageMultiplier = 1, bool applyDamage = true)
    {
        if (hasDied)
        {
            return;
        }

        characterAudio?.Attack();
        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        ResolveAttack(attackDamage * Mathf.Max(1, damageMultiplier), applyDamage);
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

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        if (hasDied || turnBasedCombat || (targetCombat != null && targetCombat.IsInCombat)
            || target == null || targetDamageable == null || targetDamageable.IsDead)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
        if (horizontalDistance <= attackRange)
        {
            ResolveAttack(attackDamage, applyDamage: true);
        }

        attackDamageCoroutine = null;
    }

    private void ResolveAttack(int damage, bool applyDamage)
    {
        if (projectileAttack != null)
            projectileAttack.Fire(target, targetDamageable, damage, applyDamage);
        else if (applyDamage && targetDamageable != null && !targetDamageable.IsDead)
            targetDamageable.TakeDamage(damage);
    }

    public bool IsWithinDetectionRange(Transform targetTransform)
    {
        return targetTransform != null
            && !hasDied
            && Mathf.Abs(targetTransform.position.x - transform.position.x) <= detectionRange;
    }

    private void HandleTarget()
    {
        if (target == null || (targetDamageable != null && targetDamageable.IsDead))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float horizontalDistance = target.position.x - transform.position.x;
        float absoluteDistance = Mathf.Abs(horizontalDistance);

        if (absoluteDistance > detectionRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (absoluteDistance <= attackRange)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            PlayAttackAnimation();
        }
    }

    private void HandleMovement()
    {
        if (target == null || (targetDamageable != null && targetDamageable.IsDead))
        {
            return;
        }

        float horizontalDistance = target.position.x - transform.position.x;
        float absoluteDistance = Mathf.Abs(horizontalDistance);
        bool shouldMove = absoluteDistance > Mathf.Max(stoppingDistance, attackRange)
            && absoluteDistance <= detectionRange;

        float horizontalVelocity = shouldMove ? Mathf.Sign(horizontalDistance) * moveSpeed : 0f;
        rb.linearVelocity = new Vector2(horizontalVelocity, rb.linearVelocity.y);
    }

    private void FaceTarget(float horizontalDistance)
    {
        if (Mathf.Approximately(horizontalDistance, 0f))
        {
            return;
        }

        bool shouldFaceRight = transform.InverseTransformVector(Vector3.right * horizontalDistance).x > 0f;
        float facingSign = shouldFaceRight == spriteFacesRight ? 1f : -1f;
        Vector3 scale = visuals.localScale;
        scale.x = Mathf.Abs(scale.x) * facingSign;
        visuals.localScale = scale;
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining -= Time.deltaTime;
        }
    }

    private void UpdateGroundedState()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    private void HandleAnimation()
    {
        characterAudio?.UpdateFootsteps(!hasDied && !turnBasedCombat && isGrounded && Mathf.Abs(rb.linearVelocity.x) > 0.01f);
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("Grounded", isGrounded);
    }

    /// <summary>
    /// Disables enemy controls, collider, and physics, and begins death animation.
    /// Called by EnemyStats when health reaches 0.
    /// </summary>
    public void Die()
    {
        if (hasDied)
        {
            return;
        }

        hasDied = true;
        characterAudio?.Death();
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;

        if (attackDamageCoroutine != null)
        {
            StopCoroutine(attackDamageCoroutine);
            attackDamageCoroutine = null;
        }

        StartCoroutine(PlayDeathSequence());
    }

    public void PlayProjectileLaunchSound() => characterAudio?.ProjectileLaunch();
    public void PlayProjectileImpactSound() => characterAudio?.ProjectileImpact();

    private IEnumerator PlayDeathSequence()
    {
        if (TryGetComponent<EnemyFlight>(out var flight)) yield return flight.LandForDeath();
        animator.SetBool("hasDied", true);
        yield return HideAfterDeathAnimation();
    }

    private IEnumerator HideAfterDeathAnimation()
    {
        float elapsed = 0f;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die") && elapsed < deathAnimationTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (elapsed < deathAnimationTimeout)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Die") && stateInfo.normalizedTime >= 1f)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        visuals.gameObject.SetActive(false);
    }
}
