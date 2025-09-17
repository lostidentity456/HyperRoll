using UnityEngine;
[CreateAssetMenu(fileName = "Effect_GrantRandomBlessing", menuName = "HyperRoll/Effects/Grant Random Blessing")]
public class Effect_GrantRandomBlessing : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.GrantRandomUnownedBlessing(targetPlayer);
    }
}