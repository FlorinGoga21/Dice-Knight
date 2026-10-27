using TMPro;
using UnityEngine;

/// <summary>
/// Coordinates turn-based encounters between the player and enemies.
/// Character-specific movement, health, and attack behavior stays on each character.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public sealed partial class CombatController : MonoBehaviour
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

    private bool waitingForPlayerRoll;
    private bool blockChosenThisTurn;
    private int blockUsesRemaining;
    private int healingPotionsRemaining;
    private int[] lastDiceResults;
    private int turnNumber;
    private float encounterCheckTimer;
    private RoomSaveData pendingRoomSave;

    public bool IsInCombat => encounterCoroutine != null;
    public int PlayerDiceCount => diceRollUI != null ? Mathf.Max(1, diceRollUI.ActiveDiceCount) : 1;
    public int HealingPotionsRemaining => healingPotionsRemaining;

    public void CaptureRoomSave(RoomSaveData data)
    {
        data.inCombat = IsInCombat;
        data.combatEnemyId = activeEnemy != null ? activeEnemy.Stats.SaveId : null;
        data.turn = turnNumber;
        data.blocks = blockUsesRemaining;
    }

    public void RestoreRoomSave(int potions, RoomSaveData data)
    {
        healingPotionsRemaining = Mathf.Max(0, potions);
        pendingRoomSave = data != null && data.inCombat ? data : null;
        UpdateHealingUsageLabel();
    }

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        playerStats = GetComponent<PlayerStats>();
        InitializeCombatUI();
    }

    private void OnEnable()
    {
        combatInputActions ??= new PlayerInputActions();

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
        combatInputActions?.Combat.Disable();

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

    private void OnDestroy()
    {
        combatInputActions?.Dispose();
    }
}
