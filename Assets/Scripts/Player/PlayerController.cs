using System.Collections;
using TMPro;
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

    [Header("---Damage Flash---")]
    [SerializeField] private Material damageFlashMaterial;
    [SerializeField] private float damageFlashDuration = 0.1f;

    [Header("---Camera Follow---")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float cameraFollowOffset;
    [SerializeField] private float damageShakeDuration = 0.15f;
    [SerializeField] private float damageShakeMagnitude = 0.12f;

    [Header("---Turn-Based Combat---")]
    [SerializeField] private float enemyDetectionRange = 12f;
    [SerializeField] private DiceRollUI diceRollUI;
    [SerializeField] private GameObject playerDice;
    [SerializeField] private GameObject rollButton;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private float combatUIFadeDuration = 0.35f;
    [SerializeField] private float turnTextDuration = 0.7f;
    [SerializeField] private float turnTextRise = 24f;
    [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, -1f);
    [SerializeField] private float rollDisplayScale = 2f;
    [SerializeField] private float winningRollScale = 1.3f;
    [SerializeField] private float rollDisplayRise = 0.8f;
    [SerializeField] private float rollDisplayAppearDuration = 0.35f;
    [SerializeField] private float rollDisplayDuration = 1.5f;
    [SerializeField] private float rollDisplayFadeDuration = 0.35f;

    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerInputActions inputActions;

    private float moveInput;
    private bool isFacingRight = true;
    private bool isGrounded;
    private bool wasGrounded;
    private bool hasDied;
    private bool gameplayEnabled = true;
    private SpriteRenderer[] spriteRenderers;
    private Material[] damageFlashMaterials;
    private Coroutine damageFlashCoroutine;
    private Coroutine damageShakeCoroutine;
    private Coroutine attackDamageCoroutine;
    private float attackCooldownRemaining;
    private IDamageable ownDamageable;
    private Vector3 damageShakeOffset;
    private EnemyController activeEnemy;
    private Coroutine turnCoroutine;
    private SpriteRenderer playerRollDisplay;
    private SpriteRenderer enemyRollDisplay;
    private bool waitingForPlayerRoll;
    private int playerRollResult;
    private int turnNumber;
    private CanvasGroup playerDiceCanvasGroup;
    private CanvasGroup rollButtonCanvasGroup;
    private Coroutine combatUIFadeCoroutine;
    private Coroutine turnTextCoroutine;
    private Vector2 turnTextStartPosition;
    private Color turnTextColor;
    private PlayerStats playerStats;
    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        ownDamageable = GetComponentInParent<IDamageable>();
        playerStats = GetComponent<PlayerStats>();
        animator = animator != null ? animator : GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        SetupDamageFlashMaterials();
        playerCamera = playerCamera != null ? playerCamera : Camera.main;
        inputActions = new PlayerInputActions();
        diceRollUI = diceRollUI != null ? diceRollUI : FindAnyObjectByType<DiceRollUI>();
        playerDice = playerDice != null ? playerDice : GameObject.Find("PlayerDice");
        rollButton = rollButton != null ? rollButton : GameObject.Find("RollButton");
        CreateTurnTextIfNeeded();
        InitializeTurnTextAnimationState();
        playerDiceCanvasGroup = GetOrAddCanvasGroup(playerDice);
        rollButtonCanvasGroup = GetOrAddCanvasGroup(rollButton);
        SetCombatUIVisible(false);
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled += HandlePlayerDiceRolled;
        }
        UpdateGroundedState();
        wasGrounded = isGrounded;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable()
    {
        inputActions.Disable();

        if (damageShakeCoroutine != null)
        {
            StopCoroutine(damageShakeCoroutine);
            damageShakeCoroutine = null;
        }

        damageShakeOffset = Vector3.zero;
    }
    private void OnDestroy()
    {
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled -= HandlePlayerDiceRolled;
        }

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
        if (hasDied || !gameplayEnabled || turnCoroutine != null)
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
        CheckForEnemyEncounter();
    }

    private void FixedUpdate()
    {
        if (hasDied || !gameplayEnabled)
        {
            return;
        }

        if (turnCoroutine == null)
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
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        movementDust?.Stop(
            withChildren: true,
            stopBehavior: ParticleSystemStopBehavior.StopEmitting
        );
        animator.SetBool("hasDied", true);
        StartCoroutine(QuitGame());
    }

    public void SetGameplayEnabled(bool enabled)
    {
        gameplayEnabled = enabled;
        if (!enabled)
        {
            moveInput = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void CreateTurnTextIfNeeded()
    {
        if (turnText != null)
        {
            return;
        }

        Canvas canvas = playerDice != null
            ? playerDice.GetComponentInParent<Canvas>()
            : rollButton != null ? rollButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogError($"{nameof(PlayerController)} requires a Canvas for the turn text.", this);
            return;
        }

        GameObject turnTextObject = new GameObject("TurnText");
        turnTextObject.transform.SetParent(canvas.transform, false);
        RectTransform rectTransform = turnTextObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -40f);
        rectTransform.sizeDelta = new Vector2(200f, 40f);

        turnText = turnTextObject.AddComponent<TextMeshProUGUI>();
        turnText.alignment = TextAlignmentOptions.Center;
        turnText.fontSize = 24f;
        turnText.raycastTarget = false;
    }

    private void InitializeTurnTextAnimationState()
    {
        if (turnText == null)
        {
            return;
        }

        turnTextStartPosition = turnText.rectTransform.anchoredPosition;
        turnTextColor = turnText.color;
    }

    private void SetCombatUIVisible(bool visible)
    {
        if (combatUIFadeCoroutine != null)
        {
            StopCoroutine(combatUIFadeCoroutine);
            combatUIFadeCoroutine = null;
        }

        playerStats?.SetCombatHeartsVisible(visible, combatUIFadeDuration);

        if (playerDice != null)
        {
            playerDice.SetActive(visible);
        }

        if (rollButton != null)
        {
            rollButton.SetActive(visible);
        }

        if (!visible)
        {
            SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
            SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
            if (turnTextCoroutine != null)
            {
                StopCoroutine(turnTextCoroutine);
                turnTextCoroutine = null;
            }

            if (turnText != null)
            {
                turnText.gameObject.SetActive(false);
            }

            return;
        }

        SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
        combatUIFadeCoroutine = StartCoroutine(FadeCombatUIIn());
    }

    private IEnumerator FadeCombatUIIn()
    {
        float duration = Mathf.Max(0.05f, combatUIFadeDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            SetCanvasGroupAlpha(playerDiceCanvasGroup, alpha);
            SetCanvasGroupAlpha(rollButtonCanvasGroup, alpha);
            yield return null;
        }

        SetCanvasGroupAlpha(playerDiceCanvasGroup, 1f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 1f);
        combatUIFadeCoroutine = null;
    }

    private void ShowTurnText()
    {
        if (turnText == null)
        {
            return;
        }

        if (turnTextCoroutine != null)
        {
            StopCoroutine(turnTextCoroutine);
        }

        turnText.text = $"Turn {turnNumber}";
        turnText.gameObject.SetActive(true);
        turnText.rectTransform.anchoredPosition = turnTextStartPosition;
        turnText.color = turnTextColor;
        turnTextCoroutine = StartCoroutine(AnimateTurnText());
    }

    private IEnumerator AnimateTurnText()
    {
        float duration = Mathf.Max(0.05f, turnTextDuration);
        Vector2 startPosition = turnTextStartPosition;
        Vector2 endPosition = startPosition + Vector2.up * turnTextRise;
        Color startColor = turnTextColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            turnText.rectTransform.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                smoothProgress);
            startColor.a = 1f - progress;
            turnText.color = startColor;
            yield return null;
        }

        turnText.gameObject.SetActive(false);
        turnTextCoroutine = null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        return canvasGroup != null ? canvasGroup : target.AddComponent<CanvasGroup>();
    }

    private static void SetCanvasGroupAlpha(CanvasGroup canvasGroup, float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }

    private void HandlePlayerDiceRolled(int result)
    {
        if (!waitingForPlayerRoll)
        {
            return;
        }

        playerRollResult = result;
        waitingForPlayerRoll = false;
    }

    private void ShowRoll(SpriteRenderer display, int result, Transform owner)
    {
        if (diceRollUI == null || owner == null)
        {
            return;
        }

        if (display == null)
        {
            GameObject displayObject = new GameObject("Combat Roll");
            display = displayObject.AddComponent<SpriteRenderer>();
            display.sortingOrder = 100;
            if (ReferenceEquals(owner, transform))
            {
                playerRollDisplay = display;
            }
            else
            {
                enemyRollDisplay = display;
            }
        }

        display.sprite = diceRollUI.GetPipSprite(0, result);
        display.transform.position = owner.position + rollDisplayOffset;
        display.transform.localScale = Vector3.one * rollDisplayScale;
        display.color = new Color(1f, 1f, 1f, 0f);
        display.enabled = display.sprite != null;
    }

    private IEnumerator AnimateRollDisplaysIn()
    {
        float duration = Mathf.Max(0.05f, rollDisplayAppearDuration);
        Vector3 playerEndPosition = playerRollDisplay != null
            ? playerRollDisplay.transform.position
            : Vector3.zero;
        Vector3 enemyEndPosition = enemyRollDisplay != null
            ? enemyRollDisplay.transform.position
            : Vector3.zero;
        float elapsed = 0f;

        if (playerRollDisplay != null)
        {
            playerRollDisplay.transform.position = playerEndPosition - Vector3.up * rollDisplayRise;
        }

        if (enemyRollDisplay != null)
        {
            enemyRollDisplay.transform.position = enemyEndPosition - Vector3.up * rollDisplayRise;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            AnimateRollDisplayEntrance(
                playerRollDisplay,
                playerEndPosition,
                smoothProgress);
            AnimateRollDisplayEntrance(
                enemyRollDisplay,
                enemyEndPosition,
                smoothProgress);

            yield return null;
        }

        SetRollDisplayEntranceState(playerRollDisplay, playerEndPosition);
        SetRollDisplayEntranceState(enemyRollDisplay, enemyEndPosition);
    }

    private void AnimateRollDisplayEntrance(
        SpriteRenderer display,
        Vector3 endPosition,
        float progress)
    {
        if (display == null)
        {
            return;
        }

        display.transform.position = endPosition - Vector3.up * (rollDisplayRise * (1f - progress));
        Color color = display.color;
        color.a = progress;
        display.color = color;
    }

    private static void SetRollDisplayEntranceState(
        SpriteRenderer display,
        Vector3 endPosition)
    {
        if (display == null)
        {
            return;
        }

        display.transform.position = endPosition;
        Color color = display.color;
        color.a = 1f;
        display.color = color;
    }

    private IEnumerator EnlargeWinningRoll(SpriteRenderer winningDisplay)
    {
        if (winningDisplay == null)
        {
            yield break;
        }

        float duration = Mathf.Min(0.2f, Mathf.Max(0.05f, rollDisplayFadeDuration));
        Vector3 baseScale = Vector3.one * rollDisplayScale;
        Vector3 winningScale = baseScale * Mathf.Max(1f, winningRollScale);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            winningDisplay.transform.localScale = Vector3.Lerp(
                baseScale,
                winningScale,
                smoothProgress);
            yield return null;
        }

        winningDisplay.transform.localScale = winningScale;
    }

    private IEnumerator AnimateRollDisplaysUpAndFade()
    {
        float duration = Mathf.Max(0.05f, rollDisplayFadeDuration);
        Vector3 playerStartPosition = playerRollDisplay != null
            ? playerRollDisplay.transform.position
            : Vector3.zero;
        Vector3 enemyStartPosition = enemyRollDisplay != null
            ? enemyRollDisplay.transform.position
            : Vector3.zero;
        Color playerStartColor = playerRollDisplay != null
            ? playerRollDisplay.color
            : Color.white;
        Color enemyStartColor = enemyRollDisplay != null
            ? enemyRollDisplay.color
            : Color.white;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            AnimateRollDisplay(
                playerRollDisplay,
                playerStartPosition,
                playerStartColor,
                smoothProgress);
            AnimateRollDisplay(
                enemyRollDisplay,
                enemyStartPosition,
                enemyStartColor,
                smoothProgress);

            yield return null;
        }

        HideRollDisplay(playerRollDisplay);
        HideRollDisplay(enemyRollDisplay);
    }

    private void AnimateRollDisplay(
        SpriteRenderer display,
        Vector3 startPosition,
        Color startColor,
        float progress)
    {
        if (display == null)
        {
            return;
        }

        display.transform.position = startPosition + Vector3.up * (rollDisplayRise * progress);
        Color color = startColor;
        color.a = startColor.a * (1f - progress);
        display.color = color;
    }

    private static void HideRollDisplay(SpriteRenderer display)
    {
        if (display != null)
        {
            display.enabled = false;
        }
    }

    private static void DestroyRollDisplay(ref SpriteRenderer display)
    {
        if (display == null)
        {
            return;
        }

        Destroy(display.gameObject);
        display = null;
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

    public void PlayAttackAnimationVisualOnly()
    {
        if (hasDied)
        {
            return;
        }

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