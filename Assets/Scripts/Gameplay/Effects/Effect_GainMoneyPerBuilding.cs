using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GainMoneyPerBuilding", menuName = "HyperRoll/Effects/Gain Money Per Building")]
public class Effect_GainMoneyPerBuilding : EffectLogic
{
    [Tooltip("The flat amount of money to grant.")]
    public int flatAmount;
    [Tooltip("The bonus money granted for each building owned.")]
    public int amountPerBuilding;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        targetPlayer.money += (flatAmount + amountPerBuilding * gameManager.GetBuildingsCountForPlayer(targetPlayer)) * (gameManager.GetCurrentStage() + 1);
        gameManager.GetUIManager().UpdatePlayerMoney(targetPlayer);
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
    }
}