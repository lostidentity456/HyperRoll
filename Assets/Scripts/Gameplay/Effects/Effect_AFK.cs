using UnityEngine;
[CreateAssetMenu(fileName = "Effect_AFK", menuName = "HyperRoll/Effects/AFK")]
public class Effect_AFK : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.isAFK = true;
        gameManager.GetUIManager().LogDuelMessage("You went AFK! You will lose your next turn.");
    }
}