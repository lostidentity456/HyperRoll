using UnityEngine;
[CreateAssetMenu(fileName = "Effect_MixedBlessing", menuName = "HyperRoll/Effects/Mixed Blessing")]
public class Effect_MixedBlessing : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.GrantRandomUnownedBlessing(targetPlayer);
        gameManager.InflictRandomCurse(targetPlayer);
    }
}