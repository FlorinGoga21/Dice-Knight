using System;
using System.Linq;
using HeyHeyThere.PixelUIFree;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ConfigureSettingsMenu
{
    private const string Theme = "Assets/External Assets/PixelUIFree/Prefabs/Wood/";

    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring settings.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Preserve unsaved scene edits first.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            var menu = UnityEngine.Object.FindAnyObjectByType<MainMenu>();
            if (menu.GetComponent<SettingsMenuController>() != null) return "Settings already configured.";
            var menuSettings = new SerializedObject(menu);
            var mainButtons = (CanvasGroup)menuSettings.FindProperty("mainButtons").objectReferenceValue;
            var start = (UnityEngine.UI.Button)menuSettings.FindProperty("startButton").objectReferenceValue;
            var exit = (UnityEngine.UI.Button)menuSettings.FindProperty("exitButton").objectReferenceValue;
            var settingsButton = UnityEngine.Object.Instantiate(start.gameObject, mainButtons.transform, false);
            settingsButton.name = "SettingsButton";
            var pixelButton = settingsButton.GetComponent<PixelButton>();
            pixelButton.text = "Settings";
            pixelButton.onClick.RemoveAllListeners();
            var settingsRect = (RectTransform)settingsButton.transform;
            settingsRect.anchoredPosition = new Vector2(((RectTransform)exit.transform).anchoredPosition.x,
                ((RectTransform)exit.transform).anchoredPosition.y);
            ((RectTransform)exit.transform).anchoredPosition += Vector2.down * 220f;
            settingsButton.transform.SetSiblingIndex(start.transform.GetSiblingIndex() + 1);

            var overlay = new GameObject("Settings Overlay", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            overlay.transform.SetParent(menu.transform.parent, false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var dim = new GameObject("Dim Background", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            dim.transform.SetParent(overlay.transform, false);
            var dimRect = (RectTransform)dim.transform;
            dimRect.anchorMin = Vector2.zero;
            dimRect.anchorMax = Vector2.one;
            dimRect.offsetMin = dimRect.offsetMax = Vector2.zero;
            dim.GetComponent<UnityEngine.UI.Image>().color = new Color(0.015f, 0.02f, 0.04f, 0.78f);

            var window = Make("Window", overlay.transform);
            window.name = "Settings Window";
            var rect = (RectTransform)window.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(256f, 180f);
            rect.localScale = Vector3.one * 4f;
            var layout = window.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 10);
            layout.spacing = 8f;
            layout.childForceExpandHeight = false;
            var title = window.GetComponentInChildren<PixelLabel>();
            title.text = "Settings";
            title.Text.alignment = TextAlignmentOptions.Center;
            Height(title.gameObject, 16f);
            TMP_Text musicValue, sfxValue;
            var music = VolumeRow("Music", window.transform, out musicValue);
            var sfx = VolumeRow("SFX", window.transform, out sfxValue);
            var checkbox = Make("Checkbox", window.transform);
            checkbox.name = "Fullscreen";
            checkbox.GetComponentInChildren<TMP_Text>().text = "Fullscreen";
            Height(checkbox, 20f);
            var toggle = checkbox.GetComponent<UnityEngine.UI.Toggle>();
            var closeRow = new GameObject("Close Row", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
            closeRow.transform.SetParent(window.transform, false);
            var closeLayout = closeRow.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            closeLayout.childAlignment = TextAnchor.MiddleCenter;
            closeLayout.childForceExpandWidth = closeLayout.childForceExpandHeight = false;
            Height(closeRow, 22f);
            var close = Make("Button", closeRow.transform).GetComponent<PixelButton>();
            close.name = "Close Settings";
            close.text = "Back";
            close.SetIcon(null);
            var closeSize = close.GetComponent<UnityEngine.UI.LayoutElement>();
            closeSize.preferredWidth = 80f;
            closeSize.preferredHeight = 20f;
            Focus(music);
            Focus(sfx);
            Focus(toggle);
            var controls = new UnityEngine.UI.Selectable[] { music, sfx, toggle, close };
            for (int i = 0; i < controls.Length; i++)
                controls[i].navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = i > 0 ? controls[i - 1] : controls[controls.Length - 1],
                    selectOnDown = controls[(i + 1) % controls.Length] };

            var controller = menu.gameObject.AddComponent<SettingsMenuController>();
            var fields = new SerializedObject(controller);
            Set(fields, "overlay", overlay);
            Set(fields, "mainButtons", mainButtons);
            Set(fields, "musicSlider", music);
            Set(fields, "sfxSlider", sfx);
            Set(fields, "fullscreenToggle", toggle);
            Set(fields, "closeButton", close);
            Set(fields, "musicValue", musicValue);
            Set(fields, "sfxValue", sfxValue);
            fields.ApplyModifiedPropertiesWithoutUndo();
            Set(menuSettings, "settingsButton", pixelButton);
            Set(menuSettings, "settingsMenu", controller);
            menuSettings.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);
            Canvas.ForceUpdateCanvases();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return new { scene = scene.path, overlay = overlay.name, controls = controls.Length };
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    private static GameObject Make(string name, Transform parent)
    {
        return (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Theme + name + ".prefab"), parent);
    }

    private static void Height(GameObject item, float height)
    {
        var element = item.GetComponent<UnityEngine.UI.LayoutElement>() ?? item.AddComponent<UnityEngine.UI.LayoutElement>();
        element.minHeight = element.preferredHeight = height;
        element.flexibleHeight = 0f;
    }

    private static UnityEngine.UI.Slider VolumeRow(string name, Transform parent, out TMP_Text value)
    {
        var row = new GameObject(name + " Volume Row", typeof(RectTransform), typeof(UnityEngine.UI.VerticalLayoutGroup));
        row.transform.SetParent(parent, false);
        Height(row, 30f);
        var layout = row.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        var heading = new GameObject("Heading", typeof(RectTransform), typeof(UnityEngine.UI.HorizontalLayoutGroup));
        heading.transform.SetParent(row.transform, false);
        Height(heading, 10f);
        var headingLayout = heading.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        headingLayout.childForceExpandHeight = false;
        var label = Make("Label", heading.transform).GetComponent<PixelLabel>();
        label.text = name;
        var number = Make("Label", heading.transform).GetComponent<PixelLabel>();
        number.text = "100%";
        number.Text.alignment = TextAlignmentOptions.Right;
        value = number.Text;
        var slider = Make("Slider", row.transform).GetComponent<UnityEngine.UI.Slider>();
        slider.name = name + " Volume";
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        Height(slider.gameObject, 14f);
        return slider;
    }

    private static void Focus(UnityEngine.UI.Selectable control)
    {
        var go = new GameObject("Selection Focus", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.LayoutElement));
        go.transform.SetParent(control.transform, false);
        go.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/External Assets/PixelUIFree/Sprites/themes/wood/focus.png");
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = new Color32(255, 220, 60, 255);
        image.raycastTarget = false;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = new Vector2(-2f, -2f);
        image.rectTransform.offsetMax = new Vector2(2f, 2f);
        image.enabled = false;
        control.gameObject.AddComponent<ButtonHoverFocus>().Initialize(control, image);
    }

    private static void Set(SerializedObject target, string name, UnityEngine.Object value) => target.FindProperty(name).objectReferenceValue = value;
}
