using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GainMoneyPerLap", menuName = "HyperRoll/Effects/Gain Money Per Lap")]
public class Effect_GainMoneyPerLap : EffectLogic
{
    [Tooltip("The flat amount of money to grant.")]
    public int baseAmount;
    [Tooltip("The bonus money granted for each completed lap.")]
    public int amountPerLap;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        int laps = targetPlayer.passiveState.lapsCompleted;
        int bonusAmount = laps * amountPerLap;
        int totalGained = baseAmount + bonusAmount;

        targetPlayer.money += totalGained;

        gameManager.GetUIManager().UpdatePlayerMoney(targetPlayer);
        gameManager.GetUIManager().LogDuelMessage(
            $"Investment Matures! Gained ${baseAmount} + ${bonusAmount} bonus for {laps} completed laps!"
        );
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
    }
}