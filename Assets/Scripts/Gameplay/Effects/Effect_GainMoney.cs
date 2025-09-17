using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GainMoney", menuName = "HyperRoll/Effects/Gain Money")]
public class Effect_GainMoney : EffectLogic
{
    [Tooltip("The flat amount of money to grant.")]
    public int amount;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.money += amount * (gameManager.GetCurrentStage() + 1);
        gameManager.GetUIManager().UpdatePlayerMoney(targetPlayer); 
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
    }
}