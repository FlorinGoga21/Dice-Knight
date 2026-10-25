using System;
using UnityEngine;

/// <summary>Device preferences, independent of run save slots.</summary>
public static class GamePreferences
{
    private const string MusicKey = "DiceKnight.Settings.MusicVolume";
    private const string SfxKey = "DiceKnight.Settings.SfxVolume";
    private const string FullscreenKey = "DiceKnight.Settings.Fullscreen";
    private static bool loaded;
    private static float musicVolume;
    private static float sfxVolume;
    private static bool fullscreen;

    public static event Action VolumeChanged;
    public static float MusicVolume { get { Load(); return musicVolume; } }
    public static float SfxVolume { get { Load(); return sfxVolume; } }
    public static bool Fullscreen { get { Load(); return fullscreen; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        loaded = false;
        VolumeChanged = null;
    }

    public static void Load()
    {
        if (loaded) return;
        loaded = true;
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicKey, 1f));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxKey, 1f));
        fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) != 0;
        ApplyFullscreen();
    }

    public static void SetMusicVolume(float value)
    {
        Load();
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MusicKey, musicVolume);
        VolumeChanged?.Invoke();
    }

    public static void SetSfxVolume(float value)
    {
        Load();
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxKey, sfxVolume);
        VolumeChanged?.Invoke();
    }

    public static void SetFullscreen(bool value)
    {
        Load();
        fullscreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        ApplyFullscreen();
    }

    private static void ApplyFullscreen()
    {
#if !UNITY_EDITOR
        Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
#endif
    }

    public static void Save() => PlayerPrefs.Save();
}
