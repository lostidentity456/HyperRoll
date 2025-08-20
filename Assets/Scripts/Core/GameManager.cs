using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static TileData;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // References
    [Header("Game References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject playerPrefab;

    [Header("Game Data")]
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
    [SerializeField][Range(0, 1)] private float specialRollChance = 1f / 6f;

    [Header("Chance Cards")]
    [SerializeField] private List<ChanceCardData> chanceCardDeck;
    private PlayerController playerDrawingCard;
    private ChanceCardData currentlyDrawnCard;

    // --- State Variables ---
    private List<PlayerController> players = new List<PlayerController>();
    private GameState currentState;
    private int turnNumber = 1;
    private int duelWinnerIndex;

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
            Instance = this;
        }
    }

    void Start()
    {
        SpawnPlayers();
        UpdateGameState(GameState.WaitingForPlayerTurnStart);
    }

    // --- State Machine ---

    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("GAME STATE CHANGED TO: " + newState);

        switch (currentState)
        {
            case GameState.WaitingForPlayerTurnStart:
                uiManager.ShowRpsChoicePanel();
                uiManager.LogDuelMessage($"[Turn {turnNumber}] Choose your move!");
                break;

            case GameState.DuelRolling:
                uiManager.HideRpsChoicePanel();
                break;

            case GameState.TurnEnd:
                turnNumber++;
                UpdateGameState(GameState.WaitingForPlayerTurnStart);
                break;
        }
    }
    // --- Player Actions ---
    public void OnPlayerChoseAttack(RPS_Choice playerChoice)
    {
        if (currentState != GameState.WaitingForPlayerTurnStart) return;
        UpdateGameState(GameState.DuelRolling);

        // Bot makes a random choice
        RPS_Choice botChoice = (RPS_Choice)Random.Range(0, 3);

        StartCoroutine(ControlledDuelSequence(playerChoice, botChoice));
    }

    public void PlayerChoseRock()
    {
        OnPlayerChoseAttack(RPS_Choice.Rock);
    }

    public void PlayerChosePaper()
    {
        OnPlayerChoseAttack(RPS_Choice.Paper);
    }

    public void PlayerChoseScissors()
    {
        OnPlayerChoseAttack(RPS_Choice.Scissors);
    }

    public void OnPlayerChoseRandom()
    {
        if (currentState != GameState.WaitingForPlayerTurnStart) return;
        UpdateGameState(GameState.DuelRolling);

        StartCoroutine(RandomDuelSequence());
    }

    // --- Coroutines for Timed Game Flow ---

    private IEnumerator RandomDuelSequence()
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.diceRollSfx);
        // --- Roll Dice ---
        uiManager.LogDuelMessage($"[Turn {turnNumber}] Dueling...");
        int[] playerDiceSums = new int[2];
        DuelChoice[] duelChoices = new DuelChoice[2];

        for (int i = 0; i < players.Count; i++)
        {
            int dice1 = Random.Range(1, 7);
            int dice2 = Random.Range(1, 7);
            playerDiceSums[i] = dice1 + dice2;
            duelChoices[i] = GetDuelChoiceFromRoll(dice1, dice2);
        }
        yield return StartCoroutine(RunDuelAndTurns(duelChoices, playerDiceSums));
    }

    private IEnumerator ControlledDuelSequence(RPS_Choice playerChoice, RPS_Choice botChoice)
    {
        // This is the new sequence for when the player makes a specific choice
        int[] playerDiceSums = new int[2];
        DuelChoice[] duelChoices = new DuelChoice[2];

        // Roll dice that match the player's choice
        var playerRoll = RollDiceForOutcome(playerChoice);
        playerDiceSums[0] = playerRoll.dice1 + playerRoll.dice2;
        duelChoices[0] = new DuelChoice(playerChoice, playerRoll.isSpecial);

        // Roll dice that match the bot's choice
        var botRoll = RollDiceForOutcome(botChoice);
        playerDiceSums[1] = botRoll.dice1 + botRoll.dice2;
        duelChoices[1] = new DuelChoice(botChoice, botRoll.isSpecial);

        yield return StartCoroutine(RunDuelAndTurns(duelChoices, playerDiceSums));
    }

    private IEnumerator RunDuelAndTurns(DuelChoice[] duelChoices, int[] playerDiceSums)
    {
        uiManager.ShowDuelResults(duelChoices[0], duelChoices[1], playerDiceSums[0], playerDiceSums[1]);
        yield return new WaitForSeconds(turnDelay);

        var duelResult = DetermineDuelWinner(duelChoices[0], duelChoices[1]);
        if (duelResult.winner == -1) // TIE
        {
            uiManager.LogDuelMessage("DUEL IS A TIE! Starting a new turn...");
            yield return new WaitForSeconds(turnDelay);
            UpdateGameState(GameState.TurnEnd);
            yield break;
        }

        duelWinnerIndex = duelResult.winner;
        int duelLoserIndex = (duelWinnerIndex == 0) ? 1 : 0;

        uiManager.LogDuelMessage($"Player {duelWinnerIndex + 1} wins the duel!");
        yield return new WaitForSeconds(turnDelay);

        // Execute turns, winner first
        if (duelWinnerIndex == 0) // Human is first
        {
            yield return StartCoroutine(PlayerTurnSequence(duelWinnerIndex, duelLoserIndex, playerDiceSums));
        }
        else // Bot is first
        {
            yield return StartCoroutine(BotTurnSequence(duelWinnerIndex, duelLoserIndex, playerDiceSums));
        }
    }

    private IEnumerator PlayerTurnSequence(int playerIndex, int loserIndex, int[] playerDiceSums)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Player {playerIndex + 1}'s turn...");

        yield return StartCoroutine(ExecuteSinglePlayerTurn(playerIndex, playerDiceSums));

        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete);

        yield return new WaitForSeconds(turnDelay / 2);

        // Next turn logic
        if (playerIndex == duelWinnerIndex)
        {
            if (loserIndex == 0) yield return StartCoroutine(PlayerTurnSequence(loserIndex, playerIndex, playerDiceSums));
            else yield return StartCoroutine(BotTurnSequence(loserIndex, playerIndex, playerDiceSums));
        }
        else
        {
            UpdateGameState(GameState.TurnEnd);
        }
    }

    private IEnumerator BotTurnSequence(int playerIndex, int loserIndex, int[] playerDiceSums)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Bot's (Player {playerIndex + 1}) turn...");

        yield return StartCoroutine(ExecuteSinglePlayerTurn(playerIndex, playerDiceSums));

        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete);
        yield return new WaitForSeconds(turnDelay / 2); // A small matching pause

        if (playerIndex == duelWinnerIndex)
        {
            if (loserIndex == 0) yield return StartCoroutine(PlayerTurnSequence(loserIndex, playerIndex, playerDiceSums));
            else yield return StartCoroutine(BotTurnSequence(loserIndex, playerIndex, playerDiceSums));
        }
        else
        {
            UpdateGameState(GameState.TurnEnd);
        }
    }

    // --- Reusable Action and Helper Methods ---

    private IEnumerator ExecuteSinglePlayerTurn(int playerIndex, int[] playerDiceSums)
    {
        PlayerController player = players[playerIndex];
        int spacesToMove = playerDiceSums[playerIndex];
        yield return StartCoroutine(MovePlayerTileByTile(player, spacesToMove));
        UpdateGameState(GameState.ProcessingTile);
        ProcessLandedTile(player);
    }

    // --- Dice Logic ---
    private (int dice1, int dice2, bool isSpecial) RollDiceForOutcome(RPS_Choice choice)
    {
        bool isSpecial = (Random.value < specialRollChance); // 1/6 chance

        // --- Step 2: Define all possible dice pairs for the chosen RPS type ---
        List<(int, int)> normalPairs = new List<(int, int)>();
        List<(int, int)> specialPairs = new List<(int, int)>();

        if (choice == RPS_Choice.Rock)
        {
            // Normal pairs that sum to 3, 6, 9, or 12
            normalPairs = new List<(int, int)> { (1, 2), (2, 1), (1, 5), (2, 4), (4, 2), (5, 1),
                                             (3, 6), (4, 5), (5, 4), (6, 3) };
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (3, 3), (6, 6) };
        }
        else if (choice == RPS_Choice.Paper)
        {
            // Normal pairs that sum to 4, 7, or 10
            normalPairs = new List<(int, int)> { (1, 3), (3, 1), (1, 6), (2, 5), (3, 4), (4, 3),
                                             (5, 2), (6, 1), (4, 6), (6, 4) };
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (2, 2), (5, 5) };
        }
        else // Scissors
        {
            // Normal pairs that sum to 2, 5, 8, or 11
            normalPairs = new List<(int, int)> { (1, 4), (2, 3), (3, 2), (4, 1), (2, 6), (3, 5),
                                             (5, 3), (6, 2), (5, 6), (6, 5) };
            // Special (double) pairs. The sum of 2 (1+1) is a double.
            specialPairs = new List<(int, int)> { (1, 1), (4, 4) };
        }

        // --- Step 3: Pick a roll from the correct list ---
        if (isSpecial && specialPairs.Any()) // If we decided it's special AND there are special options
        {
            // Pick a random pair from the special list
            var chosenPair = specialPairs[Random.Range(0, specialPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, true); // Return it, flagged as special
        }
        else
        {
            // Otherwise, pick a random pair from the normal list
            var chosenPair = normalPairs[Random.Range(0, normalPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, false); // Return it, flagged as NOT special
        }
    }

    private IEnumerator MoveAndProcessSequence(PlayerController player, int spacesToMove)
    {
        yield return StartCoroutine(MovePlayerTileByTile(player, spacesToMove));

        yield return new WaitForSeconds(0.25f);

        UpdateGameState(GameState.ProcessingTile);
        ProcessLandedTile(player);
    }

    private void ApplyCardEffect(PlayerController player, ChanceCardData card)
    {
        // Log the event to the UI for player feedback.
        uiManager.LogDuelMessage($"{player.name} drew: {card.cardTitle}!");

        // Use a switch statement to determine what the card does.
        switch (card.effect)
        {
            case ChanceCardEffect.GainMoneyFlat:
                player.money += card.moneyAmount;
                uiManager.UpdatePlayerMoney(player);
                // Let's add a satisfying sound effect for getting money!
                SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx); // Reusing the "cha-ching"
                break;

            case ChanceCardEffect.NextDuelIsGuaranteedWin:
                player.hasGuaranteedWin = true;
                // You could add a special UI icon or sound here to indicate a buff.
                break;

            // Add more 'case' statements here as you create new card effects.

            default:
                Debug.LogWarning($"No effect implemented for card type: {card.effect}");
                break;
        }
    }

    public void ResumeAfterChanceCard()
    {
        // The effect is already applied. We just need to resume the game.
        playerDrawingCard = null;
        currentlyDrawnCard = null;
        UpdateGameState(GameState.ProcessingComplete);
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
                UpdateGameState(GameState.ProcessingComplete);
            }
            else if (landedNode.initialTileType == TileType.Chance)
            {
                uiManager.LogDuelMessage("Bot landed on Chance!");

                UpdateGameState(GameState.DrawingChanceCard);

                playerDrawingCard = activePlayer; // Remember the bot is the one drawing
                currentlyDrawnCard = chanceCardDeck[Random.Range(0, chanceCardDeck.Count)];

                // Apply the effect to the bot immediately.
                ApplyCardEffect(playerDrawingCard, currentlyDrawnCard);

                // Start the UI coroutine to show the card, then the game will resume.
                StartCoroutine(uiManager.ShowChanceCardCoroutine(currentlyDrawnCard));
            } else {
                uiManager.LogDuelMessage($"Bot landed on {landedNode.initialTileType}. No action.");
                UpdateGameState(GameState.ProcessingComplete);
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
            else if (landedNode.initialTileType == TileType.Chance)
            {
                UpdateGameState(GameState.DrawingChanceCard);

                playerDrawingCard = activePlayer;
                currentlyDrawnCard = chanceCardDeck[Random.Range(0, chanceCardDeck.Count)];

                ApplyCardEffect(playerDrawingCard, currentlyDrawnCard);

                StartCoroutine(uiManager.ShowChanceCardCoroutine(currentlyDrawnCard));
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