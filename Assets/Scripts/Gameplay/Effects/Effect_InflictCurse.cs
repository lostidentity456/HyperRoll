using UnityEngine;
[CreateAssetMenu(fileName = "Effect_InflictCurse", menuName = "HyperRoll/Effects/Inflict Curse")]
public class Effect_InflictCurse : EffectLogic
{
    public CurseType curseToInflict;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.InflictCurse(targetPlayer, curseToInflict);
    }
}