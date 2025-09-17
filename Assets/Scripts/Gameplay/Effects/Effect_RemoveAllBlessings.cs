using UnityEngine;

[CreateAssetMenu(fileName = "Effect_RemoveAllBlessings", menuName = "HyperRoll/Effects/Remove All Blessings")]
public class Effect_RemoveAllBlessings : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        if (targetPlayer.passiveState.acquiredBlessings.Count > 0)
        {
            gameManager.GetUIManager().LogDuelMessage($"{targetPlayer.name} feels a wave of amnesia... All Blessings are lost!");

            // Reset the blessing list
            targetPlayer.passiveState.acquiredBlessings.Clear();

            // Reset all associated stats to their defaults
            targetPlayer.passiveState.blessingOfOneTaxMultiplier = 0f;
            targetPlayer.passiveState.blessingOfFiveCounter = 0;
            targetPlayer.passiveState.bonusStepsPool = 0;
        }
        else
        {
            // If the player has no blessings, the card does nothing.
            gameManager.GetUIManager().LogDuelMessage("A wave of amnesia washes over you, but you had nothing to forget!");
        }
    }
}