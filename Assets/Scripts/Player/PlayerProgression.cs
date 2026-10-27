using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;

/// <summary>Awards run XP, applies chosen upgrades, and presents post-fight rewards.</summary>
[RequireComponent(typeof(PlayerStats))]
public sealed class PlayerProgression : MonoBehaviour
{
    [Header("---Level UI---")]
    [SerializeField] private PlayerLevelUI levelUIPrefab;

    private PlayerLevelState state = new PlayerLevelState();
    private PlayerStats stats;
    private PlayerController player;
    private CombatController combat;
    private PlayerLevelUI levelUI;
    private bool rewardPending;
    private int previousLevel = 1;
    private int previousExperience;

    public PlayerLevelState State => state;
    public int BonusDamage => state.BonusDamage;
    public Sprite LuckIcon => levelUI != null ? levelUI.LuckIcon : null;
    public TMPro.TMP_FontAsset LevelFont => levelUI != null ? levelUI.LabelFont : null;
    public bool BlocksGameplay => (levelUI != null && levelUI.IsPresenting)
        || (state.PendingUpgrades > 0 && (combat == null || !combat.IsInCombat));
    public Vector3 HealthOffset => stats != null ? stats.OverheadOffset : Vector3.up * 1.4f;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        player = GetComponent<PlayerController>();
        combat = GetComponent<CombatController>();
    }

    private IEnumerator Start()
    {
        if (levelUIPrefab == null)
        {
            Debug.LogError("PlayerProgression: Level UI prefab is not assigned.", this);
            yield break;
        }
        levelUI = Instantiate(levelUIPrefab);
        levelUI.Bind(this, stats);
        while (SceneTransition.IsPlaying || (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
            yield return null;
        if (state.PendingUpgrades > 0) ShowPostFightProgress();
    }

    public void GrantExperience(int amount)
    {
        if (stats == null || stats.IsDead || amount <= 0) return;
        if (!rewardPending)
        {
            previousLevel = state.Level;
            previousExperience = state.Experience;
        }
        state.GrantExperience(amount);
        rewardPending = true;
        if (combat == null || !combat.IsInCombat) ShowPostFightProgress();
    }

    public void ShowPostFightProgress()
    {
        if (stats == null || stats.IsDead || levelUI == null || levelUI.IsPresenting
            || (!rewardPending && state.PendingUpgrades == 0)) return;
        player?.PrepareForTurnCombat();
        levelUI.ShowProgress(rewardPending ? previousLevel : state.Level,
            rewardPending ? previousExperience : state.Experience);
        rewardPending = false;
    }

    public bool TryChooseUpgrade(PlayerUpgrade upgrade)
    {
        if (stats == null || stats.IsDead || Time.timeScale <= 0f || !state.TryChooseUpgrade(upgrade)) return false;
        if (upgrade == PlayerUpgrade.Health) stats.IncreaseMaxHealth(PlayerLevelState.HealthPerUpgrade);
        return true;
    }

    public bool TryGetLuckReroll(int[] results, out int dieIndex, out int reroll)
    {
        dieIndex = PlayerLevelState.GetLuckDieIndex(results);
        reroll = 1;
        if (state.LuckChance <= 0f || dieIndex < 0 || Random.value >= state.LuckChance)
            return false;
        reroll = Random.Range(results[dieIndex] + 1, 7);
        return true;
    }

    public bool CompleteLuckReroll(int[] results, int reroll) => state.TryApplyLuck(results, 0f, reroll);

    public PlayerLevelState CaptureSave() => state.Copy();

    public void RestoreSave(PlayerLevelState saved)
    {
        state = saved != null && saved.IsValid() ? saved.Copy() : new PlayerLevelState();
        rewardPending = false;
        previousLevel = state.Level;
        previousExperience = state.Experience;
    }

    private void OnDestroy()
    {
        if (levelUI != null) Destroy(levelUI.gameObject);
    }
}
