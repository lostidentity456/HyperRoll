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
    [SerializeField] private GameObject buildingVisualPrefab;

    [Header("Game Data")]
    [SerializeField] private BuildingData houseData;
    [SerializeField] private BuildingData shopData;

    [Header("Visual Settings")]
    [SerializeField] private Material player1ColorMaterial;
    [SerializeField] private Material player2ColorMaterial;
    [SerializeField] private float tileHeight = 1.5f;

    [Header("Building Materials")]
    [SerializeField] private Material player1BuildingMaterial;
    [SerializeField] private Material player2BuildingMaterial;

    [Header("Player Setup")]
    [SerializeField] private CharacterData player1Character;
    [SerializeField] private CharacterData player2Character;

    // --- Game Settings ---
    [Header("Game Settings")]
    [SerializeField] private float turnDelay = 1.5f;
    [SerializeField][Range(0, 1)] private float specialRollChance = 1f / 6f;

    [Header("Chance Cards")]
    [SerializeField] private List<ChanceCardData> chanceCardDeck;
    private PlayerController playerDrawingCard;
    private ChanceCardData currentlyDrawnCard;

    [Header("Stage System")]
    [SerializeField] private int duelsPerStage = 10;
    private int currentStage = 0;
    private int duelCountInCurrentStage = 0;
    private float taxMultiplier = 1.0f;

    [Header("Gambler System")]
    [SerializeField] private List<TreasureThreshold> treasureThresholds;

    // --- State Variables ---
    private List<PlayerController> players = new List<PlayerController>();
    private GameState currentState;
    private int turnNumber = 1;
    private int duelWinnerIndex;
    private int athleteBonusMovement = 0;

    private PlayerController playerMakingChoice;
    private TileNode tileForChoice;

    // Singleton Pattern
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

    // 0. Initiaize Player and Game State

    // A. State Machine
    public void UpdateGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log("GAME STATE CHANGED TO: " + newState);

        switch (currentState)
        {
            // Game Flow Explanation:
            // 1. Game Start : Player chooses RPS move when the panel appears.
            // Bot mades a random choice.
            // A set of dice outcomes are possible for each choice.
            // If one chooses Rock, the dice will sum to 3, 6, 9, or 12.
            // If one chooses Paper, the dice will sum to 4, 7, or 10.
            // If one chooses Scissors, the dice will sum to 2, 5, 8, or 11.
            case GameState.WaitingForPlayerTurnStart:
                uiManager.ShowRpsChoicePanel();
                uiManager.LogDuelMessage($"[Turn {turnNumber}] Choose your move!");
                break;

            // 2. Duel : Dice are rolled and its outcomes are based from choices above.
            case GameState.DuelRolling:
                uiManager.HideRpsChoicePanel();
                break;

            // 3. Duel Result : The winner is determined based on the rules, with exceptions.

            // 3. Player Movement : The winner moves first, followed by the loser.
            // The amount of spaces moved is the sum of the two dice.


            // 4. Turn Processing : The landed tile is processed, both for player and bot.

            // 5. End Turn : After both players have taken their turns, the turn ends.
            // Passive income are collected, check for stage advancement.
            // Loop back to step 1 for the next turn.
            case GameState.TurnEnd:
                uiManager.LogDuelMessage("Collecting income and taxes...");
                CollectPassiveIncome();

                duelCountInCurrentStage++;

                uiManager.LogDuelMessage
                    ($"Duel {duelCountInCurrentStage}/{duelsPerStage} of Stage {currentStage}");

                // Check if this duel was the last one of the stage
                if (duelCountInCurrentStage >= duelsPerStage)
                {
                    // If it was, advance to the next stage
                    AdvanceToNextStage();
                }
                turnNumber++;
                UpdateGameState(GameState.WaitingForPlayerTurnStart);
                break;
        }
    }

    // B. Game Start, Spawn and initialize players
    void Start()
    {
        SpawnPlayers();
        UpdateGameState(GameState.WaitingForPlayerTurnStart);
    }

    private void SpawnPlayers()
    {
        SpawnPlayer(0); // Player 1 (Human)
        SpawnPlayer(1); // Player 2 (Bot)
    }
    private void SpawnPlayer(int playerId)
    {
        GameObject playerObject = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        playerObject.name = (playerId == 0) ? "Player" : "Bot";

        Renderer playerRenderer = playerObject.GetComponentInChildren<Renderer>();
        if (playerRenderer != null)
        {
            if (playerId == 0) { playerRenderer.material = player1ColorMaterial; }
            else { playerRenderer.material = player2ColorMaterial; }
        }

        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        players.Add(playerController);

        CharacterData charToAssign = (playerId == 0) ? player1Character : player2Character;
        playerController.Initialize(playerId, charToAssign);

        if (playerId == 1)
        {
            int halfwayPoint = boardManager.GetPathLength() / 2;
            playerController.currentPathIndex = halfwayPoint;
        }

        Vector3 startPos = boardManager.GetWorldPositionFromPathIndex
            (playerController.currentPathIndex);
        float playerRadius = playerController.transform.localScale.y / 2f;
        startPos.y = (tileHeight / 2f) + playerRadius;
        playerController.transform.position = startPos;
        uiManager.UpdatePlayerMoney(playerController);
    }

    // 1. Game Start 

    // Handles player's specific choice
    public void OnPlayerChoseAttack(RPS_Choice playerChoice)
    {
        if (currentState != GameState.WaitingForPlayerTurnStart) return;
        UpdateGameState(GameState.DuelRolling);

        // (Specialist Exclusive) a check if this roll should be a double roll.
        bool forceSpecial = CheckAndApplySpecialistProc();

        // Bot makes a random choice
        RPS_Choice botChoice = (RPS_Choice)Random.Range(0, 3);

        StartCoroutine(ControlledDuelSequence(playerChoice, botChoice, forceSpecial));
    }
    // Handles player's random choice
    public void OnPlayerChoseRandom()
    {
        if (currentState != GameState.WaitingForPlayerTurnStart) return;
        UpdateGameState(GameState.DuelRolling);

        // (Specialist Exclusive) a check if this roll should be a double roll.
        bool forceSpecial = CheckAndApplySpecialistProc();

        StartCoroutine(RandomDuelSequence(forceSpecial));
    }

    // 2. Duel 

    // 2.1. Controlled Duel : This happens when the player makes a specific choice.
    private IEnumerator ControlledDuelSequence(RPS_Choice playerChoice,
        RPS_Choice botChoice, bool forcePlayerSpecial = false)
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.diceRollSfx);
        uiManager.LogDuelMessage($"[Turn {turnNumber}] Dueling...");

        int[] playerDiceSums = new int[2];
        DuelChoice[] duelChoices = new DuelChoice[2];

        // Use helper method to process each player's roll.
        ProcessPlayerRoll(0, playerChoice, playerDiceSums, duelChoices, forcePlayerSpecial);
        ProcessPlayerRoll(1, botChoice, playerDiceSums, duelChoices);

        yield return StartCoroutine(RunDuelAndTurns(duelChoices, playerDiceSums));
    }

    // 2.2. Random Duel : This happens when the player chooses the Random option.
    private IEnumerator RandomDuelSequence(bool forcePlayerSpecial = false)
    {
        SoundManager.Instance.PlaySound(SoundManager.Instance.diceRollSfx);
        uiManager.LogDuelMessage($"[Turn {turnNumber}] Dueling...");

        int[] playerDiceSums = new int[2];
        DuelChoice[] duelChoices = new DuelChoice[2];

        ProcessPlayerRoll(0, (RPS_Choice)Random.Range(0, 3),
            playerDiceSums, duelChoices, forcePlayerSpecial);
        ProcessPlayerRoll(1, (RPS_Choice)Random.Range(0, 3),
            playerDiceSums, duelChoices);

        yield return StartCoroutine(RunDuelAndTurns(duelChoices, playerDiceSums));
    }
    

    // 3. Duel Result 
    private IEnumerator RunDuelAndTurns(DuelChoice[] duelChoices, int[] playerDiceSums)
    {
        // Show the duel results on the UI, and determine the winner.
        uiManager.ShowDuelResults(duelChoices[0], duelChoices[1], playerDiceSums[0], playerDiceSums[1]);
        yield return new WaitForSeconds(turnDelay);

        var duelResult = DetermineDuelWinner(duelChoices[0], duelChoices[1]);

        // (Negotiator Exclusive) Alternate tie results to wins
        // This part can't be refactored because it alternates duel results.
        if (duelResult.winner == -1)
        {
            PlayerController p1 = players[0];
            PlayerController p2 = players[1];

            // Check if only one player is the Negotiator.
            bool p1IsNegotiator = p1.characterData?.passiveAbility == CharacterPassive.TheNegotiator;
            bool p2IsNegotiator = p2.characterData?.passiveAbility == CharacterPassive.TheNegotiator;

            if (p1IsNegotiator && !p2IsNegotiator) // Player 1 is the Negotiator
            {
                duelResult.winner = 0; // Player 1 now wins the tie.
                p1.passiveState.negotiatorTokens++;
                uiManager.LogDuelMessage("Negotiator Passive: Tie becomes a win! +1 Token.");
            }
            else if (p2IsNegotiator && !p1IsNegotiator) // Player 2 is the Negotiator
            {
                duelResult.winner = 1; // Player 2 now wins the tie.
                p2.passiveState.negotiatorTokens++;
                uiManager.LogDuelMessage("Negotiator Passive: Tie becomes a win! +1 Token.");
            }
            // If both are negotiators, nothing changes.
        }

        // If duel is still a tie, both players choose again (Back to State 1)
        if (duelResult.winner == -1)
        {
            uiManager.LogDuelMessage("DUEL IS A TIE! Choose your move again.");
            yield return new WaitForSeconds(turnDelay);

            UpdateGameState(GameState.WaitingForPlayerTurnStart);

            yield break;
        }

        // Duel is not a tie, proceed with winner and loser logic.
        duelWinnerIndex = duelResult.winner;
        int duelLoserIndex = (duelWinnerIndex == 0) ? 1 : 0;

        PlayerController winner = players[duelWinnerIndex];
        PlayerController loser = players[duelLoserIndex];

        // Duel result announcement
        uiManager.LogDuelMessage($"Player {duelWinnerIndex + 1} wins the duel" +
            (duelResult.isSpecialWin ? " with a SPECIAL WIN!" : "!"));

        // Post-duel role-based passive processing
        ProcessPostDuelPassives(winner, loser, ref duelResult);

        yield return new WaitForSeconds(turnDelay);

        // Turn order based on duel result

        if (duelWinnerIndex == 0) // Player is first
        {
            yield return StartCoroutine(PlayerTurnSequence(duelWinnerIndex, duelLoserIndex, playerDiceSums));
        }
        else // Bot is first
        {
            yield return StartCoroutine(BotTurnSequence(duelWinnerIndex, duelLoserIndex, playerDiceSums));
        }
    }

    // 4. Turn Processing

    // 4.1. Player Turn Sequence : This handles the player's turn.
    private IEnumerator PlayerTurnSequence(int playerIndex, int loserIndex, int[] playerDiceSums)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Player {playerIndex + 1}'s turn...");

        PlayerController player = players[playerIndex];
        int baseMovement = playerDiceSums[playerIndex];
        int finalMovement = baseMovement;

        // --- Phase 1: Pre-Movement Passives (The Athlete) ---
        if (player.characterData?.passiveAbility == CharacterPassive.TheAthlete)
        {
            athleteBonusMovement = Random.Range(1, 4);
            uiManager.ShowAthleteChoicePanel();
            UpdateGameState(GameState.WaitingForAthleteChoice);

            // Pause this coroutine until the player clicks Yes or No.
            yield return new WaitUntil(() => currentState != GameState.WaitingForAthleteChoice);

            if (athleteBonusMovement > 0)
            {
                finalMovement = baseMovement + athleteBonusMovement;
                athleteBonusMovement = 0; // Reset
            }
        }

        // --- Phase 2: Execute Movement ---
        yield return StartCoroutine(ExecuteSinglePlayerTurn(playerIndex, finalMovement));

        // --- Phase 3: Post-Movement Passives (Architect, Thief) ---
        // All the complex "passing Go" logic is now in this one clean method call.
        ProcessPostMovementPassives(player, finalMovement);

        // --- Phase 4: Wait for Tile Processing ---
        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete);
        yield return new WaitForSeconds(turnDelay / 2);

        // --- Phase 5: Determine Next Turn ---
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

    // 4.2. Bot Turn Sequence : This handles the bot's turn.
    private IEnumerator BotTurnSequence(int playerIndex, int loserIndex, int[] playerDiceSums)
    {
        UpdateGameState(GameState.PlayerMoving);
        uiManager.LogDuelMessage($"Bot's (Player {playerIndex + 1}) turn...");

        PlayerController bot = players[playerIndex];
        int botMovement = playerDiceSums[playerIndex];

        yield return StartCoroutine(ExecuteSinglePlayerTurn(playerIndex, botMovement));

        yield return new WaitUntil(() => currentState == GameState.ProcessingComplete);
        yield return new WaitForSeconds(turnDelay / 2); // A small matching pause

        if (playerIndex == duelWinnerIndex)
        {
            if (loserIndex == 0) 
                yield return StartCoroutine(PlayerTurnSequence(loserIndex, playerIndex, playerDiceSums));
            else yield return StartCoroutine(BotTurnSequence(loserIndex, playerIndex, playerDiceSums));
        }
        else
        {
            UpdateGameState(GameState.TurnEnd);
        }
    }

    // 4.3. Single Player Turn Execution
    private IEnumerator ExecuteSinglePlayerTurn(int playerIndex, int spacesToMove)
    {
        PlayerController player = players[playerIndex];
        yield return StartCoroutine(MovePlayerTileByTile(player, spacesToMove));
        UpdateGameState(GameState.ProcessingTile);
        ProcessLandedTile(player);
    }

    // 4.3.1. Move Player (tile by tile)
    private IEnumerator MovePlayerTileByTile(PlayerController player, int spacesToMove)
    {
        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < spacesToMove; i++)
        {
            player.currentPathIndex = (player.currentPathIndex + 1) % boardManager.GetPathLength();

            // Get the world position of the VERY NEXT tile on the path.
            Vector3 nextTilePos = boardManager.GetWorldPositionFromPathIndex(player.currentPathIndex);

            SoundManager.Instance.PlaySound(SoundManager.Instance.pieceMoveSfx);
            // Call smooth movement function to move just one tile.
            float stepDuration = 0.15f;
            yield return StartCoroutine(MovePlayerSmoothly(player, nextTilePos, stepDuration));

            // (Athlete Exclusive) Give money for each space moved.
            if (player.characterData?.passiveAbility == CharacterPassive.TheAthlete)
            {
                // The bonus increases with the stage. Stage 0 = $5, Stage 1 = $10, etc.
                int moneyPerSpace = 5 * (currentStage + 1);
                int totalMoneyGained = spacesToMove * moneyPerSpace;

                if (totalMoneyGained > 0)
                {
                    player.money += totalMoneyGained;
                    uiManager.UpdatePlayerMoney(player);
                    uiManager.LogDuelMessage($"{player.name} (Athlete) gained ${totalMoneyGained} for moving!");
                }
            }
        }
    }
    // 4.3.2. Process actions on the landed tile
    private void ProcessLandedTile(PlayerController activePlayer)
    {
        TileNode landedNode = boardManager.GetNodeFromPathIndex(activePlayer.currentPathIndex);

        if (activePlayer.playerId == 1) // If it's the bot
        {
            ProcessBotLanding(landedNode);
        }
        else // It's the human player
        {
            ProcessPlayerLanding(landedNode);
        }
    }

    // 4.3.2.1. Process actions for player
    private void ProcessPlayerLanding(TileNode landedNode)
    {
        PlayerController player = players[0];

        // Player lands on a buildable tile.
        if (landedNode.initialTileType == TileType.Buildable)
        {
            // It's an unowned tile, player is offered to build.
            if (landedNode.owner == null) 
            {
                playerMakingChoice = player;
                tileForChoice = landedNode;
                uiManager.ShowBuildPanel(player, houseData, shopData);
                UpdateGameState(GameState.WaitingForPlayerChoice);
            }
            else // It's owned 
            {
                // Owned by opponent, pay tax
                if (landedNode.owner != player)
                {
                    HandleTaxPayment(player, landedNode.owner, landedNode);
                }

                // End the process for the player
                if (currentState != GameState.GameOver)
                {
                    UpdateGameState(GameState.ProcessingComplete);
                }
            }
        }
        // Landing on a Chance tile
        else if (landedNode.initialTileType == TileType.Chance)
        {
            UpdateGameState(GameState.DrawingChanceCard);
            playerDrawingCard = player;
            currentlyDrawnCard = chanceCardDeck[Random.Range(0, chanceCardDeck.Count)];
            ApplyCardEffect(playerDrawingCard, currentlyDrawnCard);
            StartCoroutine(uiManager.ShowChanceCardCoroutine(currentlyDrawnCard));
        }
        // Any other tile
        else
        {
            uiManager.LogDuelMessage($"Landed on {landedNode.initialTileType}. No action.");
            UpdateGameState(GameState.ProcessingComplete);
        }
    }

    // 4.3.2.2. Process actions for bot
    private void ProcessBotLanding(TileNode landedNode)
    {
        PlayerController bot = players[1];

        // Landing on a buildable, unowned tile.
        if (landedNode.initialTileType == TileType.Buildable && landedNode.owner == null)
        {
            BuildingData buildingToBuild = houseData;
            if (bot.money >= buildingToBuild.buildingCost)
            {
                bot.money -= buildingToBuild.buildingCost;
                landedNode.owner = bot;
                landedNode.currentBuilding = buildingToBuild;
                landedNode.buildingLevel = 1;

                uiManager.UpdatePlayerMoney(bot);
                // FOR NOW, TILE WILL HAVE BUILDING COLOR. THIS IS TEMPORARY.
                boardManager.UpdateTileVisual(landedNode, player2BuildingMaterial);
                boardManager.UpdateBuildingVisual
                    (landedNode, player2BuildingMaterial, buildingVisualPrefab);
                uiManager.LogDuelMessage($"Bot built a {buildingToBuild.buildingName}!");
                SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
            }
            else
            {
                uiManager.LogDuelMessage("Bot chose not to build to save money.");
            }
            UpdateGameState(GameState.ProcessingComplete);
        }
        // Landing on an opponent's property.
        else if (landedNode.owner != null && landedNode.owner != bot)
        {
            HandleTaxPayment(bot, landedNode.owner, landedNode);

            if (currentState != GameState.GameOver)
            {
                UpdateGameState(GameState.ProcessingComplete);
            }
        }
        // Landing on a Chance tile
        else if (landedNode.initialTileType == TileType.Chance)
        {
            UpdateGameState(GameState.DrawingChanceCard);
            playerDrawingCard = bot;
            currentlyDrawnCard = chanceCardDeck[Random.Range(0, chanceCardDeck.Count)];
            ApplyCardEffect(playerDrawingCard, currentlyDrawnCard);
            StartCoroutine(uiManager.ShowChanceCardCoroutine(currentlyDrawnCard));
        }
        // Any other tile 
        else
        {
            uiManager.LogDuelMessage($"Bot landed on {landedNode.initialTileType}. No action.");
            UpdateGameState(GameState.ProcessingComplete);
        }
    }

    // 5. End Turn (this is handled in the state machine)



    //=================================================================================

    // --- Helper Methods ---

    // A. General Game Logic Helpers

    // 1. ProcessPlayerRoll : Rolls dice for a given player and processes the outcome.
    private void ProcessPlayerRoll(int playerIndex, RPS_Choice rpsChoice,
                               int[] playerDiceSums, DuelChoice[] duelChoices, bool forceSpecial = false)
    {
        var rollResult = RollDiceForOutcome(rpsChoice, forceSpecial);
        playerDiceSums[playerIndex] = rollResult.dice1 + rollResult.dice2;
        duelChoices[playerIndex] = new DuelChoice(rpsChoice, rollResult.isSpecial);
    }


    // 2. RollDiceForOutcome : With a given outcome, roll dice that match the outcome. 
    // (Provides logic for ProcessPlayerRoll)
    private (int dice1, int dice2, bool isSpecial) RollDiceForOutcome
        (RPS_Choice choice, bool forceSpecial = false)
    {
        bool isSpecial = forceSpecial || (Random.value < specialRollChance); // 1/6 chance

        // Define all possible dice pairs for the chosen RPS type
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
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (1, 1), (4, 4) };
        }

        // Pick a roll from the correct list

        // - Special roll
        if (isSpecial && specialPairs.Any())
        {
            // Pick a random pair from the special list
            var chosenPair = specialPairs[Random.Range(0, specialPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, true); // Return it, flagged as special
        }
        else // - Normal roll
        {
            // Otherwise, pick a random pair from the normal list
            var chosenPair = normalPairs[Random.Range(0, normalPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, false); // Return it, flagged as NOT special
        }
    }

    // 3. DetermineDuelWinner : Given two players' choices, determine the winner.
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
            if (p1.isSpecial && !p2.isSpecial) { return (0, false); }
            else if (!p1.isSpecial && p2.isSpecial) { return (1, false); }
            else { return (-1, false); } // Tie
        }
    }

    // 4. ProcessPostDuelPassives : Handle passive abilities that trigger after a duel.
    // (Helper for RunDuelAndTurns)
    private void ProcessPostDuelPassives(PlayerController winner, PlayerController loser,
        ref (int winner, bool isSpecialWin) duelResult)
    {
        ProcessNegotiatorPassive(winner, loser, ref duelResult);
        ProcessDuelistPassive(winner, loser);
        ProcessGamblerPassive(winner, loser, duelResult);
    }

    // 5. ProcessPostMovementPassives : Handle passive abilities that trigger after movement.
    private void ProcessPostMovementPassives(PlayerController player, int movementAmount)
    {
        // (Architect & Thief) Did the player pass their Go tile? (this logic is identical for both)
        if (player.characterData?.passiveAbility == CharacterPassive.TheArchitect ||
            player.characterData?.passiveAbility == CharacterPassive.TheThief)
        {
            int goTileIndex = (player.playerId == 0) ? 0 : boardManager.GetPathLength() / 2;
            int previousPathIndex = (player.currentPathIndex - movementAmount + boardManager.GetPathLength()) % boardManager.GetPathLength();

            bool passedGo = (previousPathIndex > player.currentPathIndex);

            if (passedGo)
            {
                if (player.characterData.passiveAbility == CharacterPassive.TheArchitect)
                {
                    UpgradeRandomBuildingForPlayer(player);
                }

                if (player.characterData.passiveAbility == CharacterPassive.TheThief)
                {
                    if (player.passiveState.thiefLapIsClean)
                    {
                        uiManager.LogDuelMessage("Thief Passive: Clean lap completed! Heist successful!");
                        player.hasTaxImmunity = true;
                        player.passiveState.willStealNextIncome = true;
                    }
                    else
                    {
                        uiManager.LogDuelMessage("Thief Passive: Lap completed, but tax was paid. No heist.");
                    }
                    player.passiveState.thiefLapIsClean = true; // Reset for the new lap.
                }
            }
        }
    }

    // MovePlayerSmoothly : Coroutine to move a player smoothly to the next tile.
    // (Helper for MovePlayerTileByTile)
    private IEnumerator MovePlayerSmoothly(PlayerController player, Vector3 targetPosition, float duration)
    {
        float playerRadius = player.transform.localScale.y / 2f;

        targetPosition.y = (tileHeight / 2f) + playerRadius;

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

    // HandleTaxPayment : Perform tax calculation to transfer from one player to other
    private void HandleTaxPayment(PlayerController payingPlayer, PlayerController receivingPlayer,
        TileNode propertyNode)
    {
        // Get the building data and level from the node
        BuildingData building = propertyNode.currentBuilding;
        int level = propertyNode.buildingLevel;

        // Calculate the tax amount
        float taxRate = building.GetTaxRateForLevel(level);
        int taxAmount = Mathf.RoundToInt(building.buildingCost * taxRate);

        if (receivingPlayer.characterData.passiveAbility == CharacterPassive.TheDuelist)
        {
            if (receivingPlayer.passiveState.duelistWinStreak >= 5) taxAmount *= 5;
            else if (receivingPlayer.passiveState.duelistWinStreak >= 3) taxAmount *= 2;
        }

        // Check for bankruptcy
        if (payingPlayer.money < taxAmount)
        {
            uiManager.LogDuelMessage($"{payingPlayer.name} is bankrupt! {receivingPlayer.name} wins!");
            UpdateGameState(GameState.GameOver);
            return;
        }

        // Check for tax immunity.
        if (payingPlayer.hasTaxImmunity)
        {
            payingPlayer.hasTaxImmunity = false;
            uiManager.LogDuelMessage($"{payingPlayer.name} used Tax Immunity to avoid paying tax!");
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }
        else
        {
            // Perform the transaction.
            payingPlayer.money -= taxAmount;
            receivingPlayer.money += taxAmount;
            uiManager.UpdatePlayerMoney(payingPlayer);
            uiManager.UpdatePlayerMoney(receivingPlayer);
            uiManager.LogDuelMessage
                ($"{payingPlayer.name} paid ${taxAmount} tax to {receivingPlayer.name}!");
        }

        // (Thief Exclusive) Thief pays tax, so their lap is no longer clean.
        if (payingPlayer.characterData?.passiveAbility == CharacterPassive.TheThief)
        {
            payingPlayer.passiveState.thiefLapIsClean = false;
        }
    }

    // ApplyCardEffect : With a drawn card from Chance pool, apply its effect to player
    private void ApplyCardEffect(PlayerController player, ChanceCardData card)
    {
        // Log the event to the UI for player feedback.
        uiManager.LogDuelMessage($"{player.name} drew: {card.cardTitle}!");

        // Use a switch statement to determine what the card does.
        switch (card.effect)
        {
            case ChanceCardEffect.GainMoneyFlat:
                int flatMoneyGained = card.moneyAmount;

                // Duelist logic
                if (player.characterData.passiveAbility == CharacterPassive.TheDuelist)
                {
                    if (player.passiveState.duelistWinStreak >= 5) flatMoneyGained *= 5;
                    else if (player.passiveState.duelistWinStreak >= 3) flatMoneyGained *= 2;
                }
                uiManager.UpdatePlayerMoney(player);
                SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx); // Reusing the "cha-ching"
                break;

            case ChanceCardEffect.NextDuelIsGuaranteedWin:
                player.hasGuaranteedWin = true;
                // You could add a special UI icon or sound here to indicate a buff.
                break;

            case ChanceCardEffect.GainMoneyPerBuilding:
                int buildingCount = CountBuildingsForPlayer(player);
                int moneyPerBuildingGained = card.moneyAmount +
                    (buildingCount * card.moneyPerBuilding);

                if (player.characterData.passiveAbility == CharacterPassive.TheDuelist)
                {
                    if (player.passiveState.duelistWinStreak >= 5) moneyPerBuildingGained *= 5;
                    else if (player.passiveState.duelistWinStreak >= 3) moneyPerBuildingGained *= 2;
                }

                player.money += moneyPerBuildingGained;
                uiManager.UpdatePlayerMoney(player);

                // Give some extra feedback!
                uiManager.LogDuelMessage
                    ($"Gained ${moneyPerBuildingGained} from {buildingCount} properties!");
                if (moneyPerBuildingGained > 0)
                {
                    SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
                }
                break;

            case ChanceCardEffect.TaxImmunity:
                player.hasTaxImmunity = true;
                // You could add a UI icon here later to show the player has the buff.
                uiManager.LogDuelMessage("You gained one-time tax immunity!");
                break;

            default:
                Debug.LogWarning($"No effect implemented for card type: {card.effect}");
                break;
        }
    }

    // CollectPassiveIncome : Called at the end of each turn to collect income from properties.
    private void CollectPassiveIncome()
    {
        int[] incomeThisTurn = new int[2] { 0, 0 };

        // Get a list of all properties on the board
        List<TileNode> allProperties = boardManager.GetAllPropertyNodes();

        foreach (TileNode property in allProperties)
        {
            if (property.owner != null && property.currentBuilding != null)
            {
                // Calculate Gross Income -> exponential growth
                float currentIncomeRate =
                    property.currentBuilding.baseIncomeRate * Mathf.Pow(2, property.buildingLevel - 1);
                int grossIncome = Mathf.RoundToInt
                    (property.currentBuilding.buildingCost * currentIncomeRate);

                if (property.owner.characterData.passiveAbility == CharacterPassive.TheEconomist)
                {
                    // The Economist gets 100% more (double) income
                    grossIncome *= 2;
                }

                if (property.owner.characterData.passiveAbility == CharacterPassive.TheDuelist)
                {
                    // The Duelist gets bonus income based on their win streak
                    if (property.owner.passiveState.duelistWinStreak >= 5)
                    {
                        grossIncome *= 5; // Pentuple income from 5+ streak
                    }
                    else if (property.owner.passiveState.duelistWinStreak >= 3)
                    {
                        grossIncome *= 2; // Double income from 3+ streak
                    }
                }

                incomeThisTurn[property.owner.playerId] += grossIncome;
            }
        }
        if (incomeThisTurn[0] > 0)
        {
            Debug.Log($"Player earned a total of ${incomeThisTurn[0]} passive income from properties!");
        }
        if (incomeThisTurn[1] > 0)
        {
            Debug.Log($"Bot earned a total of ${incomeThisTurn[0]} passive income from properties!");
        }

        PlayerController thief = players.Find(p => p.passiveState.willStealNextIncome);
        if (thief != null)
        {
            int targetPlayerId = (thief.playerId == 0) ? 1 : 0; // Find the other player
            int stolenAmount = incomeThisTurn[targetPlayerId];

            if (stolenAmount > 0)
            {
                // The stage scaling you wanted!
                float stealMultiplier = 1.0f + currentStage; // Steal 100% more per stage
                stolenAmount = Mathf.RoundToInt(stolenAmount * stealMultiplier);

                // Perform the transfer
                incomeThisTurn[thief.playerId] += stolenAmount;
                incomeThisTurn[targetPlayerId] -= stolenAmount; // The victim losts the amount

                uiManager.LogDuelMessage($"{thief.name} stole ${stolenAmount} from the opponent!");
            }

            // Consume the flag
            thief.passiveState.willStealNextIncome = false;
        }

        for (int i = 0; i < players.Count; i++)
        {
            players[i].money += incomeThisTurn[i];
        }

        uiManager.UpdatePlayerMoney(players[0]);
        uiManager.UpdatePlayerMoney(players[1]);
    }

    // AdvanceToNextStage : Called after every 10 duels to increase the stage and tax multiplier.
    // This is followed by calling AnnounceNewStage to inform the player.
    private void AdvanceToNextStage()
    {
        // Reset the duel counter for the new stage
        duelCountInCurrentStage = 0;

        // Increment the stage, but cap it at 5
        currentStage = Mathf.Min(currentStage + 1, 5);

        // Update the tax multiplier based on the new stage
        switch (currentStage)
        {
            case 1: taxMultiplier = 1.5f; break;
            case 2: taxMultiplier = 2.0f; break;
            case 3: taxMultiplier = 3.0f; break;
            case 4: taxMultiplier = 4.0f; break;
            case 5: taxMultiplier = 5.0f; break;
            default: taxMultiplier = 1.0f; break; // Stage 0
        }

        // Announce the new stage to the player.
        StartCoroutine(AnnounceNewStage());
    }
    private IEnumerator AnnounceNewStage()
    {
        uiManager.LogDuelMessage($"Stage {currentStage} begins! Rent is now multiplied by x{taxMultiplier}!");

        // Here is where we trigger the Power-Up selection UI.
        // It now correctly triggers after Stage 0 (entering Stage 1), Stage 2 (entering 3), and Stage 4 (entering 5).
        if (currentStage == 1 || currentStage == 3 || currentStage == 5)
        {
            // Give the player a moment to read the stage announcement.
            yield return new WaitForSeconds(turnDelay);

            // TODO: Show the Power-Up selection UI for the human player.
            // For now, we will just log a message.
            Debug.Log($"--- PLAYER {players[0].name} GETS TO CHOOSE A POWER-UP! ---");
        }
    }

    // CountBuildingsForPlayer : Returns the number of buildings owned by a player.
    private int CountBuildingsForPlayer(PlayerController player)
    {
        int buildingCount = 0;
        buildingCount = boardManager.GetBuildingCountForPlayer(player);
        return buildingCount;
    }

    // ApplyReward : Applies a reward to a player
    // Helper for ProcessGamblerCashOut and potentially other places.
    private void ApplyReward(PlayerController player, RewardData reward)
    {
        uiManager.LogDuelMessage($"{player.name} got a bonus reward: {reward.rewardName}!");
        switch (reward.type)
        {
            case RewardType.GainMoney:
                player.money += reward.amount;
                uiManager.UpdatePlayerMoney(player);
                break;
                // Add cases for other reward types here.
        }
    }

    // B. Role-exclusive Logic Helpers

    // 1. (Architect Exclusive) Upgrade a random building
    private void UpgradeRandomBuildingForPlayer(PlayerController player)
    {
        List<TileNode> ownedProperties = boardManager.GetAllPropertyNodes().
            FindAll(node => node.owner == player);
        if (ownedProperties.Any())
        {
            // Find a random property and "level it up" (we'll define what that means later)
            TileNode randomProperty = ownedProperties[Random.Range(0, ownedProperties.Count)];
            randomProperty.buildingLevel = Mathf.Min(randomProperty.buildingLevel + 1, 3);
            uiManager.LogDuelMessage($"{player.name} (Architect) upgraded their {randomProperty.currentBuilding.buildingName} to Level {randomProperty.buildingLevel}!");
        }
    }

    // 2. (Athlete Exclusive) Handle the player's choice (Yes or No) from the UI panel.
    public void PlayerChoseAthleteBonus(bool accepted)
    {
        if (currentState != GameState.WaitingForAthleteChoice) return;

        if (accepted)
        {
            uiManager.LogDuelMessage($"Athlete bonus accepted! Moving an extra {athleteBonusMovement} spaces!");
        }
        else
        {
            uiManager.LogDuelMessage("Athlete bonus declined.");
            athleteBonusMovement = 0; // Set to 0 so no bonus is added
        }
        UpdateGameState(GameState.PlayerMoving);
    }

    // 3. (Duelist Exclusive) Check if Duelist streak is broken or increased.
    private void ProcessDuelistPassive(PlayerController winner, PlayerController loser)
    {
        // Handle the loser's streak being broken.
        if (loser.characterData?.passiveAbility == CharacterPassive.TheDuelist)
        {
            if (loser.passiveState.duelistWinStreak >= 3)
            {
                uiManager.LogDuelMessage($"{loser.name}'s win streak of {loser.passiveState.duelistWinStreak} was broken!");
            }
            loser.passiveState.duelistWinStreak = 0;
        }

        // Handle the winner's streak increasing.
        if (winner.characterData?.passiveAbility == CharacterPassive.TheDuelist)
        {
            winner.passiveState.duelistWinStreak++;
            if (winner.passiveState.duelistWinStreak >= 2)
            {
                uiManager.LogDuelMessage($"{winner.name} is now on a {winner.passiveState.duelistWinStreak} win streak!");
            }
        }
    }

    // 6.1. (Gambler Exclusive) Add treasures on loss, and cash out on special win.
    private void ProcessGamblerPassive(PlayerController winner, PlayerController loser, (int winner, bool isSpecialWin) duelResult)
    {
        // Handle the loser gaining treasures.
        if (loser.characterData?.passiveAbility == CharacterPassive.TheGambler)
        {
            int treasuresGained = Random.Range(1, 7);
            loser.passiveState.gamblerTreasures += treasuresGained;
            uiManager.LogDuelMessage($"{loser.name} (Gambler) lost but gained {treasuresGained} treasures! (Total: {loser.passiveState.gamblerTreasures})");
        }

        // Handle the winner cashing out on a special win.
        if (winner.characterData?.passiveAbility == CharacterPassive.TheGambler && duelResult.isSpecialWin)
        {
            ProcessGamblerCashOut(winner);
        }
    }

    // 6.2. (Gambler Exclusive) Process the cash out when Gambler special-wins a duel.
    // (Helper for ProcessGamblerPassive)
    private void ProcessGamblerCashOut(PlayerController gambler)
    {
        int treasureValue = gambler.passiveState.gamblerTreasures;
        if (treasureValue <= 0) return;

        // Find the correct threshold ---
        TreasureThreshold currentThreshold = null;
        foreach (var threshold in treasureThresholds)
        {
            if (treasureValue >= threshold.valueRequired)
            {
                currentThreshold = threshold;
            }
            else
            {
                break;
            }
        }

        if (currentThreshold == null)
        {
            Debug.LogError("No valid treasure threshold found!");
            return;
        }

        int moneyGained = treasureValue * currentThreshold.moneyPerValue;
        gambler.money += moneyGained;
        uiManager.LogDuelMessage($"{gambler.name} cashed out {treasureValue} treasures for ${moneyGained}!");

        // --- 3. Check for and Award an Additional Reward ---
        if (currentThreshold.rewardPool != null && currentThreshold.rewardPool.Any())
        {
            // Pick a random reward from the pool for this threshold.
            RewardData bonusReward = currentThreshold.rewardPool[Random.Range(0, currentThreshold.rewardPool.Count)];

            // Apply the bonus reward (we'll need to expand ApplyCardEffect for this).
            ApplyReward(gambler, bonusReward);
        }

        // --- 4. Finalize ---
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        uiManager.UpdatePlayerMoney(gambler);
        gambler.passiveState.gamblerTreasures = 0; // Reset treasures
    }

    // 9. (Negotiator Exclusive) Apply special win negation if enough tokens.
    // (Helper for ProcessPostDuelPassives)
    private void ProcessNegotiatorPassive(PlayerController winner, PlayerController loser, 
        ref (int winner, bool isSpecialWin) duelResult)
    {
        // Check if the loser is the Negotiator and can negate a special win.
        if (duelResult.isSpecialWin && loser.characterData?.passiveAbility == CharacterPassive.TheNegotiator
            && loser.passiveState.negotiatorTokens > 0)
        {
            loser.passiveState.negotiatorTokens--;
            duelResult.isSpecialWin = false; 
            uiManager.LogDuelMessage($"{loser.name} used 3 tokens to negate the special win!");
        }
    }

    //11. (Specialist Exclusive) check and apply the dice roll to be special
    private bool CheckAndApplySpecialistProc()
    {
        PlayerController player = players[0];

        // Check if player is Specialist
        if (player.characterData == null 
            || player.characterData.passiveAbility != CharacterPassive.TheSpecialist)
        {
            return false;
        }

        player.passiveState.duelsSinceLastSpecial++;

        // Determine how many duels are required at the current stage.
        int requiredRolls = (currentStage >= 3) ? 3 : 4;

        // Counter check
        if (player.passiveState.duelsSinceLastSpecial >= requiredRolls)
        {
            uiManager.LogDuelMessage("Specialist Passive: This roll is a guaranteed Special!");
            player.passiveState.duelsSinceLastSpecial = 0; // Reset the counter
            return true; // Activate the passive
        }
        return false;
    }


    // C. UI Interaction Methods

    // Duel Choice UI Interaction
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

    // ResumeAfterChanceCard : Called by UI when the Chance card is shown, after a delay.
    // It only clears card data and player that draws the card, and resume the game.
    public void ResumeAfterChanceCard()
    {
        // The effect is already applied, resume the game
        playerDrawingCard = null;
        currentlyDrawnCard = null;
        UpdateGameState(GameState.ProcessingComplete);
    }

    // Build Panel UI Interaction
    // PlayerChoseToBuild : Handles the case when the player chooses to build a specific building.
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

            // FOR NOW, TILE WILL HAVE BUILDING COLOR. THIS IS TEMPORARY.
            boardManager.UpdateTileVisual(tileForChoice, player1BuildingMaterial);
            boardManager.UpdateBuildingVisual(tileForChoice, player1BuildingMaterial, buildingVisualPrefab);

            tileForChoice.buildingLevel = 1;

            uiManager.UpdatePlayerMoney(playerMakingChoice);
            uiManager.LogDuelMessage($"Player {playerMakingChoice.playerId + 1} built a {selectedBuilding.buildingName}!");

            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }
        else
        {
            uiManager.LogDuelMessage("Not enough money!");
        }

        playerMakingChoice = null;
        tileForChoice = null;

        UpdateGameState(GameState.ProcessingComplete);
    }
    // PlayerChoseToPass : Handles the case when the player chooses not to build.
    public void PlayerChoseToPass()
    {
        if (currentState != GameState.WaitingForPlayerChoice) return;
        uiManager.LogDuelMessage("Player chose not to build.");
        playerMakingChoice = null;
        tileForChoice = null;
        UpdateGameState(GameState.ProcessingComplete);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}