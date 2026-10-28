using UnityEngine;

// Append abilities; existing values may be referenced by authored assets.
public enum DiceAbility { None = 0, Lucky = 1, Even = 2, Odd = 3 }

/// <summary>Reusable die identity, visuals and ability; never mutable run state.</summary>
[CreateAssetMenu(menuName = "Dice Knight/Dice Definition")]
public sealed class DiceDefinition : ScriptableObject
{
    [Header("---Identity---")]
    [Tooltip("Stable save identifier. Do not rename after use in saves.")]
    [SerializeField] private string saveId;
    [SerializeField] private DiceAbility ability;
    [Header("---Visuals---")]
    [Tooltip("Faces ordered from one to six pips.")]
    [SerializeField] private Sprite[] pipFaces;
    [SerializeField] private Sprite[] rollFrames;

    public string SaveId => saveId;
    public DiceAbility Ability => ability;
    public int InitialFace => ability == DiceAbility.Even ? 2 : 1;
    public Sprite GetFace(int result) => pipFaces != null && result >= 1 && result <= pipFaces.Length ? pipFaces[result - 1] : null;
    public int FrameCount => rollFrames != null ? rollFrames.Length : 0;
    public Sprite GetFrame(int index) => FrameCount > 0 ? rollFrames[index % FrameCount] : null;
}
