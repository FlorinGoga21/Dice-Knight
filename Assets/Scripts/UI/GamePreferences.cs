using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Device preferences, independent of run save slots.</summary>
public static class GamePreferences
{
    private const string MusicKey = "DiceKnight.Settings.MusicVolume";
    private const string SfxKey = "DiceKnight.Settings.SfxVolume";
    private const string FullscreenKey = "DiceKnight.Settings.Fullscreen";
    private const string ResolutionWidthKey = "DiceKnight.Settings.ResolutionWidth";
    private const string ResolutionHeightKey = "DiceKnight.Settings.ResolutionHeight";
    private static bool loaded;
    private static float musicVolume;
    private static float sfxVolume;
    private static bool fullscreen;
    private static Vector2Int resolution;

    public static event Action VolumeChanged;
    public static float MusicVolume { get { Load(); return musicVolume; } }
    public static float SfxVolume { get { Load(); return sfxVolume; } }
    public static bool Fullscreen { get { Load(); return fullscreen; } }
    public static Vector2Int Resolution { get { Load(); return resolution; } }

    public static Vector2Int[] GetAvailableResolutions()
    {
        var sizes = new List<Vector2Int>();
        foreach (var mode in Screen.resolutions)
        {
            var size = new Vector2Int(mode.width, mode.height);
            if (!sizes.Contains(size)) sizes.Add(size);
        }
        var current = new Vector2Int(Screen.width, Screen.height);
        if (!sizes.Contains(current)) sizes.Add(current);
        sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return sizes.ToArray();
    }

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
        resolution = new Vector2Int(PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width),
            PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height));
        if (resolution.x <= 0 || resolution.y <= 0
            || Array.IndexOf(GetAvailableResolutions(), resolution) < 0)
            resolution = new Vector2Int(Screen.width, Screen.height);
        ApplyDisplay();
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
        ApplyDisplay();
    }

    public static void SetResolution(Vector2Int value)
    {
        Load();
        if (value.x <= 0 || value.y <= 0) return;
        resolution = value;
        PlayerPrefs.SetInt(ResolutionWidthKey, value.x);
        PlayerPrefs.SetInt(ResolutionHeightKey, value.y);
        ApplyDisplay();
    }

    private static void ApplyDisplay()
    {
#if !UNITY_EDITOR
        Screen.SetResolution(resolution.x, resolution.y,
            fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
#endif
    }

    public static void Save() => PlayerPrefs.Save();
}
