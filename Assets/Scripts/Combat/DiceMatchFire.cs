using UnityEngine;

/// <summary>Loops the fire behind a matching result die and follows its visibility.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class DiceMatchFire : MonoBehaviour
{
    [Header("---Fire Animation---")]
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 30f;
    [Tooltip("Fire size relative to the result die's sprite width.")]
    [SerializeField, Min(0.01f)] private float size = 2.2f;

    [Header("---Ignition---")]
    [Tooltip("Seconds before the flames begin appearing after a match is revealed.")]
    [SerializeField, Min(0f)] private float ignitionDelay = 0.15f;
    [Tooltip("Seconds for the flames to fade in and grow to full size.")]
    [SerializeField, Min(0.01f)] private float ignitionDuration = 0.5f;

    private SpriteRenderer fireRenderer;
    private SpriteRenderer dieRenderer;
    private float elapsed;
    private float ignitionElapsed;
    private Vector3 fullScale;

    private void Awake()
    {
        fireRenderer = GetComponent<SpriteRenderer>();
        fireRenderer.enabled = false;
    }

    public void Initialize(SpriteRenderer die, int frameOffset)
    {
        dieRenderer = die;
        transform.SetParent(die.transform, false);
        transform.localPosition = Vector3.down * die.sprite.bounds.size.y * 0.12f;
        fullScale = Vector3.one * die.sprite.bounds.size.x * size;
        transform.localScale = fullScale * 0.35f;
        fireRenderer.sortingLayerID = die.sortingLayerID;
        fireRenderer.sortingOrder = die.sortingOrder - 1;
        elapsed = frameOffset / Mathf.Max(1f, framesPerSecond);
        ignitionElapsed = 0f;
    }

    private void LateUpdate()
    {
        if (dieRenderer == null || frames == null || frames.Length == 0)
        {
            fireRenderer.enabled = false;
            return;
        }

        elapsed += Time.deltaTime;
        ignitionElapsed += Time.deltaTime;
        float ignition = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((ignitionElapsed - ignitionDelay) / Mathf.Max(0.01f, ignitionDuration)));
        transform.localScale = fullScale * Mathf.Lerp(0.35f, 1f, ignition);
        fireRenderer.sprite = frames[Mathf.FloorToInt(elapsed * framesPerSecond) % frames.Length];
        float alpha = dieRenderer.color.a * ignition;
        fireRenderer.color = new Color(1f, 1f, 1f, alpha);
        fireRenderer.enabled = dieRenderer.enabled && alpha > 0f;
    }
}
