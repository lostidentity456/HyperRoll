using UnityEngine;
[CreateAssetMenu(fileName = "Effect_BalanceMoney", menuName = "HyperRoll/Effects/Balance Money")]
public class Effect_BalanceMoney : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.BalanceAllPlayerMoney();
    }
}