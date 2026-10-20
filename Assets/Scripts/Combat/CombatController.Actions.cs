using UnityEngine;

public sealed partial class CombatController
{
    private bool CanShowPotionFocus => IsInCombat
        && healingPotionsRemaining > 0
        && playerStats != null
        && !playerStats.IsDead
        && !IsPotionActionLocked;

    private bool IsPotionActionLocked => blockChosenThisTurn
        || (diceRollUI != null && diceRollUI.IsRolling);

    private bool CanUseHealingPotion => IsInCombat
        && healingPotionsRemaining > 0
        && playerStats != null
        && !playerStats.IsDead
        && playerStats.CurrentHealth < playerStats.MaxHealthValue
        && !IsPotionActionLocked;

    private void UseHealingPotion()
    {
        if (!CanUseHealingPotion)
        {
            return;
        }

        int healthBefore = playerStats.CurrentHealth;
        playerStats.Heal(HealthHeartsUI.HealthPerFullHeart);
        if (playerStats.CurrentHealth == healthBefore)
        {
            return;
        }

        healingPotionsRemaining--;
        UpdateHealingUsageLabel();
        UpdateHealingPotionButtonState();
    }

    private void UpdateHealingUsageLabel()
    {
        if (healingUsageLabel != null)
        {
            healingUsageLabel.text = healingPotionsRemaining.ToString();
        }
    }

    private void UpdateHealingPotionButtonState()
    {
        if (healingPotionButton == null)
        {
            return;
        }

        bool canUsePotion = CanUseHealingPotion;
        healingPotionButton.interactable = canUsePotion;
        healingPotionHoverFocus?.SetFocusAvailable(CanShowPotionFocus);
        Color tint = IsPotionActionLocked ? healingPotionBusyTint : Color.white;
        if (healingPotionButtonGraphic != null)
        {
            healingPotionButtonGraphic.color = MultiplyColor(healingPotionButtonEnabledColor, tint);
        }
        if (healingPotionImage != null)
        {
            healingPotionImage.color = MultiplyColor(healingPotionIconEnabledColor, tint);
        }
        if (healingUsageLabel != null)
        {
            healingUsageLabel.color = MultiplyColor(healingUsageEnabledColor, tint);
        }
    }

    private static Color MultiplyColor(Color color, Color tint)
    {
        return new Color(
            color.r * tint.r,
            color.g * tint.g,
            color.b * tint.b,
            color.a * tint.a);
    }

    private void SetupHealingPotionFocus()
    {
        if (healingPotionButton == null)
        {
            return;
        }

        HeyHeyThere.PixelUIFree.PixelButton potionPixelButton = healingPotionButton
            .GetComponent<HeyHeyThere.PixelUIFree.PixelButton>();
        healingPotionFocusGraphic = potionPixelButton != null ? potionPixelButton.focus : null;

        HeyHeyThere.PixelUIFree.PixelButton rollPixelButton = rollButton != null
            ? rollButton.GetComponent<HeyHeyThere.PixelUIFree.PixelButton>()
            : null;
        if (healingPotionFocusGraphic == null && rollPixelButton != null && rollPixelButton.focus != null)
        {
            GameObject focusObject = Instantiate(rollPixelButton.focus.gameObject, healingPotionButton.transform, false);
            focusObject.name = "PotionHoverFocus";
            RectTransform focusRect = focusObject.transform as RectTransform;
            if (focusRect != null)
            {
                focusRect.anchorMin = Vector2.zero;
                focusRect.anchorMax = Vector2.one;
                focusRect.offsetMin = Vector2.zero;
                focusRect.offsetMax = Vector2.zero;
                focusRect.localScale = Vector3.one;
                focusRect.SetAsLastSibling();
            }

            healingPotionFocusGraphic = focusObject.GetComponent<UnityEngine.UI.Graphic>();
        }

        if (healingPotionFocusGraphic == null)
        {
            return;
        }

        healingPotionFocusGraphic.color = new Color(1f, 0.86f, 0.2f, 1f);
        healingPotionFocusGraphic.raycastTarget = false;
        healingPotionFocusGraphic.enabled = false;
        if (potionPixelButton != null)
        {
            potionPixelButton.focus = healingPotionFocusGraphic;
        }

        healingPotionHoverFocus = AttachButtonHoverFocus(healingPotionButton, healingPotionFocusGraphic);
    }

    private static ButtonHoverFocus AttachButtonHoverFocus(
        UnityEngine.UI.Button button,
        UnityEngine.UI.Graphic focusGraphic)
    {
        if (button == null || focusGraphic == null)
        {
            return null;
        }

        focusGraphic.color = new Color(1f, 0.86f, 0.2f, 1f);
        focusGraphic.raycastTarget = false;
        focusGraphic.enabled = false;

        ButtonHoverFocus hoverFocus = button.GetComponent<ButtonHoverFocus>();
        if (hoverFocus == null)
        {
            hoverFocus = button.gameObject.AddComponent<ButtonHoverFocus>();
        }

        hoverFocus.Initialize(button, focusGraphic);
        return hoverFocus;
    }

    private void UpdateBlockButtonLabel()
    {
        if (blockUsageLabel != null)
        {
            blockUsageLabel.text = $"Uses: {blockUsesRemaining}";
        }
    }
}
