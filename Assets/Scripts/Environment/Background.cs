using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>
    /// Creates an endlessly scrolling parallax background by spawning adjacent chunks
    /// and repositioning them as the camera moves horizontally.
    /// </summary>
    public class Background : MonoBehaviour
    {
        [Header("---Movement Settings---")]
        [SerializeField] private Transform playerCamera;
        [SerializeField] private float chunkWidth = 20f;

        private Camera targetCamera;
        private bool isClone;

        private void Start()
        {
            if (!isClone)
            {
                CreateAdjacentChunks();
            }

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            if (playerCamera != null)
            {
                targetCamera = playerCamera.GetComponent<Camera>();
            }
        }

        private void LateUpdate()
        {
            if (playerCamera == null || targetCamera == null)
            {
                return;
            }

            RecycleChunk();
        }

        private void CreateAdjacentChunks()
        {
            CreateChunk(-chunkWidth);
            CreateChunk(chunkWidth);
        }

        private void CreateChunk(float offset)
        {
            GameObject chunk = Instantiate(gameObject, transform.position, transform.rotation);
            chunk.name = $"{gameObject.name} (Infinite Chunk)";
            chunk.transform.position += Vector3.right * offset;
            chunk.GetComponent<Background>().isClone = true;
        }

        private void RecycleChunk()
        {
            float cameraHalfWidth = targetCamera.orthographicSize * targetCamera.aspect;
            float leftEdge = playerCamera.position.x - cameraHalfWidth;
            float rightEdge = playerCamera.position.x + cameraHalfWidth;

            if (transform.position.x + chunkWidth < leftEdge)
            {
                transform.position += Vector3.right * (chunkWidth * 2f);
            }
            else if (transform.position.x - chunkWidth > rightEdge)
            {
                transform.position -= Vector3.right * (chunkWidth * 2f);
            }
        }
    }
}