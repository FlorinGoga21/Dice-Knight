using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HeyHeyThere.SpriteFXFree
{
    /// <summary>
    /// The demo: every effect installed, each on a sprite of the Godot pack's demo art. Click a sprite to
    /// hit it (SpriteFX.Flash).
    /// </summary>
    public class SpriteDemo : MonoBehaviour
    {
        public const float W = 1280f, H = 720f;  // world pixels on screen, 100 to a unit: the camera's view, as the Godot demo's window
        const float Header = 64f;
        static readonly Color Ground = new Color(0.07f, 0.07f, 0.11f), Panel = new Color(0.15f, 0.17f, 0.27f, 0.85f);

        /// <summary>Effect, sprite and settings: the effects installed show, in this order.</summary>
        static readonly (string effect, string art, Action<SpriteFX> set)[] Cells =
        {
            ("outline", "slime", f => f.Set("outline_color", Color.white).Set("width", 1f)),
            ("glow_outline", "crystal", f => f.Set("glow_color", new Color(0.35f, 0.8f, 1f)).Set("radius", 6f).Set("grow", 6f)),
            ("dissolve", "ghost", f => f.Set("edge_color", new Color(1f, 0.5f, 0.1f))),
            ("burn", "chest", null),
            ("flash", "slime", null),
            ("palette_swap", "slime", null),
            ("hologram", "ghost", null),
            ("freeze", "slime", null),
            ("wind_sway", "bush", f => f.Set("strength", 5f)),
            ("shine", "coin", null),
            ("glitch", "heart", f => f.Set("strength", 0.7f)),
            ("hue_shift", "potion", f => f.Set("speed", 0.35f)),
            ("stone", "mushroom", null),
            ("wave", "potion", f => f.Set("amplitude", 1.5f)),
            ("blink", "heart", f => f.Set("rate", 6f)),
            ("pulse_tint", "mushroom", null),
            ("jelly", "slime", f => f.Set("amount", 0.1f)),
            ("gradient_map", "chest", null),
            ("chromatic", "ghost", f => f.Set("offset", 2f).Set("wobble_speed", 3f)),
            ("cloak", "ghost", null),
            ("rim_light", "mushroom", f => f.Set("rim_color", new Color(1f, 0.85f, 0.5f)).Set("width", 2f)),
            ("invert", "sword", null),
            ("dither_fade", "potion", null),
            ("silhouette", "sword", f => f.Set("tint_color", new Color(0.08f, 0.08f, 0.14f))),
            ("pixelate", "heart", null),
        };

        /// <summary>The demo's sprites, by name: slime, ghost, chest and the rest.</summary>
        public Sprite[] art;
        public Canvas canvas;

        readonly List<(string effect, SpriteRenderer sprite)> shown = new List<(string, SpriteRenderer)>();
        float t;
        Font font;

        void Awake()
        {
            if (!canvas)
                Build();
        }

        public void Build()
        {
            DemoInput.Ensure();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cells = Cells.Where(c => SpriteFX.ShaderOf(c.effect) && art.Any(a => a.name == c.art)).ToArray();
            int cols = cells.Length > 9 ? 5 : 3;
            var cell = new Vector2(W / cols, (H - Header) / Mathf.Ceil(cells.Length / (float)cols));
            MakeBackdrop(cells.Length, cols, cell);

            canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(W, H);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var catcher = Add<Image>("Clicks", 0f, 0f, W, H, canvas.transform);
            catcher.color = Color.clear;
            var r = catcher.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            catcher.gameObject.AddComponent<Clicks>().demo = this;

            for (int i = 0; i < cells.Length; i++)
            {
                var (effect, artName, _) = cells[i];
                var center = new Vector2((i % cols + 0.5f) * cell.x, Header + i / cols * cell.y + cell.y * 0.43f);
                var s = new GameObject(effect).AddComponent<SpriteRenderer>();
                s.transform.SetParent(transform, false);
                s.sprite = art.First(a => a.name == artName);
                float k = Mathf.Min(3f * cell.y / 131f, 88f * cell.y / 131f / s.sprite.rect.height);
                s.transform.localScale = new Vector3(k, k, 1f);
                s.transform.position = World(center.x, center.y);
                Dress(effect, SpriteFX.Apply(s, effect));
                shown.Add((effect, s));
                var label = Label(effect, effect.Replace("_", " "), 15, center.x - cell.x / 2f, Header + (i / cols + 1) * cell.y - 26f,
                    cell.x, 20f, new Color(0.75f, 0.78f, 0.9f));
                label.alignment = TextAnchor.MiddleCenter;
            }
            Label("Title", $"{"Sprite FX Free".ToUpperInvariant()}  ·  {cells.Length} shaders for Unity", 26, 24f, 14f, 800f, 36f, Color.white);
            Label("Hint", "click a sprite", 15, W - 130f, 24f, 120f, 20f, new Color(0.55f, 0.58f, 0.7f));
        }

        /// <summary>A cell's effect as the demo shows it: its settings, then what Setup gives.</summary>
        public static void Dress(string effect, SpriteFX fx)
        {
            if (fx == null)
                return;
            Cells.FirstOrDefault(c => c.effect == effect).set?.Invoke(fx);
            Setup(effect, fx);
        }

        /// <summary>What a settings list can't give: palette_swap's colours, gradient_map's gradient.</summary>
        public static void Setup(string effect, SpriteFX fx)
        {
            switch (effect)
            {
                case "palette_swap":  // the slime's greens to reds
                    fx.Palette(new Color[] { new Color32(56, 183, 100, 255), new Color32(37, 113, 121, 255), new Color32(167, 240, 112, 255) },
                        new Color[] { new Color32(177, 62, 83, 255), new Color32(93, 39, 93, 255), new Color32(239, 125, 87, 255) });
                    break;
                case "gradient_map":
                    var g = new Gradient();
                    g.SetKeys(new[] { new GradientColorKey(new Color(0.1f, 0.02f, 0.2f), 0f), new GradientColorKey(new Color(0.9f, 0.25f, 0.35f), 0.45f),
                        new GradientColorKey(new Color(1f, 0.95f, 0.6f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                    fx.SetGradient(g);
                    break;
            }
        }

        /// <summary>The one-shot effects on a 3-second loop, as the demo runs them, at <paramref name="time"/> seconds.</summary>
        public static void Drive(string effect, SpriteFX fx, float time)
        {
            float loop = time % 3f / 3f;
            float ping = Mathf.Clamp01(1f - Mathf.Abs(loop * 2f - 1f) * 1.4f);  // 0 to 1 to 0, with holds
            switch (effect)
            {
                case "dissolve":
                case "burn":
                    fx.Set("progress", ping);
                    break;
                case "dither_fade":
                    fx.Set("fade", ping);
                    break;
                case "silhouette":
                    fx.Set("amount", ping);
                    break;
                case "invert":
                    fx.Set("amount", time % 1.5f < 0.12f ? 1f : 0f);
                    break;
                case "flash":
                    fx.Set("flash_amount", Mathf.Max(0f, 1f - time % 1.2f / 0.2f));
                    break;
                case "pixelate":
                    fx.Set("pixel_size", 1 + (int)(ping * 5f));
                    break;
            }
        }

        /// <summary>Every cell's effect at <paramref name="time"/>.</summary>
        public void DriveAll(float time)
        {
            foreach (var (effect, sprite) in shown)
            {
                var fx = SpriteFX.Of(sprite);
                if (fx != null && fx.Effect == effect)
                    Drive(effect, fx, time);
            }
        }

        void Update()
        {
            t += Time.deltaTime;
            DriveAll(t);
        }

        public void Hit(Vector2 screenPoint)
        {
            var at = (Vector2)Camera.main.ScreenToWorldPoint(screenPoint);
            foreach (var (effect, sprite) in shown)
            {
                var b = sprite.bounds;
                if (at.x < b.min.x || at.x > b.max.x || at.y < b.min.y || at.y > b.max.y)
                    continue;
                var fx = SpriteFX.Flash(sprite, Color.white, 0.2f);
                if (fx != null)
                    fx.Finished += () => Dress(effect, SpriteFX.Apply(sprite, effect));
            }
        }

        /// <summary>Godot's backdrop: faint diagonal stripes, a panel under each cell.</summary>
        void MakeBackdrop(int count, int cols, Vector2 cell)
        {
            int w = (int)W, h = (int)H;
            var px = new Color[w * h];
            var stripe = Color.Lerp(Ground, Color.white, 0.025f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int d = (x + y) % 24;  // y down: the lines run from bottom left to top right, 8 pixels wide
                    px[(h - 1 - y) * w + x] = Mathf.Min(d, 24 - d) < 5.66f ? stripe : Ground;
                }
            var panel = new Color(Panel.r, Panel.g, Panel.b);
            for (int i = 0; i < count; i++)
            {
                int x0 = (int)(i % cols * cell.x + 6f), y0 = (int)(Header + i / cols * cell.y + 6f);
                int x1 = (int)(i % cols * cell.x + cell.x - 6f), y1 = (int)(Header + i / cols * cell.y + cell.y - 6f);
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++)
                    {
                        int at = (h - 1 - y) * w + x;
                        px[at] = Color.Lerp(px[at], panel, Panel.a);
                    }
            }
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Backdrop" };
            tex.SetPixels(px);
            tex.Apply();
            var s = new GameObject("Backdrop").AddComponent<SpriteRenderer>();
            s.transform.SetParent(transform, false);
            s.transform.position = new Vector3(0f, 0f, 1f);
            s.sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.sortingOrder = -10;
        }

        /// <summary>A pixel of the demo's window (y down from its top-left) in the world.</summary>
        static Vector3 World(float x, float y) => new Vector3((x - W / 2f) / 100f, (H / 2f - y) / 100f, 0f);

        class Clicks : MonoBehaviour, IPointerClickHandler
        {
            public SpriteDemo demo;

            public void OnPointerClick(PointerEventData e) => demo.Hit(e.position);
        }

        static T Add<T>(string name, float x, float y, float w, float h, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);  // top left, y down, as Godot's
            r.anchoredPosition = new Vector2(x, -y);
            r.sizeDelta = new Vector2(w, h);
            return go.AddComponent<T>();
        }

        Text Label(string name, string text, int size, float x, float y, float w, float h, Color color)
        {
            var t = Add<Text>(name, x, y, w, h, canvas.transform);
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.raycastTarget = false;
            return t;
        }
    }
}
