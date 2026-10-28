using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HeyHeyThere.SceneTransitionsFree;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Owns the selected slot, run stats, scene restoration, and autosave timer.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class SaveManager : MonoBehaviour
{
    [SerializeField] private bool autosaveEnabled = true;
    [SerializeField, Min(1f)] private float autosaveInterval = 120f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.8f;

    private static SaveManager instance;
    public static SaveManager Instance => instance;
    public int ActiveSlot { get; private set; }
    public string SaveDirectory { get; private set; }
    public bool IsLoading { get; private set; }
    public bool RunEnded { get; private set; }

    private int endedSlot;

    private PlayerSaveData runPlayer;
    private SaveGameData pendingLoad;
    private float playSeconds;
    private float autosaveElapsed;
    private bool worldReady;
    private bool sceneInitialized;
    private bool saveOnReady;
    private readonly List<TrackedEnemy> enemies = new List<TrackedEnemy>();

    private sealed class TrackedEnemy
    {
        internal EnemyStats stats;
        internal EnemySaveData original;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingleton() => instance = null;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        SaveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void Start()
    {
        // Entering Play mode directly in a room may load it before our scene callback is registered.
        if (!sceneInitialized) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void Update()
    {
        if (instance != this || !worldReady || Time.timeScale <= 0f || SceneManager.GetActiveScene().name == "MainMenu") return;
        playSeconds += Time.unscaledDeltaTime;
        if (!autosaveEnabled || ActiveSlot == 0) return;
        autosaveElapsed += Time.unscaledDeltaTime;
        if (autosaveElapsed < autosaveInterval || !CanSave(out _)) return;
        if (!TrySave(ActiveSlot, out string message))
        {
            autosaveElapsed = Mathf.Max(0f, autosaveInterval - 15f);
            Debug.LogWarning("Autosave: " + message);
        }
    }

    public bool CanSave(out string message)
    {
        message = "";
        string scene = SceneManager.GetActiveScene().name;
        if (RunEnded) message = "This run has ended. Start a new run.";
        else if (!worldReady || IsLoading || SceneTransition.IsPlaying || (scene != "MapScene" && scene != "MainScene"))
            message = "Wait for the scene to finish loading.";
        else if (scene == "MapScene")
        {
            var map = FindAnyObjectByType<RunMapController>();
            if (map == null || map.IsBusy || RunMapState.Rooms.Count == 0) message = "Wait until the player reaches the room.";
        }
        else
        {
            var player = FindAnyObjectByType<PlayerStats>();
            var combat = FindAnyObjectByType<CombatController>();
            if (player == null || player.IsDead) message = "A living player is required to save.";
            else if (combat != null && combat.IsInCombat) message = "Saving and loading are unavailable during combat.";
            else if (FindAnyObjectByType<CoinPickup>() != null) message = "Resume and let the coin pickups finish before saving.";
        }
        return message.Length == 0;
    }

    public bool TryStartSlot(int slot, out string scene, out string message)
    {
        scene = "MapScene";
        SaveGameData data;
        if (SaveFileStore.Exists(SaveDirectory, slot))
        {
            if (!SaveFileStore.TryRead(SaveDirectory, slot, out data, out message)) return false;
        }
        else
        {
            data = new SaveGameData { slot = slot, savedUtc = DateTime.UtcNow.ToString("O") };
            if (!SaveFileStore.TryWrite(SaveDirectory, data, out message)) return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(data.sceneName)) { message = "The saved scene is unavailable."; return false; }
        PrepareLoad(data);
        scene = data.sceneName;
        return true;
    }

    public bool TryLoad(int slot, out string message)
    {
        if (RunEnded) { message = "This run has ended. Return to the main menu to choose another run."; return false; }
        if (FindAnyObjectByType<CombatController>() is CombatController combat && combat.IsInCombat)
        { message = "Saving and loading are unavailable during combat."; return false; }
        if (IsLoading || SceneTransition.IsPlaying) { message = "A scene transition is already running."; return false; }
        if (!SaveFileStore.TryRead(SaveDirectory, slot, out var data, out message)) return false;
        if (!Application.CanStreamedLevelBeLoaded(data.sceneName)) { message = "The saved scene is unavailable."; return false; }
        PrepareLoad(data);
        foreach (var pause in FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include)) pause.PrepareForSceneLoad();
        Time.timeScale = 0f;
        SceneTransition.ChangeScene(data.sceneName, "clouds", new SceneTransition.Options { Duration = transitionDuration });
        return true;
    }

    private void PrepareLoad(SaveGameData data)
    {
        RunEnded = false;
        endedSlot = 0;
        ActiveSlot = data.slot;
        pendingLoad = data;
        runPlayer = data.player;
        playSeconds = data.playSeconds;
        autosaveElapsed = 0f;
        IsLoading = true;
        worldReady = false;
        saveOnReady = data.map.rooms.Count == 0 && data.sceneName == "MapScene";
        RunMapState.Restore(data.map);
    }

    public bool TrySave(int slot, out string message)
    {
        if (!CanSave(out message)) return false;
        CaptureRunPlayer();
        var data = new SaveGameData
        {
            slot = slot, savedUtc = DateTime.UtcNow.ToString("O"),
            sceneName = SceneManager.GetActiveScene().name, playSeconds = playSeconds,
            map = RunMapState.Capture(), player = runPlayer
        };
        if (data.sceneName == "MainScene")
        {
            data.room = new RoomSaveData { mapRoom = RunMapState.CurrentRoomIndex };
            foreach (var tracked in enemies)
            {
                var state = tracked.stats != null ? CaptureEnemy(tracked.stats) : tracked.original;
                if (tracked.stats == null) { state.defeated = true; state.health = 0; }
                data.room.enemies.Add(state);
            }
            FindAnyObjectByType<CombatController>()?.CaptureRoomSave(data.room);
            FindAnyObjectByType<CombatRoomBounds>()?.CaptureSave(data.room);
        }
        if (!SaveFileStore.TryWrite(SaveDirectory, data, out message)) return false;
        ActiveSlot = slot;
        autosaveElapsed = 0f;
        return true;
    }

    public bool TryDeleteSlot(int slot, out string message)
    {
        if (IsLoading || SceneTransition.IsPlaying)
        { message = "Wait for the scene transition to finish."; return false; }
        if (FindAnyObjectByType<CombatController>() is CombatController combat && combat.IsInCombat)
        { message = "Save slots cannot be deleted during combat."; return false; }
        if (!SaveFileStore.TryDelete(SaveDirectory, slot, out message)) return false;
        if (ActiveSlot == slot)
        {
            ActiveSlot = 0;
            autosaveElapsed = 0f;
        }
        return true;
    }

    public void CaptureRunPlayer()
    {
        if (RunEnded) return;
        var player = FindAnyObjectByType<PlayerStats>();
        if (player == null) return;
        var controller = player.GetComponent<PlayerController>();
        var body = player.GetComponent<Rigidbody2D>();
        var diceIds = player.GetComponent<CombatController>()?.CaptureDiceIds();
        runPlayer = new PlayerSaveData
        {
            health = player.CurrentHealth, maxHealth = player.MaxHealthValue, gold = player.Currency,
            hasProgression = player.GetComponent<PlayerProgression>() != null,
            progression = player.GetComponent<PlayerProgression>()?.CaptureSave(),
            equippedDiceIds = diceIds, hasDiceLoadout = diceIds != null,
            potions = player.GetComponent<CombatController>()?.HealingPotionsRemaining ?? 0,
            position = player.transform.position, rotation = player.transform.rotation,
            velocity = body != null ? body.linearVelocity : Vector2.zero,
            facingRight = controller == null || controller.IsFacingRight,
            cameraPosition = Camera.main != null ? Camera.main.transform.position : Vector3.zero
        };
    }

    public bool TryEndRun(out string message)
    {
        if (!RunEnded)
        {
            endedSlot = ActiveSlot;
            RunEnded = true;
            StopAllCoroutines();
            ActiveSlot = 0;
            pendingLoad = null;
            runPlayer = null;
            playSeconds = autosaveElapsed = 0f;
            worldReady = saveOnReady = IsLoading = false;
            enemies.Clear();
            RunMapState.ResetRun();
        }
        message = "";
        return endedSlot == 0 || SaveFileStore.TryEndRun(SaveDirectory, endedSlot, out message);
    }

    public bool TryStartNewRun(out string message)
    {
        if (!RunEnded) { message = "The current run has not ended."; return false; }
        if (!Application.CanStreamedLevelBeLoaded("MapScene")) { message = "The map scene is unavailable."; return false; }
        if (!TryEndRun(out message)) return false;
        // A direct Editor run has no selected slot. The new run can be saved from Pause later.
        int slot = endedSlot;
        var data = new SaveGameData { slot = slot, savedUtc = DateTime.UtcNow.ToString("O") };
        if (slot != 0 && !SaveFileStore.TryWrite(SaveDirectory, data, out message)) return false;
        PrepareLoad(data);
        return true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        sceneInitialized = true;
        bool resumeAfterLoad = IsLoading;
        if (resumeAfterLoad) Time.timeScale = 0f;
        worldReady = false;
        enemies.Clear();
        if (scene.name == "MainScene")
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var spawner in root.GetComponentsInChildren<EnemySpawner>())
                    if (spawner.isActiveAndEnabled) spawner.SpawnRoom(pendingLoad?.room);

            foreach (var root in scene.GetRootGameObjects())
                foreach (var enemy in root.GetComponentsInChildren<EnemyStats>(true))
                    enemies.Add(new TrackedEnemy { stats = enemy, original = CaptureEnemy(enemy) });
            if (runPlayer != null)
            {
                var player = FindAnyObjectByType<PlayerStats>();
                if (player != null)
                {
                    player.RestoreSave(runPlayer);
                    player.GetComponent<CombatController>()?.RestoreRoomSave(runPlayer.potions, pendingLoad?.room);
                    if (pendingLoad?.room != null)
                    {
                        player.GetComponent<PlayerController>()?.RestoreSaveTransform(runPlayer);
                        if (Camera.main != null) Camera.main.transform.position = runPlayer.cameraPosition;
                    }
                }
            }
            if (pendingLoad?.room != null)
                foreach (var saved in pendingLoad.room.enemies)
                    enemies.Find(e => e.original.id == saved.id)?.stats?.RestoreSave(saved);
            FindAnyObjectByType<CombatRoomBounds>()?.Initialize(pendingLoad?.room);
        }
        pendingLoad = null;
        StartCoroutine(SceneReady(scene, resumeAfterLoad));
    }

    private IEnumerator SceneReady(Scene scene, bool resumeAfterLoad)
    {
        yield return null; // Scene Start methods initialize map and UI before a checkpoint is written.
        while (SceneTransition.IsPlaying) yield return null;
        if (scene != SceneManager.GetActiveScene()) yield break;
        worldReady = true;
        IsLoading = false;
        if (resumeAfterLoad) Time.timeScale = 1f;
        if (saveOnReady)
        {
            saveOnReady = false;
            if (ActiveSlot != 0 && !TrySave(ActiveSlot, out string message)) Debug.LogWarning(message);
        }
    }

    private static EnemySaveData CaptureEnemy(EnemyStats enemy)
    {
        var data = new EnemySaveData
        {
            id = enemy.SaveId, spawnerId = enemy.SpawnerId, prefabId = enemy.PrefabId,
            weaponId = enemy.GetComponent<EnemyWeaponVariation>()?.WeaponId,
            health = enemy.CurrentHealth, maxHealth = enemy.MaxHealth,
            defeated = enemy.IsDead || !enemy.gameObject.activeSelf,
            position = enemy.transform.position, rotation = enemy.transform.rotation
        };
        enemy.GetComponent<EnemyFlight>()?.CaptureSave(data);
        return data;
    }
}
