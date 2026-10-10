using UnityEngine;

public class PlayerShadow : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float heightOffset = 0.01f;

    private Collider2D[] playerColliders;

    private void Awake()
    {
        transform.SetParent(null, true);
        playerColliders = player.GetComponentsInChildren<Collider2D>();
    }

    private void LateUpdate()
    {
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
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            player.position,
            Vector2.down,
            Mathf.Infinity,
            groundLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (!IsPlayerCollider(hit.collider))
            {
                return hit;
            }
        }

        return default;
    }

    private bool IsPlayerCollider(Collider2D collider)
    {
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