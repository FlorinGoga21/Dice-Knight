using TMPro;
using UnityEngine;

internal static class FloatingNumberVisuals
{
    internal static TextMeshPro Create(
        string objectName,
        string value,
        Vector3 position,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        int sortingOrder)
    {
        var numberObject = new GameObject(objectName);
        numberObject.transform.position = position;

        TextMeshPro text = numberObject.AddComponent<TextMeshPro>();
        text.text = value;
        if (font != null)
        {
            text.font = font;
        }

        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.sortingOrder = sortingOrder;
        return text;
    }

    internal static void AnimateFrame(
        TextMeshPro text,
        Vector3 startPosition,
        Vector3 endPosition,
        float progress,
        float jumpHeight)
    {
        float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
        float arcHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
        text.transform.position =
            Vector3.Lerp(startPosition, endPosition, smoothProgress) + Vector3.up * arcHeight;

        Color textColor = text.color;
        textColor.a = 1f - progress;
        text.color = textColor;
    }
}
