using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates one shared audio prefab and adds clip slots to the existing character prefabs.</summary>
public static class ConfigureGameAudio
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before configuring audio.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene edits first.");
        foreach (string path in new[] { "Assets/Art/Player/Knight.prefab", "Assets/Prefabs/Enemies/Skeleton.prefab",
            "Assets/Prefabs/Enemies/Zombie.prefab", "Assets/Prefabs/Enemies/Slime.prefab", "Assets/Prefabs/Enemies/DiceDemon.prefab" })
        {
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (prefab.GetComponent<CharacterAudio>() == null)
                {
                    prefab.AddComponent<CharacterAudio>();
                    PrefabUtility.SaveAsPrefabAsset(prefab, path);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        const string audioPath = "Assets/Prefabs/GameAudio.prefab";
        if (!File.Exists(audioPath))
        {
            var root = new GameObject("Game Audio");
            try
            {
                var audio = root.AddComponent<GameAudio>();
                var fields = new SerializedObject(audio);
                string[] names = { "Music", "Gameplay SFX", "UI SFX" };
                string[] properties = { "musicSource", "gameplaySource", "uiSource" };
                for (int i = 0; i < names.Length; i++)
                {
                    var child = new GameObject(names[i]);
                    child.transform.SetParent(root.transform, false);
                    var source = child.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.spatialBlend = 0f;
                    source.loop = i == 0;
                    fields.FindProperty(properties[i]).objectReferenceValue = source;
                }
                fields.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, audioPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(audioPath);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string name in new[] { "MainMenu", "MapScene", "MainScene" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Single);
                if (UnityEngine.Object.FindAnyObjectByType<GameAudio>() == null)
                    PrefabUtility.InstantiatePrefab(audioPrefab, scene);
                // Keep scene-only controller copies covered as well as inherited prefab components.
                foreach (var controller in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
                    if (controller.GetComponent<CharacterAudio>() == null) controller.gameObject.AddComponent<CharacterAudio>();
                foreach (var controller in UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsInactive.Include))
                    if (controller.GetComponent<CharacterAudio>() == null) controller.gameObject.AddComponent<CharacterAudio>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new { audioPrefab = audioPath, scenes = 3, characters = 5, assignedClips = 0 };
    }
}
