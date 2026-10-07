using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Environment 
{
    public class Background : MonoBehaviour 
    {
        [Header("Movement Settings")]
        public float speed = -2f; // Negative speed moves left, positive moves right
		public bool isMoving = false; // Flag to control movement

        [Header("Loop Boundaries")]
        [Tooltip("The X position where the background resets.")]
        public float destinationPoint = -19.2f;

        [Tooltip("The X position where the background moves back to after reaching the destination point.")]
        public float originalPoint = 19.2f;

        private float currentX;

        void Update() 
        {
            // Move along the X axis
            if (!isMoving) return;
            currentX = transform.position.x + (speed * Time.deltaTime);
            transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

            // Check if boundary destination has been reached
            if (destinationPoint < 0f) 
            {
                if (currentX <= destinationPoint) 
                {
                    ResetPosition();
                }
            } 
            else 
            {
                if (currentX >= destinationPoint) 
                {
                    ResetPosition();
                }
            }
        }

        private void ResetPosition() 
        {
            currentX = originalPoint;
            transform.position = new Vector3(currentX, transform.position.y, transform.position.z);
        }
    }
}