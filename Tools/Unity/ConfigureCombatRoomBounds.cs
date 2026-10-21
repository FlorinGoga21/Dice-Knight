using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Adds combat boundaries to MainScene only; never edits MainMenu or shared player prefabs.</summary>
public static class ConfigureCombatRoomBounds
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before configuring boundaries.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save existing scene edits first.");

        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindAnyObjectByType<RoomSceneController>();
            if (controller == null) throw new Exception("MainScene is missing its room completion controller.");
            var bounds = controller.GetComponent<CombatRoomBounds>();
            if (bounds == null) bounds = controller.gameObject.AddComponent<CombatRoomBounds>();
            var settings = new SerializedObject(bounds);
            settings.FindProperty("roomCamera").objectReferenceValue = Camera.main;
            settings.FindProperty("player").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            settings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(bounds);
            EditorSceneManager.SaveScene(scene);
            return new { scene = scene.path, controller = controller.name, exitPadding = 3f };
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
}
