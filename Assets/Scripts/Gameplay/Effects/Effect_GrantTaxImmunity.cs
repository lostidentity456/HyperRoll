using UnityEngine;

[CreateAssetMenu(fileName = "Effect_TaxImmunity", menuName = "HyperRoll/Effects/Tax Immunity")]
public class Effect_TaxImmunity : EffectLogic
{

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.passiveState.hasTaxImmunity = true;
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
    }
}