using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private const float EncounterCheckInterval = 0.1f;
    private const float DiceDisplaySpacing = 0.7f;

    private PlayerController player;
    private PlayerStats playerStats;
    private EnemyController activeEnemy;
    private Coroutine encounterCoroutine;
    private Coroutine uiFadeCoroutine;
    private Coroutine turnTextCoroutine;
    private CanvasGroup playerDiceCanvasGroup;
    private CanvasGroup rollButtonCanvasGroup;
    private Vector2 turnTextStartPosition;
    private Color turnTextColor;
    private SpriteRenderer[] playerRollDisplays;
    private SpriteRenderer[] enemyRollDisplays;
    private bool waitingForPlayerRoll;
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
        playerDiceCanvasGroup = GetOrAddCanvasGroup(playerDice);
        rollButtonCanvasGroup = GetOrAddCanvasGroup(rollButton);
        CreateTurnTextIfNeeded();
        if (turnText != null)
        {
            turnTextStartPosition = turnText.rectTransform.anchoredPosition;
            turnTextColor = turnText.color;
        }
        SetCombatUIVisible(false);
    }

    private void OnEnable()
    {
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled += HandlePlayerDiceRolled;
        }
    }

    private void OnDisable()
    {
        if (diceRollUI != null)
        {
            diceRollUI.DiceRolled -= HandlePlayerDiceRolled;
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
        SetCombatUIVisible(false);
        activeEnemy = null;
        waitingForPlayerRoll = false;
    }

    private void Update()
    {
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

            int[] playerRolls = lastDiceResults ?? new[] { Random.Range(1, 7) };
            int playerRoll = SumArray(playerRolls);
            int playerMultiplier = ComputeDiceMultiplier(playerRolls);

            int[] enemyRolls = new int[Mathf.Max(1, activeEnemy.DiceCount)];
            for (int i = 0; i < enemyRolls.Length; i++)
            {
                enemyRolls[i] = Random.Range(1, 7);
            }
            int enemyRoll = SumArray(enemyRolls);
            int enemyMultiplier = ComputeDiceMultiplier(enemyRolls);

            playerRollDisplays = CreateRollDisplays(playerRolls, transform, "Player Roll");
            enemyRollDisplays = CreateRollDisplays(enemyRolls, activeEnemy.transform, "Enemy Roll");
            yield return AnimateDisplaysIn(playerRollDisplays, enemyRollDisplays);
            yield return new WaitForSeconds(rollDisplayDuration);

            if (playerRoll > enemyRoll)
            {
                yield return EnlargeDisplays(playerRollDisplays);
                player.PlayAttackAnimationVisualOnly();
                activeEnemy.TakeDamage(player.AttackDamage * playerMultiplier);
            }
            else if (enemyRoll > playerRoll)
            {
                yield return EnlargeDisplays(enemyRollDisplays);
                activeEnemy.PerformTurnAttack(enemyMultiplier);
            }

            yield return AnimateDisplaysOut(playerRollDisplays, enemyRollDisplays);
            DestroyRollDisplays(ref playerRollDisplays);
            DestroyRollDisplays(ref enemyRollDisplays);
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
        waitingForPlayerRoll = false;
        diceRollUI?.SetRollButtonInteractable(false);
        SetCombatUIVisible(false);
        activeEnemy = null;
        encounterCoroutine = null;
    }

    private void HandlePlayerDiceRolled(int[] results)
    {
        if (!waitingForPlayerRoll)
        {
            return;
        }

        lastDiceResults = results;
        waitingForPlayerRoll = false;
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

    private void SetCombatUIVisible(bool visible)
    {
        if (uiFadeCoroutine != null)
        {
            StopCoroutine(uiFadeCoroutine);
            uiFadeCoroutine = null;
        }

        playerStats?.SetCombatHeartsVisible(visible, combatUIFadeDuration);
        if (playerDice != null) playerDice.SetActive(visible);
        if (rollButton != null) rollButton.SetActive(visible);

        if (!visible)
        {
            SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
            SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
            if (turnTextCoroutine != null)
            {
                StopCoroutine(turnTextCoroutine);
                turnTextCoroutine = null;
            }
            if (turnText != null) turnText.gameObject.SetActive(false);
            return;
        }

        SetCanvasGroupAlpha(playerDiceCanvasGroup, 0f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 0f);
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
            yield return null;
        }
        SetCanvasGroupAlpha(playerDiceCanvasGroup, 1f);
        SetCanvasGroupAlpha(rollButtonCanvasGroup, 1f);
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

    private IEnumerator AnimateDisplaysIn(SpriteRenderer[] playerDisplays, SpriteRenderer[] enemyDisplays)
    {
        Vector3[] playerEnds = PrepareEntrance(playerDisplays);
        Vector3[] enemyEnds = PrepareEntrance(enemyDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayAppearDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateEntrance(playerDisplays, playerEnds, progress);
            AnimateEntrance(enemyDisplays, enemyEnds, progress);
            yield return null;
        }
        AnimateEntrance(playerDisplays, playerEnds, 1f);
        AnimateEntrance(enemyDisplays, enemyEnds, 1f);
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

    private IEnumerator EnlargeDisplays(SpriteRenderer[] displays)
    {
        if (displays == null) yield break;
        float duration = Mathf.Min(0.2f, Mathf.Max(0.05f, rollDisplayFadeDuration));
        Vector3 baseScale = Vector3.one * rollDisplayScale;
        Vector3 winningScale = baseScale * Mathf.Max(1f, winningRollScale);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            foreach (SpriteRenderer renderer in displays)
            {
                if (renderer != null) renderer.transform.localScale = Vector3.Lerp(baseScale, winningScale, progress);
            }
            yield return null;
        }
        foreach (SpriteRenderer renderer in displays)
        {
            if (renderer != null) renderer.transform.localScale = winningScale;
        }
    }

    private IEnumerator AnimateDisplaysOut(SpriteRenderer[] playerDisplays, SpriteRenderer[] enemyDisplays)
    {
        Vector3[] playerStarts = GetPositions(playerDisplays);
        Vector3[] enemyStarts = GetPositions(enemyDisplays);
        Color[] playerColors = GetColors(playerDisplays);
        Color[] enemyColors = GetColors(enemyDisplays);
        float duration = Mathf.Max(0.05f, rollDisplayFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            AnimateOut(playerDisplays, playerStarts, playerColors, progress);
            AnimateOut(enemyDisplays, enemyStarts, enemyColors, progress);
            yield return null;
        }
        HideDisplays(playerDisplays);
        HideDisplays(enemyDisplays);
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
}
