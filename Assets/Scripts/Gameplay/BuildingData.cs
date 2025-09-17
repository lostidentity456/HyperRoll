using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "HyperRoll/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    public int buildingCost;

    [Header("Economics")]
    [Tooltip("The base income rate at Level 1 (e.g., 0.05 for 5%)")]
    public float baseIncomeRate;

    [Header("Upgrade Progression")]
    [Tooltip("Cost to upgrade TO this level. Element 0 is cost to upgrade TO Lvl 2, Element 1 to Lvl 3, etc.")]
    public List<int> upgradeCosts;

    [Header("Tax Progression")]
    [Tooltip("The list of tax rates for each level. Element 0 is for Level 1, Element 1 for Level 2, etc.")]
    public List<float> taxRatesPerLevel;

    public float GetTaxRateForLevel(int level)
    {
        // level is 1-based, but list is 0-based
        int index = level - 1;

        // If the level is higher than the defined rates, use the highest defined rate
        if (index >= taxRatesPerLevel.Count)
        {
            if (taxRatesPerLevel.Any())
            {
                return taxRatesPerLevel.Last();
            }
            else
            {
                return 0f;
            }
        }

        // Safety check for negative indices
        if (index < 0)
        {
            return 0f;
        }

        return taxRatesPerLevel[index];
    }
}