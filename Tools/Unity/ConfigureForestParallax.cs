using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyGame.Environment;

/// <summary>Applies the forest's depth settings to its prefab and existing game scenes.</summary>
public static class ConfigureForestParallax
{
    private static readonly Dictionary<string, float> Factors = new Dictionary<string, float>
    {
        { "Layer_0000_9", 0f },       // Ground silhouette and extended foreground masks.
        { "Layer_0001_8", 0f },       // Grass stays anchored to the walking surface.
        { "Layer_0002_7", 0.12f },    // Near canopy follows the near tree plane.
        { "Layer_0003_6", 0.12f },
        { "Layer_0004_Lights", 0.38f },
        { "Layer_0005_5", 0.38f },   // Midground trees and their light shafts.
        { "Layer_0006_4", 0.58f },
        { "Layer_0007_Lights", 0.58f },
        { "Layer_0008_3", 0.78f },   // Deeper tree silhouettes.
        { "Layer_0009_2", 0.90f },
        { "Layer_0010_1", 1f },      // Available sky art; not added to existing scenes.
        { "Layer_0011_0", 1f }
    };

    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before configuring assets.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save existing scene edits before applying forest settings.");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        const string prefabPath = "Assets/Prefabs/Background.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        int prefabLayers;
        try
        {
            prefabLayers = Configure(prefab);
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        var configured = new Dictionary<string, int>();
        try
        {
            foreach (string path in new[] { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/MainScene.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = 0;
                foreach (var root in scene.GetRootGameObjects()) count += Configure(root);
                EditorSceneManager.SaveScene(scene);
                configured.Add(path, count);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        AssetDatabase.SaveAssets();
        return new { prefabLayers, scenes = configured };
    }

    private static int Configure(GameObject root)
    {
        int count = 0;
        foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.sprite == null || !Factors.TryGetValue(renderer.sprite.name, out float factor)) continue;
            var components = renderer.GetComponents<Background>();
            var background = components.FirstOrDefault(b => !PrefabUtility.IsAddedComponentOverride(b))
                ?? components.FirstOrDefault();
            foreach (var duplicate in components.Where(b => b != background))
                UnityEngine.Object.DestroyImmediate(duplicate);
            if (background == null) background = renderer.gameObject.AddComponent<Background>();
            var settings = new SerializedObject(background);
            settings.FindProperty("parallaxFactor").floatValue = factor;
            settings.FindProperty("useSpriteWidth").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            if (PrefabUtility.IsPartOfPrefabInstance(background))
                PrefabUtility.RecordPrefabInstancePropertyModifications(background);
            EditorUtility.SetDirty(background);
            count++;
        }
        return count;
    }
}
