using UnityEngine;

[CreateAssetMenu(fileName = "New Building", menuName = "HyperRoll/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName;
    public int buildingCost;
    public int baseRent;

    [Header("Visuals")]
    public Material buildingMaterial;
    // public GameObject buildingPrefab; 
}