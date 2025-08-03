using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public int playerId; // Is this Player 1 or Player 2?
    public int currentPathIndex; // Which tile on the path are we on? (e.g., tile 0, 1, 2...)
    public int money; // How much money the player has

    public void Initialize(int id)
    {
        this.playerId = id;
        this.money = 1500; // Starting money, can be changed later

        // Based on player ID, set their starting position
        // Player 1 (ID 0) starts at tile 0.
        // Player 2 (ID 1) starts at the halfway point.
        // We'll calculate the exact index in the GameManager.
        this.currentPathIndex = 0;
    }
}