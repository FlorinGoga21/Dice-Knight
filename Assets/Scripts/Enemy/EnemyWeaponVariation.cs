using System;
using UnityEngine;

/// <summary>Chooses a weapon for this enemy while preserving its existing rig and attack behavior.</summary>
public sealed class EnemyWeaponVariation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer weaponRenderer;
    [Tooltip("Allowed melee weapons. Sprite names must be unique so saves can restore them.")]
    [SerializeField] private Sprite[] weapons;
    [Tooltip("Removed weapons in older saves are replaced with the first allowed weapon.")]
    [SerializeField] private string[] retiredWeaponIds = Array.Empty<string>();

    public string WeaponId => weaponRenderer != null && weaponRenderer.sprite != null
        ? weaponRenderer.sprite.name : null;

    public void RandomizeWeapon()
    {
        if (weaponRenderer == null || weapons == null || weapons.Length == 0) return;
        weaponRenderer.sprite = weapons[UnityEngine.Random.Range(0, weapons.Length)];
    }

    public void RestoreWeapon(string weaponId)
    {
        // Older saves keep the prefab's original sword.
        if (string.IsNullOrEmpty(weaponId)) return;
        Sprite weapon = Array.Find(weapons, item => item != null && item.name == weaponId);
        if (weapon == null && Array.IndexOf(retiredWeaponIds, weaponId) >= 0)
            weapon = Array.Find(weapons, item => item != null);
        if (weapon == null)
            throw new InvalidOperationException($"{name}: saved weapon '{weaponId}' is missing from the weapon pool.");
        weaponRenderer.sprite = weapon;
    }
}
