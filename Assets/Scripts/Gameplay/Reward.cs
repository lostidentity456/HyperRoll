using UnityEngine;

public enum RewardType { 
    FreeBuilding,                // int1 = Quantity
    FreeMaxxedBuilding,      // int1 = Quantity
    PassiveIncome,             // int1 = Multiplier
    IncreasedSpecialChance, // int1 = Rate
    GoodChanceCard,          // int1 = Quantity
    RandomBlessing,           // int1 = Quantity
    BuildingBonus,              // int1 = Quantity
    TaxImmunity,                // int1 = amount of turns
    TaxMultiplier,                // int1 = added multiplier
    SpecialRolls,                  // int1 = amount of turns
} // Add more types later

[CreateAssetMenu(fileName = "New Reward", menuName = "HyperRoll/Reward")]
public class RewardData : ScriptableObject
{
    public string rewardName;
    public RewardType type;
    public int amount;
}