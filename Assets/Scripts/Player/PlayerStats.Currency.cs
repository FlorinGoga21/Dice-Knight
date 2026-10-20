using System.Collections;
using TMPro;
using UnityEngine;

public partial class PlayerStats
{
    private Material coinMaterial;
    private Canvas currencyCanvas;
    private TextMeshProUGUI currencyGainNotification;
    private RectTransform currencyGainNotificationRect;
    private Coroutine currencyGainNotificationCoroutine;
    private int currencyGainNotificationAmount;
    private Vector2 currencyGainNotificationStartPosition;
    private Coroutine currencyAnimationCoroutine;

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
                currencyCoinImage = currencyObject.GetComponentInChildren<UnityEngine.UI.Image>(true);
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
        if (currencyText == null)
            return;

        if (currencyAnimationCoroutine != null)
        {
            StopCoroutine(currencyAnimationCoroutine);
        }

        currencyAnimationCoroutine = StartCoroutine(AnimateCurrencyCounter());
    }

    private IEnumerator AnimateCurrencyCounter()
    {
        int.TryParse(currencyText.text, out int startingCurrency);
        int targetCurrency = currency;
        float elapsed = 0f;

        while (elapsed < currencyCountDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / currencyCountDuration);
            int currentDisplay = Mathf.RoundToInt(Mathf.Lerp(startingCurrency, targetCurrency, progress));
            currencyText.text = currentDisplay.ToString();
            yield return null;
        }

        currencyText.text = targetCurrency.ToString();
        currencyAnimationCoroutine = null;
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
            typeof(UnityEngine.UI.LayoutElement));
        notificationObject.transform.SetParent(currencyText.transform.parent, false);

        currencyGainNotification = notificationObject.GetComponent<TextMeshProUGUI>();
        UnityEngine.UI.LayoutElement layoutElement = notificationObject.GetComponent<UnityEngine.UI.LayoutElement>();
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
}
