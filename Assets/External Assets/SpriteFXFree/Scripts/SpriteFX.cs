using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeyHeyThere.SpriteFXFree
{
    /// <summary>
    /// One-line effects for a SpriteRenderer: a shader per effect, on a material of its own, the
    /// renderer's own material back when the effect goes.
    /// <code>
    /// SpriteFX.Flash(sprite);                                  // hit flash, then the own material is back
    /// yield return SpriteFX.DissolveOut(enemy).Wait();         // then Destroy(enemy.gameObject)
    /// SpriteFX.Apply(chest, "glow_outline").Set("glow_color", Color.yellow);
    /// SpriteFX.Clear(chest);
    /// </code>
    /// Settings are the shader's properties, by the Godot pack's names ("flash_amount") or the shader's
    /// ("FlashAmount", "_FlashAmount").
    /// </summary>
    public class SpriteFX : MonoBehaviour
    {
        /// <summary>The shaders' folder under Resources.</summary>
        public const string Folder = "Sprite FX Free";

        /// <summary>Every effect, by the Godot pack's names.</summary>
        public static readonly string[] AllEffects = { "flash", "outline", "glow_outline", "dissolve", "burn",
            "palette_swap", "silhouette", "pixelate", "wind_sway", "shine", "hologram", "freeze", "glitch",
            "hue_shift", "stone", "wave", "blink", "pulse_tint", "jelly", "gradient_map", "chromatic",
            "cloak", "rim_light", "invert", "dither_fade" };

        /// <summary>Raised when Animate (and Flash, DissolveOut and the rest) ends, after the own material is back.</summary>
        public event Action Finished;

        static readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        static readonly List<SpriteFX> cloaks = new List<SpriteFX>();
        static readonly int PpuId = Shader.PropertyToID("_PPU"), ScreenId = Shader.PropertyToID("_SfxScreen");
        static RenderTexture screen;
        static int screenFrame = -1;

        Renderer target;
        Material own, instance;
        Coroutine running;
        bool removed;

        /// <summary>The effects whose shader is installed.</summary>
        public static IReadOnlyList<string> Effects => AllEffects.Where(e => ShaderOf(e)).ToArray();

        /// <summary>An effect's shader, or null if this package lacks it.</summary>
        public static Shader ShaderOf(string effect)
        {
            if (effect == null)
                return null;
            if (!shaders.TryGetValue(effect, out var s))
                shaders[effect] = s = Resources.Load<Shader>($"{Folder}/{Pascal(effect)}");
            return s;
        }

        /// <summary>
        /// Puts an effect on <paramref name="target"/>'s renderer (a SpriteRenderer, or the component's
        /// own) and returns it, on a new material at the shader's defaults. The renderer's own material is
        /// kept and comes back with Clear. Null, with an error, for an effect not installed.
        /// </summary>
        public static SpriteFX Apply(Component target, string effect)
        {
            var shader = ShaderOf(effect);
            if (!shader)
            {
                Debug.LogError($"SpriteFX: no effect '{effect}', pick one of {string.Join(", ", Effects)}", target);
                return null;
            }
            var r = target as Renderer ?? target.GetComponent<Renderer>();
            if (!r)
                throw new ArgumentException($"SpriteFX: {target.name} has no renderer");
            var fx = Of(r) ?? r.gameObject.AddComponent<SpriteFX>();
            fx.Use(r, effect, shader);
            return fx;
        }

        /// <summary>Removes the effect from <paramref name="target"/>: its own material is back.</summary>
        public static void Clear(Component target)
        {
            var r = target as Renderer ?? target.GetComponent<Renderer>();
            if (r && Of(r) is SpriteFX fx)
                fx.Remove();
        }

        /// <summary>The effect on a renderer, if any.</summary>
        public static SpriteFX Of(Component target) =>
            target.GetComponents<SpriteFX>().FirstOrDefault(f => !f.removed);

        /// <summary>A hit flash: <paramref name="color"/> fading out over <paramref name="seconds"/>.</summary>
        public static SpriteFX Flash(Component target, Color? color = null, float seconds = 0.15f) =>
            Apply(target, "flash")?.Set("flash_color", color ?? Color.white).Animate("flash_amount", 1f, 0f, seconds);

        /// <summary>Invincibility frames for <paramref name="seconds"/>, <paramref name="rate"/> blinks a second.</summary>
        public static SpriteFX Blink(Component target, float seconds = 1f, float rate = 10f) =>
            Apply(target, "blink")?.Set("rate", rate).Hold(seconds);

        /// <summary>Leaves the sprite invisible (progress 1) when done: destroy it, or Clear it.</summary>
        public static SpriteFX DissolveOut(Component target, float seconds = 0.8f, Color? edge = null) =>
            Apply(target, "dissolve")?.Set("edge_color", edge ?? new Color(1f, 0.55f, 0.1f)).Animate("progress", 0f, 1f, seconds, false);

        public static SpriteFX DissolveIn(Component target, float seconds = 0.8f, Color? edge = null) =>
            Apply(target, "dissolve")?.Set("edge_color", edge ?? new Color(0.4f, 0.8f, 1f)).Animate("progress", 1f, 0f, seconds);

        /// <summary>Burns away from <paramref name="origin"/> (in UV, (0, 0) the bottom left; the bottom middle by default).</summary>
        public static SpriteFX BurnOut(Component target, float seconds = 1.2f, Vector2? origin = null) =>
            Apply(target, "burn")?.Set("origin", (Vector4)(origin ?? new Vector2(0.5f, 0f))).Animate("progress", 0f, 1f, seconds, false);

        public static SpriteFX DitherOut(Component target, float seconds = 0.5f) =>
            Apply(target, "dither_fade")?.Animate("fade", 0f, 1f, seconds, false);

        public static SpriteFX DitherIn(Component target, float seconds = 0.5f) =>
            Apply(target, "dither_fade")?.Animate("fade", 1f, 0f, seconds);

        /// <summary>The effect's name.</summary>
        public string Effect { get; private set; }

        /// <summary>The material drawn with: set its properties here, or through Set.</summary>
        public Material Material => instance;

        public Renderer Renderer => target;

        /// <summary>True while Animate (or Blink's hold) runs.</summary>
        public bool Running => running != null;

        public SpriteFX Set(string property, float value)
        {
            instance.SetFloat(Id(property), value);
            return this;
        }

        public SpriteFX Set(string property, Color value)
        {
            instance.SetColor(Id(property), value);
            return this;
        }

        public SpriteFX Set(string property, Vector4 value)
        {
            instance.SetVector(Id(property), value);
            return this;
        }

        public SpriteFX Set(string property, Texture value)
        {
            instance.SetTexture(Id(property), value);
            return this;
        }

        public float Get(string property) => instance.GetFloat(Id(property));

        public Color GetColor(string property) => instance.GetColor(Id(property));

        /// <summary>gradient_map's gradient, dark on the left, light on the right.</summary>
        public SpriteFX SetGradient(Gradient gradient)
        {
            var tex = new Texture2D(256, 1, TextureFormat.RGBA32, false)
            {
                name = "SpriteFX gradient", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave
            };
            tex.SetPixels(Enumerable.Range(0, 256).Select(i => gradient.Evaluate(i / 255f)).ToArray());
            tex.Apply();
            return Set("gradient", tex);
        }

        /// <summary>palette_swap's colours: each of <paramref name="from"/> becomes its <paramref name="to"/>, up to 8.</summary>
        public SpriteFX Palette(Color[] from, Color[] to)
        {
            int n = Mathf.Min(8, from.Length, to.Length);
            for (int i = 0; i < n; i++)
                Set($"_From{i}", from[i]).Set($"_To{i}", to[i]);
            return Set("count", n);
        }

        /// <summary>
        /// Runs a property from <paramref name="from"/> to <paramref name="to"/> over <paramref name="seconds"/>,
        /// then, with <paramref name="restore"/>, gives the renderer its own material back; then Finished.
        /// </summary>
        public SpriteFX Animate(string property, float from, float to, float seconds, bool restore = true)
        {
            int id = Id(property);
            instance.SetFloat(id, from);
            return Run(t => instance.SetFloat(id, Mathf.Lerp(from, to, t)), seconds, restore);
        }

        /// <summary>A coroutine's wait for the end of Animate.</summary>
        public CustomYieldInstruction Wait() => new WaitWhile(() => this && Running);

        SpriteFX Hold(float seconds) => Run(_ => { }, seconds, true);

        SpriteFX Run(Action<float> step, float seconds, bool restore)
        {
            if (running != null)
                StopCoroutine(running);
            running = StartCoroutine(Ticks(step, seconds, restore));
            return this;
        }

        IEnumerator Ticks(Action<float> step, float seconds, bool restore)
        {
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                step(t / seconds);
                yield return null;
            }
            step(1f);
            running = null;
            if (restore)
                Remove();
            Finished?.Invoke();
        }

        void Use(Renderer r, string effect, Shader shader)
        {
            if (running != null)
                StopCoroutine(running);
            running = null;
            if (!target)
            {
                target = r;
                own = r.sharedMaterial;
            }
            var old = instance;
            instance = new Material(shader) { name = effect, hideFlags = HideFlags.DontSave };
            Effect = effect;
            Kill(old);
            r.sharedMaterial = instance;
            Sync();
            if (effect == "cloak")
            {
                if (!cloaks.Contains(this))
                    cloaks.Add(this);
            }
            else
                cloaks.Remove(this);
        }

        void Remove()
        {
            if (removed)
                return;
            removed = true;
            if (running != null)
                StopCoroutine(running);
            running = null;
            cloaks.Remove(this);
            if (target && target.sharedMaterial == instance)
                target.sharedMaterial = own;
            Kill(instance);
            instance = null;
            Kill(this);
        }

        /// <summary>The sprite's pixels per unit, for the effects that move its corners in texture pixels.</summary>
        void Sync()
        {
            if (instance && target is SpriteRenderer s && s.sprite)
                instance.SetFloat(PpuId, s.sprite.pixelsPerUnit);
        }

        void LateUpdate()
        {
            Sync();
            if (Effect == "cloak" && screenFrame != Time.frameCount)
            {
                screenFrame = Time.frameCount;
                RenderScreens(Camera.main);
            }
        }

        /// <summary>
        /// The cloak reads the screen: the main camera drawn into a texture beforehand, without the cloaks,
        /// once a frame. This does it for a camera now, for one rendered by hand.
        /// </summary>
        public static void RenderScreens(Camera cam)
        {
            if (!cam)
                return;
            int width = cam.pixelWidth, height = cam.pixelHeight;
            if (!screen || screen.width != width || screen.height != height)
            {
                if (screen)
                    screen.Release();
                screen = new RenderTexture(width, height, 24) { name = "SpriteFX screen", hideFlags = HideFlags.DontSave };
            }
            var hidden = cloaks.Where(f => f && f.isActiveAndEnabled && f.target && f.target.enabled).Select(f => f.target).ToList();
            foreach (var r in hidden)
                r.enabled = false;
            var before = cam.targetTexture;
            cam.targetTexture = screen;
            try
            {
                cam.Render();
            }
            finally
            {
                cam.targetTexture = before;
                foreach (var r in hidden)
                    r.enabled = true;
            }
            Shader.SetGlobalTexture(ScreenId, screen);
        }

        void OnDestroy()
        {
            if (!removed)
            {
                removed = true;
                cloaks.Remove(this);
                if (target && target.sharedMaterial == instance)
                    target.sharedMaterial = own;
            }
            Kill(instance);
        }

        static int Id(string property)
        {
            if (!property.StartsWith("_"))
                property = "_" + Pascal(property);
            return Shader.PropertyToID(property);
        }

        /// <summary>"flash_amount" to "FlashAmount"; "Progress" stays.</summary>
        static string Pascal(string name) =>
            string.Concat(name.Split('_').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));

        static void Kill(UnityEngine.Object o)
        {
            if (!o)
                return;
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }
    }
}
