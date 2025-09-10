using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Treasure Threshold", menuName = "HyperRoll/Treasure Threshold")]
public class TreasureThreshold : ScriptableObject
{
    [Tooltip("The number of treasure 'value' needed to reach this tier.")]
    public int valueRequired;

    [Tooltip("The amount of money gained per treasure value at this tier.")]
    public int moneyPerValue;

    [Tooltip("The pool of additional rewards unlocked at this tier.")]
    public List<RewardData> rewardPool;
}