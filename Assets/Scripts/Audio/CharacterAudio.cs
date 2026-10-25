using UnityEngine;

/// <summary>Assign separate clips on Knight and each enemy prefab. Empty slots are silent.</summary>
public sealed class CharacterAudio : MonoBehaviour
{
    [Header("---Character Sound Effects---")]
    [SerializeField] private AudioClip[] footsteps;
    [SerializeField] private AudioClip jump;
    [SerializeField] private AudioClip land;
    [SerializeField] private AudioClip attack;
    [SerializeField] private AudioClip hurt;
    [SerializeField] private AudioClip death;
    [SerializeField] private AudioClip projectileLaunch;
    [SerializeField] private AudioClip projectileImpact;

    [Header("---Playback---")]
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 0.45f;
    [Tooltip("Seconds between movement sounds while grounded. No sounds play without assigned clips.")]
    [SerializeField, Min(0.05f)] private float footstepInterval = 0.28f;
    private float stepTimer;
    private System.Random variation;

    private void Awake() => variation = new System.Random(System.Guid.NewGuid().GetHashCode());

    public void UpdateFootsteps(bool moving)
    {
        if (!moving || Time.timeScale <= 0f) { stepTimer = 0f; return; }
        if (footsteps == null || footsteps.Length == 0) return;
        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;
        stepTimer = footstepInterval;
        GameAudio.PlaySfx(footsteps[variation.Next(footsteps.Length)], volume * footstepVolume);
    }

    public void Jump() => GameAudio.PlaySfx(jump, volume);
    public void Land() => GameAudio.PlaySfx(land, volume);
    public void Attack() => GameAudio.PlaySfx(attack, volume);
    public void Hurt() => GameAudio.PlaySfx(hurt, volume);
    public void Death() => GameAudio.PlaySfx(death, volume);
    public void ProjectileLaunch() => GameAudio.PlaySfx(projectileLaunch, volume);
    public void ProjectileImpact() => GameAudio.PlaySfx(projectileImpact, volume);
}
