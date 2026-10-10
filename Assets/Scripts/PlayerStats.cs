using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("---Health---")]
    [SerializeField] private Transform healthContainer;
    [SerializeField] private Image[] healthIcons;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Material heartDissolveMaterial;
    [SerializeField] private float heartDissolveDuration = 0.35f;
    [SerializeField] private Material heartOutlineMaterial;

    [Header("---Currency---")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image currencyCoinImage;
    [SerializeField] private Material coinShineMaterial;
    [SerializeField] private float currencyGainNotificationDuration = 0.7f;
    [SerializeField] private float currencyGainNotificationRise = 24f;
    [SerializeField] private Color currencyGainNotificationColor = Color.white;

    [Header("---Player---")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private int MaxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int currency = 0;
    public bool IsDead => currentHealth <= 0;
    public Transform CurrencyTarget => currencyCoinImage != null ? currencyCoinImage.transform : null;
    public Sprite CurrencySprite => currencyCoinImage != null ? currencyCoinImage.sprite : null;
    private Material[] heartMaterials;
    private Coroutine[] heartDissolveCoroutines;
    private Image[] heartOutlineIcons;
    private Material[] heartOutlineMaterials;
    private Material coinMaterial;
    private Canvas currencyCanvas;
    private TextMeshProUGUI currencyGainNotification;
    private RectTransform currencyGainNotificationRect;
    private Coroutine currencyGainNotificationCoroutine;
    private int currencyGainNotificationAmount;
    private Vector2 currencyGainNotificationStartPosition;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        CacheHealthUI();
        CacheCurrencyUI();
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        UpdateUI(false);
    }

    private void OnDestroy()
    {
        if (heartMaterials != null)
        {
            foreach (Material material in heartMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }

        if (coinMaterial != null)
        {
            Destroy(coinMaterial);
        }

        if (heartOutlineMaterials != null)
        {
            foreach (Material material in heartOutlineMaterials)
            {
                if (material != null)
                {
                    Destroy(material);
                }
            }
        }
    }

    private void Update()
    {
        AddCurrency();
        TestTakeDamage();
    }

    private void CacheHealthUI()
    {
        if (healthContainer == null)
        {
            GameObject healthObject = GameObject.Find("PlayerHealth");
            healthContainer = healthObject != null ? healthObject.transform : null;
        }

        if (healthContainer == null)
        {
            return;
        }

        if (healthIcons == null || healthIcons.Length == 0)
        {
            healthIcons = healthContainer.GetComponentsInChildren<Image>(true);
        }

        SetupHeartDissolveMaterials();
        SetupHeartOutlineIcons();

        if (healthIcons.Length > 0 && fullHeartSprite == null)
        {
            fullHeartSprite = healthIcons[0].sprite;
        }

        foreach (Image icon in healthIcons)
        {
            string iconName = icon.transform.parent != null
                ? icon.transform.parent.name.ToLowerInvariant()
                : icon.name.ToLowerInvariant();

            if (iconName.Contains("half") && halfHeartSprite == null)
            {
                halfHeartSprite = icon.sprite;
            }
            else if (iconName.Contains("empty") && emptyHeartSprite == null)
            {
                emptyHeartSprite = icon.sprite;
            }
        }
    }

    private void SetupHeartOutlineIcons()
    {
        if (heartOutlineMaterial == null || healthIcons == null)
        {
            return;
        }

        heartOutlineIcons = new Image[healthIcons.Length];
        heartOutlineMaterials = new Material[healthIcons.Length];

        for (int i = 0; i < healthIcons.Length; i++)
        {
            GameObject outlineObject = new GameObject("Heart Outline", typeof(RectTransform), typeof(Image));
            outlineObject.transform.SetParent(healthIcons[i].transform, false);
            outlineObject.transform.SetAsLastSibling();

            RectTransform outlineTransform = (RectTransform)outlineObject.transform;
            outlineTransform.anchorMin = Vector2.zero;
            outlineTransform.anchorMax = Vector2.one;
            outlineTransform.offsetMin = Vector2.zero;
            outlineTransform.offsetMax = Vector2.zero;

            Image outlineIcon = outlineObject.GetComponent<Image>();
            outlineIcon.raycastTarget = false;
            outlineIcon.sprite = healthIcons[i].sprite;

            Material material = new Material(heartOutlineMaterial);
            material.SetFloat("_OutlineOnly", 1f);
            outlineIcon.material = material;

            heartOutlineIcons[i] = outlineIcon;
            heartOutlineMaterials[i] = material;
        }
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
            currencyCanvas = currencyText != null
                ? currencyText.GetComponentInParent<Canvas>(true)
                : null;
            return;
        }

        coinMaterial = new Material(coinShineMaterial);
        coinMaterial.SetFloat("_Progress", -1f);
        currencyCoinImage.material = coinMaterial;
        currencyCanvas = currencyText != null
            ? currencyText.GetComponentInParent<Canvas>(true)
            : null;
    }

    private void UpdateUI(bool animateEmptyHearts)
    {
        UpdateHealthUI(animateEmptyHearts);
        UpdateCurrencyUI();
    }

    private void AddCurrency()
    {
        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
        {
            AddCurrency(10);
        }
    }

    private void TestTakeDamage()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            TakeDamage(10);
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHealthUI(true);

        if (playerController != null)
        {
            playerController.FlashWhenDamaged();
            
            if(currentHealth > 0)
            {
                playerController.PlayDamageAnimation();
            }
        }

        if (currentHealth == 0 && playerController != null)
        {
            playerController.Die();
        }
    }

    private void UpdateHealthUI(bool animateEmptyHearts)
    {
        if (healthIcons == null || healthIcons.Length == 0)
        {
            return;
        }

        int healthPerHeart = Mathf.Max(1, MaxHealth / healthIcons.Length);
        int halfHeartHealth = Mathf.Max(1, healthPerHeart / 2);

        for (int i = 0; i < healthIcons.Length; i++)
        {
            int heartHealth = currentHealth - i * healthPerHeart;
            Image icon = healthIcons[i];

            bool wasEmpty = icon.sprite == emptyHeartSprite;

            if (heartHealth >= healthPerHeart)
            {
                icon.sprite = fullHeartSprite;
            }
            else if (heartHealth >= halfHeartHealth)
            {
                icon.sprite = halfHeartSprite;
            }
            else if (animateEmptyHearts && !wasEmpty)
            {
                StartHeartDissolve(i);
            }
            else
            {
                icon.sprite = emptyHeartSprite;
            }

            icon.enabled = icon.sprite != null && icon.sprite != emptyHeartSprite;
            SyncHeartOutline(i);

            if (icon.sprite != emptyHeartSprite)
            {
                SetHeartDissolveProgress(i, 0f);
            }
        }

    }

    private void SetupHeartDissolveMaterials()
        {
            if (heartDissolveMaterial == null || healthIcons == null)
            {
                return;
            }

            heartMaterials = new Material[healthIcons.Length];
            heartDissolveCoroutines = new Coroutine[healthIcons.Length];

            for (int i = 0; i < healthIcons.Length; i++)
            {
                Material material = new Material(heartDissolveMaterial);
                material.SetFloat("_Progress", 0f);
                healthIcons[i].material = material;
                heartMaterials[i] = material;
            }
        }

    private void StartHeartDissolve(int heartIndex)
        {
            if (heartDissolveCoroutines == null || heartIndex >= heartDissolveCoroutines.Length)
            {
                return;
            }

            if (heartDissolveCoroutines[heartIndex] != null)
            {
                StopCoroutine(heartDissolveCoroutines[heartIndex]);
            }

            heartDissolveCoroutines[heartIndex] = StartCoroutine(DissolveHeart(heartIndex));
        }

    private IEnumerator DissolveHeart(int heartIndex)
        {
            float elapsed = 0f;

            while (elapsed < heartDissolveDuration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / heartDissolveDuration);
                SetHeartDissolveProgress(heartIndex, progress);
                yield return null;
            }

            SetHeartDissolveProgress(heartIndex, 1f);
            healthIcons[heartIndex].sprite = emptyHeartSprite;
            healthIcons[heartIndex].enabled = false;
            SyncHeartOutline(heartIndex);
            heartDissolveCoroutines[heartIndex] = null;
        }

    private void SetHeartDissolveProgress(int heartIndex, float progress)
        {
            if (heartMaterials == null || heartIndex >= heartMaterials.Length
                || heartMaterials[heartIndex] == null)
            {
                return;
            }

            heartMaterials[heartIndex].SetFloat("_Progress", progress);
    }

    private void SyncHeartOutline(int heartIndex)
    {
        if (heartOutlineIcons == null || heartIndex >= heartOutlineIcons.Length)
        {
            return;
        }

        Image outlineIcon = heartOutlineIcons[heartIndex];
        outlineIcon.sprite = healthIcons[heartIndex].sprite;
        outlineIcon.enabled = healthIcons[heartIndex].enabled;
    }

    private void UpdateCurrencyUI()
    {
        if (currencyText != null)
        {
            currencyText.text = currency.ToString();
        }
    }

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
        Vector2 startScreenPosition = RectTransformUtility.WorldToScreenPoint(
            currencyCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : currencyCanvas.worldCamera,
            startWorldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            startScreenPosition,
            currencyCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : currencyCanvas.worldCamera,
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
}
