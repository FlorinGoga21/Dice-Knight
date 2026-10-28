/// <summary>Die abilities consume player statistics without changing them.</summary>
public static class DiceRules
{
    public static int Roll(DiceAbility ability)
    {
        if (ability == DiceAbility.Even || ability == DiceAbility.Odd)
        {
            // Low/middle/high faces use 6/15, 5/15 and 4/15 tickets for both types.
            int ticket = UnityEngine.Random.Range(0, 15);
            int evenFace = ticket < 6 ? 2 : ticket < 11 ? 4 : 6;
            return ability == DiceAbility.Odd ? evenFace - 1 : evenFace;
        }
        return UnityEngine.Random.Range(1, 7);
    }

    public static bool CanTriggerLucky(DiceAbility ability, int face, float luckChance, float chanceRoll)
    {
        return ability == DiceAbility.Lucky && face >= 1 && face < 6
            && luckChance > 0f && chanceRoll >= 0f && chanceRoll < luckChance;
    }

    public static bool TryApplyLucky(int[] results, int index, int reroll)
    {
        if (results == null || index < 0 || index >= results.Length
            || results[index] < 1 || results[index] >= 6 || reroll <= results[index] || reroll > 6) return false;
        results[index] = reroll;
        return true;
    }
}
