using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "HyperRoll/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    public int buildingCost;
    public int baseRent;
    
    // We can add more properties later, like special abilities or upgrade costs!
}