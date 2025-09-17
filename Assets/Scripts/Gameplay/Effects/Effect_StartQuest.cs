using UnityEngine;
[CreateAssetMenu(fileName = "Effect_StartQuest", menuName = "HyperRoll/Effects/Start Quest")]
public class Effect_StartQuest : EffectLogic
{
    public QuestType questToStart;
    public int questTargetValue;
    public RewardData questReward; // Drag a Reward asset here (e.g., "Reward_GrantBlessingOfOne")

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.StartQuest(this);
    }
}