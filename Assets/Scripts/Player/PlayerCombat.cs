using System.Collections;
using UnityEngine;

/// <summary>
/// Partial implementation of PlayerController handling enemy detection
/// and turn-based dice combat sequencing.
/// </summary>
public partial class PlayerController
{
    private float encounterCheckTimer;
    private const float EncounterCheckInterval = 0.1f;

    private void CheckForEnemyEncounter()
    {
        if (activeEnemy != null || turnCoroutine != null)
        {
            return;
        }

        encounterCheckTimer -= Time.deltaTime;
        if (encounterCheckTimer > 0f)
        {
            return;
        }
        encounterCheckTimer = EncounterCheckInterval;

        EnemyController nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (EnemyController enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || enemy.IsDead)
            {
                continue;
            }

            float distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);
            if (distance <= enemyDetectionRange && distance < nearestDistance)
            {
                nearestEnemy = enemy;
                nearestDistance = distance;
            }
        }

        if (nearestEnemy == null)
        {
            return;
        }

        activeEnemy = nearestEnemy;
        if (diceRollUI == null)
        {
            Debug.LogError($"{nameof(PlayerController)} requires a {nameof(DiceRollUI)} for turn-based combat.", this);
            activeEnemy = null;
            return;
        }

        StopMovementAndSetIdle();
        turnNumber = 0;
        SetCombatUIVisible(true);
        turnCoroutine = StartCoroutine(RunTurnBasedCombat());
    }

    private void StopMovementAndSetIdle()
    {
        moveInput = 0f;
        rb.linearVelocity = Vector2.zero;
        animator.SetFloat("Speed", 0f);
        animator.SetBool("Grounded", isGrounded);

        movementDust?.Stop(
            withChildren: true,
            stopBehavior: ParticleSystemStopBehavior.StopEmitting
        );
    }

    private IEnumerator RunTurnBasedCombat()
    {
        activeEnemy.SetTurnBasedCombat(true);
        activeEnemy.Stats?.SetCombatHeartsVisible(true, combatUIFadeDuration);

        while (!hasDied && activeEnemy != null && !activeEnemy.IsDead)
        {
            turnNumber++;
            ShowTurnText();
            waitingForPlayerRoll = true;
            diceRollUI.SetRollButtonInteractable(true);

            while (waitingForPlayerRoll && !hasDied && activeEnemy != null && !activeEnemy.IsDead)
            {
                yield return null;
            }

            diceRollUI.SetRollButtonInteractable(false);
            if (hasDied || activeEnemy == null || activeEnemy.IsDead)
            {
                break;
            }

            int playerRoll = playerRollResult;
            int enemyRoll = Random.Range(1, 7);

            ShowRoll(playerRollDisplay, playerRoll, transform);
            ShowRoll(enemyRollDisplay, enemyRoll, activeEnemy.transform);
            yield return AnimateRollDisplaysIn();

            yield return new WaitForSeconds(rollDisplayDuration);

            SpriteRenderer winningDisplay = playerRoll > enemyRoll
                ? playerRollDisplay
                : enemyRoll > playerRoll ? enemyRollDisplay : null;

            if (winningDisplay != null)
            {
                yield return EnlargeWinningRoll(winningDisplay);
            }

            if (playerRoll > enemyRoll)
            {
                PlayAttackAnimationVisualOnly();
                activeEnemy.TakeDamage(attackDamage);
            }
            else if (enemyRoll > playerRoll)
            {
                activeEnemy.PerformTurnAttack();
            }

            yield return AnimateRollDisplaysUpAndFade();
            yield return new WaitForSeconds(attackHitDelay + attackCooldown);
        }

        if (activeEnemy != null)
        {
            activeEnemy.Stats?.SetCombatHeartsVisible(false, combatUIFadeDuration);
            activeEnemy.SetTurnBasedCombat(false);
        }

        DestroyRollDisplay(ref playerRollDisplay);
        DestroyRollDisplay(ref enemyRollDisplay);
        waitingForPlayerRoll = false;
        diceRollUI?.SetRollButtonInteractable(false);
        SetCombatUIVisible(false);
        activeEnemy = null;
        turnCoroutine = null;
    }
}
