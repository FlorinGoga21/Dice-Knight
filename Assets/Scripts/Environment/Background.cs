using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>
    /// Moves a repeating sprite layer with the camera while keeping its tiles seamless.
    /// Matching authored tiles share one owner instead of creating overlapping copies.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class Background : MonoBehaviour
    {
        [Header("---Movement Settings---")]
        [SerializeField] private Transform playerCamera;
        [Tooltip("0 keeps the layer in the world; 1 follows the camera. Use larger values for more distant scenery.")]
        [SerializeField, Range(0f, 1f)] private float parallaxFactor;

        [Header("---Infinite Scrolling---")]
        [Tooltip("Use the sprite's scaled world width so adjacent tiles meet without gaps.")]
        [SerializeField] private bool useSpriteWidth = true;
        [Tooltip("World-space repeat width when Use Sprite Width is disabled.")]
        [Min(0.01f)]
        [SerializeField] private float chunkWidth = 20f;

        private Camera targetCamera;
        private SpriteRenderer spriteRenderer;
        private Background owner;
        private readonly List<Transform> tiles = new List<Transform>();
        private readonly List<GameObject> generatedTiles = new List<GameObject>();
        private float startingCameraX;
        private float startingCenterX;
        private float centerOffset;
        private float repeatWidth;

        private void Start()
        {
            if (owner != null || GetComponent<Background>() != this
                || !TryGetComponent(out spriteRenderer) || spriteRenderer.sprite == null)
                return; // A container must not clone the entire layered background.

            if (playerCamera == null && Camera.main != null)
                playerCamera = Camera.main.transform;
            if (playerCamera != null)
                targetCamera = playerCamera.GetComponent<Camera>();
            if (targetCamera == null || !targetCamera.orthographic)
                return;

            // Start order is not guaranteed. Whichever authored tile starts first
            // claims its matching siblings, and only that owner performs the looping.
            owner = this;
            startingCameraX = playerCamera.position.x;
            startingCenterX = spriteRenderer.bounds.center.x;
            centerOffset = startingCenterX - transform.position.x;
            repeatWidth = Mathf.Max(0.01f, useSpriteWidth ? spriteRenderer.bounds.size.x : chunkWidth);

            if (transform.parent != null)
            {
                foreach (Transform sibling in transform.parent)
                {
                    if (sibling.TryGetComponent(out Background candidate) && MatchesLayer(candidate))
                    {
                        candidate.owner = this;
                        tiles.Add(sibling);
                    }
                }
            }
            else
                tiles.Add(transform);

            if (tiles.Count == 0)
                return;

            EnsureCoverage();
            PositionTiles();
        }

        private void LateUpdate()
        {
            if (owner != this || playerCamera == null || targetCamera == null)
                return;

            EnsureCoverage();
            PositionTiles();
        }

        private bool MatchesLayer(Background candidate)
        {
            if (!candidate.enabled || candidate.owner != null && candidate != this
                || !candidate.gameObject.activeInHierarchy
                || !candidate.TryGetComponent(out SpriteRenderer other))
                return false;

            return other.sprite == spriteRenderer.sprite
                && other.sharedMaterial == spriteRenderer.sharedMaterial
                && other.color == spriteRenderer.color
                && other.sortingLayerID == spriteRenderer.sortingLayerID
                && other.sortingOrder == spriteRenderer.sortingOrder
                && other.flipX == spriteRenderer.flipX && other.flipY == spriteRenderer.flipY
                && other.drawMode == spriteRenderer.drawMode && other.size == spriteRenderer.size
                && candidate.useSpriteWidth == useSpriteWidth
                && Mathf.Approximately(candidate.parallaxFactor, parallaxFactor)
                && (useSpriteWidth || Mathf.Approximately(candidate.chunkWidth, chunkWidth))
                && (candidate.playerCamera == null || candidate.playerCamera == playerCamera)
                && Mathf.Approximately(candidate.transform.position.y, transform.position.y)
                && Mathf.Approximately(candidate.transform.position.z, transform.position.z)
                && candidate.transform.rotation == transform.rotation
                && candidate.transform.lossyScale == transform.lossyScale;
        }

        private void EnsureCoverage()
        {
            float viewWidth = targetCamera.orthographicSize * targetCamera.aspect * 2f;
            int needed = Mathf.Max(3, Mathf.CeilToInt(viewWidth / repeatWidth) + 2);
            while (tiles.Count < needed)
            {
                GameObject tile = Instantiate(gameObject, transform.parent);
                tile.name = $"{gameObject.name} (Infinite Chunk)";
                tile.GetComponent<Background>().owner = this;
                tiles.Add(tile.transform);
                generatedTiles.Add(tile);
            }
        }

        private void PositionTiles()
        {
            float cameraX = playerCamera.position.x;
            float layerCenter = startingCenterX + (cameraX - startingCameraX) * parallaxFactor;
            // Choose the nearest repeat directly, so reversals and teleports do not
            // need multiple frames of recycling to catch up with the camera.
            float center = layerCenter + Mathf.Round((cameraX - layerCenter) / repeatWidth) * repeatWidth;
            int middle = tiles.Count / 2;

            for (int i = 0; i < tiles.Count; i++)
            {
                Vector3 position = tiles[i].position;
                position.x = center + (i - middle) * repeatWidth - centerOffset;
                tiles[i].position = position;
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject tile in generatedTiles)
                if (tile != null) Destroy(tile);
        }
    }
}
