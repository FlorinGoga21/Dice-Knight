using UnityEngine;

public partial class DiceRollUI
{
    [Header("---Dice Types---")]
    [Tooltip("One definition per slot, in HUD order. Empty entries use the legacy slot visuals and have no ability.")]
    [SerializeField] private DiceDefinition[] equippedDice;
    [Tooltip("Definitions that can be resolved when restoring stable dice IDs from a save.")]
    [SerializeField] private DiceDefinition[] availableDice;

    public DiceDefinition GetDefinition(int index) => equippedDice != null && index >= 0 && index < equippedDice.Length ? equippedDice[index] : null;

    public int RollFace(int index)
    {
        var definition = GetDefinition(index);
        return DiceRules.Roll(definition != null ? definition.Ability : DiceAbility.None);
    }

    private int GetRollFrameCount(int index)
    {
        var definition = GetDefinition(index);
        return definition != null ? definition.FrameCount : GetDiceVisuals(index)?.rollFrames?.Length ?? 0;
    }

    private Sprite GetRollFrame(int index, int frame)
    {
        var definition = GetDefinition(index);
        return definition != null ? definition.GetFrame(frame) : GetDiceVisuals(index).rollFrames[frame];
    }

    public Sprite GetBasicPipSprite(int result)
    {
        if (availableDice != null)
            foreach (var definition in availableDice)
                if (definition != null && definition.SaveId == "basic") return definition.GetFace(result);
        return GetLegacyPipSprite(0, result);
    }

    public bool TryGetLuckyReroll(int index, int face, float luckChance, out int reroll)
    {
        reroll = face;
        var definition = GetDefinition(index);
        if (index < 0 || index >= ActiveDiceCount || definition == null
            || definition.Ability != DiceAbility.Lucky || face < 1 || face >= 6 || luckChance <= 0f) return false;
        if (!DiceRules.CanTriggerLucky(definition.Ability, face, luckChance, Random.value)) return false;
        reroll = Random.Range(face + 1, 7);
        return true;
    }

    public string[] CaptureDiceIds()
    {
        var ids = new string[ActiveDiceCount];
        for (int i = 0; i < ids.Length; i++) ids[i] = GetDefinition(i) != null ? GetDefinition(i).SaveId : "basic";
        return ids;
    }

    public bool RestoreDiceIds(string[] ids)
    {
        if (ids == null) return true; // Old saves retain the authored starting loadout.
        if (ids.Length < 1 || ids.Length > GetDiceSlotCount()) return false;
        var restored = new DiceDefinition[ids.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            if (availableDice != null)
                foreach (var definition in availableDice)
                    if (definition != null && definition.SaveId == ids[i]) { restored[i] = definition; break; }
            if (restored[i] == null) return false; // Do not partially replace a loadout.
        }
        equippedDice = restored;
        activeDiceCount = ids.Length;
        UpdateDiceAvailability();
        HighlightAllActiveDice();
        for (int i = 0; i < activeDiceCount; i++) diceImages[i].sprite = GetPipSprite(i, GetDefinition(i).InitialFace);
        return true;
    }
}
