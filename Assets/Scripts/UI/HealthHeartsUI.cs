using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders an overhead, world-space heart health bar where each full heart represents 20 HP
/// and each half heart represents 10 HP.
/// Automatically creates and manages the exact number of heart icons to match max health.
/// Keeps upright orientation so hearts never mirror when the character flips.
/// </summary>
public class HealthHeartsUI : MonoBehaviour
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
    private HorizontalLayoutGroup layoutGroup;

    private readonly List<Image> heartIcons = new List<Image>();
    private readonly List<Image> outlineIcons = new List<Image>();
    private readonly List<Material> dissolveMaterials = new List<Material>();
    private readonly List<Material> outlineMaterials = new List<Material>();
    private readonly List<Coroutine> dissolveCoroutines = new List<Coroutine>();

    private Coroutine fadeCoroutine;
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
        EnsureStructure();

        int targetHearts = Mathf.Max(1, Mathf.CeilToInt(maxHealth / (float)HealthPerFullHeart));
        if (heartIcons.Count != targetHearts)
        {
            SyncHeartCount(targetHearts);
        }

        for (int i = 0; i < heartIcons.Count; i++)
        {
            int heartHealth = currentHealth - (i * HealthPerFullHeart);
            Image icon = heartIcons[i];
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
    }

    /// <summary>
    /// Instantly sets the visibility of the overhead heart bar.
    /// </summary>
    public void SetVisible(bool visible)
    {
        EnsureStructure();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }

        gameObject.SetActive(true);
    }

    /// <summary>
    /// Smoothly fades the overhead heart bar in or out.
    /// </summary>
    public void Fade(bool fadeIn, float duration = -1f)
    {
        EnsureStructure();

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        float fadeDuration = duration > 0f ? duration : defaultFadeDuration;
        fadeCoroutine = StartCoroutine(FadeRoutine(fadeIn ? 1f : 0f, fadeDuration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        gameObject.SetActive(true);
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }

        fadeCoroutine = null;
    }

    private void EnsureStructure()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        // UI elements require a RectTransform on the root
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 150;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        transform.localScale = Vector3.one * uiScale;

        // Ensure container for layout
        Transform container = transform.Find("HeartContainer");
        GameObject containerObj;
        if (container == null)
        {
            containerObj = new GameObject("HeartContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            containerObj.transform.SetParent(transform, false);
            container = containerObj.transform;
        }
        else
        {
            containerObj = container.gameObject;
        }

        containerRect = container as RectTransform;
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;

        layoutGroup = containerObj.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = containerObj.AddComponent<HorizontalLayoutGroup>();
        }

        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        layoutGroup.spacing = spacing;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
    }

    private void SyncHeartCount(int targetCount)
    {
        // Add hearts if count is lower than required
        while (heartIcons.Count < targetCount)
        {
            int index = heartIcons.Count;
            CreateHeartSlot(index);
        }

        // Enable or disable to match target count
        for (int i = 0; i < heartIcons.Count; i++)
        {
            bool active = i < targetCount;
            if (heartIcons[i] != null)
            {
                heartIcons[i].transform.parent.gameObject.SetActive(active);
            }
        }

        // Adjust container width to center cleanly
        float totalWidth = (targetCount * heartSize.x) + ((targetCount - 1) * spacing);
        containerRect.sizeDelta = new Vector2(totalWidth, heartSize.y);
    }

    private void CreateHeartSlot(int index)
    {
        GameObject slotObj = new GameObject($"Heart_{index}", typeof(RectTransform), typeof(LayoutElement));
        slotObj.transform.SetParent(containerRect, false);

        LayoutElement layoutElement = slotObj.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = heartSize.x;
        layoutElement.preferredHeight = heartSize.y;

        RectTransform slotRect = (RectTransform)slotObj.transform;
        slotRect.sizeDelta = heartSize;

        // Main Heart Image
        GameObject heartObj = new GameObject("HeartIcon", typeof(RectTransform), typeof(Image));
        heartObj.transform.SetParent(slotObj.transform, false);
        RectTransform heartRect = (RectTransform)heartObj.transform;
        heartRect.anchorMin = Vector2.zero;
        heartRect.anchorMax = Vector2.one;
        heartRect.offsetMin = Vector2.zero;
        heartRect.offsetMax = Vector2.zero;

        Image heartImg = heartObj.GetComponent<Image>();
        heartImg.raycastTarget = false;
        heartImg.sprite = fullHeartSprite;

        Material dissolveMatInstance = null;
        if (heartDissolveMaterial != null)
        {
            dissolveMatInstance = new Material(heartDissolveMaterial);
            dissolveMatInstance.SetFloat("_Progress", 0f);
            heartImg.material = dissolveMatInstance;
        }

        heartIcons.Add(heartImg);
        dissolveMaterials.Add(dissolveMatInstance);
        dissolveCoroutines.Add(null);

        // Heart Outline Image
        Image outlineImg = null;
        Material outlineMatInstance = null;

        if (heartOutlineMaterial != null)
        {
            GameObject outlineObj = new GameObject("HeartOutline", typeof(RectTransform), typeof(Image));
            outlineObj.transform.SetParent(heartObj.transform, false);
            RectTransform outlineRect = (RectTransform)outlineObj.transform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = Vector2.zero;
            outlineRect.offsetMax = Vector2.zero;

            outlineImg = outlineObj.GetComponent<Image>();
            outlineImg.raycastTarget = false;
            outlineImg.sprite = fullHeartSprite;

            outlineMatInstance = new Material(heartOutlineMaterial);
            outlineMatInstance.SetFloat("_OutlineOnly", 1f);
            outlineImg.material = outlineMatInstance;
        }

        outlineIcons.Add(outlineImg);
        outlineMaterials.Add(outlineMatInstance);
    }

    private void StartHeartDissolve(int index)
    {
        if (index < 0 || index >= dissolveCoroutines.Count)
        {
            return;
        }

        if (dissolveCoroutines[index] != null)
        {
            StopCoroutine(dissolveCoroutines[index]);
        }

        dissolveCoroutines[index] = StartCoroutine(DissolveHeartRoutine(index));
    }

    private IEnumerator DissolveHeartRoutine(int index)
    {
        float elapsed = 0f;

        while (elapsed < heartDissolveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / heartDissolveDuration);
            SetHeartDissolveProgress(index, progress);
            yield return null;
        }

        SetHeartDissolveProgress(index, 1f);

        if (index < heartIcons.Count && heartIcons[index] != null)
        {
            heartIcons[index].sprite = emptyHeartSprite;
            heartIcons[index].enabled = false;
            SyncOutline(index);
        }

        dissolveCoroutines[index] = null;
    }

    private void SetHeartDissolveProgress(int index, float progress)
    {
        if (index < 0 || index >= dissolveMaterials.Count)
        {
            return;
        }

        Material mat = dissolveMaterials[index];
        if (mat != null)
        {
            mat.SetFloat("_Progress", progress);
        }
    }

    private void SyncOutline(int index)
    {
        if (index < 0 || index >= outlineIcons.Count)
        {
            return;
        }

        Image outline = outlineIcons[index];
        if (outline != null && index < heartIcons.Count && heartIcons[index] != null)
        {
            outline.sprite = heartIcons[index].sprite;
            outline.enabled = heartIcons[index].enabled;
        }
    }

    private void CleanupMaterials()
    {
        foreach (Material mat in dissolveMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        dissolveMaterials.Clear();

        foreach (Material mat in outlineMaterials)
        {
            if (mat != null)
            {
                Destroy(mat);
            }
        }
        outlineMaterials.Clear();
    }
}
