using UnityEngine;
[CreateAssetMenu(fileName = "Effect_TiesAreLosses", menuName = "HyperRoll/Effects/Ties Are Losses")]
public class Effect_TiesAreLosses : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        if (targetPlayer.characterData?.passiveAbility == CharacterPassive.TheNegotiator)
        {
            gameManager.GetUIManager().LogDuelMessage("Negotiator is immune to Misfortune!");
            return; // The card does nothing
        }

        targetPlayer.passiveState.tiesAreLosses = true;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer}'s ties will now count as a loss until they special win.");
    }
}