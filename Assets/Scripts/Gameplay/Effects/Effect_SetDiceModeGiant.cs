using UnityEngine;

[CreateAssetMenu(fileName = "Effect_SetDiceModeGiant", menuName = "HyperRoll/Effects/Set Dice Mode Giant")]
public class Effect_SetDiceModeGiant : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.SetNextDuelDiceMode(DiceMode.Giant);
    }
}