using System;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Renders only a disposable preview scene; does not enter Play mode or change preferences.</summary>
public static class PreviewSettingsMenu
{
    public static object Main()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/MainMenu.unity");
        RenderTexture target = null;
        Texture2D image = null;
        var originalActive = RenderTexture.active;
        try
        {
            var roots = scene.GetRootGameObjects();
            var canvases = roots.SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).ToArray();
            var overlay = canvases.First(canvas => canvas.name == "Settings Overlay");
            foreach (var canvas in canvases) canvas.gameObject.SetActive(canvas == overlay);
            overlay.gameObject.SetActive(true);
            var camera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).First();
            camera.scene = scene;
            target = new RenderTexture(1920, 1080, 24);
            target.Create();
            camera.targetTexture = target;
            overlay.renderMode = RenderMode.ScreenSpaceCamera;
            overlay.worldCamera = camera;
            overlay.planeDistance = 1f;
            var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            Directory.CreateDirectory("output/previews");
            string path = "output/previews/settings-menu.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            return Path.GetFullPath(path);
        }
        finally
        {
            RenderTexture.active = originalActive;
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
