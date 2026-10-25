using System.Collections;
using TMPro;
using UnityEngine;

public sealed partial class CombatController
{
    private Coroutine uiFadeCoroutine;
    private Coroutine turnTextCoroutine;
    private CanvasGroup playerDiceCanvasGroup;
    private CanvasGroup rollButtonCanvasGroup;
    private CanvasGroup blockButtonCanvasGroup;
    private CanvasGroup healingPotionCanvasGroup;
    private TMP_Text blockUsageLabel;
    private TMP_Text healingUsageLabel;
    private UnityEngine.UI.Graphic healingPotionButtonGraphic;
    private UnityEngine.UI.Image healingPotionImage;
    private UnityEngine.UI.Graphic healingPotionFocusGraphic;
    private ButtonHoverFocus healingPotionHoverFocus;
    private Color healingPotionBusyTint = new Color(0.7843f, 0.7843f, 0.7843f, 0.502f);
    private Color healingPotionButtonEnabledColor = Color.white;
    private Color healingPotionIconEnabledColor = Color.white;
    private Color healingUsageEnabledColor = Color.white;
    private Vector2 turnTextStartPosition;
    private Color turnTextColor;

    private void InitializeCombatUI()
    {
        diceRollUI = diceRollUI != null ? diceRollUI : FindAnyObjectByType<DiceRollUI>();
        playerDice = playerDice != null ? playerDice : GameObject.Find("PlayerDice");
        rollButton = rollButton != null ? rollButton : GameObject.Find("RollButton");
        blockButton = blockButton != null ? blockButton : FindButtonIncludingInactive("BlockButton");
        healingPotionButton = healingPotionButton != null
            ? healingPotionButton
            : FindButtonIncludingInactive("HealingPotion");
        blockUsageLabel = blockButton != null
            ? blockButton.transform.Find("UsageLabel")?.GetComponent<TMP_Text>()
            : null;
        healingUsageLabel = healingPotionButton != null
            ? healingPotionButton.transform.Find("HealingUsages")?.GetComponent<TMP_Text>()
            : null;
        healingPotionImage = healingPotionButton != null
            ? healingPotionButton.transform.Find("HealingPotionImage")?.GetComponent<UnityEngine.UI.Image>()
            : null;
        healingPotionButtonGraphic = healingPotionButton != null ? healingPotionButton.targetGraphic : null;
        if (healingPotionButton != null)
        {
            // Keep the icon's appearance under our control so full health does not gray it out.
            healingPotionButton.transition = UnityEngine.UI.Selectable.Transition.None;
        }
        SetupHealingPotionFocus();
        UnityEngine.UI.Button rollButtonComponent = rollButton != null
            ? rollButton.GetComponent<UnityEngine.UI.Button>()
            : null;
        // Combat uses direct Roll/Heal/Block actions. Keeping these out of Submit
        // navigation prevents South from submitting a different, previously clicked action.
        foreach (var button in new[] { rollButtonComponent, blockButton, healingPotionButton })
        {
            if (button == null) continue;
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        }
        HeyHeyThere.PixelUIFree.PixelButton rollPixelButton = rollButtonComponent
            as HeyHeyThere.PixelUIFree.PixelButton;
        AttachButtonHoverFocus(
            rollButtonComponent,
            rollPixelButton != null ? rollPixelButton.focus : null);
        HeyHeyThere.PixelUIFree.PixelButton blockPixelButton = blockButton
            as HeyHeyThere.PixelUIFree.PixelButton;
        AttachButtonHoverFocus(
            blockButton,
            blockPixelButton != null ? blockPixelButton.focus : null);
        if (rollButtonComponent != null)
        {
            healingPotionBusyTint = rollButtonComponent.colors.disabledColor;
        }
        if (healingPotionButtonGraphic != null)
        {
            healingPotionButtonEnabledColor = healingPotionButtonGraphic.color;
        }
        if (healingPotionImage != null)
        {
            healingPotionIconEnabledColor = healingPotionImage.color;
        }
        if (healingUsageLabel != null)
        {
            healingUsageEnabledColor = healingUsageLabel.color;
        }
        healingPotionsRemaining = Mathf.Max(0, startingHealingPotionCount);
        UpdateHealingUsageLabel();
        rollResultFont = ResolveRollResultFont();
        playerDiceCanvasGroup = GetOrAddCanvasGroup(playerDice);
        rollButtonCanvasGroup = GetOrAddCanvasGroup(rollButton);
        blockButtonCanvasGroup = GetOrAddCanvasGroup(blockButton != null ? blockButton.gameObject : null);
        healingPotionCanvasGroup = GetOrAddCanvasGroup(healingPotionButton != null ? healingPotionButton.gameObject : null);
        CreateTurnTextIfNeeded();
        if (turnText != null)
        {
            turnTextStartPosition = turnText.rectTransform.anchoredPosition;
            turnTextColor = turnText.color;
        }
        SetCombatUIVisible(false, instant: true);
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
            Debug.LogError($"{nameof(CombatController)} requires a Canvas for the turn text.", this);
            return;
        }

