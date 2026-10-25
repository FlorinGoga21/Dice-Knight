using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Matches the shared player/enemy flash to Dice Demon without copying its normal palette.</summary>
public static class MatchDamageFlash
{
    public static object Main()
    {
        var demon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/DiceDemon.prefab");
        var referenceSettings = new SerializedObject(demon.GetComponent<EnemyController>());
        var reference = (Material)referenceSettings.FindProperty("damageFlashMaterial").objectReferenceValue;
        float duration = referenceSettings.FindProperty("damageFlashDuration").floatValue;
        var shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerFlashMaterial.mat");
        var paths = new[] { "Assets/Prefabs/Enemies/Skeleton.prefab", "Assets/Prefabs/Enemies/Zombie.prefab",
            "Assets/Prefabs/Enemies/Slime.prefab", "Assets/Art/Player/Knight.prefab" };
        // Read effective prefab values so variants such as Zombie are included without creating overrides.
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Component controller = root.GetComponent<EnemyController>();
            if (controller == null) controller = root.GetComponent<PlayerController>();
            var settings = new SerializedObject(controller);
            if (settings.FindProperty("damageFlashMaterial").objectReferenceValue != shared
                || !Mathf.Approximately(settings.FindProperty("damageFlashDuration").floatValue, duration))
                throw new InvalidOperationException(path + " has different flash wiring; inspect before changing it.");
        }
        Undo.RecordObject(shared, "Match Dice Demon damage flash");
        shared.SetColor("_FlashColor", reference.GetColor("_FlashColor"));
        shared.SetFloat("_FlashAmount", 0f);
        EditorUtility.SetDirty(shared);
        AssetDatabase.SaveAssetIfDirty(shared);
        return new { characters = paths.Concat(new[] { "Assets/Prefabs/Enemies/DiceDemon.prefab" }).ToArray(),
            color = shared.GetColor("_FlashColor").ToString(), duration };
    }
}
