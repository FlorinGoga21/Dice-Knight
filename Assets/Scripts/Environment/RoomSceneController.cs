using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared room completion and controller exit after all enemies and rewards resolve.</summary>
public sealed class RoomSceneController : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    private bool isReturningToMap;
    private PlayerInputActions inputActions;
    private CombatController combat;

    public bool IsReturningToMap => isReturningToMap;

    private void OnEnable()
    {
        inputActions ??= new PlayerInputActions();
        inputActions.Player.ReturnToMap.Enable();
    }

    private void OnDisable() => inputActions?.Player.ReturnToMap.Disable();

    private void OnDestroy() => inputActions?.Dispose();

    private void Start()
    {
        combat = FindAnyObjectByType<CombatController>();
    }

    private void Update()
    {
        if (!inputActions.Player.ReturnToMap.WasPressedThisFrame()) return;
        // Q retains the existing development shortcut; controller exit cannot skip encounters or rewards.
        if (inputActions.Player.ReturnToMap.activeControl?.device is Gamepad)
        {
            if (combat != null && combat.IsInCombat) return;
            foreach (var enemy in FindObjectsByType<EnemyStats>())
                if (!enemy.IsDead || enemy.DeathRewardsPending) return;
            if (FindAnyObjectByType<CoinPickup>() != null) return;
            var player = FindAnyObjectByType<PlayerStats>();
            if (player == null || player.IsDead) return;
        }
        CompleteRoom();
    }

    public void CompleteRoom()
    {
        if (isReturningToMap || SceneTransition.IsPlaying || Time.timeScale <= 0f)
            return;
        if (!Application.CanStreamedLevelBeLoaded(mapSceneName))
        {
            Debug.LogError($"RoomSceneController: Scene '{mapSceneName}' is not in Build Settings.", this);
            return;
        }

        isReturningToMap = true;
        GameAudio.Play(GameAudio.Cue.RoomCompleted);
        SaveManager.Instance?.CaptureRunPlayer();
        RunMapState.CompleteCurrentRoom();
        Time.timeScale = 1f;
        SceneTransition.ChangeScene(mapSceneName, transitionStyle,
            new SceneTransition.Options { Duration = transitionDuration });
    }
}