        GameObject textObject = new GameObject("TurnText");
        textObject.transform.SetParent(canvas.transform, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(200f, 40f);
        turnText = textObject.AddComponent<TextMeshProUGUI>();
        turnText.alignment = TextAlignmentOptions.Center;
        turnText.fontSize = 24f;
        turnText.raycastTarget = false;
    }

    private void SetCombatUIVisible(bool visible, bool instant = false)
    {
        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
            uiFadeCoroutine = null;
        }

        playerStats?.SetCombatHeartsVisible(visible, combatUIFadeDuration);
        if (visible)
        {
            if (playerDice != null) playerDice.SetActive(true);
            if (rollButton != null) rollButton.SetActive(true);
            if (blockButton != null) blockButton.gameObject.SetActive(true);
            if (healingPotionButton != null) healingPotionButton.gameObject.SetActive(true);
        }
        else if (instant)
        {
            if (playerDice != null) playerDice.SetActive(false);
            if (rollButton != null) rollButton.SetActive(false);
            if (blockButton != null) blockButton.gameObject.SetActive(false);
            if (healingPotionButton != null) healingPotionButton.gameObject.SetActive(false);
        }

        if (!visible && instant)
        {
            SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
            SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
            SetCanvasGroupAlpha(blockButtonCanvasGroup, 0f);
            SetCanvasGroupAlpha(healingPotionCanvasGroup, 0f);
            SetCanvasGroupVisibility(playerDiceCanvasGroup, false);
            SetCanvasGroupVisibility(rollButtonCanvasGroup, false);
            SetCanvasGroupVisibility(blockButtonCanvasGroup, false);
            SetCanvasGroupVisibility(healingPotionCanvasGroup, false);
            if (turnTextCoroutine != null)
            {
                StopCoroutine(turnTextCoroutine);
                turnTextCoroutine = null;
            }
            if (turnText != null) turnText.gameObject.SetActive(false);
            return;
        }

        if (!visible)
        {
            if (healingPotionButton != null) healingPotionButton.interactable = false;
            SetCanvasGroupInteractable(playerDiceCanvasGroup, false);
            SetCanvasGroupInteractable(rollButtonCanvasGroup, false);
            SetCanvasGroupInteractable(blockButtonCanvasGroup, false);
            SetCanvasGroupInteractable(healingPotionCanvasGroup, false);
            if (turnTextCoroutine != null)
            {
                StopCoroutine(turnTextCoroutine);
                turnTextCoroutine = null;
            }
            if (turnText != null) turnText.gameObject.SetActive(false);
            uiFadeCoroutine = StartCoroutine(FadeCombatUIOut());
            return;
        }

        SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
        SetCanvasGroupAlpha(blockButtonCanvasGroup, 0f);
        SetCanvasGroupAlpha(healingPotionCanvasGroup, 0f);
        SetCanvasGroupInteractable(playerDiceCanvasGroup, true);
        SetCanvasGroupInteractable(rollButtonCanvasGroup, true);
        SetCanvasGroupInteractable(blockButtonCanvasGroup, true);
        SetCanvasGroupInteractable(healingPotionCanvasGroup, true);
        UpdateHealingPotionButtonState();
        uiFadeCoroutine = StartCoroutine(FadeCombatUIIn());
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
            SetCanvasGroupAlpha(blockButtonCanvasGroup, alpha);
            SetCanvasGroupAlpha(healingPotionCanvasGroup, alpha);
            yield return null;
        }
        SetCanvasGroupAlpha(playerDiceCanvasGroup, 1f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 1f);
        SetCanvasGroupAlpha(blockButtonCanvasGroup, 1f);
        SetCanvasGroupAlpha(healingPotionCanvasGroup, 1f);
        uiFadeCoroutine = null;
    }

    private IEnumerator FadeCombatUIOut()
    {
        float duration = Mathf.Max(0.05f, combatUIFadeDuration);
        float playerDiceAlpha = playerDiceCanvasGroup != null ? playerDiceCanvasGroup.alpha : 0f;
        float rollAlpha = rollButtonCanvasGroup != null ? rollButtonCanvasGroup.alpha : 0f;
        float blockAlpha = blockButtonCanvasGroup != null ? blockButtonCanvasGroup.alpha : 0f;
        float potionAlpha = healingPotionCanvasGroup != null ? healingPotionCanvasGroup.alpha : 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            SetCanvasGroupAlpha(playerDiceCanvasGroup, Mathf.Lerp(playerDiceAlpha, 0f, progress));
            SetCanvasGroupAlpha(rollButtonCanvasGroup, Mathf.Lerp(rollAlpha, 0f, progress));
            SetCanvasGroupAlpha(blockButtonCanvasGroup, Mathf.Lerp(blockAlpha, 0f, progress));
            SetCanvasGroupAlpha(healingPotionCanvasGroup, Mathf.Lerp(potionAlpha, 0f, progress));
            yield return null;
        }

        SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
        SetCanvasGroupAlpha(blockButtonCanvasGroup, 0f);
        SetCanvasGroupAlpha(healingPotionCanvasGroup, 0f);
        if (playerDice != null) playerDice.SetActive(false);
        if (rollButton != null) rollButton.SetActive(false);
        if (blockButton != null) blockButton.gameObject.SetActive(false);
        if (healingPotionButton != null) healingPotionButton.gameObject.SetActive(false);
        uiFadeCoroutine = null;
    }

    private void ShowTurnText()
    {
        if (turnText == null) return;
        if (turnTextCoroutine != null) StopCoroutine(turnTextCoroutine);
        turnText.text = $"Turn {turnNumber}";
        turnText.gameObject.SetActive(true);
        turnText.rectTransform.anchoredPosition = turnTextStartPosition;
        turnText.color = turnTextColor;
        turnTextCoroutine = StartCoroutine(AnimateTurnText());
    }

    private IEnumerator AnimateTurnText()
    {
        float duration = Mathf.Max(0.05f, turnTextDuration);
        Vector2 endPosition = turnTextStartPosition + Vector2.up * turnTextRise;
        Color color = turnTextColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(0f, 1f, progress);
            turnText.rectTransform.anchoredPosition = Vector2.Lerp(turnTextStartPosition, endPosition, smooth);
            color.a = 1f - progress;
            turnText.color = color;
            yield return null;
        }
        turnText.gameObject.SetActive(false);
        turnTextCoroutine = null;
    }

    private static CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup group = target.GetComponent<CanvasGroup>();
        return group != null ? group : target.AddComponent<CanvasGroup>();
    }

    private static void SetCanvasGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null) group.alpha = alpha;
    }

    private static void SetCanvasGroupVisibility(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        SetCanvasGroupInteractable(group, visible);
    }

    private static void SetCanvasGroupInteractable(CanvasGroup group, bool interactable)
    {
        if (group == null) return;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    private static UnityEngine.UI.Button FindButtonIncludingInactive(string buttonName)
    {
        foreach (UnityEngine.UI.Button button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include))
        {
            if (button != null && button.gameObject.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }
}
