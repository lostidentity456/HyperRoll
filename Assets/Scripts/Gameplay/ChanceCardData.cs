using UnityEngine;

[CreateAssetMenu(fileName = "New Chance Card", menuName = "HyperRoll/Chance Card")]
public class ChanceCardData : ScriptableObject
{
    [Header("Card Info")]
    public string cardTitle;
    [TextArea(3, 5)] // This makes the description box in the Inspector bigger
    public string cardDescription;

    [Header("Card Logic")]
    public ChanceCardEffect effect;

    // --- Parameters for the effects ---
    // Not all cards will use all parameters, but we include them here for flexibility.

    [Tooltip("Used for GainMoneyFlat and as the base amount for GainMoneyPerBuilding.")]
    public int moneyAmount;

    [Tooltip("Used for GainMoneyPerBuilding")]
    public int moneyPerBuilding;

    [Tooltip("Used for BuildRandomFreeBuilding")]
    public BuildingData buildingToGrant; // e.g., A free House

}