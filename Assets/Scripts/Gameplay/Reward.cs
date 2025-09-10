using UnityEngine;

public enum RewardType { GainMoney, FreeBuilding, TaxImmunity } // Add more types later

[CreateAssetMenu(fileName = "New Reward", menuName = "HyperRoll/Reward")]
public class RewardData : ScriptableObject
{
    public string rewardName;
    public RewardType type;
    public int amount; // e.g., amount of money
    // public BuildingData buildingToGrant; // For the FreeBuilding type
}