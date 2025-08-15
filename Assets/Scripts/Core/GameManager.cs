using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static TileData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // References to Other Objects
    [Header("Game References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject playerPrefab;

    [Header("Available Buildings")]
    [SerializeField] private BuildingData houseData;
    [SerializeField] private BuildingData shopData;

    [Header("Player Visuals")]
    [SerializeField] private Material player1ColorMaterial; // For the player piece
    [SerializeField] private Material player2ColorMaterial; // For the bot piece

    [Header("Building Materials")]
    [SerializeField] private Material player1BuildingMaterial; // For player's buildings
    [SerializeField] private Material player2BuildingMaterial; // For bot's buildings

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
                //StartCoroutine(BotTurnSequence());
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
        SoundManager.Instance.PlaySound(SoundManager.Instance.diceRollSfx);
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
        int duelLoserIndex = (duelWinnerIndex == 0) ? 1 : 0;

        uiManager.LogDuelMessage($"Player {duelWinnerIndex + 1} wins the duel!");
        yield return new WaitForSeconds(turnDelay);

        // --- Start Turns, Winner First ---
        // Player 1 (index 0) is the human. Player 2 (index 1) is the bot.
        if (duelWinnerIndex == 0) // Human player won.
        {
            StartCoroutine(PlayerTurnSequence(duelWinnerIndex, duelLoserIndex));
        }
        else // Bot won.
        {
            StartCoroutine(BotTurnSequence(duelWinnerIndex, duelLoserIndex));
        }
    }

    private IEnumerator PlayerTurnSequence(int playerIndex, int loserIndex)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Player {playerIndex + 1}'s turn...");

        // Starts the MoveAndProcessSequence coroutine.
        ExecuteSinglePlayerTurn(playerIndex);

        // Wait for the processing to be fully complete.
        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete
            || currentState == GameState.TurnEnd);

        // Check whose turn is next (logic remains the same).
        if (playerIndex == duelWinnerIndex)
        {
            if (loserIndex == 0) StartCoroutine(PlayerTurnSequence(loserIndex, playerIndex));
            else StartCoroutine(BotTurnSequence(loserIndex, playerIndex));
        }
        else
        {
            UpdateGameState(GameState.TurnEnd);
        }
    }

    private IEnumerator BotTurnSequence(int playerIndex, int loserIndex)
    {
        UpdateGameState(GameState.PlayerMoving); // Set state for the bot's turn
        uiManager.LogDuelMessage($"Bot's (Player {playerIndex + 1}) turn...");

        // This starts the MoveAndProcessSequence for the bot.
        ExecuteSinglePlayerTurn(playerIndex);

        // Wait for the bot's processing to be fully complete.
        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete || currentState == GameState.TurnEnd);

        // Check whose turn is next
        if (playerIndex == duelWinnerIndex)
        {
            StartCoroutine(PlayerTurnSequence(loserIndex, playerIndex));
        }
        else
        {
            UpdateGameState(GameState.TurnEnd);
        }
    }

    // --- Reusable Action and Helper Methods ---

    private void ExecuteSinglePlayerTurn(int playerIndex)
    {
        PlayerController player = players[playerIndex];
        int spacesToMove = playerDiceSums[playerIndex];

        StartCoroutine(MoveAndProcessSequence(player, spacesToMove));
    }

    private IEnumerator MoveAndProcessSequence(PlayerController player, int spacesToMove)
    {
        yield return StartCoroutine(MovePlayerTileByTile(player, spacesToMove));

        yield return new WaitForSeconds(0.25f);

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
                    boardManager.UpdateTileVisual(landedNode, player2BuildingMaterial);
                    UpdateGameState(GameState.ProcessingComplete);
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
            if (landedNode.initialTileType == TileType.Buildable)
            {
                if (landedNode.owner == null)
                {
                    // Unowned buildable tile: Ask the player to choose.
                    playerMakingChoice = activePlayer;
                    tileForChoice = landedNode;
                    uiManager.ShowBuildPanel();
                    UpdateGameState(GameState.WaitingForPlayerChoice); // Game waits here.
                }
                else
                {
                    // Tile is owned by someone (either bot or player).
                    if (landedNode.owner != activePlayer)
                    {
                        // Pay rent to the bot. This is an instant action.
                        int rent = landedNode.currentBuilding.baseRent;
                        activePlayer.money -= rent;
                        landedNode.owner.money += rent;
                        uiManager.UpdatePlayerMoney(activePlayer);
                        uiManager.UpdatePlayerMoney(landedNode.owner);
                        uiManager.LogDuelMessage($"Player {activePlayer.playerId + 1} paid ${rent} rent to Bot!");
                    }
                    UpdateGameState(GameState.ProcessingComplete);
                }
            }
            else
            {
                uiManager.LogDuelMessage($"Landed on {landedNode.initialTileType}. No action.");
                UpdateGameState(GameState.ProcessingComplete);
            }
        }
    }

    public void PlayerChoseToBuild(string buildingTypeName)
    {
        if (currentState != GameState.WaitingForPlayerChoice) return;

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

            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
            boardManager.UpdateTileVisual(tileForChoice, player1BuildingMaterial);
        }
        else
        {
            uiManager.LogDuelMessage("Not enough money!");
        }

        playerMakingChoice = null;
        tileForChoice = null;

        UpdateGameState(GameState.ProcessingComplete);
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
            if (playerId == 0) { playerRenderer.material = player1ColorMaterial; }
            else { playerRenderer.material = player2ColorMaterial; }
        }

        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        players.Add(playerController);
        playerController.Initialize(playerId);

        if (playerId == 1)
        {
            int halfwayPoint = boardManager.GetPathLength() / 2;
            playerController.currentPathIndex = halfwayPoint;
        }

        Vector3 startPos = boardManager.GetWorldPositionFromPathIndex
            (playerController.currentPathIndex);
        startPos.y += playerController.transform.localScale.y;
        playerController.transform.position = startPos;
        uiManager.UpdatePlayerMoney(playerController);
    }

    private IEnumerator MovePlayerTileByTile(PlayerController player, int spacesToMove)
    {
        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < spacesToMove; i++)
        {
            player.currentPathIndex = (player.currentPathIndex + 1) % boardManager.GetPathLength();

            // Get the world position of the VERY NEXT tile on the path.
            Vector3 nextTilePos = boardManager.GetWorldPositionFromPathIndex(player.currentPathIndex);

            SoundManager.Instance.PlaySound(SoundManager.Instance.pieceMoveSfx);
            // Call our existing smooth movement function to move just ONE tile.
            float stepDuration = 0.15f;
            yield return StartCoroutine(MovePlayerSmoothly(player, nextTilePos, stepDuration));

            // yield return new WaitForSeconds(0.05f);
        }
    }

    private IEnumerator MovePlayerSmoothly(PlayerController player, Vector3 targetPosition, float duration)
    {
        // The piece is slightly elevated so it doesn't clip through the board.
        targetPosition.y += player.transform.localScale.y;

        Vector3 startPosition = player.transform.position;
        float elapsedTime = 0;

        // This loop will run until the piece has reached its destination.
        while (elapsedTime < duration)
        {
            player.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);

            // Add the time since the last frame to our counter.
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        player.transform.position = targetPosition;
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

    private (int winner, bool isSpecialWin) DetermineDuelWinner(DuelChoice p1, DuelChoice p2)
    {
        if (p1.type != p2.type)
        {
            if ((p1.type == RPS_Choice.Rock && p2.type == RPS_Choice.Scissors) ||
                (p1.type == RPS_Choice.Paper && p2.type == RPS_Choice.Rock) ||
                (p1.type == RPS_Choice.Scissors && p2.type == RPS_Choice.Paper))
            {
                return (0, p1.isSpecial); // Returns winner and if it was special
            }
            else
            {
                return (1, p2.isSpecial);
            }
        }
        else
        {
            if (p1.isSpecial && !p2.isSpecial) { return (0, true); }
            else if (!p1.isSpecial && p2.isSpecial) { return (1, true); }
            else { return (-1, false); } // Tie
        }
    }
}