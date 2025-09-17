using UnityEngine;

[CreateAssetMenu(fileName = "Effect_LoseMoney", menuName = "HyperRoll/Effects/Lose Money")]
public class Effect_LoseMoney : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.money -= targetPlayer.money / 5;
        gameManager.GetUIManager().UpdatePlayerMoney(targetPlayer);
        }
}