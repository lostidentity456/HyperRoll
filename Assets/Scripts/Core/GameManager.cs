using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TileData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Button rollDiceButton;

    [Header("Game State")]
    private List<PlayerController> players = new List<PlayerController>();
    private int turnNumber = 1;
    private PlayerController playerMakingChoice;
    private TileNode tileForChoice;

    [Header("Game Data")]
    [SerializeField] private BuildingData houseData;
    [SerializeField] private BuildingData shopData;

    [Header("Player Visuals")]
    [SerializeField] private Material player1Material;
    [SerializeField] private Material player2Material;

    // Store the results of each player's roll during a duel
    private int[] playerDiceSums = new int[2];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        SpawnPlayers();
        // Starts the duel
        rollDiceButton.onClick.AddListener(StartDiceDuel);
        UIManager.Instance.LogDuelMessage("");
    }

    public void StartDiceDuel()
    {
        // These will store the dice sums that actually result in a win.
        int[] winningDiceSums = new int[2];

        var duelResult = (winner: -1, loser: -1, isSpecialWin: false);

        while (duelResult.winner == -1)
        {
            DuelChoice[] duelChoices = new DuelChoice[2];
            // This is a temporary array just for this loop iteration
            int[] currentRollSums = new int[2];

            for (int i = 0; i < players.Count; i++)
            {
                int dice1 = Random.Range(1, 7);
                int dice2 = Random.Range(1, 7);
                currentRollSums[i] = dice1 + dice2; // Store in temporary array
                duelChoices[i] = GetDuelChoiceFromRoll(dice1, dice2);
            }

            duelResult = DetermineDuelWinner(duelChoices[0], duelChoices[1]);

            if (duelResult.winner != -1)
            {
                // A winner was found! This is the roll that counts.
                // Copy the temporary sums to our "official" winning sums array.
                winningDiceSums = currentRollSums;
            }
            else
            {
                UIManager.Instance.LogDuelMessage("DUEL IS A TIE! Automatically re-rolling...");
            }
        }

        // Now we use the winningDiceSums for movement
        playerDiceSums = winningDiceSums;

        string winTypeText = duelResult.isSpecialWin ? " with a SPECIAL WIN!" : " with a normal win.";
        UIManager.Instance.LogDuelMessage($"[Turn {turnNumber}] Player {duelResult.winner + 1} wins the duel" + winTypeText);

        ExecuteTurnsInOrder(duelResult.winner, duelResult.loser);
    }

    private void ExecuteTurnsInOrder(int firstPlayerIndex, int secondPlayerIndex)
    {
        Debug.Log($"--- Player {firstPlayerIndex + 1}'s Turn ---");
        ExecuteSinglePlayerTurn(firstPlayerIndex);

        Debug.Log($"--- Player {secondPlayerIndex + 1}'s Turn ---");
        ExecuteSinglePlayerTurn(secondPlayerIndex);

        Debug.Log("--- End of Turn ---");
        turnNumber++;
    }

    private void ExecuteSinglePlayerTurn(int playerIndex)
    {
        PlayerController activePlayer = players[playerIndex];
        int totalMovement = playerDiceSums[playerIndex]; // Each player moves by their own dice roll

        activePlayer.currentPathIndex = (activePlayer.currentPathIndex + totalMovement) % boardManager.GetPathLength();
        MovePlayerToTile(activePlayer);
        ProcessLandedTile(activePlayer);
    }

    // --- Helper Methods ---

    private RPS_Choice GetRpsFromDiceSum(int diceSum)
    {
        // Custom rule for Rock, Paper and Scissors
        // 0 = Rock, 1 = Paper, 2 = Scissors
        int remainder = diceSum % 3;
        return (RPS_Choice)remainder;
    }

    private DuelChoice GetDuelChoiceFromRoll(int dice1, int dice2)
    {
        int diceSum = dice1 + dice2;
        bool isDouble = (dice1 == dice2);

        // Same rule as before: Remainder when dividing by 3.
        int remainder = diceSum % 3;
        RPS_Choice choiceType = (RPS_Choice)remainder;

        // Create and return our new, more descriptive DuelChoice
        return new DuelChoice(choiceType, isDouble);
    }

    private (int winner, int loser, bool isSpecialWin) DetermineDuelWinner(DuelChoice p1, DuelChoice p2)
    {
        // --- Case 1: The RPS types are different (e.g. Rock vs Paper) ---
        if (p1.type != p2.type)
        {
            if ((p1.type == RPS_Choice.Rock && p2.type == RPS_Choice.Scissors) ||
                (p1.type == RPS_Choice.Paper && p2.type == RPS_Choice.Rock) ||
                (p1.type == RPS_Choice.Scissors && p2.type == RPS_Choice.Paper))
            {
                // Player 1 wins. Is it a special win? Only if P1's choice was special.
                return (0, 1, p1.isSpecial); // (winner, loser, isSpecialWin)
            }
            else
            {
                // Player 2 wins. Is it a special win? Only if P2's choice was special.
                return (1, 0, p2.isSpecial); // (winner, loser, isSpecialWin)
            }
        }
        // --- Case 2: The RPS types are the same (e.g. Rock vs Rock) ---
        else
        {
            // Now we check the "special" status as a tie-breaker
            if (p1.isSpecial && !p2.isSpecial)
            {
                // Player 1's special type beats Player 2's normal type
                return (0, 1, true); // It's inherently a special win
            }
            else if (!p1.isSpecial && p2.isSpecial)
            {
                // Player 2's special type beats Player 1's normal type
                return (1, 0, true);
            }
            else
            {
                // Both are special or both are normal. It's a true tie.
                return (-1, -1, false); // No winner, no loser
            }
        }
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

        Renderer playerRenderer = playerObject.GetComponentInChildren<Renderer>();

        if (playerRenderer != null)
        {
            // Based on the player's ID, assign the correct material
            if (playerId == 0) // Player 1
            {
                playerRenderer.material = player1Material;
            }
            else if (playerId == 1) // Player 2
            {
                playerRenderer.material = player2Material;
            }
        }

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
        UIManager.Instance.UpdatePlayerMoney(playerController);
    }

    // A helper method to physically move a player piece
    private void MovePlayerToTile(PlayerController player)
    {
        Vector3 newPos = boardManager.GetWorldPositionFromPathIndex(player.currentPathIndex);

        // Add a small height offset so the piece sits ON TOP of the tile
        newPos.y += player.transform.localScale.y;

        player.transform.position = newPos;
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
        if (buildableNode.owner == null)
        {
            // The tile is unowned. PAUSE the game flow and ASK the player.
            playerMakingChoice = activePlayer; // Store who is choosing
            tileForChoice = buildableNode;      // Store what tile they are on

            // Tell the UI Manager to show the question panel
            UIManager.Instance.ShowBuildPanel();
            // The game now waits for the player to click a button...
        }
        else
        {
            // The tile is owned, so rent logic proceeds as normal.
            if (buildableNode.owner != activePlayer)
            {
                int rent = buildableNode.currentBuilding.baseRent;
                activePlayer.money -= rent;
                buildableNode.owner.money += rent;
                UIManager.Instance.UpdatePlayerMoney(activePlayer);
                UIManager.Instance.UpdatePlayerMoney(buildableNode.owner);
                UIManager.Instance.LogDuelMessage($"Player {activePlayer.playerId + 1} paid ${rent} rent to Player {buildableNode.owner.playerId + 1}!");
            }
        }
    }

    public void PlayerChoseToBuild(string buildingTypeName)
    {
        BuildingData selectedBuilding = null;
        if (buildingTypeName == "House") selectedBuilding = houseData;
        if (buildingTypeName == "Shop") selectedBuilding = shopData;

        if (playerMakingChoice.money >= selectedBuilding.buildingCost)
        {
            playerMakingChoice.money -= selectedBuilding.buildingCost;
            tileForChoice.owner = playerMakingChoice;
            tileForChoice.currentBuilding = selectedBuilding;

            UIManager.Instance.UpdatePlayerMoney(playerMakingChoice);
            UIManager.Instance.LogDuelMessage($"Player {playerMakingChoice.playerId + 1} built a {selectedBuilding.buildingName}!");

            // TODO: Change the tile's color using selectedBuilding.buildingMaterial
        }
        else
        {
            UIManager.Instance.LogDuelMessage("Not enough money!");
        }

        // Clear the choice state
        playerMakingChoice = null;
        tileForChoice = null;
    }
}