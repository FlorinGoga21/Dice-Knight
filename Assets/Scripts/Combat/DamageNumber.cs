using TMPro;
using UnityEngine;

/// <summary>
/// Spawns floating, fading damage number indicators that arc upwards and sideways
/// when an entity takes damage.
/// </summary>
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

    /// <summary>
    /// Spawns and animates a floating damage number at this entity's position.
    /// </summary>
    public void ShowDamage(int damage)
    {
        TextMeshPro text = FloatingNumberVisuals.Create(
            "Damage Number", $"-{damage}", transform.position + Vector3.up * verticalOffset,
            font, fontSize, color, sortingOrder);
        Vector3 startPosition = text.transform.position;
        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        Vector3 endPosition = startPosition + Vector3.right * horizontalDirection * horizontalDistance;

        StartCoroutine(FloatingNumberVisuals.Animate(text, startPosition, endPosition, lifetime, jumpHeight));
    }
}
