using TMPro;
using UnityEngine;

public sealed partial class RunMapController
{
    private void BuildMapUI()
    {
        if (canvasRoot == null)
            return;

        UnityEngine.UI.Image statusPanel = CreatePanel("Room Status Panel", canvasRoot, new Color32(27, 24, 22, 215), new Vector2(0.32f, 0.052f), new Vector2(0.68f, 0.112f));
        statusText = CreateText("Room Status", statusPanel.transform, "Choose a connected room", statusFont, statusFontSize, TextAlignmentOptions.Center);
        // Pixel fonts can trigger TMP's ellipsis recursion when a status message is too wide.
        statusText.overflowMode = TextOverflowModes.Truncate;

        legendBackground = CreatePanel("Map Legend", canvasRoot, legendBackgroundColor, Vector2.one, Vector2.one);
        legendRoot = legendBackground.rectTransform;
        legendTitle = CreateText("Legend Title", legendRoot, "Legend", legendFont, legendTitleFontSize, TextAlignmentOptions.MidlineLeft);
        CreateLegendEntry("Player", playerSprite, true);
        CreateLegendEntry("Enemy", enemySprite);
        CreateLegendEntry("Shop", shopSprite);
        CreateLegendEntry("Random", randomSprite);
        CreateLegendEntry("Treasure", treasureSprite);
        ApplyTextAppearance();
    }

    private void CreateLegendEntry(string label, Sprite sprite, bool isPlayer = false)
    {
        RectTransform row = CreateRect(label + " Legend Entry", legendRoot);
        RectTransform backing = CreateRect("Icon Background", row);
        UnityEngine.UI.Image background = backing.gameObject.AddComponent<UnityEngine.UI.Image>();
        background.sprite = nodeCircleSprite;
        background.raycastTarget = false;
        RectTransform iconRect = CreateRect("Icon", row);
        UnityEngine.UI.Image icon = iconRect.gameObject.AddComponent<UnityEngine.UI.Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        legendEntries.Add(new LegendEntry
        {
            rect = row,
            background = background,
            icon = icon,
            label = CreateText("Label", row, label, legendFont, legendFontSize, TextAlignmentOptions.MidlineLeft),
            isPlayer = isPlayer
        });
    }

    private TMP_FontAsset ResolveFont(TMP_FontAsset fontOverride)
    {
        return fontOverride != null ? fontOverride : mapFont != null ? mapFont : TMP_Settings.defaultFontAsset;
    }

    private void StyleText(TMP_Text text, TMP_FontAsset fontOverride, float size)
    {
        text.font = ResolveFont(fontOverride);
        text.fontSize = size;
        text.fontStyle = fontStyle;
        text.color = textColor;
    }

    private void ApplyTextAppearance()
    {
        if (statusText != null)
            StyleText(statusText, statusFont, statusFontSize);
        if (legendRoot == null)
            return;

        legendRoot.gameObject.SetActive(showLegend);
        Vector2 corner;
        switch (legendCorner)
        {
            case LegendCorner.TopLeft: corner = new Vector2(0f, 1f); break;
            case LegendCorner.BottomLeft: corner = Vector2.zero; break;
            case LegendCorner.BottomRight: corner = new Vector2(1f, 0f); break;
            default: corner = Vector2.one; break;
        }
        const float padding = 12f;
        const float titleGap = 6f;
        float titleHeight = legendTitleFontSize + 8f;
        float backingSize = legendIconSize + 8f;
        float rowHeight = Mathf.Max(legendRowHeight, Mathf.Max(backingSize, legendFontSize + 6f));
        legendRoot.anchorMin = legendRoot.anchorMax = legendRoot.pivot = corner;
        legendRoot.anchoredPosition = new Vector2(corner.x == 0f ? legendInset.x : -legendInset.x,
            corner.y == 0f ? legendInset.y : -legendInset.y);
        legendRoot.sizeDelta = new Vector2(legendWidth, padding * 2f + titleHeight + titleGap + legendEntries.Count * rowHeight);
        legendBackground.color = legendBackgroundColor;
        StyleText(legendTitle, legendFont, legendTitleFontSize);
        legendTitle.fontStyle |= FontStyles.Bold;
        legendTitle.rectTransform.anchorMin = legendTitle.rectTransform.anchorMax = legendTitle.rectTransform.pivot = new Vector2(0f, 1f);
        legendTitle.rectTransform.anchoredPosition = new Vector2(padding, -padding);
        legendTitle.rectTransform.sizeDelta = new Vector2(legendWidth - padding * 2f, titleHeight);

        for (int i = 0; i < legendEntries.Count; i++)
        {
            LegendEntry entry = legendEntries[i];
            entry.rect.anchorMin = entry.rect.anchorMax = entry.rect.pivot = new Vector2(0f, 1f);
            entry.rect.anchoredPosition = new Vector2(padding, -padding - titleHeight - titleGap - i * rowHeight);
            entry.rect.sizeDelta = new Vector2(legendWidth - padding * 2f, rowHeight);
            entry.background.enabled = !entry.isPlayer;
            entry.background.color = nodeBackgroundColor;
            entry.background.rectTransform.anchorMin = entry.background.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            entry.background.rectTransform.anchoredPosition = new Vector2(backingSize * 0.5f, 0f);
            entry.background.rectTransform.sizeDelta = Vector2.one * backingSize;
            entry.icon.rectTransform.anchorMin = entry.icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            entry.icon.rectTransform.anchoredPosition = new Vector2(backingSize * 0.5f, 0f);
            entry.icon.rectTransform.sizeDelta = Vector2.one * legendIconSize;
            entry.icon.color = entry.isPlayer ? playerIconColor : roomIconColor;
            StyleText(entry.label, legendFont, legendFontSize);
            entry.label.rectTransform.offsetMin = new Vector2(backingSize + 10f, 0f);
            entry.label.rectTransform.offsetMax = Vector2.zero;
        }
    }

    private RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        return rect;
    }

    private UnityEngine.UI.Image CreatePanel(string name, RectTransform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, TMP_FontAsset font, float size, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TMP_Text text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        StyleText(text, font, size);
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }
}
