using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GrantGuaranteedWin", menuName = "HyperRoll/Effects/Grant Guaranteed Win")]
public class Effect_GrantGuaranteedWin : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.hasGuaranteedWin = true;
        gameManager.GetUIManager().LogDuelMessage($"Next duel is a guaranteed win for {targetPlayer}!");
    }
}