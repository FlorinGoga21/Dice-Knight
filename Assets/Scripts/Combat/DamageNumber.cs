using System.Collections;
using TMPro;
using UnityEngine;

public sealed class DamageNumber : MonoBehaviour
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
    [SerializeField] private Color color = Color.red;
    [SerializeField] private int sortingOrder = 100;

    public void ShowDamage(int damage)
    {
        GameObject damageNumberObject = new GameObject("Damage Number");
        damageNumberObject.transform.position = transform.position + Vector3.up * verticalOffset;

        TextMeshPro text = damageNumberObject.AddComponent<TextMeshPro>();
        text.text = $"-{damage}";
        if (font != null)
        {
            text.font = font;
        }
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.sortingOrder = sortingOrder;

        Vector3 startPosition = damageNumberObject.transform.position;
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        Vector3 endPosition = startPosition
            + Vector3.right * horizontalDirection * horizontalDistance;
        StartCoroutine(Animate(damageNumberObject, text, startPosition, endPosition));
    }

    private IEnumerator Animate(
        GameObject damageNumberObject,
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

            damageNumberObject.transform.position =
                Vector3.Lerp(startPosition, endPosition, smoothProgress)
                + Vector3.up * arcHeight;

            Color color = text.color;
            color.a = 1f - progress;
            text.color = color;
            yield return null;
        }

        Destroy(damageNumberObject);
    }
}
