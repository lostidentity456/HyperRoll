using UnityEngine;
[CreateAssetMenu(fileName = "Effect_Sanctioned", menuName = "HyperRoll/Effects/Sanctioned")]
public class Effect_Sanctioned : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.cannotUpgrade = true;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer} cannot upgrade buildings until they pay taxes.");
    }
}