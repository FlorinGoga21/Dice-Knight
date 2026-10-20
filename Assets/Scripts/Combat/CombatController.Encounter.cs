using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class CombatController
{
    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        UpdateHealingPotionButtonState();
        if (IsInCombat || player == null || !player.CanStartTurnCombat)
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

            if (pendingRoomSave != null && enemy.Stats.SaveId == pendingRoomSave.combatEnemyId)
            {
                nearestEnemy = enemy;
                break;
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

        if (diceRollUI == null)
        {
            Debug.LogError($"{nameof(CombatController)} requires a {nameof(DiceRollUI)} for turn-based combat.", this);
            return;
        }

        activeEnemy = nearestEnemy;
        player.PrepareForTurnCombat();
        turnNumber = 0;
        blockUsesRemaining = BlocksPerEncounter;
        if (pendingRoomSave != null && nearestEnemy.Stats.SaveId == pendingRoomSave.combatEnemyId)
        {
            turnNumber = Mathf.Max(0, pendingRoomSave.turn - 1);
            blockUsesRemaining = Mathf.Clamp(pendingRoomSave.blocks, 0, BlocksPerEncounter);
        }
        pendingRoomSave = null;
        UpdateBlockButtonLabel();
        SetCombatUIVisible(true);
        encounterCoroutine = StartCoroutine(RunEncounter());
    }

    private IEnumerator RunEncounter()
    {
        activeEnemy.SetTurnBasedCombat(true);
        activeEnemy.Stats?.SetCombatHeartsVisible(true, combatUIFadeDuration);

        while (player != null && player.CanStartTurnCombat && activeEnemy != null && !activeEnemy.IsDead)
        {
            turnNumber++;
            ShowTurnText();
            blockChosenThisTurn = false;
            player.SetBlockingPose(false);
            if (playerDice != null) playerDice.SetActive(true);
            SetCanvasGroupVisibility(playerDiceCanvasGroup, true);
            if (blockButton != null) blockButton.interactable = blockUsesRemaining > 0;
            lastDiceResults = null;
            waitingForPlayerRoll = true;
            diceRollUI.SetRollButtonInteractable(true);

            while (waitingForPlayerRoll && player != null && player.CanStartTurnCombat
                && activeEnemy != null && !activeEnemy.IsDead)
            {
                yield return null;
            }

            diceRollUI.SetRollButtonInteractable(false);
            if (player == null || !player.CanStartTurnCombat || activeEnemy == null || activeEnemy.IsDead)
            {
                break;
            }

            int[] playerRolls = blockChosenThisTurn ? null : lastDiceResults ?? new[] { Random.Range(1, 7) };
            int playerRoll = playerRolls != null ? SumArray(playerRolls) : 0;
            int playerMultiplier = playerRolls != null ? ComputeDiceMultiplier(playerRolls) : 1;

            int[] enemyRolls = new int[Mathf.Max(1, activeEnemy.DiceCount)];
            for (int i = 0; i < enemyRolls.Length; i++)
            {
                enemyRolls[i] = Random.Range(1, 7);
            }
            int enemyRoll = SumArray(enemyRolls);
            int enemyMultiplier = ComputeDiceMultiplier(enemyRolls);

            playerRollDisplays = blockChosenThisTurn
                ? CreateBlockDisplay(transform, "Player Block")
                : CreateRollDisplays(playerRolls, transform, "Player Roll");
            enemyRollDisplays = CreateRollDisplays(enemyRolls, activeEnemy.transform, "Enemy Roll");
            rollResultDisplays = CreateRollResultDisplays(
                playerRoll,
                enemyRoll,
                playerRollDisplays,
                enemyRollDisplays,
                blockChosenThisTurn);
            yield return AnimateDisplaysIn(playerRollDisplays, enemyRollDisplays, rollResultDisplays);
            yield return new WaitForSeconds(rollDisplayDuration);
            if (rollResultDisplays != null && rollResultDisplays.Length > RollComparisonDisplayIndex)
            {
                yield return PopComparisonDisplay(rollResultDisplays[RollComparisonDisplayIndex]);
            }

            if (!blockChosenThisTurn && playerRoll > enemyRoll)
            {
                yield return EnlargeDisplays(playerRollDisplays, rollResultDisplays != null ? rollResultDisplays[0] : null);
                player.PlayAttackAnimationVisualOnly();
                activeEnemy.TakeDamage(player.AttackDamage * playerMultiplier);
            }
            else if (blockChosenThisTurn)
            {
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier, applyDamage: false);
            }
            else if (enemyRoll > playerRoll)
            {
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier);
            }

            yield return AnimateDisplaysOut(playerRollDisplays, enemyRollDisplays, rollResultDisplays);
            DestroyRollDisplays(ref playerRollDisplays);
            DestroyRollDisplays(ref enemyRollDisplays);
            DestroyRollResultDisplays(ref rollResultDisplays);
            yield return new WaitForSeconds(player.AttackHitDelay + player.AttackCooldown);
        }

        FinishEncounter();
    }

    private void FinishEncounter()
    {
        if (activeEnemy != null)
        {
            activeEnemy.Stats?.SetCombatHeartsVisible(false, combatUIFadeDuration);
            activeEnemy.SetTurnBasedCombat(false);
        }

        DestroyRollDisplays(ref playerRollDisplays);
        DestroyRollDisplays(ref enemyRollDisplays);
        DestroyRollResultDisplays(ref rollResultDisplays);
        waitingForPlayerRoll = false;
        blockChosenThisTurn = false;
        player?.SetBlockingPose(false);
        diceRollUI?.SetRollButtonInteractable(false);
        if (blockButton != null) blockButton.interactable = false;
        SetCombatUIVisible(false);
        activeEnemy = null;
        encounterCoroutine = null;
    }

    private void HandlePlayerDiceRolled(int[] results)
    {
        if (!waitingForPlayerRoll || blockChosenThisTurn)
        {
            return;
        }

        lastDiceResults = results;
        waitingForPlayerRoll = false;
    }

    private void HandlePlayerRollStarted()
    {
        UpdateHealingPotionButtonState();

        if (waitingForPlayerRoll && blockButton != null)
        {
            blockButton.interactable = false;
        }
    }

    public void ChooseToBlock()
    {
        if (!waitingForPlayerRoll || blockChosenThisTurn || blockUsesRemaining <= 0
            || diceRollUI == null || diceRollUI.IsRolling)
        {
            return;
        }

        blockChosenThisTurn = true;
        blockUsesRemaining--;
        UpdateBlockButtonLabel();
        player.SetBlockingPose(true);
        SetCanvasGroupVisibility(playerDiceCanvasGroup, false);
        diceRollUI.SetRollButtonInteractable(false);
        if (blockButton != null) blockButton.interactable = false;
        waitingForPlayerRoll = false;
        UpdateHealingPotionButtonState();
    }

    private static int ComputeDiceMultiplier(int[] rolls)
    {
        if (rolls == null || rolls.Length < 2) return 1;
        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int roll in rolls)
        {
            counts.TryGetValue(roll, out int count);
            counts[roll] = count + 1;
        }
        if (rolls.Length >= 3 && counts.ContainsValue(3)) return 3;
        return counts.ContainsValue(2) || counts.ContainsValue(3) ? 2 : 1;
    }

    private static int SumArray(int[] values)
    {
        int sum = 0;
        foreach (int value in values) sum += value;
        return sum;
    }
}
