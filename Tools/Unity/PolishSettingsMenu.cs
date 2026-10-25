using System;
using HeyHeyThere.PixelUIFree;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishSettingsMenu
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before changing settings UI.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene edits first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var controller = UnityEngine.Object.FindAnyObjectByType<SettingsMenuController>();
            var fields = new SerializedObject(controller);
            var overlay = (GameObject)fields.FindProperty("overlay").objectReferenceValue;
            var window = overlay.transform.Find("Settings Window");
            // These fixed-size pixel controls use explicit rectangles; inherited layout groups
            // otherwise compete with their prefab's nested labels and minimum sizes.
            foreach (var layout in window.GetComponentsInChildren<UnityEngine.UI.LayoutGroup>(true)) layout.enabled = false;
            Rect(window, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(264f, 200f), new Vector2(0.5f, 0.5f));
            window.localScale = new Vector3(3f, 3f, 1f);
            var title = window.Find("Title").GetComponent<PixelLabel>();
            Rect(title.transform, new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(232f, 20f));
            Label(title, "Settings", TextAlignmentOptions.Center);
            foreach (var text in title.GetComponentsInChildren<TMP_Text>(true)) text.fontSize = 16f;
            var separator = window.Find("Settings Divider");
            if (separator == null)
            {
                var go = new GameObject("Settings Divider", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(window, false);
                separator = go.transform;
                go.GetComponent<UnityEngine.UI.Image>().color = new Color32(55, 33, 23, 255);
                go.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
            }
            Rect(separator, new Vector2(0.5f, 1f), new Vector2(0f, -37f), new Vector2(232f, 1f));
            StyleVolume(window.Find("Music Volume Row"), "Music", -46f);
            StyleVolume(window.Find("SFX Volume Row"), "SFX", -92f);
            var fullscreen = (UnityEngine.UI.Toggle)fields.FindProperty("fullscreenToggle").objectReferenceValue;
            Rect(fullscreen.transform, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(232f, 20f));
            fullscreen.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            var box = fullscreen.transform.Find("Box");
            Rect(box, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(12f, 12f), new Vector2(0f, 0.5f));
            Stretch(box.Find("Check"));
            var label = fullscreen.GetComponentInChildren<TMP_Text>();
            Rect(label.transform, new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(190f, 14f), new Vector2(0f, 0.5f));
            label.text = "Fullscreen";
            label.fontSize = 8f;
            label.color = new Color32(255, 235, 188, 255);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            var close = (PixelButton)fields.FindProperty("closeButton").objectReferenceValue;
            Rect(close.transform.parent, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(232f, 20f));
            Rect(close.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 20f), new Vector2(0.5f, 0.5f));
            close.text = "Back";
            close.SetIcon(null);
            Stretch(close.label.transform);
            close.label.fontSize = 8f;
            close.label.alignment = TextAlignmentOptions.Center;
            close.label.color = new Color32(255, 235, 188, 255);
            close.textNormal = close.textHover = close.textPressed = new Color32(255, 235, 188, 255);
            close.Refresh();
            close.image.material = null;
            close.label.fontMaterial = close.label.fontSharedMaterial;
            Stretch(close.focus.transform);
            foreach (var text in window.GetComponentsInChildren<TMP_Text>(true))
            {
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }
            foreach (var component in window.GetComponentsInChildren<Component>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return "Settings uses aligned, spaced pixel controls; sliders accept all values between 0 and 1.";
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static void StyleVolume(Transform row, string name, float top)
    {
        Rect(row, new Vector2(0.5f, 1f), new Vector2(0f, top), new Vector2(232f, 38f));
        var background = row.GetComponent<UnityEngine.UI.Image>() ?? row.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/External Assets/PixelUIFree/Sprites/themes/wood/panel_inset.png");
        background.type = UnityEngine.UI.Image.Type.Sliced;
        background.raycastTarget = false;
        var heading = row.Find("Heading");
        Rect(heading, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(216f, 12f));
        var labels = heading.GetComponentsInChildren<PixelLabel>(true);
        Rect(labels[0].transform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(150f, 12f), new Vector2(0f, 0.5f));
        Rect(labels[1].transform, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(50f, 12f), new Vector2(1f, 0.5f));
        Label(labels[0], name, TextAlignmentOptions.MidlineLeft);
        Label(labels[1], "100%", TextAlignmentOptions.MidlineRight);
        var slider = row.GetComponentInChildren<UnityEngine.UI.Slider>(true);
        slider.wholeNumbers = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(1f);
        Rect(slider.transform, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(216f, 14f));
    }

    private static void Label(PixelLabel label, string text, TextAlignmentOptions alignment)
    {
        label.text = text;
        label.color = new Color32(255, 235, 188, 255);
        label.shadowColor = new Color32(35, 20, 15, 255);
        label.Text.fontSize = 8f;
        label.Text.alignment = alignment;
        foreach (var rect in label.GetComponentsInChildren<RectTransform>(true))
            if (rect.transform != label.transform) Stretch(rect);
        foreach (var tmp in label.GetComponentsInChildren<TMP_Text>(true))
        {
            tmp.fontSize = 8f;
            tmp.alignment = alignment;
        }
    }

    private static void Stretch(Transform target)
    {
        if (target == null) return;
        var rect = (RectTransform)target;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void Rect(Transform target, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
    {
        var rect = (RectTransform)target;
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot ?? new Vector2(0.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
