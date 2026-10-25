using System;
using System.IO;
using HeyHeyThere.PixelUIFree;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureSettingsGear
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before editing the menu.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene edits first.");
        Directory.CreateDirectory("Assets/Art/UI");
        var circle = CreateSprite("Settings Round Button", 24, false, false);
        var gear = CreateSprite("Settings Gear", 16, true, false);
        var ring = CreateSprite("Settings Round Focus", 24, false, true);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
            var data = new SerializedObject(menu);
            var button = (PixelButton)data.FindProperty("settingsButton").objectReferenceValue;
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-72f, -72f);
            rect.sizeDelta = new Vector2(24f, 24f);
            rect.localScale = new Vector3(6f, 6f, 1f);
            button.text = "";
            button.SetIcon(gear);
            button.icon.color = new Color32(245, 221, 160, 255);
            button.icon.rectTransform.sizeDelta = new Vector2(16f, 16f);
            var iconSize = button.icon.GetComponent<UnityEngine.UI.LayoutElement>() ?? button.icon.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            iconSize.preferredWidth = iconSize.preferredHeight = 16f;
            button.image.sprite = circle;
            button.image.type = UnityEngine.UI.Image.Type.Simple;
            button.image.color = Color.white;
            button.image.material = null;
            button.icon.material = null;
            button.focus.material = null;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            button.spriteState = default;
            button.image.overrideSprite = null;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = colors.selectedColor = new Color32(255, 235, 175, 255);
            colors.pressedColor = new Color32(200, 160, 90, 255);
            button.colors = colors;
            button.focus.GetComponent<UnityEngine.UI.Image>().sprite = ring;
            button.focus.GetComponent<UnityEngine.UI.Image>().type = UnityEngine.UI.Image.Type.Simple;
            button.focus.color = Color.white;
            button.focus.rectTransform.offsetMin = button.focus.rectTransform.offsetMax = Vector2.zero;
            var layout = button.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            button.padding = new RectOffset(4, 4, 4, 4);
            button.pressedPadding = new RectOffset(4, 4, 5, 3);
            var exit = (UnityEngine.UI.Button)data.FindProperty("exitButton").objectReferenceValue;
            ((RectTransform)exit.transform).anchoredPosition = new Vector2(0f, -1250f);
            var start = (UnityEngine.UI.Button)data.FindProperty("startButton").objectReferenceValue;
            start.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnDown = exit, selectOnUp = button, selectOnRight = button };
            exit.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnUp = start, selectOnDown = button, selectOnRight = button };
            button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnDown = start, selectOnLeft = start, selectOnUp = exit };
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Settings is now a small round gear in the upper-right corner.";
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static Sprite CreateSprite(string name, int size, bool gear, bool ring)
    {
        string path = "Assets/Art/UI/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            try
            {
                var pixels = new Color32[size * size];
                float middle = (size - 1) * 0.5f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - middle, dy = y - middle;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (gear)
                    {
                        float angle = Mathf.Atan2(dy, dx);
                        float outer = Mathf.Cos(angle * 8f) > 0f ? 7.5f : 5.7f;
                        if (radius >= 2.2f && radius <= outer) color = new Color32(255, 240, 195, 255);
                    }
                    else if (radius <= 11.5f)
                    {
                        if (ring) { if (radius > 9.5f) color = new Color32(255, 220, 60, 255); }
                        else color = radius > 10.5f ? new Color32(35, 23, 17, 255)
                            : radius > 9.5f ? new Color32(190, 143, 76, 255)
                            : new Color32(91, 62, 35, 255);
                    }
                    pixels[y * size + x] = color;
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
