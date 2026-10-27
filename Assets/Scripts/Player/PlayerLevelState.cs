using System;
using UnityEngine;

public enum PlayerUpgrade { Health, Luck, Damage }

/// <summary>Mutable progression for one run. Independent of scenes and UI.</summary>
[Serializable]
public sealed class PlayerLevelState
{
    public const int ExperiencePerEnemy = 100;
    public const int HealthPerUpgrade = HealthHeartsUI.HealthPerHalfHeart;
    public const int DamagePerUpgrade = 10;
    public const int DamageLevelInterval = 3;
    public const int MaximumLuckUpgrades = 5;
    public const int MaximumLevel = int.MaxValue / 100;

    [SerializeField] private int level = 1;
    [SerializeField] private int experience;
    [SerializeField] private int healthUpgrades;
    [SerializeField] private int luckUpgrades;
    [SerializeField] private int damageUpgrades;

    public int Level => level;
    public int Experience => experience;
    public int ExperienceToNextLevel => level * 100;
    public int SpentUpgrades => healthUpgrades + luckUpgrades + damageUpgrades;
    public int PendingUpgrades => level - 1 - SpentUpgrades;
    public int UpgradeLevel => SpentUpgrades + 2;
    public float LuckChance => luckUpgrades * 0.05f;
    public int BonusDamage => damageUpgrades * DamagePerUpgrade;
    public bool CanUpgradeLuck => luckUpgrades < MaximumLuckUpgrades;
    public bool CanUpgradeDamage => PendingUpgrades > 0 && UpgradeLevel % DamageLevelInterval == 0;

    public void GrantExperience(int amount)
    {
        if (amount <= 0) return;
        long remaining = (long)experience + amount;
        while (level < MaximumLevel && remaining >= ExperienceToNextLevel)
        {
            remaining -= ExperienceToNextLevel;
            level++;
        }
        experience = (int)Math.Min(remaining, ExperienceToNextLevel - 1L);
    }

    public bool TryChooseUpgrade(PlayerUpgrade upgrade)
    {
        if (PendingUpgrades <= 0) return false;
        switch (upgrade)
        {
            case PlayerUpgrade.Health: healthUpgrades++; return true;
            case PlayerUpgrade.Luck:
                if (!CanUpgradeLuck) return false;
                luckUpgrades++;
                return true;
            case PlayerUpgrade.Damage:
                if (!CanUpgradeDamage) return false;
                damageUpgrades++;
                return true;
            default: return false;
        }
    }

    public bool TryApplyLuck(int[] results, float chanceRoll, int reroll)
    {
        if (LuckChance <= 0f || chanceRoll < 0f || chanceRoll >= LuckChance
            || results == null || results.Length == 0 || reroll < 1 || reroll > 6) return false;
        int lowest = GetLuckDieIndex(results);
        if (lowest < 0 || reroll <= results[lowest]) return false;
        results[lowest] = reroll;
        return true;
    }

    public static int GetLuckDieIndex(int[] results)
    {
        if (results == null || results.Length == 0) return -1;
        int lowest = 0;
        for (int i = 0; i < results.Length; i++)
        {
            if (results[i] < 1 || results[i] > 6) return -1;
            if (results[i] < results[lowest]) lowest = i;
        }
        return results[lowest] < 6 ? lowest : -1;
    }

    public static int GetDiceMultiplier(int[] results)
    {
        if (results == null || results.Length < 2) return 1;
        int highestCount = 1;
        for (int i = 0; i < results.Length; i++)
        {
            int count = 0;
            for (int j = 0; j < results.Length; j++)
                if (results[j] == results[i]) count++;
            highestCount = Math.Max(highestCount, count);
        }
        return highestCount >= 3 ? 3 : highestCount == 2 ? 2 : 1;
    }

    public bool IsValid()
    {
        if (level < 1 || level > MaximumLevel || experience < 0 || experience >= ExperienceToNextLevel
            || healthUpgrades < 0 || luckUpgrades < 0 || luckUpgrades > MaximumLuckUpgrades || damageUpgrades < 0) return false;
        long spent = (long)healthUpgrades + luckUpgrades + damageUpgrades;
        return spent < level && damageUpgrades <= (spent + 1) / DamageLevelInterval;
    }

    public PlayerLevelState Copy() => new PlayerLevelState
    {
        level = level, experience = experience, healthUpgrades = healthUpgrades,
        luckUpgrades = luckUpgrades, damageUpgrades = damageUpgrades
    };
}
