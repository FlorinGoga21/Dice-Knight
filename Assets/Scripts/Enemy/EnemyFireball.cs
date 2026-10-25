using UnityEngine;

/// <summary>Animated, aimed projectile that applies its damage once on arrival.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class EnemyFireball : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField, Min(1f)] private float framesPerSecond = 12f;
    [SerializeField, Min(0.05f)] private float travelDuration = 0.55f;

    private SpriteRenderer spriteRenderer;
    private EnemyController source;
    private Transform target;
    private Collider2D targetCollider;
    private IDamageable damageable;
    private Vector3 origin;
    private float elapsed;
    private int damage;
    private bool applyDamage;

    public void Launch(EnemyController owner, Transform destination, IDamageable victim, int amount, bool canDamage)
    {
        source = owner;
        target = destination;
        targetCollider = destination.GetComponent<Collider2D>();
        damageable = victim;
        damage = amount;
        applyDamage = canDamage;
        origin = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (source == null || source.IsDead || target == null || damageable == null || damageable.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;
        Vector3 destination = targetCollider != null ? targetCollider.bounds.center : target.position;
        destination.z = origin.z;
        Vector3 direction = destination - origin;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        transform.position = Vector3.Lerp(origin, destination, Mathf.Clamp01(elapsed / travelDuration));
        if (frames != null && frames.Length > 0)
            spriteRenderer.sprite = frames[(int)(elapsed * framesPerSecond) % frames.Length];

        if (elapsed < travelDuration) return;
        source.PlayProjectileImpactSound();
        if (applyDamage) damageable.TakeDamage(damage);
        Destroy(gameObject);
    }
}
