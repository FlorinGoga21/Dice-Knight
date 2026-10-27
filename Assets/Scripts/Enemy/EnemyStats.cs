using UnityEngine;

/// <summary>
/// Manages enemy health, overhead heart health UI, damage numbers, and coin drops upon defeat.
/// Mirrors PlayerStats with overhead hearts (20 HP per full heart, 10 HP per half heart).
/// </summary>
public class EnemyStats : MonoBehaviour, IDamageable
{
    [SerializeField] private string persistentSaveId;
    [Header("---Health Settings---")]
    [SerializeField] private int maxHealth = 40;
    [SerializeField] private int currentHealth = 40;

    [Header("---Overhead Hearts UI---")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Material heartDissolveMaterial;
    [SerializeField] private float heartDissolveDuration = 0.35f;
    [SerializeField] private Material heartOutlineMaterial;
    [SerializeField] private Vector3 overheadOffset = new Vector3(0f, 1.1f, 0f);

    [Header("---References---")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private DamageNumber damageNumber;

    [Header("---Coins Dropped---")]
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

    private bool isDead;
    private string spawnerId;
    private string prefabId;
    private ICurrencyReceiver currencyTarget;
    private HealthHeartsUI healthHeartsUI;
    private bool refreshHearts;

    public bool IsDead => isDead || currentHealth <= 0;
    public bool DeathRewardsPending { get; private set; }
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public string SpawnerId => spawnerId;
    public string PrefabId => prefabId;
    public string SaveId
    {
        get
        {
            if (!string.IsNullOrEmpty(persistentSaveId)) return persistentSaveId;
            string path = "";
            for (Transform item = transform; item != null; item = item.parent)
                path = item.name + "[" + item.GetSiblingIndex() + "]/" + path;
            return path;
        }
    }

    public void SetSpawnIdentity(string saveId, string sourceSpawnerId, string sourcePrefabId)
    {
        persistentSaveId = saveId;
        spawnerId = sourceSpawnerId;
        prefabId = sourcePrefabId;
    }

    public void RestoreSave(EnemySaveData data)
    {
        DeathRewardsPending = false;
        maxHealth = data.maxHealth;
        currentHealth = data.health;
        isDead = data.defeated || currentHealth <= 0;
        transform.SetPositionAndRotation(data.position, data.rotation);
        if (TryGetComponent(out Rigidbody2D body)) { body.position = data.position; body.linearVelocity = Vector2.zero; }
        GetComponent<EnemyFlight>()?.RestoreSave(data);
        healthHeartsUI?.SetMaxHealth(maxHealth, currentHealth);
        if (isDead)
        {
            if (healthHeartsUI != null) healthHeartsUI.gameObject.SetActive(false);
            gameObject.SetActive(false); // Restoring a defeat must not drop the reward again.
        }
    }

    private void Awake()
    {
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        if (damageNumber == null)
        {
            damageNumber = GetComponent<DamageNumber>();
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        InitializeOverheadHearts();
    }

    private void Start()
    {
        ResolveCurrencyTarget();

        if (healthHeartsUI != null)
        {
            healthHeartsUI.SetVisible(false);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(HealthHeartsUI.HealthPerFullHeart, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

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
            healthHeartsUI.SetMaxHealth(maxHealth, currentHealth);
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
                "EnemyOverheadHearts",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasGroup),
                typeof(HealthHeartsUI)
            );
            healthHeartsUI = overheadObj.GetComponent<HealthHeartsUI>();
        }

        // Borrow visuals from PlayerStats if not explicitly assigned in this enemy's inspector
        if (fullHeartSprite == null)
        {
            PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                fullHeartSprite = playerStats.FullHeartSprite;
                halfHeartSprite = playerStats.HalfHeartSprite;
                emptyHeartSprite = playerStats.EmptyHeartSprite;
                heartDissolveMaterial = playerStats.HeartDissolveMaterial;
                heartOutlineMaterial = playerStats.HeartOutlineMaterial;
            }
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

        healthHeartsUI.SetMaxHealth(maxHealth, currentHealth);
    }

    // ==========================================
    // IDamageable Implementation
    // ==========================================

    /// <summary>
    /// Applies damage to the enemy, updates overhead hearts, triggers feedback, and handles death.
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        damageNumber?.ShowDamage(damage);

        if (healthHeartsUI != null)
        {
            healthHeartsUI.SetVisible(true);
            healthHeartsUI.UpdateHearts(currentHealth, maxHealth, animateEmptyHearts: true);
        }

        if (enemyController != null)
        {
            enemyController.FlashWhenDamaged();

            if (currentHealth > 0)
            {
                enemyController.PlayDamageAnimation();
            }
        }

        if (currentHealth == 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Controls fading the enemy's overhead hearts in/out during turn-based combat encounters.
    /// </summary>
    public void SetCombatHeartsVisible(bool visible, float fadeDuration)
    {
        if (healthHeartsUI != null)
        {
            healthHeartsUI.Fade(visible, fadeDuration);
        }
    }

    /// <summary>
    /// Handles enemy death sequence: hides hearts, spawns coin rewards, and notifies controller.
    /// </summary>
    public void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        // RestoreSave handles defeated enemies without calling Die, so restored kills never grant XP again.
        ResolveCurrencyTarget();
        if (currencyTarget is PlayerStats player && !player.IsDead)
            player.GetComponent<PlayerProgression>()?.GrantExperience(PlayerLevelState.ExperiencePerEnemy);

        if (healthHeartsUI != null)
        {
            healthHeartsUI.SetVisible(false);
        }

        if (enemyController != null)
        {
            enemyController.Die();
        }

        DeathRewardsPending = true;
        if (TryGetComponent<EnemyFlight>(out var flight))
            StartCoroutine(SpawnCoinsAfterLanding(flight));
        else
        {
            SpawnCoins();
            DeathRewardsPending = false;
        }
    }

    private System.Collections.IEnumerator SpawnCoinsAfterLanding(EnemyFlight flight)
    {
        while (flight != null && !flight.DeathLandingComplete) yield return null;
        SpawnCoins();
        DeathRewardsPending = false;
    }

    private void ResolveCurrencyTarget()
    {
        if (currencyTarget != null)
        {
            return;
        }

        // First try to find the player by tag.
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            currencyTarget = player.GetComponentInParent<ICurrencyReceiver>();
        }

        // Fallback: if tag lookup fails, search for any PlayerStats component in the scene.
        if (currencyTarget == null)
        {
            PlayerStats fallbackPlayer = FindAnyObjectByType<PlayerStats>();
            if (fallbackPlayer != null)
            {
                currencyTarget = fallbackPlayer as ICurrencyReceiver;
            }
        }
    }

    private void SpawnCoins()
    {
        ResolveCurrencyTarget();

        if (currencyTarget == null)
        {
            return;
        }

        Sprite pickupSprite = coinSprite != null ? coinSprite : currencyTarget.CurrencySprite;
        if (pickupSprite == null)
        {
            return;
        }

        int minCoins = Mathf.Max(0, minimumCoinsDropped);
        int maxCoins = Mathf.Max(minCoins, maximumCoinsDropped);
        int coinsToSpawn = Random.Range(minCoins, maxCoins + 1);

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
                Mathf.Max(coinJumpHorizontalRange.x, coinJumpHorizontalRange.y)
            );
            Vector2 heightRange = new Vector2(
                Mathf.Min(coinJumpHeightRange.x, coinJumpHeightRange.y),
                Mathf.Max(coinJumpHeightRange.x, coinJumpHeightRange.y)
            );

            Vector2 launchOffset = new Vector2(Random.Range(horizontalRange.x, horizontalRange.y), 0f);
            float launchHeight = Random.Range(heightRange.x, heightRange.y);

            pickup.Initialize(
                currencyTarget,
                coinValue,
                coinSpawnDelay,
                coinJumpDuration,
                launchOffset,
                launchHeight,
                coinFlightDuration
            );
        }
    }
}
