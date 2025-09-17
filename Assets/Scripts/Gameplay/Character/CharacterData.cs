using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "HyperRoll/Character Data")]
public class CharacterData : ScriptableObject
{
    // Character properties
    public string characterName;               // "The Economist"
    public string characterDescription;       // "Gains 100% more passive income."
    public CharacterPassive passiveAbility;   // Defined in an enum, "TheEconomist" in this case
}