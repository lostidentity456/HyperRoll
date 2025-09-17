using UnityEngine;

[CreateAssetMenu(fileName = "Effect_SetDiceModeTiny", menuName = "HyperRoll/Effects/Set Dice Mode Tiny")]
public class Effect_SetDiceModeTiny : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.SetNextDuelDiceMode(DiceMode.Tiny);
    }
}