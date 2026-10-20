using UnityEngine;

/// <summary>Adds vertical hovering while EnemyController handles horizontal movement and combat.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(EnemyController), typeof(Rigidbody2D))]
public sealed class EnemyFlight : MonoBehaviour
{
    [SerializeField, Min(0f)] private float hoverHeight = 0.25f;
    [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
    [SerializeField, Min(0.1f)] private float bobPeriod = 2.4f;
    [SerializeField, Min(0.05f)] private float landingDuration = 0.3f;

    private Rigidbody2D body;
    private EnemyController enemy;
    private float centerY;
    private float phase;
    private bool initialized;

    public bool DeathLandingComplete { get; private set; }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        enemy = GetComponent<EnemyController>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void Start()
    {
        if (!initialized) BeginHover();
    }

    public void BeginHover()
    {
        if (initialized) return;
        centerY = body.position.y + hoverHeight;
        phase = Random.Range(0f, Mathf.PI * 2f);
        initialized = true;
        Vector3 position = transform.position;
        position.y = centerY + Mathf.Sin(phase) * bobAmplitude;
        transform.position = position;
        body.position = position;
    }

    private void FixedUpdate()
    {
        if (!initialized || enemy.IsDead || !body.simulated) return;

        phase = (phase + Mathf.PI * 2f * Time.fixedDeltaTime / bobPeriod) % (Mathf.PI * 2f);
        Vector2 position = body.position;
        position.x += body.linearVelocity.x * Time.fixedDeltaTime;
        position.y = centerY + Mathf.Sin(phase) * bobAmplitude;
        body.MovePosition(position);
    }

    public void CaptureSave(EnemySaveData data)
    {
        data.hasFlightState = initialized;
        data.flightCenterY = centerY;
        data.flightPhase = phase;
    }

    public System.Collections.IEnumerator LandForDeath()
    {
        Vector3 start = transform.position;
        float groundY = initialized ? centerY - hoverHeight : start.y - hoverHeight;
        float elapsed = 0f;
        while (elapsed < landingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / landingDuration);
            Vector3 position = start;
            position.y = Mathf.Lerp(start.y, groundY, progress * progress);
            transform.position = position;
            body.position = position;
            yield return null;
        }
        DeathLandingComplete = true;
    }

    public void RestoreSave(EnemySaveData data)
    {
        centerY = data.hasFlightState ? data.flightCenterY : transform.position.y;
        phase = data.hasFlightState ? data.flightPhase : 0f;
        initialized = true;
    }
}
