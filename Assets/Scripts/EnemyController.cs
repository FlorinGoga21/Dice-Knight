using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public class EnemyController : MonoBehaviour
{
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

    [Header("---Ground Check---")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("---Animation---")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visuals;
    [SerializeField] private bool spriteFacesRight = true;

    [Header("---Death---")]
    [SerializeField] private float deathAnimationTimeout = 5f;

    [Header("---Damage Flash---")]
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("---Health---")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private Slider healthBar;

    [Header("---Coins---")]
    [Min(0)]
    [SerializeField] private int minimumCoinsDropped = 3;
    [Min(0)]
    [SerializeField] private int maximumCoinsDropped = 3;
    [SerializeField] private int coinValue = 1;
    [SerializeField] private Sprite coinSprite;
    [SerializeField] private float coinSpawnDelay = 0.5f;
    [SerializeField] private float coinJumpDuration = 0.35f;
    [SerializeField] private Vector2 coinJumpHorizontalRange = new Vector2(-0.5f, 0.5f);
    [SerializeField] private Vector2 coinJumpHeightRange = new Vector2(0.5f, 1f);
    [SerializeField] private float coinFlightDuration = 0.5f;

    private Rigidbody2D rb;
    private Collider2D col;
    private float attackCooldownRemaining;
    private Coroutine attackDamageCoroutine;
    private bool isGrounded;
    private bool hasDied;
    private int currentHealth;
    private Canvas healthBarCanvas;
    private PlayerStats playerStats;
    private SpriteRenderer[] spriteRenderers;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = animator != null ? animator : GetComponent<Animator>();
        visuals = visuals != null ? visuals : transform;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetupDamageFlashMaterials();
        currentHealth = maxHealth;
        healthBarCanvas = healthBar != null
            ? healthBar.GetComponentInParent<Canvas>(true)
            : null;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = maxHealth;
            SetHealthBarVisible(false);
        }
    }

    public class CoinPickup : MonoBehaviour
    {
        private PlayerStats target;
        private int value;
        private float delay;
        private float jumpDuration;
        private Vector2 jumpOffset;
        private float jumpHeight;
        private float flightDuration;
        private Camera mainCamera;

        public void Initialize(
            PlayerStats playerStats,
            int currencyValue,
            float pickupDelay,
            float launchDuration,
            Vector2 launchOffset,
            float launchHeight,
            float duration)
        {
            target = playerStats;
            value = currencyValue;
            delay = pickupDelay;
            jumpDuration = Mathf.Max(0.05f, launchDuration);
            jumpOffset = launchOffset;
            jumpHeight = Mathf.Max(0f, launchHeight);
            flightDuration = Mathf.Max(0.05f, duration);
            mainCamera = Camera.main;
            StartCoroutine(FlyToCurrency());
        }

        private IEnumerator FlyToCurrency()
        {
            yield return new WaitForSeconds(delay);

            if (target == null || target.IsDead || target.CurrencyTarget == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 startPosition = transform.position;
            Vector3 jumpEndPosition = startPosition + (Vector3)jumpOffset;
            float jumpElapsed = 0f;

            while (jumpElapsed < jumpDuration)
            {
                jumpElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(jumpElapsed / jumpDuration);
                float arcHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                transform.position = Vector3.Lerp(startPosition, jumpEndPosition, progress)
                    + Vector3.up * arcHeight;
                yield return null;
            }

            startPosition = transform.position;
            float elapsed = 0f;

            while (elapsed < flightDuration)
            {
                if (target == null || target.IsDead || target.CurrencyTarget == null)
                {
                    Destroy(gameObject);
                    yield break;
                }

                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / flightDuration);
                Vector3 targetPosition = GetCurrencyWorldPosition();
                transform.position = Vector3.Lerp(startPosition, targetPosition, progress * progress);
                yield return null;
            }

            target.AddCurrency(value);
            Destroy(gameObject);
        }

        private Vector3 GetCurrencyWorldPosition()
        {
            Transform currencyTarget = target.CurrencyTarget;
            RectTransform rectTransform = currencyTarget as RectTransform;
            Canvas canvas = currencyTarget.GetComponentInParent<Canvas>();

            if (mainCamera == null || rectTransform == null || canvas == null)
            {
                return currencyTarget.position;
            }

            Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, rectTransform.position);
            float distanceFromCamera = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            return mainCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera)
            );
        }
    }

    private void OnDestroy()
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

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            target = player != null ? player.transform : null;
        }

        playerStats = target != null ? target.GetComponentInParent<PlayerStats>() : null;
    }

    private void Update()
    {
        if (hasDied)
        {
            return;
        }

        UpdateGroundedState();
        UpdateAttackCooldown();
        HandleTarget();
        HandleAnimation();
    }

    private void FixedUpdate()
    {
        if (hasDied)
        {
            return;
        }

        HandleMovement();
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

    public void TakeDamage(int damage)
    {
        if (hasDied || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        ShowHealthBar();
        FlashWhenDamaged();
        PlayDamageAnimation();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void FlashWhenDamaged()
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

        if (healthBar != null)
        {
            SetHealthBarVisible(false);
        }

        animator.SetBool("hasDied", true);
        SetHealthBarVisible(false);
        SpawnCoins();
        StartCoroutine(HideAfterDeathAnimation());
    }

    private IEnumerator HideAfterDeathAnimation()
    {
        float elapsed = 0f;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die")
            && elapsed < deathAnimationTimeout)
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

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        if (hasDied || target == null)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        PlayerStats playerStats = target.GetComponentInParent<PlayerStats>();
        if (playerStats != null && playerStats.IsDead)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
        if (horizontalDistance <= attackRange)
        {
            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
            }
        }

        attackDamageCoroutine = null;
    }

    private void HandleTarget()
    {
        if (target == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        PlayerStats playerStats = target.GetComponentInParent<PlayerStats>();
        if (playerStats != null && playerStats.IsDead)
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
        if (target == null)
        {
            return;
        }

        PlayerStats playerStats = target.GetComponentInParent<PlayerStats>();
        if (playerStats != null && playerStats.IsDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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

    private void HandleAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool("Grounded", isGrounded);
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining -= Time.deltaTime;
        }
    }

    private void ShowHealthBar()
    {
        if (healthBar == null)
        {
            return;
        }

        SetHealthBarVisible(true);
        healthBar.value = currentHealth;
    }

    private void SetHealthBarVisible(bool visible)
    {
        if (healthBar == null)
        {
            return;
        }

        if (healthBarCanvas != null)
        {
            healthBarCanvas.gameObject.SetActive(visible);
        }

        healthBar.gameObject.SetActive(visible);
    }

    private void SpawnCoins()
    {
        if (playerStats == null)
        {
            return;
        }

        Sprite pickupSprite = coinSprite != null ? coinSprite : playerStats.CurrencySprite;
        if (pickupSprite == null)
        {
            return;
        }

        int minimumCoins = Mathf.Max(0, minimumCoinsDropped);
        int maximumCoins = Mathf.Max(minimumCoins, maximumCoinsDropped);
        int coinsToSpawn = Random.Range(minimumCoins, maximumCoins + 1);

        for (int i = 0; i < coinsToSpawn; i++)
        {
            GameObject coinObject = new GameObject("Coin Pickup");
            coinObject.transform.position = transform.position + Vector3.up * 0.2f;

            SpriteRenderer renderer = coinObject.AddComponent<SpriteRenderer>();
            renderer.sprite = pickupSprite;
            renderer.sortingOrder = 20;

            CoinPickup pickup = coinObject.AddComponent<CoinPickup>();
            Vector2 horizontalRange = new Vector2(
                Mathf.Min(coinJumpHorizontalRange.x, coinJumpHorizontalRange.y),
                Mathf.Max(coinJumpHorizontalRange.x, coinJumpHorizontalRange.y));
            Vector2 heightRange = new Vector2(
                Mathf.Min(coinJumpHeightRange.x, coinJumpHeightRange.y),
                Mathf.Max(coinJumpHeightRange.x, coinJumpHeightRange.y));
            Vector2 launchOffset = new Vector2(Random.Range(horizontalRange.x, horizontalRange.y), 0f);
            float launchHeight = Random.Range(heightRange.x, heightRange.y);

            pickup.Initialize(
                playerStats,
                coinValue,
                coinSpawnDelay,
                coinJumpDuration,
                launchOffset,
                launchHeight,
                coinFlightDuration);
        }
    }

    private void UpdateGroundedState()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);
        isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }
}
