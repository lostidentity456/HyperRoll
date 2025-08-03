using System.Collections.Generic;
using UnityEngine;
using static TileData;

public class GameManager : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private UnityEngine.UI.Button rollDiceButton;

    [Header("Game State")]
    private List<PlayerController> players = new List<PlayerController>();
    private int currentPlayerIndex = 0;

    void Start()
    {
        SpawnPlayers();
        rollDiceButton.onClick.AddListener(TakeTurn);
    }

    private void SpawnPlayers()
    {
        // Player 1
        SpawnPlayer(0);

        // Player 2
        SpawnPlayer(1);
    }

    private void SpawnPlayer(int playerId)
    {
        GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerObject.name = $"Player {playerId + 1}";

        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        players.Add(playerController);

        playerController.Initialize(playerId);

        // --- Set Starting Position ---
        if (playerId == 1)
        {
            // Player 2 starts halfway around the board
            int halfwayPoint = boardManager.GetPathLength() / 2;
            playerController.currentPathIndex = halfwayPoint;
        }

        // Move the player to their starting tile on the board
        MovePlayerToTile(playerController);
    }

    // A helper method to physically move a player piece
    private void MovePlayerToTile(PlayerController player)
    {
        Vector3 newPos = boardManager.GetWorldPositionFromPathIndex(player.currentPathIndex);

        // Add a small height offset so the piece sits ON TOP of the tile
        newPos.y += player.transform.localScale.y;

        player.transform.position = newPos;
    }

    public void TakeTurn()
    {
        // Figure out who is the current player
        PlayerController activePlayer = players[currentPlayerIndex];

        // Roll the dice
        int diceRoll = Random.Range(1, 7); // Generates a number from 1 to 6
        Debug.Log($"Player {activePlayer.playerId + 1} rolled a {diceRoll}");

        // Update the player's data
        // Makes the path loop back to the start
        activePlayer.currentPathIndex =
            (activePlayer.currentPathIndex + diceRoll) % boardManager.GetPathLength();

        // Move the player's visual piece
        MovePlayerToTile(activePlayer);

        // --- After moving, what happens? ---
        ProcessLandedTile(activePlayer);

        // Switch to the next player for the next turn
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
    }

    private void ProcessLandedTile(PlayerController activePlayer)
    {
        // First, get the TileNode the player landed on from the BoardManager
        TileNode landedNode = boardManager.GetNodeFromPathIndex(activePlayer.currentPathIndex);

        Debug.Log($"Player {activePlayer.playerId + 1} landed on a tile of type: {landedNode.initialTileType}");

        // Use a switch statement to handle the different tile types
        switch (landedNode.initialTileType)
        {
            case TileType.Buildable:
                ProcessBuildableTile(activePlayer, landedNode);
                break;

            case TileType.Go:
                // For now, just a message. Later, this could give bonus money.
                Debug.Log("Landed on GO!");
                break;

            case TileType.Chance:
                // We will add Chance card logic here in a future phase.
                Debug.Log("Landed on CHANCE!");
                break;
        }
    }

    // Add this new method to GameManager.cs

    private void ProcessBuildableTile(PlayerController activePlayer, TileNode buildableNode)
    {
        // --- Scenario 1: The tile is unowned ---
        if (buildableNode.owner == null)
        {
            int buildingCost = 100; // Let's set a simple cost for now

            // Does the player have enough money to build?
            if (activePlayer.money >= buildingCost)
            {
                // Yes! Deduct the cost, set the owner, and set the building level
                activePlayer.money -= buildingCost;
                buildableNode.owner = activePlayer;
                buildableNode.buildingLevel = 1;

                Debug.Log($"Player {activePlayer.playerId + 1} BUILT a level {buildableNode.buildingLevel} property! They have {activePlayer.money} money left.");

                // TODO: Change the color of the tile to the player's color
            }
            else
            {
                // Not enough money
                Debug.Log($"Player {activePlayer.playerId + 1} can't afford to build. (Needs {buildingCost}, has {activePlayer.money})");
            }
        }
        // --- Scenario 2: The tile is already owned ---
        else
        {
            // Is it owned by the player who just landed on it?
            if (buildableNode.owner == activePlayer)
            {
                // Landed on your own property. Maybe upgrade it in the future?
                Debug.Log($"Player {activePlayer.playerId + 1} landed on their own property.");
            }
            // It's owned by an opponent!
            else
            {
                int rent = 50 * buildableNode.buildingLevel; // Calculate rent

                // Pay rent to the owner
                activePlayer.money -= rent;
                buildableNode.owner.money += rent;

                Debug.Log($"Player {activePlayer.playerId + 1} paid ${rent} rent to Player {buildableNode.owner.playerId + 1}!");
                Debug.Log($"Player {activePlayer.playerId + 1} now has {activePlayer.money}. Player {buildableNode.owner.playerId + 1} now has {buildableNode.owner.money}.");
            }
        }
    } 
}