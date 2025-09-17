using UnityEngine;
[CreateAssetMenu(fileName = "Effect_Drought", menuName = "HyperRoll/Effects/Drought")]
public class Effect_Drought : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.cannotGainPassive = true;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer} cannot gain passive income until they collect tax.");
    }
}