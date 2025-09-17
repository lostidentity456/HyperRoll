using UnityEngine;
[CreateAssetMenu(fileName = "Effect_SingleDice", menuName = "HyperRoll/Effects/Single Dice")]
public class Effect_SingleDice : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.singleDiceTurns = 3;
        gameManager.GetUIManager().LogDuelMessage($"{targetPlayer} loses focys and will only roll one dice for the next 3 duels.");
    }
}