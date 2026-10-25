using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Partial implementation of PlayerController handling input, horizontal movement,
/// jumping, ground checks, camera follow, dust particles, and melee attack.
/// </summary>
public partial class PlayerController
{
    private CombatRoomBounds roomCameraBounds;

    public void SetRoomCameraBounds(CombatRoomBounds bounds) => roomCameraBounds = bounds;

    private void HandleInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>().x;
    }

    private void HandleMovement()
    {
        rb.linearVelocity = new Vector2(GetHorizontalMovementVelocity(), rb.linearVelocity.y);
    }

    private float GetHorizontalMovementVelocity()
    {
        float velocity = moveInput * moveSpeed;
        return roomCameraBounds != null
            ? roomCameraBounds.ClampMovementVelocity(velocity, col, Time.fixedDeltaTime)
            : velocity;
    }

    private void HandleCameraFollow()
    {
        if (playerCamera == null)
        {
            return;
        }

        Vector3 cameraPositionWithoutShake = playerCamera.transform.position - damageShakeOffset;
        Vector3 targetPosition = cameraPositionWithoutShake;
        targetPosition.x = transform.position.x + cameraFollowOffset;
        if (roomCameraBounds != null)
            targetPosition.x = roomCameraBounds.ClampCameraX(targetPosition.x);

        Vector3 nextPosition = Vector3.Lerp(
            cameraPositionWithoutShake,
            targetPosition,
            cameraFollowSpeed * Time.deltaTime
        ) + damageShakeOffset;
        if (roomCameraBounds != null)
            nextPosition.x = roomCameraBounds.ClampCameraX(nextPosition.x);
        playerCamera.transform.position = nextPosition;
    }

    private void HandleRotation()
    {
        if (Mathf.Approximately(moveInput, 0f))
        {
            return;
        }

        if ((moveInput > 0f && isFacingRight) || (moveInput < 0f && !isFacingRight))
        {
            return;
        }

        transform.Rotate(0f, 180f, 0f);
        isFacingRight = moveInput > 0f;
    }

    private void HandleJump()
    {
        if (!inputActions.Player.Jump.WasPressedThisFrame() || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        characterAudio?.Jump();
    }

    private void HandleAttack()
    {
        if (inputActions.Player.Attack.activeControl?.device is Mouse && Mouse.current != null
            && EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId))
        {
            return;
        }

        if (!inputActions.Player.Attack.WasPressedThisFrame())
        {
            return;
        }

        if (Mathf.Abs(moveInput) > attackMovementThreshold)
        {
            return;
        }

        PlayAttackAnimation();
    }

    private IEnumerator ApplyAttackDamage()
    {
        yield return new WaitForSeconds(attackHitDelay);

        if (hasDied)
        {
            attackDamageCoroutine = null;
            yield break;
        }

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, attackRange);
        foreach (Collider2D hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable != null
                && !ReferenceEquals(damageable, ownDamageable)
                && !damageable.IsDead)
            {
                damageable.TakeDamage(attackDamage);
                break;
            }
        }

        attackDamageCoroutine = null;
    }

    private void UpdateAttackCooldown()
    {
        if (attackCooldownRemaining > 0f)
        {
            attackCooldownRemaining -= Time.deltaTime;
        }
    }

    private void HandleAnimation()
    {
        animator.SetFloat("Speed", Mathf.Abs(GetHorizontalMovementVelocity()) / Mathf.Max(0.01f, moveSpeed));
        animator.SetBool("Grounded", isGrounded);
    }

    private void HandleParticles()
    {
        characterAudio?.UpdateFootsteps(isGrounded && Mathf.Abs(GetHorizontalMovementVelocity()) > 0.01f);
        if (!wasGrounded && isGrounded) characterAudio?.Land();
        HandleMovementDust();
        HandleLandingDust();

        wasGrounded = isGrounded;
    }

    private void HandleMovementDust()
    {
        bool isMoving = Mathf.Abs(GetHorizontalMovementVelocity()) > 0.01f;

        if (movementDust == null)
        {
            return;
        }

        if (isGrounded && isMoving)
        {
            if (!movementDust.isPlaying)
            {
                movementDust.Play();
            }

            return;
        }

        if (movementDust.isPlaying)
        {
            movementDust.Stop(
                withChildren: true,
                stopBehavior: ParticleSystemStopBehavior.StopEmitting
            );
        }
    }

    private void HandleLandingDust()
    {
        if (landingDust != null && !wasGrounded && isGrounded)
        {
            landingDust.Play();
        }
    }

    private void UpdateGroundedState()
    {
        isGrounded = IsGrounded();
    }

    private bool IsGrounded()
    {
        Bounds bounds = col.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y);

        return Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }
}
