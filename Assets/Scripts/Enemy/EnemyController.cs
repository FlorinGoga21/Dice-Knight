using System.Collections;
using UnityEngine;

/// <summary>
/// Controls enemy movement, targeting AI, attack animations, damage flashing,
/// and turn-based combat actions. Works in tandem with EnemyStats for health and rewards.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyController : MonoBehaviour, IDamageable
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
    [SerializeField, Range(1, 3)] private int diceCount = 1;

    /// <summary>
    /// Number of dice this enemy rolls during turn-based combat. Editable in the Inspector.
    /// </summary>
    public int DiceCount => diceCount;

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
    private SpriteRenderer[] spriteRenderers;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private Coroutine attackDamageCoroutine;
    private Coroutine damageShakeCoroutine;

    private float attackCooldownRemaining;
    private bool isGrounded;
    private bool hasDied;
    private bool turnBasedCombat;
    private Vector3 damageShakeOffset;

    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    public bool IsDead => (stats != null && stats.IsDead) || hasDied;
    public EnemyStats Stats => stats;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = animator != null ? animator : GetComponent<Animator>();
        visuals = visuals != null ? visuals : transform;

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

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        targetDamageable = target != null ? target.GetComponentInParent<IDamageable>() : null;

        // Prevent target damageable from matching this enemy's own damageable
        if (ReferenceEquals(targetDamageable, this) || (stats != null && ReferenceEquals(targetDamageable, stats)))
        {
            target = null;
            targetDamageable = null;
        }
    }

    private void Update()
    {
        if (hasDied)
        {
            return;
        }

        if (turnBasedCombat)
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
        if (hasDied || turnBasedCombat)
        {
            return;
        }

        HandleMovement();
    }

    private void OnDestroy()
    {
        CleanupDamageFlashMaterials();
    }

    // ==========================================
    // IDamageable Implementation & Forwarding
    // ==========================================

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

    // ==========================================
    // Combat & Turn-Based Combat
    // ==========================================

    public void SetTurnBasedCombat(bool enabled)
    {
        turnBasedCombat = enabled;
        if (enabled)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void PerformTurnAttack(int damageMultiplier = 1, bool applyDamage = true)
    {
        if (hasDied)
        {
            return;
        }

        animator.ResetTrigger("Attack");
        animator.SetTrigger("Attack");

        if (applyDamage && targetDamageable != null && !targetDamageable.IsDead)
        {
            targetDamageable.TakeDamage(attackDamage * Mathf.Max(1, damageMultiplier));
        }
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

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        if (hasDied || target == null || targetDamageable == null || targetDamageable.IsDead)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
        if (horizontalDistance <= attackRange)
        {
            targetDamageable.TakeDamage(attackDamage);
        }

        attackDamageCoroutine = null;
    }

    // ==========================================
    // AI, Movement & Targeting
    // ==========================================

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

        FaceTarget(horizontalDistance);

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

        bool shouldFaceRight = horizontalDistance > 0f;
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

    // ==========================================
    // Visuals, Animations & Effects
    // ==========================================

    private void HandleAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("Grounded", isGrounded);
    }

    public void ShakeWhenDamaged()
    {
        if (hasDied || playerCamera == null || damageShakeDuration <= 0f || damageShakeMagnitude <= 0f)
        {
            return;
        }

        if (damageShakeCoroutine != null)
        {
            StopCoroutine(damageShakeCoroutine);
        }

        damageShakeCoroutine = StartCoroutine(DamageShake());
    }

    private IEnumerator DamageShake()
    {
        if (playerCamera == null)
        {
            damageShakeCoroutine = null;
            yield break;
        }

        // Cache the original camera position.
        Vector3 originalCamPos = playerCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < damageShakeDuration)
        {
            float strength = Mathf.Lerp(
                damageShakeMagnitude,
                0f,
                elapsed / damageShakeDuration
            );
            // Apply a random offset to the camera's position.
            Vector2 offset2D = Random.insideUnitCircle * strength;
            playerCamera.transform.position = originalCamPos + new Vector3(offset2D.x, offset2D.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore the original camera position.
        playerCamera.transform.position = originalCamPos;
        damageShakeCoroutine = null;
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

    public void FlashWhenDamaged()
    {
        if (hasDied || damageFlashMaterials == null)
        {
            return;
        }

        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }

        damageFlashCoroutine = StartCoroutine(DamageFlash());
        ShakeWhenDamaged();
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
            if (material != null)
            {
                material.SetFloat(FlashAmount, amount);
            }
        }
    }

    private void SetupDamageFlashMaterials()
    {
        if (damageFlashMaterial == null || spriteRenderers == null)
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

    private void CleanupDamageFlashMaterials()
    {
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

    // ==========================================
    // Death Handling
    // ==========================================

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
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        col.enabled = false;

        if (attackDamageCoroutine != null)
        {
            StopCoroutine(attackDamageCoroutine);
            attackDamageCoroutine = null;
        }

        animator.SetBool("hasDied", true);
        StartCoroutine(HideAfterDeathAnimation());
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
