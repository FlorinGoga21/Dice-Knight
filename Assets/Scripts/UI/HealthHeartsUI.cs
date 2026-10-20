using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Renders an overhead, world-space heart health bar where each full heart represents 20 HP
/// and each half heart represents 10 HP.
/// Automatically creates and manages the exact number of heart icons to match max health.
/// Keeps upright orientation so hearts never mirror when the character flips.
/// </summary>
public partial class HealthHeartsUI : MonoBehaviour
{
    public const int HealthPerFullHeart = 20;
    public const int HealthPerHalfHeart = 10;

    [Header("---Heart Sprites---")]
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;

    [Header("---Materials & Animation---")]
    [SerializeField] private Material heartDissolveMaterial;
    [SerializeField] private float heartDissolveDuration = 0.35f;
    [SerializeField] private Material heartOutlineMaterial;
    [SerializeField, Min(0.01f)] private float healingStepDuration = 0.16f;
    [SerializeField, Min(1f)] private float healingPulseScale = 1.25f;

    [Header("---Layout & Size---")]
    [SerializeField] private Vector2 heartSize = new Vector2(16f, 16f);
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float uiScale = 0.015f;

    [Header("---Positioning---")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private Vector3 overheadOffset = new Vector3(0f, 1.4f, 0f);

    [Header("---Visibility---")]
    [SerializeField] private bool startVisible = false;
    [SerializeField] private float defaultFadeDuration = 0.35f;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform containerRect;
    private UnityEngine.UI.HorizontalLayoutGroup layoutGroup;

    private readonly List<UnityEngine.UI.Image> heartIcons = new List<UnityEngine.UI.Image>();
    private readonly List<RectTransform> heartSlotRects = new List<RectTransform>();
    private readonly List<UnityEngine.UI.Image> outlineIcons = new List<UnityEngine.UI.Image>();
    private readonly List<Material> dissolveMaterials = new List<Material>();
    private readonly List<Material> outlineMaterials = new List<Material>();
    private readonly List<Coroutine> dissolveCoroutines = new List<Coroutine>();

    private Coroutine fadeCoroutine;
    private Coroutine healingCoroutine;
    private int displayedHealth;
    private bool hasDisplayedHealth;
    private bool isInitialized;

    public Transform FollowTarget
    {
        get => followTarget;
        set => followTarget = value;
    }

    public Vector3 OverheadOffset
    {
        get => overheadOffset;
        set => overheadOffset = value;
    }

    private void Awake()
    {
        EnsureStructure();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = startVisible ? 1f : 0f;
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null) { return; }

        // Keep positioned directly above the target without mirroring when the target rotates
        transform.position = followTarget.position + overheadOffset;
        transform.rotation = Quaternion.identity;
    }

    private void OnDestroy()
    {
        CleanupMaterials();
    }

    /// <summary>
    /// Configures the heart visual sprites and materials.
    /// </summary>
    public void ConfigureVisuals(
        Sprite full,
        Sprite half,
        Sprite empty,
        Material dissolveMat = null,
        Material outlineMat = null,
        float dissolveDuration = 0.35f)
    {
        if (full != null) fullHeartSprite = full;
        if (half != null) halfHeartSprite = half;
        if (empty != null) emptyHeartSprite = empty;
        if (dissolveMat != null) heartDissolveMaterial = dissolveMat;
        if (outlineMat != null) heartOutlineMaterial = outlineMat;
        heartDissolveDuration = Mathf.Max(0.05f, dissolveDuration);
    }

    public void ConfigureHealingAnimation(float stepDuration, float pulseScale)
    {
        healingStepDuration = Mathf.Max(0.01f, stepDuration);
        healingPulseScale = Mathf.Max(1f, pulseScale);
    }

    /// <summary>
    /// Sets the max health, dynamically generating or pruning heart icons
    /// (1 heart for every 20 HP), and updates current visual state.
    /// </summary>
    public void SetMaxHealth(int maxHealth, int currentHealth)
    {
        EnsureStructure();

        int clampedMax = Mathf.Max(HealthPerFullHeart, maxHealth);
        int targetHearts = Mathf.CeilToInt(clampedMax / (float)HealthPerFullHeart);

        SyncHeartCount(targetHearts);
        UpdateHearts(currentHealth, clampedMax, animateEmptyHearts: false);
    }

    /// <summary>
    /// Updates the fill of each heart based on the current health.
    /// </summary>
    public void UpdateHearts(int currentHealth, int maxHealth, bool animateEmptyHearts = true)
    {
        StopHealingAnimation();
        RenderHearts(currentHealth, maxHealth, animateEmptyHearts);
    }

    /// <summary>
    /// Gradually fills missing health in half-heart steps, briefly popping each heart as it fills.
    /// Gameplay health is updated by the caller immediately; this only animates the display.
    /// </summary>
    public void AnimateHealing(int currentHealth, int maxHealth)
    {
        EnsureStructure();
        StopHealingAnimation();

        if (!hasDisplayedHealth || currentHealth <= displayedHealth || !gameObject.activeInHierarchy)
        {
            RenderHearts(currentHealth, maxHealth, animateEmptyHearts: false);
            return;
        }

        healingCoroutine = StartCoroutine(HealingRoutine(currentHealth, maxHealth));
    }

    private void RenderHearts(int currentHealth, int maxHealth, bool animateEmptyHearts)
    {
        EnsureStructure();

        int targetHearts = Mathf.Max(1, Mathf.CeilToInt(maxHealth / (float)HealthPerFullHeart));
        if (heartIcons.Count != targetHearts)
        {
            SyncHeartCount(targetHearts);
        }

        for (int i = 0; i < heartIcons.Count; i++)
        {
            int heartHealth = currentHealth - (i * HealthPerFullHeart);
            UnityEngine.UI.Image icon = heartIcons[i];
            bool wasEmpty = icon.sprite == emptyHeartSprite || !icon.enabled;

            if (heartHealth >= HealthPerFullHeart)
            {
                icon.sprite = fullHeartSprite;
                icon.enabled = true;
                SetHeartDissolveProgress(i, 0f);
            }
            else if (heartHealth >= HealthPerHalfHeart)
            {
                icon.sprite = halfHeartSprite;
                icon.enabled = true;
                SetHeartDissolveProgress(i, 0f);
            }
            else if (animateEmptyHearts && !wasEmpty && gameObject.activeInHierarchy)
            {
                StartHeartDissolve(i);
            }
            else
            {
                icon.sprite = emptyHeartSprite;
                icon.enabled = false;
                SetHeartDissolveProgress(i, 1f);
            }

            SyncOutline(i);
        }

        displayedHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        hasDisplayedHealth = true;
    }
}
