using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class CombatController
{
    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        HandleCombatInput();
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
        GameAudio.Instance?.BeginCombat();
        encounterCoroutine = StartCoroutine(RunEncounter());
        combatInputActions.Combat.Enable();
    }

    private IEnumerator RunEncounter()
    {
        activeEnemy.SetTurnBasedCombat(true, this);
        activeEnemy.Stats?.SetCombatHeartsVisible(true, combatUIFadeDuration);

        while (player != null && player.CanStartTurnCombat && activeEnemy != null && !activeEnemy.IsDead)
        {
            turnNumber++;
            GameAudio.Play(GameAudio.Cue.TurnStarted);
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
            if (!blockChosenThisTurn && playerProgression != null
                && playerProgression.TryGetLuckReroll(playerRolls, out int luckyDie, out int reroll))
            {
                yield return ShowLuckyReroll(playerRolls, luckyDie, reroll, enemyRoll);
                playerRoll = SumArray(playerRolls);
                playerMultiplier = ComputeDiceMultiplier(playerRolls);
            }
            yield return new WaitForSeconds(rollDisplayDuration);
            if (rollResultDisplays != null && rollResultDisplays.Length > RollComparisonDisplayIndex)
            {
                yield return PopComparisonDisplay(rollResultDisplays[RollComparisonDisplayIndex]);
            }

            if (!blockChosenThisTurn && playerRoll > enemyRoll)
            {
                GameAudio.Play(GameAudio.Cue.RollWin);
                yield return EnlargeDisplays(playerRollDisplays, rollResultDisplays != null ? rollResultDisplays[0] : null);
                player.PlayAttackAnimationVisualOnly();
                activeEnemy.TakeDamage(player.AttackDamage * playerMultiplier);
            }
            else if (blockChosenThisTurn)
            {
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier, applyDamage: false);
                GameAudio.Play(GameAudio.Cue.BlockImpact);
            }
            else if (enemyRoll > playerRoll)
            {
                GameAudio.Play(GameAudio.Cue.RollLose);
                yield return EnlargeDisplays(enemyRollDisplays, rollResultDisplays != null ? rollResultDisplays[1] : null);
                activeEnemy.PerformTurnAttack(enemyMultiplier);
            }
            else GameAudio.Play(GameAudio.Cue.RollTie);

            while (activeEnemy != null && activeEnemy.IsAttackInProgress) yield return null;

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
        GameAudio.Instance?.EndCombat(playerStats != null && !playerStats.IsDead && activeEnemy != null && activeEnemy.IsDead);
        combatInputActions.Combat.Disable();

        if (activeEnemy != null)
        {
            activeEnemy.Stats?.SetCombatHeartsVisible(false, combatUIFadeDuration);
            activeEnemy.SetTurnBasedCombat(false);
        }

        DestroyRollDisplays(ref playerRollDisplays);
        DestroyRollDisplays(ref enemyRollDisplays);
        DestroyRollResultDisplays(ref rollResultDisplays);
        DestroyLuckyFeedback();
        waitingForPlayerRoll = false;
        blockChosenThisTurn = false;
        player?.SetBlockingPose(false);
        diceRollUI?.SetRollButtonInteractable(false);
        if (blockButton != null) blockButton.interactable = false;
        SetCombatUIVisible(false);
        activeEnemy = null;
        encounterCoroutine = null;
        if (playerStats != null && !playerStats.IsDead)
            playerStats.GetComponent<PlayerProgression>()?.ShowPostFightProgress();
    }

    public void EndForPlayerDeath()
    {
        StopAllCoroutines();
        FinishEncounter();
        SetCombatUIVisible(false, instant: true);
        foreach (var enemy in FindObjectsByType<EnemyController>(FindObjectsInactive.Exclude))
            if (!enemy.IsDead) enemy.SetTurnBasedCombat(true);
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
        GameAudio.Play(GameAudio.Cue.BlockRaised);
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
        return PlayerLevelState.GetDiceMultiplier(rolls);
    }

    private static int SumArray(int[] values)
    {
        int sum = 0;
        foreach (int value in values) sum += value;
        return sum;
    }
}
