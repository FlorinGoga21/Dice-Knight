using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Two-dimensional music, gameplay SFX, and UI SFX with independent saved category volumes.</summary>
[DefaultExecutionOrder(-1100)]
public sealed class GameAudio : MonoBehaviour
{
    private const float CoinPickupClusterGap = 0.15f;

    [Header("---Music Clips---")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip mapMusic;
    [SerializeField] private AudioClip roomMusic;
    [SerializeField] private AudioClip combatMusic;
    [SerializeField] private AudioClip bossMusic;
    [Tooltip("Optional room-specific music. An empty field uses Room Music.")]
    [SerializeField] private AudioClip shopMusic;
    [SerializeField] private AudioClip treasureMusic;
    [SerializeField] private AudioClip eventMusic;
    [SerializeField, Min(0f)] private float musicFadeDuration = 0.4f;

    [Header("---Menu Sound Effects---")]
    [SerializeField] private AudioClip uiFocus;
    [SerializeField] private AudioClip uiConfirm;
    [SerializeField] private AudioClip uiBack;
    [SerializeField] private AudioClip settingChanged;
    [SerializeField] private AudioClip pause;
    [SerializeField] private AudioClip resume;
    [SerializeField] private AudioClip saveSuccess;
    [SerializeField] private AudioClip loadSuccess;
    [SerializeField] private AudioClip deleteSave;

    [Header("---Combat Sound Effects---")]
    [SerializeField] private AudioClip encounterStarted;
    [SerializeField] private AudioClip encounterWon;
    [SerializeField] private AudioClip turnStarted;
    [SerializeField] private AudioClip diceRoll;
    [SerializeField] private AudioClip diceSettled;
    [SerializeField] private AudioClip rollWin;
    [SerializeField] private AudioClip rollLose;
    [SerializeField] private AudioClip rollTie;
    [SerializeField] private AudioClip blockRaised;
    [SerializeField] private AudioClip blockImpact;
    [SerializeField] private AudioClip healingPotion;

    [Header("---World Sound Effects---")]
    [SerializeField] private AudioClip coinPickup;
    [SerializeField] private AudioClip mapTravel;
    [SerializeField] private AudioClip roomEntered;
    [SerializeField] private AudioClip roomCompleted;

    [Header("---Audio Sources---")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource gameplaySource;
    [SerializeField] private AudioSource uiSource;

    private static GameAudio instance;
    private Coroutine musicFade;
    private AudioClip requestedMusic;
    private float musicGain = 1f;
    private float lastFocusTime = -1f;
    private float lastSettingTime = -1f;
    private float lastCoinPickupTime = float.NegativeInfinity;
    private bool sceneInitialized;
    public static GameAudio Instance => instance;

    public enum Cue
    {
        UiFocus, UiConfirm, UiBack, SettingChanged, Pause, Resume, SaveSuccess, LoadSuccess, DeleteSave,
        EncounterStarted, EncounterWon, TurnStarted, DiceRoll, DiceSettled, RollWin, RollLose, RollTie,
        BlockRaised, BlockImpact, HealingPotion, CoinPickup, MapTravel, RoomEntered, RoomCompleted
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSingleton() => instance = null;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        musicSource = Prepare(musicSource, "Music", true);
        gameplaySource = Prepare(gameplaySource, "Gameplay SFX", false);
        uiSource = Prepare(uiSource, "UI SFX", false);
        GamePreferences.Load();
        GamePreferences.VolumeChanged += ApplyVolumes;
        ApplyVolumes();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private AudioSource Prepare(AudioSource source, string sourceName, bool loop)
    {
        if (source == null)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            source = child.AddComponent<AudioSource>();
        }
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        return source;
    }

    private void Start()
    {
        if (instance != this) return;
        if (!sceneInitialized) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        GamePreferences.VolumeChanged -= ApplyVolumes;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }

    private void OnApplicationPause(bool paused) { if (paused && instance == this) GamePreferences.Save(); }
    private void OnApplicationQuit() { if (instance == this) GamePreferences.Save(); }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneInitialized = true;
        gameplaySource.Stop();
        lastCoinPickupTime = float.NegativeInfinity;
        PlayMusic(SceneMusic(scene.name));
        foreach (var selectable in FindObjectsByType<UnityEngine.UI.Selectable>(FindObjectsInactive.Include))
        {
            if (selectable.gameObject.scene != scene || selectable.GetComponent<UIAudioFeedback>() != null) continue;
            selectable.gameObject.AddComponent<UIAudioFeedback>();
        }
        if (scene.name == "MainScene") Play(Cue.RoomEntered);
    }

    private AudioClip SceneMusic(string name)
    {
        if (name == "MainMenu") return mainMenuMusic;
        if (name == "MapScene") return mapMusic;
        if (name != "MainScene") return null;
        switch (RunMapState.CurrentRoomType)
        {
            case RunMapState.RoomType.Shop: return shopMusic != null ? shopMusic : roomMusic;
            case RunMapState.RoomType.Treasure: return treasureMusic != null ? treasureMusic : roomMusic;
            case RunMapState.RoomType.Random: return eventMusic != null ? eventMusic : roomMusic;
            case RunMapState.RoomType.Boss: return bossMusic != null ? bossMusic : roomMusic;
            default: return roomMusic;
        }
    }

    public void BeginCombat()
    {
        Play(Cue.EncounterStarted);
        var clip = RunMapState.CurrentRoomType == RunMapState.RoomType.Boss && bossMusic != null ? bossMusic : combatMusic;
        if (clip != null) PlayMusic(clip);
    }

    public void EndCombat(bool won)
    {
        if (won) Play(Cue.EncounterWon);
        PlayMusic(SceneMusic(SceneManager.GetActiveScene().name));
    }

    public void SetPaused(bool paused)
    {
        if (paused) gameplaySource.Pause(); else gameplaySource.UnPause();
        Play(paused ? Cue.Pause : Cue.Resume);
    }

    public static void Play(Cue cue)
    {
        if (instance == null) return;
        if (cue == Cue.UiFocus)
        {
            if (Time.unscaledTime - instance.lastFocusTime < 0.06f) return;
            instance.lastFocusTime = Time.unscaledTime;
        }
        if (cue == Cue.SettingChanged)
        {
            if (Time.unscaledTime - instance.lastSettingTime < 0.08f) return;
            instance.lastSettingTime = Time.unscaledTime;
        }
        AudioClip clip = instance.ClipFor(cue);
        if (clip == null) return;
        if (cue == Cue.CoinPickup)
        {
            // Keep one sound for consecutive arrivals, rearming after a gap between clusters.
            float now = Time.time;
            bool sameCluster = now - instance.lastCoinPickupTime < CoinPickupClusterGap;
            instance.lastCoinPickupTime = now;
            if (sameCluster) return;
        }
        AudioSource source = cue <= Cue.DeleteSave ? instance.uiSource : instance.gameplaySource;
        source.PlayOneShot(clip);
    }

    public static void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (instance != null && clip != null) instance.gameplaySource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioClip ClipFor(Cue cue)
    {
        switch (cue)
        {
            case Cue.UiFocus: return uiFocus;
            case Cue.UiConfirm: return uiConfirm;
            case Cue.UiBack: return uiBack;
            case Cue.SettingChanged: return settingChanged;
            case Cue.Pause: return pause;
            case Cue.Resume: return resume;
            case Cue.SaveSuccess: return saveSuccess;
            case Cue.LoadSuccess: return loadSuccess;
            case Cue.DeleteSave: return deleteSave;
            case Cue.EncounterStarted: return encounterStarted;
            case Cue.EncounterWon: return encounterWon;
            case Cue.TurnStarted: return turnStarted;
            case Cue.DiceRoll: return diceRoll;
            case Cue.DiceSettled: return diceSettled;
            case Cue.RollWin: return rollWin;
            case Cue.RollLose: return rollLose;
            case Cue.RollTie: return rollTie;
            case Cue.BlockRaised: return blockRaised;
            case Cue.BlockImpact: return blockImpact;
            case Cue.HealingPotion: return healingPotion;
            case Cue.CoinPickup: return coinPickup;
            case Cue.MapTravel: return mapTravel;
            case Cue.RoomEntered: return roomEntered;
            case Cue.RoomCompleted: return roomCompleted;
            default: return null;
        }
    }

    private void ApplyVolumes()
    {
        musicSource.volume = GamePreferences.MusicVolume * musicGain;
        gameplaySource.volume = uiSource.volume = GamePreferences.SfxVolume;
    }

    private void PlayMusic(AudioClip clip)
    {
        if (requestedMusic == clip && (musicFade != null || musicSource.clip == clip)) return;
        requestedMusic = clip;
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(ChangeMusic(clip));
    }

    private IEnumerator ChangeMusic(AudioClip clip)
    {
        if (musicSource.isPlaying) yield return FadeMusic(0f);
        musicSource.Stop();
        musicSource.clip = clip;
        musicGain = 0f;
        ApplyVolumes();
        if (clip != null)
        {
            musicSource.Play();
            yield return FadeMusic(1f);
        }
        musicFade = null;
    }

    private IEnumerator FadeMusic(float target)
    {
        float start = musicGain;
        for (float elapsed = 0f; elapsed < musicFadeDuration; elapsed += Time.unscaledDeltaTime)
        {
            musicGain = Mathf.Lerp(start, target, elapsed / musicFadeDuration);
            ApplyVolumes();
            yield return null;
        }
        musicGain = target;
        ApplyVolumes();
    }
}
