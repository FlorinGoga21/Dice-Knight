using UnityEngine;

public class DummyPlayerWalk : MonoBehaviour
{
    [SerializeField] private float speed = 1f;
    [SerializeField] private Animator animator;
    [SerializeField] private Camera followCamera;
    [SerializeField] private float cameraFollowSpeed = 5f;
    [SerializeField] private float cameraOffset;
    [SerializeField] private ParticleSystem movementDust;

    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Grounded = Animator.StringToHash("Grounded");

    public float CameraFollowOffset => cameraOffset;

    public void SetCameraOffset(float offset)
    {
        cameraOffset = offset;
    }

    private void Awake()
    {
        // The Knight prefab has an Animator on both the visual root and UnitRoot.
        // UnitRoot is the one the gameplay controller drives for the walking pose.
        if (animator == null || animator.transform == transform)
        {
            foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
            {
                if (candidate.gameObject.name == "UnitRoot")
                {
                    animator = candidate;
                    break;
                }
            }
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (followCamera == null)
            followCamera = Camera.main;

        if (movementDust == null)
        {
            foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particleSystem.gameObject.name == "MovementDust")
                {
                    movementDust = particleSystem;
                    break;
                }
            }
        }

        if (animator != null)
        {
            animator.SetBool(Grounded, true);
            animator.SetFloat(Speed, 1f);
        }

        if (movementDust != null)
        {
            // This particle system emits by distance, but the dummy moves via Transform
            // rather than Rigidbody velocity. Make it measure the Transform's motion.
            ParticleSystem.MainModule dustMain = movementDust.main;
            dustMain.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

            if (!movementDust.isPlaying)
                movementDust.Play();
        }
    }

    private void Update()
    {
        transform.position += Vector3.right * speed * Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool(Grounded, true);
            animator.SetFloat(Speed, 1f);
        }

        if (movementDust != null && !movementDust.isPlaying)
            movementDust.Play();
    }

    private void LateUpdate()
    {
        if (followCamera == null)
            return;

        Vector3 targetPosition = followCamera.transform.position;
        targetPosition.x = transform.position.x + cameraOffset;
        followCamera.transform.position = Vector3.Lerp(
            followCamera.transform.position,
            targetPosition,
            cameraFollowSpeed * Time.deltaTime
        );
    }
}
