using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- Core Data ---
    public int playerId;
    public string playerName; 
    public int currentPathIndex;
    public int money;
    public CharacterData characterData;

    public CharacterState passiveState;

    // --- Initialization ---
    public void Initialize(int id, CharacterData charData)
    {
        this.playerId = id;
        this.characterData = charData;
        this.playerName = (id == 0) ? "Player" : "Bot"; // Default name
        if (charData != null)
        {
            this.playerName = charData.characterName;
        }

        // The Major gets more starting money
        this.money = (charData?.passiveAbility == CharacterPassive.TheMajor) ? 25000 : 10000;

        this.currentPathIndex = 0;
        this.passiveState = new CharacterState();
    }
}