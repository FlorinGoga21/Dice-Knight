using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Spawns floating, fading heal number indicators that arc upwards and sideways
/// when an entity is healed.
/// </summary>
public sealed class HealNumber : MonoBehaviour
{
    [Header("---Animation---")]
    [Min(0.05f)]
    [SerializeField] private float lifetime = 0.8f;
    [Min(0f)]
    [SerializeField] private float horizontalDistance = 0.6f;
    [Min(0f)]
    [SerializeField] private float jumpHeight = 0.7f;
    [SerializeField] private float verticalOffset = 0.8f;

    [Header("---Appearance---")]
    [SerializeField] private TMP_FontAsset font;
    [Min(0.1f)]
    [SerializeField] private float fontSize = 4f;
    [SerializeField] private Color color = Color.green;
    [SerializeField] private int sortingOrder = 100;

    /// <summary>
    /// Spawns and animates a floating heal number at this entity's position.
    /// </summary>
    public void ShowHeal(int heal)
    {
        GameObject healNumberObject = new GameObject("Heal Number");
        healNumberObject.transform.position = transform.position + Vector3.up * verticalOffset;

        TextMeshPro text = healNumberObject.AddComponent<TextMeshPro>();
        text.text = $"+{heal}";
        if (font != null)
        {
            text.font = font;
        }

        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.sortingOrder = sortingOrder;

        Vector3 startPosition = healNumberObject.transform.position;
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        Vector3 endPosition = startPosition + Vector3.right * horizontalDirection * horizontalDistance;

        StartCoroutine(Animate(healNumberObject, text, startPosition, endPosition));
    }

    private IEnumerator Animate(
        GameObject healNumberObject,
        TextMeshPro text,
        Vector3 startPosition,
        Vector3 endPosition)
    {
        float elapsed = 0f;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / lifetime);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            float arcHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;

            healNumberObject.transform.position =
                Vector3.Lerp(startPosition, endPosition, smoothProgress) + Vector3.up * arcHeight;

            Color textColor = text.color;
            textColor.a = 1f - progress;
            text.color = textColor;
            yield return null;
        }

        Destroy(healNumberObject);
    }
}
