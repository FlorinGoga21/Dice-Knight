using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Coordinates turn-based encounters between the player and enemies.
/// Character-specific movement, health, and attack behavior stays on each character.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public sealed class CombatController : MonoBehaviour
{
    [Header("---Encounter---")]
    [SerializeField] private float enemyDetectionRange = 12f;

    [Header("---Combat UI---")]
    [SerializeField] private DiceRollUI diceRollUI;
    [SerializeField] private GameObject playerDice;
    [SerializeField] private GameObject rollButton;
    [SerializeField] private UnityEngine.UI.Button blockButton;
    [SerializeField] private UnityEngine.UI.Button healingPotionButton;
    [SerializeField, Min(0)] private int startingHealingPotionCount = 3;
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private float combatUIFadeDuration = 0.35f;
    [SerializeField] private float turnTextDuration = 0.7f;
    [SerializeField] private float turnTextRise = 24f;

    [Header("---Roll Displays---")]
    [SerializeField] private Vector3 rollDisplayOffset = new Vector3(0f, 1.8f, -1f);
    [SerializeField] private float rollDisplayScale = 2f;
    [SerializeField] private float winningRollScale = 1.3f;
    [SerializeField] private float rollDisplayRise = 0.8f;
    [SerializeField] private float rollDisplayAppearDuration = 0.35f;
    [SerializeField] private float rollDisplayDuration = 1.5f;
    [SerializeField] private float rollDisplayFadeDuration = 0.35f;
    [SerializeField] private TMP_FontAsset rollResultFont;
    [SerializeField] private float rollResultFontSize = 6f;
    [SerializeField] private float rollResultTextScale = 0.1f;
    [SerializeField] private float rollResultPadding = 0.25f;

    private const float EncounterCheckInterval = 0.1f;
    private const float DiceDisplaySpacing = 0.7f;
    private const float RollResultSortingOrderOffset = 1f;
    private const int RollComparisonDisplayIndex = 2;
    private const float RollResultBackgroundPadding = 0.18f;
    private const float RollResultBackgroundAlpha = 0.78f;
    private const string BlockShieldResourcePath = "Addons/Ver300/0_Unit/0_Sprite/8_Weapons/7_Shield/New_Shield_02";
    private const int BlocksPerEncounter = 1;

    private static Sprite rollResultBackgroundSprite;
    private static Sprite blockShieldSprite;

    private PlayerController player;
    private PlayerStats playerStats;
    private EnemyController activeEnemy;
    private Coroutine encounterCoroutine;
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
    private SpriteRenderer[] playerRollDisplays;
    private SpriteRenderer[] enemyRollDisplays;
    private TMP_Text[] rollResultDisplays;
    private SpriteRenderer rollResultsBackground;
    private bool waitingForPlayerRoll;
    private bool blockChosenThisTurn;
    private int blockUsesRemaining;
    private int healingPotionsRemaining;
    private int[] lastDiceResults;
    private int turnNumber;
    private float encounterCheckTimer;

    public bool IsInCombat => encounterCoroutine != null;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
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

    private void OnEnable()
    {
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled += HandlePlayerDiceRolled;
            diceRollUI.RollStarted += HandlePlayerRollStarted;
        }

        if (healingPotionButton != null)
        {
            healingPotionButton.onClick.AddListener(UseHealingPotion);
        }

    }

    private void OnDisable()
    {
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled -= HandlePlayerDiceRolled;
            diceRollUI.RollStarted -= HandlePlayerRollStarted;
        }

        if (healingPotionButton != null)
        {
            healingPotionButton.onClick.RemoveListener(UseHealingPotion);
        }


        if (encounterCoroutine != null)
        {
            StopCoroutine(encounterCoroutine);
            encounterCoroutine = null;
        }

        if (activeEnemy != null)
        {
            activeEnemy.Stats?.SetCombatHeartsVisible(false, combatUIFadeDuration);
            activeEnemy.SetTurnBasedCombat(false);
        }

        DestroyRollDisplays(ref playerRollDisplays);
        DestroyRollDisplays(ref enemyRollDisplays);
        DestroyRollResultDisplays(ref rollResultDisplays);
        player?.SetBlockingPose(false);
        SetCombatUIVisible(false, instant: true);
        activeEnemy = null;
        waitingForPlayerRoll = false;
    }

    private void Update()
    {
        UpdateHealingPotionButtonState();
        if (IsInCombat || player == null || !player.CanStartTurnCombat)
        {
            return;
        }

        encounterCheckTimer -= Time.deltaTime;
        if (encounterCheckTimer > 0f)
        {
            return;
        }
        encounterCheckTimer = EncounterCheckInterval;

        EnemyController nearestEnemy = null;
        float nearestDistance = float.MaxValue;
        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);
            if (distance <= enemyDetectionRange && distance < nearestDistance)
            {
                nearestEnemy = enemy;
                nearestDistance = distance;
            }
        }

        if (nearestEnemy == null)
        {
            return;
        }

        if (diceRollUI == null)
        {
            Debug.LogError($"{nameof(CombatController)} requires a {nameof(DiceRollUI)} for turn-based combat.", this);
            return;
        }

        activeEnemy = nearestEnemy;
        player.PrepareForTurnCombat();
        turnNumber = 0;
        blockUsesRemaining = BlocksPerEncounter;
        UpdateBlockButtonLabel();
        SetCombatUIVisible(true);
        encounterCoroutine = StartCoroutine(RunEncounter());
    }

    private IEnumerator RunEncounter()
    {
        activeEnemy.SetTurnBasedCombat(true);
        activeEnemy.Stats?.SetCombatHeartsVisible(true, combatUIFadeDuration);

        while (player != null && player.CanStartTurnCombat && activeEnemy != null && !activeEnemy.IsDead)
        {
            turnNumber++;
            ShowTurnText();
            blockChosenThisTurn = false;
            player.SetBlockingPose(false);
            if (playerDice != null) playerDice.SetActive(true);
            SetCanvasGroupVisibility(playerDiceCanvasGroup, true);
            if (blockButton != null) blockButton.interactable = blockUsesRemaining > 0;
            lastDiceResults = null;
            waitingForPlayerRoll = true;
            diceRollUI.SetRollButtonInteractable(true);

            while (waitingForPlayerRoll && player != null && player.CanStartTurnCombat
                && activeEnemy != null && !activeEnemy.IsDead)
            {
                yield return null;
            }

            diceRollUI.SetRollButtonInteractable(false);
            if (player == null || !player.CanStartTurnCombat || activeEnemy == null || activeEnemy.IsDead)
            {
                break;
            }

            int[] playerRolls = blockChosenThisTurn ? null : lastDiceResults ?? new[] { Random.Range(1, 7) };
            int playerRoll = playerRolls != null ? SumArray(playerRolls) : 0;
            int playerMultiplier = playerRolls != null ? ComputeDiceMultiplier(playerRolls) : 1;

            int[] enemyRolls = new int[Mathf.Max(1, activeEnemy.DiceCount)];
            for (int i = 0; i < enemyRolls.Length; i++)
            {
                enemyRolls[i] = Random.Range(1, 7);
            }
            int enemyRoll = SumArray(enemyRolls);
            int enemyMultiplier = ComputeDiceMultiplier(enemyRolls);

            playerRollDisplays = blockChosenThisTurn
                ? CreateBlockDisplay(transform, "Player Block")
                : CreateRollDisplays(playerRolls, transform, "Player Roll");
            enemyRollDisplays = CreateRollDisplays(enemyRolls, activeEnemy.transform, "Enemy Roll");
            rollResultDisplays = CreateRollResultDisplays(
                playerRoll,
                enemyRoll,
                playerRollDisplays,
                enemyRollDisplays,
                blockChosenThisTurn);
            yield return AnimateDisplaysIn(playerRollDisplays, enemyRollDisplays, rollResultDisplays);
            yield return new WaitForSeconds(rollDisplayDuration);
            if (rollResultDisplays != null && rollResultDisplays.Length > RollComparisonDisplayIndex)
            {
                yield return PopComparisonDisplay(rollResultDisplays[RollComparisonDisplayIndex]);
            }

            if (!blockChosenThisTurn && playerRoll > enemyRoll)
            {
                yield return EnlargeDisplays(playerRollDisplays, rollResultDisplays != null ? rollResultDisplays[0] : null);
                player.PlayAttackAnimationVisualOnly();
                activeEnemy.TakeDamage(player.AttackDamage * playerMultiplier);
            }
            else if (blockChosenThisTurn)
            {
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier, applyDamage: false);
            }
            else if (enemyRoll > playerRoll)
            {
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier);
            }

            yield return AnimateDisplaysOut(playerRollDisplays, enemyRollDisplays, rollResultDisplays);
            DestroyRollDisplays(ref playerRollDisplays);
            DestroyRollDisplays(ref enemyRollDisplays);
            DestroyRollResultDisplays(ref rollResultDisplays);
            yield return new WaitForSeconds(player.AttackHitDelay + player.AttackCooldown);
        }

        FinishEncounter();
    }

    private void FinishEncounter()
    {
        if (activeEnemy != null)
        {
            activeEnemy.Stats?.SetCombatHeartsVisible(false, combatUIFadeDuration);
            activeEnemy.SetTurnBasedCombat(false);
        }

        DestroyRollDisplays(ref playerRollDisplays);
        DestroyRollDisplays(ref enemyRollDisplays);
        DestroyRollResultDisplays(ref rollResultDisplays);
        waitingForPlayerRoll = false;
        blockChosenThisTurn = false;
        player?.SetBlockingPose(false);
        diceRollUI?.SetRollButtonInteractable(false);
        if (blockButton != null) blockButton.interactable = false;
        SetCombatUIVisible(false);
        activeEnemy = null;
        encounterCoroutine = null;
    }

    private void HandlePlayerDiceRolled(int[] results)
    {
        if (!waitingForPlayerRoll || blockChosenThisTurn)
        {
            return;
        }

        lastDiceResults = results;
        waitingForPlayerRoll = false;
    }

    private void HandlePlayerRollStarted()
    {
        UpdateHealingPotionButtonState();

        if (waitingForPlayerRoll && blockButton != null)
        {
            blockButton.interactable = false;
        }
    }

    public void ChooseToBlock()
    {
        if (!waitingForPlayerRoll || blockChosenThisTurn || blockUsesRemaining <= 0
            || diceRollUI == null || diceRollUI.IsRolling)
        {
            return;
        }

        blockChosenThisTurn = true;
        blockUsesRemaining--;
        UpdateBlockButtonLabel();
        player.SetBlockingPose(true);
        SetCanvasGroupVisibility(playerDiceCanvasGroup, false);
        diceRollUI.SetRollButtonInteractable(false);
        if (blockButton != null) blockButton.interactable = false;
        waitingForPlayerRoll = false;
        UpdateHealingPotionButtonState();
    }

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

    private bool IsPotionActionLocked => blockChosenThisTurn
        || (diceRollUI != null && diceRollUI.IsRolling);

    private bool CanUseHealingPotion => IsInCombat
        && healingPotionsRemaining > 0
        && playerStats != null
        && !playerStats.IsDead
        && playerStats.CurrentHealth < playerStats.MaxHealthValue
        && !IsPotionActionLocked;

    private bool CanShowPotionFocus => IsInCombat
        && healingPotionsRemaining > 0
        && playerStats != null
        && !playerStats.IsDead
        && !IsPotionActionLocked;

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

    private SpriteRenderer[] CreateRollDisplays(int[] results, Transform owner, string displayName)
    {
        if (owner == null || diceRollUI == null || results == null) return null;
        SpriteRenderer[] displays = new SpriteRenderer[results.Length];
        float startX = -(results.Length - 1) * DiceDisplaySpacing * 0.5f;
        for (int i = 0; i < results.Length; i++)
        {
            GameObject item = new GameObject($"{displayName} {i + 1}");
            SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = 100;
            renderer.sprite = diceRollUI.GetPipSprite(0, results[i]);
            renderer.transform.position = owner.position + rollDisplayOffset
                + new Vector3(startX + i * DiceDisplaySpacing, 0f, 0f);
            renderer.transform.localScale = Vector3.one * rollDisplayScale;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            renderer.enabled = renderer.sprite != null;
            displays[i] = renderer;
        }
        return displays;
    }

    private SpriteRenderer[] CreateBlockDisplay(Transform owner, string displayName)
    {
        if (owner == null) return null;

        if (blockShieldSprite == null)
        {
            blockShieldSprite = Resources.Load<Sprite>(BlockShieldResourcePath);
        }

        if (blockShieldSprite == null)
        {
            Debug.LogError($"Could not load the SPUM block shield at Resources/{BlockShieldResourcePath}.png", this);
            return null;
        }

        GameObject item = new GameObject(displayName);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 100;
        renderer.sprite = blockShieldSprite;
        renderer.transform.position = owner.position + rollDisplayOffset;
        renderer.transform.localScale = Vector3.one * rollDisplayScale;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        renderer.enabled = true;
        return new[] { renderer };
    }

    private TMP_Text[] CreateRollResultDisplays(
        int playerTotal,
        int enemyTotal,
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        bool blockAction)
    {
        if (playerDisplays == null || playerDisplays.Length == 0
            || enemyDisplays == null || enemyDisplays.Length == 0)
        {
            return null;
        }

        bool enemyIsRight = activeEnemy != null && activeEnemy.transform.position.x >= transform.position.x;
        float inwardDirection = enemyIsRight ? 1f : -1f;
        SpriteRenderer playerInnerEdge = playerDisplays[enemyIsRight ? playerDisplays.Length - 1 : 0];
        SpriteRenderer enemyInnerEdge = enemyDisplays[enemyIsRight ? 0 : enemyDisplays.Length - 1];
        Vector3 playerTotalPosition = playerInnerEdge.transform.position
            + Vector3.right * inwardDirection * (playerInnerEdge.bounds.extents.x + rollResultPadding);
        Vector3 enemyTotalPosition = enemyInnerEdge.transform.position
            - Vector3.right * inwardDirection * (enemyInnerEdge.bounds.extents.x + rollResultPadding);
        Vector3 comparisonPosition = (playerTotalPosition + enemyTotalPosition) * 0.5f;

        TMP_Text[] resultDisplays = new TMP_Text[3];
        if (!blockAction)
        {
            resultDisplays[0] = CreateWorldResultText("Player Roll Total", playerTotal.ToString(), playerTotalPosition, playerInnerEdge);
        }
        resultDisplays[1] = CreateWorldResultText("Enemy Roll Total", enemyTotal.ToString(), enemyTotalPosition, enemyInnerEdge);
        if (!blockAction)
        {
            string comparison = playerTotal > enemyTotal ? ">" : playerTotal < enemyTotal ? "<" : "=";
            resultDisplays[2] = CreateWorldResultText("Roll Comparison", comparison, comparisonPosition, playerInnerEdge);
            resultDisplays[RollComparisonDisplayIndex].transform.localScale = Vector3.zero;
        }
        rollResultsBackground = CreateRollResultsBackground(resultDisplays, playerInnerEdge);
        return resultDisplays;
    }

    private TMP_Text CreateWorldResultText(string objectName, string value, Vector3 position, SpriteRenderer referenceRenderer)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.position = position;
        textObject.transform.localScale = Vector3.one * rollResultTextScale;

        TextMeshPro text = textObject.AddComponent<TextMeshPro>();
        text.text = value;
        text.fontSize = rollResultFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(1f, 1f, 1f, 0f);
        if (rollResultFont != null)
        {
            text.font = rollResultFont;
        }

        MeshRenderer meshRenderer = textObject.GetComponent<MeshRenderer>();
        meshRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
        meshRenderer.sortingOrder = referenceRenderer.sortingOrder + (int)RollResultSortingOrderOffset;
        text.ForceMeshUpdate();
        return text;
    }

    private SpriteRenderer CreateRollResultsBackground(TMP_Text[] labels, SpriteRenderer referenceRenderer)
    {
        if (labels == null || labels.Length == 0 || referenceRenderer == null)
        {
            return null;
        }

        MeshRenderer firstLabelRenderer = null;
        foreach (TMP_Text label in labels)
        {
            if (label == null) continue;
            firstLabelRenderer = label.GetComponent<MeshRenderer>();
            if (firstLabelRenderer != null) break;
        }
        if (firstLabelRenderer == null)
        {
            return null;
        }

        Bounds bounds = firstLabelRenderer.bounds;
        for (int i = 1; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;
            MeshRenderer labelRenderer = labels[i].GetComponent<MeshRenderer>();
            if (labelRenderer != null) bounds.Encapsulate(labelRenderer.bounds);
        }

        GameObject backgroundObject = new GameObject("Roll Results Background");
        backgroundObject.transform.position = bounds.center;
        backgroundObject.transform.localScale = new Vector3(
            bounds.size.x + RollResultBackgroundPadding * 2f,
            bounds.size.y + RollResultBackgroundPadding * 2f,
            1f);

        SpriteRenderer background = backgroundObject.AddComponent<SpriteRenderer>();
        background.sprite = GetRollResultBackgroundSprite();
        background.color = new Color(0.025f, 0.035f, 0.065f, 0f);
        background.sortingLayerID = referenceRenderer.sortingLayerID;
        background.sortingOrder = referenceRenderer.sortingOrder - 1;
        return background;
    }

    private static Sprite GetRollResultBackgroundSprite()
    {
        if (rollResultBackgroundSprite == null)
        {
            rollResultBackgroundSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
            rollResultBackgroundSprite.name = "Roll Result Background";
        }

        return rollResultBackgroundSprite;
    }

    private TMP_FontAsset ResolveRollResultFont()
    {
        if (rollResultFont != null)
        {
            return rollResultFont;
        }

        foreach (TMP_FontAsset loadedFont in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (loadedFont != null && loadedFont.name == "Pixel UI TMP")
            {
                return loadedFont;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }

    private IEnumerator AnimateDisplaysIn(
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        TMP_Text[] resultDisplays)
    {
        Vector3[] playerEnds = PrepareEntrance(playerDisplays);
        Vector3[] enemyEnds = PrepareEntrance(enemyDisplays);
        Vector3[] resultEnds = PrepareTextEntrance(resultDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayAppearDuration);
        float elapsed = 0f;
        SetResultBackgroundAlpha(0f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateEntrance(playerDisplays, playerEnds, progress);
            AnimateEntrance(enemyDisplays, enemyEnds, progress);
            AnimateTextEntrance(resultDisplays, resultEnds, progress);
            SetResultBackgroundAlpha(RollResultBackgroundAlpha * progress);
            yield return null;
        }
        AnimateEntrance(playerDisplays, playerEnds, 1f);
        AnimateEntrance(enemyDisplays, enemyEnds, 1f);
        AnimateTextEntrance(resultDisplays, resultEnds, 1f);
        SetResultBackgroundAlpha(RollResultBackgroundAlpha);
    }

    private Vector3[] PrepareTextEntrance(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Vector3[] ends = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (i == RollComparisonDisplayIndex || displays[i] == null) continue;
            ends[i] = displays[i].transform.position;
            displays[i].transform.position = ends[i] - Vector3.up * rollDisplayRise;
            SetTextAlpha(displays[i], 0f);
        }
        return ends;
    }

    private void AnimateTextEntrance(TMP_Text[] displays, Vector3[] ends, float progress)
    {
        if (displays == null || ends == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (i == RollComparisonDisplayIndex || displays[i] == null) continue;
            displays[i].transform.position = ends[i] - Vector3.up * (rollDisplayRise * (1f - progress));
            SetTextAlpha(displays[i], progress);
        }
    }

    private IEnumerator PopComparisonDisplay(TMP_Text comparisonDisplay)
    {
        if (comparisonDisplay == null) yield break;

        Vector3 finalScale = Vector3.one * rollResultTextScale;
        Vector3 peakScale = finalScale * 1.35f;
        const float popDuration = 0.32f;
        float elapsed = 0f;
        SetTextAlpha(comparisonDisplay, 0f);

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / popDuration);
            float scaleProgress;
            if (progress < 0.65f)
            {
                scaleProgress = Mathf.SmoothStep(0f, 1f, progress / 0.65f);
                comparisonDisplay.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, peakScale, scaleProgress);
            }
            else
            {
                scaleProgress = Mathf.SmoothStep(0f, 1f, (progress - 0.65f) / 0.35f);
                comparisonDisplay.transform.localScale = Vector3.LerpUnclamped(peakScale, finalScale, scaleProgress);
            }

            SetTextAlpha(comparisonDisplay, Mathf.Clamp01(progress * 3f));
            yield return null;
        }

        comparisonDisplay.transform.localScale = finalScale;
        SetTextAlpha(comparisonDisplay, 1f);
    }

    private Vector3[] PrepareEntrance(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Vector3[] ends = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            ends[i] = displays[i].transform.position;
            displays[i].transform.position = ends[i] - Vector3.up * rollDisplayRise;
        }
        return ends;
    }

    private void AnimateEntrance(SpriteRenderer[] displays, Vector3[] ends, float progress)
    {
        if (displays == null || ends == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = ends[i] - Vector3.up * (rollDisplayRise * (1f - progress));
            Color color = displays[i].color;
            color.a = progress;
            displays[i].color = color;
        }
    }

    private IEnumerator EnlargeDisplays(SpriteRenderer[] displays, TMP_Text winningTotal)
    {
        if (displays == null) yield break;
        float duration = Mathf.Min(0.2f, Mathf.Max(0.05f, rollDisplayFadeDuration));
        Vector3 baseScale = Vector3.one * rollDisplayScale;
        Vector3 winningScale = baseScale * Mathf.Max(1f, winningRollScale);
        Vector3 totalBaseScale = Vector3.one * rollResultTextScale;
        Vector3 totalWinningScale = totalBaseScale * Mathf.Max(1f, winningRollScale);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            foreach (SpriteRenderer renderer in displays)
            {
                if (renderer != null) renderer.transform.localScale = Vector3.Lerp(baseScale, winningScale, progress);
            }
            if (winningTotal != null)
            {
                winningTotal.transform.localScale = Vector3.Lerp(totalBaseScale, totalWinningScale, progress);
            }
            yield return null;
        }
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) renderer.transform.localScale = winningScale;
        }
        if (winningTotal != null)
        {
            winningTotal.transform.localScale = totalWinningScale;
        }
    }

    private IEnumerator AnimateDisplaysOut(
        SpriteRenderer[] playerDisplays,
        SpriteRenderer[] enemyDisplays,
        TMP_Text[] resultDisplays)
    {
        Vector3[] playerStarts = GetPositions(playerDisplays);
        Vector3[] enemyStarts = GetPositions(enemyDisplays);
        Vector3[] resultStarts = GetTextPositions(resultDisplays);
        Color[] playerColors = GetColors(playerDisplays);
        Color[] enemyColors = GetColors(enemyDisplays);
        Color[] resultColors = GetTextColors(resultDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateOut(playerDisplays, playerStarts, playerColors, progress);
            AnimateOut(enemyDisplays, enemyStarts, enemyColors, progress);
            AnimateTextOut(resultDisplays, resultStarts, resultColors, progress);
            SetResultBackgroundAlpha(RollResultBackgroundAlpha * (1f - progress));
            yield return null;
        }
        HideDisplays(playerDisplays);
        HideDisplays(enemyDisplays);
        HideTextDisplays(resultDisplays);
        SetResultBackgroundAlpha(0f);
    }

    private Vector3[] GetTextPositions(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Vector3[] positions = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) positions[i] = displays[i].transform.position;
        }
        return positions;
    }

    private Color[] GetTextColors(TMP_Text[] displays)
    {
        if (displays == null) return null;
        Color[] colors = new Color[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) colors[i] = displays[i].color;
        }
        return colors;
    }

    private void AnimateTextOut(TMP_Text[] displays, Vector3[] starts, Color[] colors, float progress)
    {
        if (displays == null || starts == null || colors == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = starts[i] + Vector3.up * (rollDisplayRise * progress);
            Color color = colors[i];
            color.a *= 1f - progress;
            displays[i].color = color;
            SetTextAlpha(displays[i], color.a);
        }
    }

    private static void HideTextDisplays(TMP_Text[] displays)
    {
        if (displays == null) return;
        foreach (TMP_Text text in displays)
        {
            if (text != null) text.enabled = false;
        }
    }

    private void DestroyRollResultDisplays(ref TMP_Text[] displays)
    {
        if (displays != null)
        {
            foreach (TMP_Text text in displays)
            {
                if (text != null) Destroy(text.gameObject);
            }
            displays = null;
        }

        if (rollResultsBackground != null)
        {
            Destroy(rollResultsBackground.gameObject);
            rollResultsBackground = null;
        }
    }

    private void SetResultBackgroundAlpha(float alpha)
    {
        if (rollResultsBackground == null) return;
        Color color = rollResultsBackground.color;
        color.a = alpha;
        rollResultsBackground.color = color;
    }

    private static void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private Vector3[] GetPositions(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Vector3[] positions = new Vector3[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) positions[i] = displays[i].transform.position;
        }
        return positions;
    }

    private Color[] GetColors(SpriteRenderer[] displays)
    {
        if (displays == null) return null;
        Color[] colors = new Color[displays.Length];
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] != null) colors[i] = displays[i].color;
        }
        return colors;
    }

    private void AnimateOut(SpriteRenderer[] displays, Vector3[] starts, Color[] colors, float progress)
    {
        if (displays == null || starts == null || colors == null) return;
        for (int i = 0; i < displays.Length; i++)
        {
            if (displays[i] == null) continue;
            displays[i].transform.position = starts[i] + Vector3.up * (rollDisplayRise * progress);
            Color color = colors[i];
            color.a *= 1f - progress;
            displays[i].color = color;
        }
    }

    private static void HideDisplays(SpriteRenderer[] displays)
    {
        if (displays == null) return;
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) renderer.enabled = false;
        }
    }

    private static void DestroyRollDisplays(ref SpriteRenderer[] displays)
    {
        if (displays == null) return;
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) Destroy(renderer.gameObject);
        }
        displays = null;
    }

    private static int ComputeDiceMultiplier(int[] rolls)
    {
        if (rolls == null || rolls.Length < 2) return 1;
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int roll in rolls)
        {
            counts.TryGetValue(roll, out int count);
            counts[roll] = count + 1;
        }
        if (rolls.Length >= 3 && counts.ContainsValue(3)) return 3;
        return counts.ContainsValue(2) || counts.ContainsValue(3) ? 2 : 1;
    }

    private static int SumArray(int[] values)
    {
        int sum = 0;
        foreach (int value in values) sum += value;
        return sum;
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
