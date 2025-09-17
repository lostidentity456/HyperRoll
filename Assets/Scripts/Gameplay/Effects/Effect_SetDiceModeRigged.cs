using UnityEngine;

[CreateAssetMenu(fileName = "Effect_SetDiceModeRigged", menuName = "HyperRoll/Effects/Set Dice Mode Rigged")]
public class Effect_SetDiceModeRigged : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.SetNextDuelDiceMode(DiceMode.Rigged);
        gameManager.SetRiggedPlayer(targetPlayer);
    }
}