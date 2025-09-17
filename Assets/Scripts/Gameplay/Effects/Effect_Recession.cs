using UnityEngine;
[CreateAssetMenu(fileName = "Effect_Recession", menuName = "HyperRoll/Effects/Recession")]
public class Effect_Recession : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.incomeIsHalved = true;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer}'s tax income is halved until they max-level a building.");
    }
}