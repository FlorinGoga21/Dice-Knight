using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player health and currency.
/// Health is represented by overhead hearts (20 HP per full heart, 10 HP per half heart),
/// which fade in exclusively during combat encounters.
/// </summary>
public partial class PlayerStats : MonoBehaviour, IDamageable, ICurrencyReceiver
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

    [Header("---Healing Heart Animation---")]
    [SerializeField, Min(0.01f)] private float healingHeartStepDuration = 0.16f;
    [SerializeField, Min(1f)] private float healingHeartPulseScale = 1.25f;

    [Header("---Currency Settings---")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private UnityEngine.UI.Image currencyCoinImage;
    [SerializeField] private Material coinShineMaterial;
    [SerializeField] private float currencyGainNotificationDuration = 0.7f;
    [SerializeField] private float currencyGainNotificationRise = 24f;
    [SerializeField] private Color currencyGainNotificationColor = Color.white;
    [SerializeField] private float currencyCountDuration = 1.5f;

    [Header("---References---")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private DamageNumber damageNumber;
    [SerializeField] private HealNumber healNumber;
    [SerializeField] private UnityEngine.UI.Button RollButton;
    [SerializeField] private int currency = 0;

    private HealthHeartsUI healthHeartsUI;
    private bool refreshHearts;

    public bool IsDead => currentHealth <= 0;
    public bool CanReceiveCurrency => !IsDead;
    public Transform CurrencyTarget => currencyCoinImage != null ? currencyCoinImage.transform : null;
    public Sprite CurrencySprite => currencyCoinImage != null ? currencyCoinImage.sprite : null;
    public int CurrentHealth => currentHealth;
    public int MaxHealthValue => MaxHealth;
    public int Currency => currency;

    public void RestoreSave(PlayerSaveData data)
    {
        MaxHealth = data.maxHealth;
        currentHealth = data.health;
        currency = data.gold;
        healthHeartsUI?.SetMaxHealth(MaxHealth, currentHealth);
        if (currencyAnimationCoroutine != null) StopCoroutine(currencyAnimationCoroutine);
        currencyAnimationCoroutine = null;
        if (currencyText != null) currencyText.text = currency.ToString();
    }

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

        if (healNumber == null)
        {
            healNumber = GetComponent<HealNumber>();
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

        refreshHearts = true;
    }

    private void LateUpdate()
    {
        if (!refreshHearts) return;
        refreshHearts = false;

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
            healthHeartsUI.ConfigureHealingAnimation(healingHeartStepDuration, healingHeartPulseScale);
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
        healthHeartsUI.ConfigureHealingAnimation(healingHeartStepDuration, healingHeartPulseScale);

        healthHeartsUI.SetMaxHealth(MaxHealth, currentHealth);
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        int actualHealedAmount = currentHealth - previousHealth;

        if (actualHealedAmount > 0)
        {
            healNumber?.ShowHeal(actualHealedAmount);

            if (healthHeartsUI != null)
            {
                healthHeartsUI.AnimateHealing(currentHealth, MaxHealth);
            }
        }
    }

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
