using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

/// <summary>Preserves existing bindings and wires each gameplay EventSystem to the same UI actions.</summary>
public static class ConfigureControllerInput
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before configuring input.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene edits before configuring input.");

        const string path = "Assets/Settings/PlayerInputActions.inputactions";
        var editable = InputActionAsset.FromJson(File.ReadAllText(path));
        try
        {
            var pause = editable.FindAction("Player/Pause", true);
            AddBinding(pause, "<Gamepad>/start");
            var player = editable.FindActionMap("Player", true);
            var exit = player.FindAction("ReturnToMap") ?? player.AddAction("ReturnToMap", InputActionType.Button);
            AddBinding(exit, "<Keyboard>/q");
            AddBinding(exit, "<Gamepad>/buttonNorth");
            File.WriteAllText(path, editable.ToJson() + Environment.NewLine);
        }
        finally { UnityEngine.Object.DestroyImmediate(editable); }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string name in new[] { "MainMenu", "MapScene", "MainScene" })
            {
                var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity", OpenSceneMode.Single);
                var systems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
                if (systems.Length != 1) throw new InvalidOperationException(name + " must have exactly one EventSystem.");
                var module = systems[0].GetComponent<InputSystemUIInputModule>();
                if (module == null) throw new InvalidOperationException(name + " is missing its Input System UI module.");
                module.actionsAsset = asset;
                var temporaryReferences = new[] { module.move, module.submit, module.cancel, module.point,
                    module.leftClick, module.rightClick, module.middleClick, module.scrollWheel,
                    module.trackedDevicePosition, module.trackedDeviceOrientation };
                // SwapAction compares action identity, so clear generated references before assigning subassets.
                module.move = module.submit = module.cancel = module.point = module.leftClick = module.rightClick =
                    module.middleClick = module.scrollWheel = module.trackedDevicePosition = module.trackedDeviceOrientation = null;
                module.move = Reference(asset, "Navigate");
                module.submit = Reference(asset, "Submit");
                module.cancel = Reference(asset, "Cancel");
                module.point = Reference(asset, "Point");
                module.leftClick = Reference(asset, "Click");
                module.rightClick = Reference(asset, "RightClick");
                module.middleClick = Reference(asset, "MiddleClick");
                module.scrollWheel = Reference(asset, "ScrollWheel");
                module.trackedDevicePosition = Reference(asset, "TrackedDevicePosition");
                module.trackedDeviceOrientation = Reference(asset, "TrackedDeviceOrientation");
                foreach (var reference in temporaryReferences)
                    if (reference != null && !AssetDatabase.Contains(reference)) UnityEngine.Object.DestroyImmediate(reference);
                module.deselectOnBackgroundClick = false;
                systems[0].sendNavigationEvents = true;
                if (name == "MainMenu")
                    systems[0].firstSelectedGameObject = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include)
                        .First(b => b.name == "StartButton").gameObject;
                EditorUtility.SetDirty(module);
                EditorUtility.SetDirty(systems[0]);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
        return new { scenes = 3, actions = asset.actionMaps.Sum(map => map.actions.Count) };
    }

    private static InputActionReference Reference(InputActionAsset asset, string name)
    {
        // Imported action references are persistent subassets, preserving scene assignments across reloads.
        return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset)).OfType<InputActionReference>()
            .First(reference => reference.action.id == asset.FindAction("UI/" + name, true).id);
    }

    private static void AddBinding(InputAction action, string path)
    {
        if (!action.bindings.Any(binding => binding.path == path)) action.AddBinding(path);
    }
}
