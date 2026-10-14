using UnityEngine;

/// <summary>
/// Projects a ground shadow underneath the player by raycasting against the ground layer.
/// Uses a non-allocating raycast buffer to prevent garbage collection spikes.
/// </summary>
public class PlayerShadow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float heightOffset = 0.01f;

    private Collider2D[] playerColliders;
    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];

    private void Awake()
    {
        transform.SetParent(null, true);

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            playerColliders = player.GetComponentsInChildren<Collider2D>();
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        RaycastHit2D hit = FindGroundHit();
        if (!hit)
        {
            return;
        }

        transform.position = new Vector3(
            player.position.x,
            hit.point.y + heightOffset,
            transform.position.z
        );
    }

    private RaycastHit2D FindGroundHit()
    {
        int count = Physics2D.RaycastNonAlloc(
            player.position,
            Vector2.down,
            hitBuffer,
            Mathf.Infinity,
            groundLayer
        );

        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hitBuffer[i];
            if (!IsPlayerCollider(hit.collider))
            {
                return hit;
            }
        }

        return default;
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
        if (playerColliders == null)
        {
            return false;
        }

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (collider == playerCollider)
            {
                return true;
            }
        }

        return false;
    }
}