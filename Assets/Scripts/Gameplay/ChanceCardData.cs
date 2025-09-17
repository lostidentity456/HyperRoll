using UnityEngine;

[CreateAssetMenu(fileName = "New Chance Card", menuName = "HyperRoll/Chance Card")]
public class ChanceCardData : ScriptableObject
{
    [Header("Card Info")]
    public string cardTitle;
    [TextArea(3, 5)]
    public string cardDescription;
    public CardCategory category;

    [Header("Card Logic")]
    public EffectLogic effectLogic;
}