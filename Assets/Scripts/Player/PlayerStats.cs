using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages player health and currency.
/// Health is represented by overhead hearts (20 HP per full heart, 10 HP per half heart),
/// which fade in exclusively during combat encounters.
/// </summary>
public class PlayerStats : MonoBehaviour, IDamageable, ICurrencyReceiver
{
    [Header("---Health Settings---")]
    [SerializeField] private int MaxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    [Header("---Overhead Hearts UI---")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Material heartDissolveMaterial;
    [SerializeField] private float heartDissolveDuration = 0.35f;
    [SerializeField] private Material heartOutlineMaterial;
    [SerializeField] private Vector3 overheadOffset = new Vector3(0f, 1.4f, 0f);

    [Header("---Currency Settings---")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image currencyCoinImage;
    [SerializeField] private Material coinShineMaterial;
    [SerializeField] private float currencyGainNotificationDuration = 0.7f;
    [SerializeField] private float currencyGainNotificationRise = 24f;
    [SerializeField] private Color currencyGainNotificationColor = Color.white;

    [Header("---References---")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DamageNumber damageNumber;
    [SerializeField] private int currency = 0;

    private Material coinMaterial;
    private Canvas currencyCanvas;
    private TextMeshProUGUI currencyGainNotification;
    private RectTransform currencyGainNotificationRect;
    private Coroutine currencyGainNotificationCoroutine;
    private int currencyGainNotificationAmount;
    private Vector2 currencyGainNotificationStartPosition;
    private HealthHeartsUI healthHeartsUI;

    public bool IsDead => currentHealth <= 0;
    public bool CanReceiveCurrency => !IsDead;
    public Transform CurrencyTarget => currencyCoinImage != null ? currencyCoinImage.transform : null;
    public Sprite CurrencySprite => currencyCoinImage != null ? currencyCoinImage.sprite : null;
    public int CurrentHealth => currentHealth;
    public int MaxHealthValue => MaxHealth;
    public int Currency => currency;

    public Sprite FullHeartSprite => fullHeartSprite;
    public Sprite HalfHeartSprite => halfHeartSprite;
    public Sprite EmptyHeartSprite => emptyHeartSprite;
    public Material HeartDissolveMaterial => heartDissolveMaterial;
    public Material HeartOutlineMaterial => heartOutlineMaterial;

    private void Awake()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (damageNumber == null)
        {
            damageNumber = GetComponent<DamageNumber>();
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        InitializeOverheadHearts();
    }

    private void Start()
    {
        CacheCurrencyUI();
        UpdateCurrencyUI();

        // Ensure hearts start hidden until combat encounters
        if (healthHeartsUI != null)
        {
            healthHeartsUI.SetVisible(false);
        }
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        HandleDebugInput();
#endif
    }

    private void OnValidate()
    {
        MaxHealth = Mathf.Max(HealthHeartsUI.HealthPerFullHeart, MaxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

        if (healthHeartsUI != null)
        {
            healthHeartsUI.OverheadOffset = overheadOffset;
            healthHeartsUI.ConfigureVisuals(
                fullHeartSprite,
                halfHeartSprite,
                emptyHeartSprite,
                heartDissolveMaterial,
                heartOutlineMaterial,
                heartDissolveDuration
            );
            healthHeartsUI.SetMaxHealth(MaxHealth, currentHealth);
        }
    }

    private void OnDestroy()
    {
        if (coinMaterial != null)
        {
            Destroy(coinMaterial);
        }
    }

    private void InitializeOverheadHearts()
    {
        if (healthHeartsUI == null)
        {
            healthHeartsUI = GetComponentInChildren<HealthHeartsUI>(true);
        }

        if (healthHeartsUI == null)
        {
            GameObject overheadObj = new GameObject(
                "PlayerOverheadHearts",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(HealthHeartsUI)
            );
            healthHeartsUI = overheadObj.GetComponent<HealthHeartsUI>();
        }

        healthHeartsUI.FollowTarget = transform;
        healthHeartsUI.OverheadOffset = overheadOffset;
        healthHeartsUI.ConfigureVisuals(
            fullHeartSprite,
            halfHeartSprite,
            emptyHeartSprite,
            heartDissolveMaterial,
            heartOutlineMaterial,
            heartDissolveDuration
        );

        healthHeartsUI.SetMaxHealth(MaxHealth, currentHealth);
    }

    // ==========================================
    // IDamageable Implementation
    // ==========================================

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        damageNumber?.ShowDamage(damage);

        if (healthHeartsUI != null)
        {
            healthHeartsUI.UpdateHearts(currentHealth, MaxHealth, animateEmptyHearts: true);
        }

        if (playerController != null)
        {
            playerController.FlashWhenDamaged();

            if (currentHealth > 0)
            {
                playerController.PlayDamageAnimation();
            }
            else
            {
                playerController.Die();
            }
        }
    }

    /// <summary>
    /// Controls fading the overhead hearts in/out for combat encounters.
    /// </summary>
    public void SetCombatHeartsVisible(bool visible, float fadeDuration)
    {
        if (healthHeartsUI != null)
        {
            healthHeartsUI.Fade(visible, fadeDuration);
        }
    }

    // ==========================================
    // ICurrencyReceiver Implementation
    // ==========================================

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currency += amount;
        UpdateCurrencyUI();
        ShowCurrencyGainNotification(amount);
    }

    private void CacheCurrencyUI()
    {
        if (currencyCoinImage == null)
        {
            GameObject currencyObject = GameObject.Find("PlayerCurrency");
            if (currencyObject != null)
            {
                currencyCoinImage = currencyObject.GetComponentInChildren<Image>(true);
            }
        }

        if (currencyCoinImage == null || coinShineMaterial == null)
        {
            currencyCanvas = currencyText != null ? currencyText.GetComponentInParent<Canvas>(true) : null;
            return;
        }

        coinMaterial = new Material(coinShineMaterial);
        coinMaterial.SetFloat("_Progress", -1f);
        currencyCoinImage.material = coinMaterial;
        currencyCanvas = currencyText != null ? currencyText.GetComponentInParent<Canvas>(true) : null;
    }

    private void UpdateCurrencyUI()
    {
        if (currencyText != null)
        {
            currencyText.text = currency.ToString();
        }
    }

    private void ShowCurrencyGainNotification(int amount)
    {
        if (currencyText == null || currencyCanvas == null)
        {
            return;
        }

        currencyGainNotificationAmount += amount;

        if (currencyGainNotification == null)
        {
            CreateCurrencyGainNotification();
        }

        currencyGainNotification.text = $"+{currencyGainNotificationAmount}";
        currencyGainNotification.ForceMeshUpdate();
        currencyGainNotificationRect.sizeDelta = new Vector2(
            Mathf.Max(currencyGainNotification.preferredWidth + 8f, 40f),
            currencyText.rectTransform.sizeDelta.y);

        if (currencyGainNotificationCoroutine != null)
        {
            StopCoroutine(currencyGainNotificationCoroutine);
        }

        currencyGainNotificationRect.anchoredPosition = currencyGainNotificationStartPosition;
        currencyGainNotificationCoroutine = StartCoroutine(AnimateCurrencyGain());
    }

    private void CreateCurrencyGainNotification()
    {
        GameObject notificationObject = new GameObject(
            "Currency Gain",
            typeof(RectTransform),
            typeof(TextMeshProUGUI),
            typeof(LayoutElement));
        notificationObject.transform.SetParent(currencyText.transform.parent, false);

        currencyGainNotification = notificationObject.GetComponent<TextMeshProUGUI>();
        LayoutElement layoutElement = notificationObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
        currencyGainNotification.font = currencyText.font;
        currencyGainNotification.fontSize = currencyText.fontSize;
        currencyGainNotification.fontStyle = currencyText.fontStyle;
        currencyGainNotification.alignment = TextAlignmentOptions.Center;
        currencyGainNotification.color = currencyGainNotificationColor;
        currencyGainNotification.raycastTarget = false;
        currencyGainNotification.gameObject.transform.SetAsLastSibling();
        currencyGainNotificationRect = currencyGainNotification.rectTransform;

        RectTransform parentRect = currencyGainNotificationRect.parent as RectTransform;
        Vector3[] currencyCorners = new Vector3[4];
        currencyText.rectTransform.GetWorldCorners(currencyCorners);
        Vector3 startWorldPosition = (currencyCorners[0] + currencyCorners[2]) * 0.5f;

        Camera canvasCamera = currencyCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : currencyCanvas.worldCamera;

        Vector2 startScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, startWorldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            startScreenPosition,
            canvasCamera,
            out Vector2 startPosition);

        currencyGainNotificationRect.anchoredPosition = startPosition + Vector2.down * 18f;
        currencyGainNotificationStartPosition = currencyGainNotificationRect.anchoredPosition;
    }

    private IEnumerator AnimateCurrencyGain()
    {
        float duration = Mathf.Max(0.05f, currencyGainNotificationDuration);
        Vector2 startPosition = currencyGainNotificationStartPosition;
        Vector2 endPosition = startPosition + Vector2.up * currencyGainNotificationRise;
        Color startColor = currencyGainNotificationColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            currencyGainNotificationRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                smoothProgress);
            startColor.a = 1f - progress;
            currencyGainNotification.color = startColor;
            yield return null;
        }

        Destroy(currencyGainNotification.gameObject);
        currencyGainNotification = null;
        currencyGainNotificationRect = null;
        currencyGainNotificationAmount = 0;
        currencyGainNotificationCoroutine = null;
    }

    // ==========================================
    // Debug
    // ==========================================

    private void HandleDebugInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            AddCurrency(10);
        }

        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            TakeDamage(10);
        }
    }
}
