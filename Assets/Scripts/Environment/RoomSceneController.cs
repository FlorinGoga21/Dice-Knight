using System.Collections;
using HeyHeyThere.SceneTransitionsFree;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared room completion and controller exit after all enemies and rewards resolve.</summary>
public sealed class RoomSceneController : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string transitionStyle = "clouds";
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    [Header("---Room Number---")]
    [SerializeField] private TMP_Text roomNumberText;
    [Tooltip("Seconds for the room-entry label to rise and fade after the scene transition finishes.")]
    [SerializeField, Min(0.05f)] private float roomNumberDuration = 1.2f;
    [Tooltip("Distance the room-entry label rises, in Canvas units.")]
    [SerializeField, Min(0f)] private float roomNumberRise = 24f;

    private bool isReturningToMap;
    private PlayerInputActions inputActions;
    private CombatController combat;
    private Coroutine roomNumberCoroutine;

    public bool IsReturningToMap => isReturningToMap;
    public bool IsShowingRoomNumber => roomNumberCoroutine != null;

    private void OnEnable()
    {
        inputActions ??= new PlayerInputActions();
        inputActions.Player.ReturnToMap.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Player.ReturnToMap.Disable();
        if (roomNumberCoroutine != null)
        {
            StopCoroutine(roomNumberCoroutine);
            roomNumberCoroutine = null;
            if (roomNumberText != null) roomNumberText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy() => inputActions?.Dispose();

    private void Start()
    {
        combat = FindAnyObjectByType<CombatController>();
        if (roomNumberText == null) roomNumberText = FindRoomNumberText();
        if (roomNumberText != null)
        {
            roomNumberText.raycastTarget = false;
            roomNumberCoroutine = StartCoroutine(ShowRoomNumber());
        }
    }

    private TMP_Text FindRoomNumberText()
    {
        // Include the authored inactive label, and stay within this room's scene.
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.name == "RoomNumber" && label.transform.parent != null
                    && label.transform.parent.name == "PlayerUI")
                    return label;
            }
        }
        return null;
    }

    private IEnumerator ShowRoomNumber()
    {
        roomNumberText.gameObject.SetActive(false);
        while (SceneTransition.IsPlaying || (SaveManager.Instance != null && SaveManager.Instance.IsLoading))
            yield return null;

        Vector2 startPosition = roomNumberText.rectTransform.anchoredPosition;
        Color startColor = roomNumberText.color;
        roomNumberText.text = $"Room {RunMapState.CurrentRoomNumber}";
        roomNumberText.gameObject.SetActive(true);
        float duration = Mathf.Max(0.05f, roomNumberDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            roomNumberText.rectTransform.anchoredPosition = startPosition
                + Vector2.up * (roomNumberRise * Mathf.SmoothStep(0f, 1f, progress));
            Color color = startColor;
            color.a *= 1f - progress;
            roomNumberText.color = color;
            yield return null;
        }
        roomNumberText.gameObject.SetActive(false);
        roomNumberText.rectTransform.anchoredPosition = startPosition;
        roomNumberText.color = startColor;
        roomNumberCoroutine = null;
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
        if (combat != null && combat.GetComponent<PlayerProgression>() is PlayerProgression progression
            && progression.BlocksGameplay) return;
        if (SaveManager.Instance != null && SaveManager.Instance.RunEnded) return;
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
