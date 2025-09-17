using UnityEngine;

[CreateAssetMenu(fileName = "Effect_ChaosDuel", menuName = "HyperRoll/Effects/Chaos Duel")]
public class Effect_ChaosDuel : EffectLogic
{
    public int turns = 3;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.chaosRunTurns = turns;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer} loses control for {turns} duels, but their wins will be Special!");
    }
}