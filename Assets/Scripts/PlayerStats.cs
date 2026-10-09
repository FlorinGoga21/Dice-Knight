using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PlayerStats : MonoBehaviour
{
    [Header("---Health---")]
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Transform healthContainer;
    [SerializeField] private Image[] healthIcons;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite halfHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Material heartDissolveMaterial;
    [SerializeField] private float heartDissolveDuration = 0.35f;

    [Header("---Currency---")]
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private Image currencyCoinImage;
    [SerializeField] private Material coinShineMaterial;

    [Header("---Player---")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private int MaxHealth = 100;
    [SerializeField] private int currentHealth = 100;
    [SerializeField] private int currency = 0;
    private Material[] heartMaterials;
    private Coroutine[] heartDissolveCoroutines;
    private Material coinMaterial;

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
            return;
        }

        coinMaterial = new Material(coinShineMaterial);
        coinMaterial.SetFloat("_Progress", -1f);
        currencyCoinImage.material = coinMaterial;
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
            currency += 10;
            UpdateCurrencyUI();
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

            icon.enabled = icon.sprite != null;

            if (icon.sprite != emptyHeartSprite)
            {
                SetHeartDissolveProgress(i, 0f);
            }
        }

        if (healthText != null)
        {
            healthText.text = currentHealth.ToString();
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

    private void UpdateCurrencyUI()
    {
        if (currencyText != null)
        {
            currencyText.text = currency.ToString();
        }
    }
}
