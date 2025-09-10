using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- Core Data ---
    public int playerId;
    public string playerName; 
    public int currentPathIndex;
    public int money;
    public CharacterData characterData;

    // --- Status Effect Flags (from Chance Cards) ---
    public bool hasGuaranteedWin = false;
    public bool hasFreeBuildOnRandomWin = false;
    public bool hasTaxImmunity = false;

    public CharacterState passiveState;

    // --- Initialization ---
    public void Initialize(int id, CharacterData charData)
    {
        this.playerId = id;
        this.characterData = charData;
        this.playerName = (id == 0) ? "Player 1" : "Bot"; // Default name
        if (charData != null)
        {
            this.playerName = charData.characterName;
        }

        // The Major gets more starting money
        this.money = (charData?.passiveAbility == CharacterPassive.TheMajor) ? 3000 : 1500;

        this.currentPathIndex = 0;
        this.passiveState = new CharacterState();
    }
}