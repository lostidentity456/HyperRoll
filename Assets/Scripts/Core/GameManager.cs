using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static TileData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // References to Other Objects
    [Header("Game References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject playerPrefab;

    // Game Data Assets
    [Header("Game Data")]
    [SerializeField] private BuildingData houseData;
    [SerializeField] private BuildingData shopData;
    [SerializeField] private Material player1Material;
    [SerializeField] private Material player2Material;

    // --- Game Settings ---
    [Header("Game Settings")]
    [SerializeField] private float turnDelay = 1.5f; // Pause between actions

    // --- Internal State Variables ---
    private List<PlayerController> players = new List<PlayerController>();
    private GameState currentState;
    private int turnNumber = 1;

    // Variables to temporarily store the results of a duel for the turn.
    private int[] playerDiceSums = new int[2];
    private DuelChoice[] duelChoices = new DuelChoice[2];
    private int duelWinnerIndex;
    private int duelLoserIndex;

    // Variables to store who is making a choice and for what tile.
    private PlayerController playerMakingChoice;
    private TileNode tileForChoice;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            { Instance = this; }
        }
    }

    void Start()
    {
        SpawnPlayers();
        UpdateGameState(GameState.WaitingToStart);
    }

    // --- State Machine Controller ---

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("GAME STATE CHANGED TO: " + newState);

        switch (currentState)
        {
            case GameState.WaitingToStart:
                uiManager.SetRollButtonInteractable(true);
                uiManager.LogDuelMessage("Press 'Roll' to start the duel!");
                break;

            case GameState.DuelRolling:
                // Start the DuelSequence coroutine.
                uiManager.SetRollButtonInteractable(false);
                StartCoroutine(DuelSequence());
                break;

            case GameState.BotTurn:
                // Start the Bot's turn sequence.
                StartCoroutine(BotTurnSequence());
                break;

            case GameState.TurnEnd:
                // After everything is done, increment the turn counter and loop back
                turnNumber++;
                UpdateGameState(GameState.WaitingToStart);
                break;

            // These states are "in-between" states managed by the coroutines.
            // We don't need to do anything special when we enter them.
            case GameState.PlayerMoving:
            case GameState.ProcessingTile:
            case GameState.WaitingForPlayerChoice:
                break;
        }
    }

    // This is called by the "Roll" button's OnClick event.
    public void OnRollButtonPressed()
    {
        if (currentState == GameState.WaitingToStart)
        {
            UpdateGameState(GameState.DuelRolling);
        }
    }

    // --- Coroutines for Timed Game Flow ---

    private IEnumerator DuelSequence()
    {
        // --- Roll Dice ---
        uiManager.LogDuelMessage($"[Turn {turnNumber}] Dueling...");
        for (int i = 0; i < players.Count; i++)
        {
            int dice1 = Random.Range(1, 7);
            int dice2 = Random.Range(1, 7);
            playerDiceSums[i] = dice1 + dice2;
            duelChoices[i] = GetDuelChoiceFromRoll(dice1, dice2);
        }
        uiManager.ShowDuelResults
            (duelChoices[0], duelChoices[1], playerDiceSums[0], playerDiceSums[1]);

        yield return new WaitForSeconds(turnDelay); // Pause to let player see the results.

        // --- Determine Winner ---
        var duelResult = DetermineDuelWinner(duelChoices[0], duelChoices[1]);
        if (duelResult.winner == -1) // TIE
        {
            uiManager.LogDuelMessage("DUEL IS A TIE! Rerolling...");
            yield return new WaitForSeconds(turnDelay);
            UpdateGameState(GameState.DuelRolling); // Start the duel state over again.
            yield break; // Stop this coroutine because a new one is starting.
        }

        duelWinnerIndex = duelResult.winner;
        duelLoserIndex = duelResult.loser;

        uiManager.LogDuelMessage($"Player {duelWinnerIndex + 1} wins the duel!");
        yield return new WaitForSeconds(turnDelay);

        // --- Start Turns, Winner First ---
        // Player 1 (index 0) is the human. Player 2 (index 1) is the bot.
        if (duelWinnerIndex == 0) // Human player won.
        {
            StartCoroutine(PlayerTurnSequence(duelWinnerIndex));
        }
        else // Bot won.
        {
            UpdateGameState(GameState.BotTurn);
        }
    }

    private IEnumerator PlayerTurnSequence(int playerIndex)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Player {playerIndex + 1}'s turn...");
        yield return new WaitForSeconds(turnDelay);

        // This helper method does the actual moving and calls ProcessLandedTile.
        ExecuteSinglePlayerTurn(playerIndex);

        if (currentState == GameState.WaitingForPlayerChoice)
        {
            yield return new WaitUntil(() => currentState == GameState.PlayerChoiceComplete);
        }

        yield return new WaitForSeconds(turnDelay / 2); // A small extra pause after building.

        // Check whose turn is next.
        if (playerIndex == duelWinnerIndex) // The winner just finished their turn.
        {
            // Now it's the loser's turn.
            if (duelLoserIndex == 0) // Is the loser human?
            {
                StartCoroutine(PlayerTurnSequence(duelLoserIndex));
            }
            else // The loser is the bot.
            {
                UpdateGameState(GameState.BotTurn);
            }
        }
        else // The loser just finished their turn.
        {
            // The whole round is over.
            UpdateGameState(GameState.TurnEnd);
        }
    }

    private IEnumerator BotTurnSequence()
    {
        uiManager.LogDuelMessage("Bot's turn...");
        yield return new WaitForSeconds(turnDelay);

        // --- Bot Movement ---
        ExecuteSinglePlayerTurn(1); // The bot is always player index 1.
        yield return new WaitForSeconds(turnDelay);

        // After moving, check if the human's turn is next.
        if (duelWinnerIndex == 1) // Bot was the winner.
        {
            // Now it's the human loser's turn.
            StartCoroutine(PlayerTurnSequence(duelLoserIndex));
        }
        else // Human was the winner, bot was the loser.
        {
            // The whole round is over.
            UpdateGameState(GameState.TurnEnd);
        }
    }

    // --- Reusable Action and Helper Methods ---

    private void ExecuteSinglePlayerTurn(int playerIndex)
    {
        PlayerController player = players[playerIndex];
        int movement = playerDiceSums[playerIndex];

        player.currentPathIndex = (player.currentPathIndex + movement) % boardManager.GetPathLength();
        MovePlayerToTile(player);

        UpdateGameState(GameState.ProcessingTile);
        ProcessLandedTile(player);
    }

    private void ProcessLandedTile(PlayerController activePlayer)
    {
        TileNode landedNode = boardManager.GetNodeFromPathIndex(activePlayer.currentPathIndex);

        // --- Bot's Logic ---
        if (activePlayer.playerId == 1) // If it's the bot
        {
            if (landedNode.initialTileType == TileType.Buildable && landedNode.owner == null)
            {
                // Simple Bot AI: Always build a House if it can afford it.
                if (activePlayer.money >= houseData.buildingCost)
                {
                    activePlayer.money -= houseData.buildingCost;
                    landedNode.owner = activePlayer;
                    landedNode.currentBuilding = houseData;
                    uiManager.UpdatePlayerMoney(activePlayer);
                    uiManager.LogDuelMessage($"Bot built a {houseData.buildingName}!");
                    // TODO: Change tile color
                }
            }
            else if (landedNode.owner != null && landedNode.owner != activePlayer)
            {
                // Rent logic for when the bot lands on a player's property.
                int rent = landedNode.currentBuilding.baseRent;
                activePlayer.money -= rent;
                landedNode.owner.money += rent;
                uiManager.UpdatePlayerMoney(activePlayer);
                uiManager.UpdatePlayerMoney(landedNode.owner);
                uiManager.LogDuelMessage($"Bot paid ${rent} rent to Player {landedNode.owner.playerId + 1}!");
            }
        }
        // --- Human Player's Logic ---
        else
        {
            if (landedNode.initialTileType == TileType.Buildable && landedNode.owner == null)
            {
                // The tile is unowned. PAUSE the game and ASK the player.
                playerMakingChoice = activePlayer;
                tileForChoice = landedNode;

                uiManager.ShowBuildPanel();
                UpdateGameState(GameState.WaitingForPlayerChoice); // Game waits here.
            }
            else if (landedNode.owner != null && landedNode.owner != activePlayer)
            {
                // Rent logic for when the player lands on the bot's property.
                int rent = landedNode.currentBuilding.baseRent;
                activePlayer.money -= rent;
                landedNode.owner.money += rent;
                uiManager.UpdatePlayerMoney(activePlayer);
                uiManager.UpdatePlayerMoney(landedNode.owner);
                uiManager.LogDuelMessage($"Player {activePlayer.playerId + 1} paid ${rent} rent to Bot!");
            }
        }
    }

    // This method is called by the UIManager when a build button is clicked.
    public void PlayerChoseToBuild(string buildingTypeName)
    {
        if (currentState != GameState.WaitingForPlayerChoice) return; // Safety check

        BuildingData selectedBuilding = null;
        if (buildingTypeName == "House") selectedBuilding = houseData;
        if (buildingTypeName == "Shop") selectedBuilding = shopData;

        if (playerMakingChoice.money >= selectedBuilding.buildingCost)
        {
            playerMakingChoice.money -= selectedBuilding.buildingCost;
            tileForChoice.owner = playerMakingChoice;
            tileForChoice.currentBuilding = selectedBuilding;

            uiManager.UpdatePlayerMoney(playerMakingChoice);
            uiManager.LogDuelMessage($"Player {playerMakingChoice.playerId + 1} built a {selectedBuilding.buildingName}!");

            // TODO: Change the tile's color using selectedBuilding.buildingMaterial
        }
        else
        {
            uiManager.LogDuelMessage("Not enough money!");
        }

        playerMakingChoice = null;
        tileForChoice = null;

        UpdateGameState(GameState.PlayerChoiceComplete);
    }

    // --- Spawning and Initialization ---

    private void SpawnPlayers()
    {
        SpawnPlayer(0); // Player 1 (Human)
        SpawnPlayer(1); // Player 2 (Bot)
    }

    private void SpawnPlayer(int playerId)
    {
        GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerObject.name = (playerId == 0) ? "Player 1" : "Bot";

        Renderer playerRenderer = playerObject.GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
        {
            if (playerId == 0) { playerRenderer.material = player1Material; }
            else { playerRenderer.material = player2Material; }
        }

        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        players.Add(playerController);
        playerController.Initialize(playerId);

        if (playerId == 1)
        {
            int halfwayPoint = boardManager.GetPathLength() / 2;
            playerController.currentPathIndex = halfwayPoint;
        }

        MovePlayerToTile(playerController);
        uiManager.UpdatePlayerMoney(playerController);
    }

    private void MovePlayerToTile(PlayerController player)
    {
        Vector3 newPos = boardManager.GetWorldPositionFromPathIndex(player.currentPathIndex);
        newPos.y += player.transform.localScale.y;
        player.transform.position = newPos;
    }

    // --- Duel Logic Helpers ---

    private DuelChoice GetDuelChoiceFromRoll(int dice1, int dice2)
    {
        int diceSum = dice1 + dice2;
        bool isDouble = (dice1 == dice2);
        int remainder = diceSum % 3;
        RPS_Choice choiceType = (RPS_Choice)remainder;
        return new DuelChoice(choiceType, isDouble);
    }

    private (int winner, int loser, bool isSpecialWin) DetermineDuelWinner(DuelChoice p1, DuelChoice p2)
    {
        if (p1.type != p2.type)
        {
            if ((p1.type == RPS_Choice.Rock && p2.type == RPS_Choice.Scissors) ||
                (p1.type == RPS_Choice.Paper && p2.type == RPS_Choice.Rock) ||
                (p1.type == RPS_Choice.Scissors && p2.type == RPS_Choice.Paper))
            {
                return (0, 1, p1.isSpecial);
            }
            else
            {
                return (1, 0, p2.isSpecial);
            }
        }
        else
        {
            if (p1.isSpecial && !p2.isSpecial) { return (0, 1, true); }
            else if (!p1.isSpecial && p2.isSpecial) { return (1, 0, true); }
            else { return (-1, -1, false); }
        }
    }
}