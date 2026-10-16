using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeyHeyThere.HearthPixelFont
{
    /// <summary>
    /// The fonts in use: pick one, type in the box, and see it at 1x to 4x its face size and every
    /// character it has, on a PixelCanvas so each glyph pixel stays a whole number of screen pixels.
    /// </summary>
    public class FontDemo : MonoBehaviour
    {
        public TMP_FontAsset[] fonts;
        [Tooltip("Each font's face size, the one it is crisp at, as the fonts array")] public int[] sizes;
        public int artHeight = 270;
        [TextArea] public string sample = "The quick brown fox jumps over the lazy dog. 0123456789";

        static readonly Color Ink = new Color(0.93f, 0.9f, 0.82f), Dim = new Color(0.55f, 0.53f, 0.6f), Panel = new Color(0.14f, 0.13f, 0.2f);

        RectTransform samples;
        TMP_InputField input;
        Image[] buttons;
        int current;

        void Start()
        {
            DemoInput.Ensure();
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            go.AddComponent<PixelCanvas>().artHeight = artHeight;

            var screen = Column(go.transform, 6, 8);
            var r = screen;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;

            var picker = Row(screen, 4);
            buttons = fonts.Select((f, i) =>
            {
                var b = Rect(f.faceInfo.familyName, picker).gameObject.AddComponent<Image>();
                b.color = Panel;
                var layout = b.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.padding = new RectOffset(4, 4, 2, 2);
                layout.childControlWidth = layout.childControlHeight = true;
                Text(b.transform, f, sizes[i], $"{f.faceInfo.familyName} {sizes[i]}", Ink, false);
                b.gameObject.AddComponent<Button>().onClick.AddListener(() => Pick(i));
                return b;
            }).ToArray();

            input = Input(screen);
            input.onValueChanged.AddListener(_ => Show());
            samples = Column(screen, 4, 0);
            Pick(0);
        }

        void Pick(int index)
        {
            current = index;
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].color = i == index ? Dim : Panel;
            input.fontAsset = fonts[index];
            input.pointSize = sizes[index];
            Show();
        }

        void Show()
        {
            foreach (Transform child in samples)
                Destroy(child.gameObject);
            var font = fonts[current];
            var text = string.IsNullOrEmpty(input.text) ? sample : input.text;
            for (int scale = 1; scale <= 4; scale++)
                Text(samples, font, sizes[current] * scale, text, Ink, true);
            var all = new string(font.characterTable.Select(c => (char)c.unicode).Where(c => c > ' ').OrderBy(c => c).ToArray());
            Text(samples, font, sizes[current], all, Dim, true);
        }

        TMP_InputField Input(Transform parent)
        {
            var box = Rect("Input", parent);
            box.gameObject.AddComponent<Image>().color = Panel;
            var e = box.gameObject.AddComponent<LayoutElement>();
            e.minHeight = e.preferredHeight = fonts.Max(f => f.faceInfo.lineHeight) + 6;
            var area = Rect("Text Area", box);
            area.anchorMin = Vector2.zero;
            area.anchorMax = Vector2.one;
            area.offsetMin = new Vector2(4, 3);
            area.offsetMax = new Vector2(-4, -3);
            area.gameObject.AddComponent<RectMask2D>();
            var placeholder = Text(area, fonts[0], sizes[0], "Type to try it...", Dim, false);
            var text = Text(area, fonts[0], sizes[0], "", Ink, false);
            foreach (var t in new[] { placeholder, text })
            {
                t.rectTransform.anchorMin = Vector2.zero;
                t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
            }
            var field = box.gameObject.AddComponent<TMP_InputField>();
            field.textViewport = area;
            field.textComponent = text;
            field.placeholder = placeholder;
            field.caretWidth = 1;
            field.customCaretColor = true;
            field.caretColor = Ink;
            return field;
        }

        static TextMeshProUGUI Text(Transform parent, TMP_FontAsset font, float size, string text, Color colour, bool wrap)
        {
            var t = Rect("Text", parent).gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font;
            t.fontSize = size;
            t.color = colour;
            t.richText = false;
            t.raycastTarget = false;
            t.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            t.text = text;
            return t;
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Row(Transform parent, float gap)
        {
            var r = Rect("Row", parent);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = gap;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            return r;
        }

        static RectTransform Column(Transform parent, float gap, int padding)
        {
            var r = Rect("Column", parent);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = gap;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return r;
        }
    }
}
