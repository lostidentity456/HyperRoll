using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GrantOverwhelmingPower", menuName = "HyperRoll/Effects/Grant Overwhelming Power")]
public class Effect_GrantOverwhelmingPower : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.hasOverwhelmingPower = true;
        gameManager.GetUIManager().LogDuelMessage("Gained Overwhelming Power! Your next Special roll is unstoppable!");
    }
}