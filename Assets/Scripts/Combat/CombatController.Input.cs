using HeyHeyThere.SceneTransitionsFree;

public sealed partial class CombatController
{
    private PlayerInputActions combatInputActions;

    private void HandleCombatInput()
    {
        if (!IsInCombat || player == null || !player.CanStartTurnCombat
            || playerStats == null || playerStats.IsDead || SceneTransition.IsPlaying)
        {
            return;
        }

        var combat = combatInputActions.Combat;
        if (combat.Roll.WasPressedThisFrame() && waitingForPlayerRoll && !blockChosenThisTurn
            && diceRollUI != null && diceRollUI.isActiveAndEnabled && !diceRollUI.IsRolling)
        {
            diceRollUI.RollDice();
        }

        if (combat.Block.WasPressedThisFrame())
        {
            ChooseToBlock();
        }

        if (combat.Heal.WasPressedThisFrame())
        {
            UseHealingPotion();
        }
    }
}
