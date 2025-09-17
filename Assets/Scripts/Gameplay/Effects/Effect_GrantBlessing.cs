using UnityEngine;
[CreateAssetMenu(fileName = "Effect_GrantBlessing", menuName = "HyperRoll/Effects/Grant Blessing")]
public class Effect_GrantBlessing : EffectLogic
{
    public BlessingType blessingToGrant;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.GrantBlessing(targetPlayer, blessingToGrant);
    }
}