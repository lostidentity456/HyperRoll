using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static TileData;
using static UnityEditor.Experimental.GraphView.GraphView;

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
    private DiceMode currentDiceMode = DiceMode.Normal;

    [Header("Chance Card Decks")]
    [SerializeField] private List<ChanceCardData> goodChanceCards;
    [SerializeField] private List<ChanceCardData> badChanceCards;
    [SerializeField] private List<ChanceCardData> unknownChanceCards;

    // Quest System
    private Quest activeQuest = null;

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
    private PlayerController riggedPlayer = null;
    private int athleteBonusMovement = 0;
    private const int MAX_BUILDING_LEVEL = 4;

    private PlayerController playerMakingChoice;
    private TileNode tileForChoice;
    private DuelChoice[] lastDuelChoices;

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
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].passiveState.isAFK)
                    {
                        // Consume the debuff
                        players[i].passiveState.isAFK = false; 
                        uiManager.LogDuelMessage($"{players[i].name} went AFK and skips their turn!");

                        // Start a special coroutine that handles a skipped turn.
                        StartCoroutine(SkippedTurnSequence(players[i]));

                        break;
                    }
                }
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
                for (int i = 0; i < players.Count; i++)
                {
                    if (players[i].passiveState.chaosRunTurns > 0)
                    {
                        players[i].passiveState.chaosRunTurns--;
                        if (players[i].passiveState.chaosRunTurns == 0)
                        {
                            uiManager.LogDuelMessage($"Chaos Run has ended for {players[i].name}!");
                        }
                    }

                    if (players[i].passiveState.singleDiceTurns > 0)
                    {
                        players[i].passiveState.singleDiceTurns--;
                        if (players[i].passiveState.singleDiceTurns == 0)
                        {
                            uiManager.LogDuelMessage($"{players[i].name}'s rolls are back to normal!");
                        }
                    }
                }

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

    // 2.3. Skipped Turn Sequence : This happens when a player is AFK.
    private IEnumerator SkippedTurnSequence(PlayerController skippingPlayer)
    {
        PlayerController otherPlayer = players.First(p => p != skippingPlayer);
        uiManager.LogDuelMessage($"{skippingPlayer.playerName} goes AFK. {otherPlayer.playerName} wins by default.");
        yield return new WaitForSeconds(turnDelay);

        var fakeDuelResult = (winner: otherPlayer.playerId, isSpecialWin: false);
        ProcessPostDuelPassives(otherPlayer, skippingPlayer, ref fakeDuelResult);

        int[] diceSums = new int[2];
        diceSums[1] = Random.Range(1, 7) + Random.Range(1, 7);
        yield return StartCoroutine(otherPlayer.playerId == 1 ?
            BotTurnSequence
                (otherPlayer.playerId, skippingPlayer.playerId, diceSums) :
            PlayerTurnSequence
                (otherPlayer.playerId, skippingPlayer.playerId, diceSums));
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

            bool p1IsPacifist = p1.characterData?.passiveAbility == CharacterPassive.ThePacifist;
            bool p2IsPacifist = p2.characterData?.passiveAbility == CharacterPassive.ThePacifist;

            if (p1IsPacifist)
            {
                if (duelChoices[0].isSpecial && !duelChoices[1].isSpecial)
                {
                    p1.passiveState.pacifistTieBonus += 50;
                    uiManager.LogDuelMessage("Pacifist Passive: Tie bonus increased!");
                }
                p1.money += p1.passiveState.pacifistTieBonus;
                uiManager.UpdatePlayerMoney(p1);
                uiManager.LogDuelMessage($"Pacifist Passive: {p1.name} gained ${p1.passiveState.pacifistTieBonus} for the tie.");
            }
            if (p2IsPacifist) 
            {
                if (!duelChoices[0].isSpecial && duelChoices[1].isSpecial)
                {
                    p2.passiveState.pacifistTieBonus += 50;
                    uiManager.LogDuelMessage("Pacifist Passive: Tie bonus increased!");
                }
                p2.money += p2.passiveState.pacifistTieBonus;
                uiManager.UpdatePlayerMoney(p2);
                uiManager.LogDuelMessage($"Pacifist Passive: {p2.name} gained ${p2.passiveState.pacifistTieBonus} for the tie.");
            }
        }

        // Second tie check for Misfortune effect
        if (duelResult.winner == -1)
        {
            PlayerController p1 = players[0];
            PlayerController p2 = players[1];

            // Check if a player is suffering from Misfortune.
            if (p1.passiveState.tiesAreLosses)
            {
                duelResult.winner = 1; // Bot wins.
                uiManager.LogDuelMessage("Misfortune! The tie counts as a loss for Player!");
            }
            else if (p2.passiveState.tiesAreLosses)
            {
                duelResult.winner = 0; // Player wins.
                uiManager.LogDuelMessage("Misfortune! The tie counts as a loss for the Bot!");
            }
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
        int duelWinnerIndex = duelResult.winner;
        if (duelWinnerIndex == 0 && players[0].passiveState.chaosRunTurns > 0)
        {
            // If the player won during Chaos Run, make it a special win.
            duelResult.isSpecialWin = true;
        }
        int duelLoserIndex = (duelWinnerIndex == 0) ? 1 : 0;

        PlayerController winner = players[duelWinnerIndex];
        PlayerController loser = players[duelLoserIndex];

        winner.passiveState.isStunned = false;

        // If the win was special, apply the stun to the loser.
        if (duelResult.isSpecialWin)
        {
            loser.passiveState.isStunned = true;
            uiManager.LogDuelMessage($"{loser.name} is STUNNED by the special win and cannot build or upgrade this turn!");
        }

        // Duel result announcement
        uiManager.LogDuelMessage($"Player {duelWinnerIndex + 1} wins the duel" +
            (duelResult.isSpecialWin ? " with a SPECIAL WIN!" : "!"));

        // Post-duel role-based passive processing
        ProcessPostDuelPassives(winner, loser, ref duelResult);

        yield return new WaitForSeconds(turnDelay);

        // Reset dice mode to normal for next duel
        currentDiceMode = DiceMode.Normal;

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
                if (player.passiveState.isStunned)
                {
                    uiManager.LogDuelMessage("Cannot build while stunned!");
                    UpdateGameState(GameState.ProcessingComplete);
                    return;
                }
                playerMakingChoice = player;
                tileForChoice = landedNode;
                uiManager.ShowBuildPanel(player, houseData, shopData);
                UpdateGameState(GameState.WaitingForPlayerChoice);
            }
            else // It's owned 
            {
                if (landedNode.owner == player)
                {
                    if (player.passiveState.cannotUpgrade)
                    {
                        uiManager.LogDuelMessage("Cannot upgrade while sanctioned!");
                        UpdateGameState(GameState.ProcessingComplete);
                        return; // Stop the upgrade process
                    }
                    // (Eventer Exclusive) Chance card event when landing on own property
                    if (player.characterData?.passiveAbility == CharacterPassive.TheEventer)
                    {
                        TriggerChanceCardEvent(player); 
                    }
                    if (player.passiveState.isStunned)
                    {
                        uiManager.LogDuelMessage("Cannot upgrade while stunned!");
                        UpdateGameState(GameState.ProcessingComplete);
                        return;
                    }
                    if (landedNode.buildingLevel < MAX_BUILDING_LEVEL)
                    {
                        // Get the cost to upgrade TO the next level. The list is 0-indexed.
                        int upgradeCost = landedNode.currentBuilding.upgradeCosts[landedNode.buildingLevel - 1];
                        if (player.money >= upgradeCost)
                        {
                            // Player can afford it. Ask them if they want to upgrade.
                            playerMakingChoice = player;
                            tileForChoice = landedNode;
                            uiManager.ShowUpgradePanel(landedNode, upgradeCost);
                            UpdateGameState(GameState.WaitingForUpgradeChoice);
                            return; // Wait for player input
                        }
                    }
                }
                else
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
            TriggerChanceCardEvent(player);
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
            TriggerChanceCardEvent(bot);
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

    // 0. Getters & Setters

    // 0.1. Get Current Stage
    public int GetCurrentStage()
    {
        return currentStage;
    }

    // 0.2. Get UIManager
    public UIManager GetUIManager() 
    { return uiManager; }

    // 0.3. Get the amount of buildings a player has
    public int GetBuildingsCountForPlayer(PlayerController player)
    {
        int buildingCount = 0;
        buildingCount = boardManager.GetBuildingCountForPlayer(player);
        return buildingCount;
    }

    // 0.4. Get BoardManager
    public BoardManager GetBoardManager()
    {
        return boardManager;
    }

    // 0.5. Get Player Color Material
    public Material GetPlayerColorMaterial(int playerId)
    {
        return (playerId == 0) ? player1ColorMaterial : player2ColorMaterial;
    }

    // 0.6. Get Player Building Material
    public Material GetPlayerBuildingMaterial(int playerId)
    {
        return (playerId == 0) ? player1BuildingMaterial : player2BuildingMaterial;
    }

    // 0.7. Get Building Visual Prefab
    public GameObject GetBuildingVisualPrefab()
    {
        return buildingVisualPrefab;
    }
    // 0.8. Get Opponent Of Target Player
    public PlayerController GetOpponentOf(PlayerController player)
    {
        return players.FirstOrDefault(p => p.playerId != player.playerId);
    }
    // 0.9. Set Rigged Player
    public void SetRiggedPlayer(PlayerController player)
    {
        riggedPlayer = player;
    }

    // 1. ProcessPlayerRoll : Rolls dice for a given player and processes the outcome.
    private void ProcessPlayerRoll(int playerIndex, RPS_Choice rpsChoice,
                               int[] playerDiceSums, DuelChoice[] duelChoices, bool forceSpecial = false)
    {
        var rollResult = RollDiceForOutcome(rpsChoice, forceSpecial);
        CheckForBlessingTriggers(players[playerIndex],
            rollResult.dice1, rollResult.dice2);
        PlayerController player = players[playerIndex];

        if (player.passiveState.singleDiceTurns > 0)
        {
            // Player is unfocused. Force their second die to be 0.
            rollResult.dice2 = 0;
            // A single dice can't be a special/double.
            rollResult.isSpecial = false;
            uiManager.LogDuelMessage($"{player.name} is Unfocused, rolling only one dice!");
        }

        if (currentDiceMode == DiceMode.Rigged &&
            riggedPlayer.playerId == playerIndex)
        {
            // alter logic to fit Specialist passive
            rollResult.dice1 = 6;
            if (forceSpecial) rollResult.dice2 = 6;
            rollResult.isSpecial = rollResult.dice2 == 6;
        }

        int sixesRolled = (rollResult.dice1 == 6 ? 1 : 0) + (rollResult.dice2 == 6 ? 1 : 0);
        if (sixesRolled > 0) CheckQuestProgress(player, QuestType.RollSixes, sixesRolled);

        playerDiceSums[playerIndex] = rollResult.dice1 + rollResult.dice2;
        duelChoices[playerIndex] = new DuelChoice(rpsChoice, rollResult.isSpecial);
    }


    // 2. RollDiceForOutcome : With a given outcome, roll dice that match the outcome. 
    // (Provides logic for ProcessPlayerRoll)
    private (int dice1, int dice2, bool isSpecial) RollDiceForOutcome
        (RPS_Choice choice, bool forceSpecial = false)
    {
        bool isSpecial = forceSpecial || (Random.value < specialRollChance); 

        // Define all possible dice pairs for the chosen RPS type
        List<(int, int)> normalPairs = new List<(int, int)>();
        List<(int, int)> specialPairs = new List<(int, int)>();

        if (choice == RPS_Choice.Rock)
        {
            // Normal pairs that sum to 3, 6, 9, or 12
            normalPairs = new List<(int, int)> { (1, 2), (2, 1), (1, 5), (2, 4), (4, 2),
                (5, 1), (3, 6), (4, 5), (5, 4), (6, 3) };
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (3, 3), (6, 6) };
        }
        else if (choice == RPS_Choice.Paper)
        {
            // Normal pairs that sum to 4, 7, or 10
            normalPairs = new List<(int, int)> { (1, 3), (3, 1), (1, 6), (2, 5), (3, 4),
                (4, 3), (5, 2), (6, 1), (4, 6), (6, 4) };
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (2, 2), (5, 5) };
        }
        else // Scissors
        {
            // Normal pairs that sum to 2, 5, 8, or 11
            normalPairs = new List<(int, int)> { (1, 4), (2, 3), (3, 2), (4, 1), (2, 6), 
                 (3, 5), (5, 3), (6, 2), (5, 6), (6, 5) };
            // Special (double) pairs
            specialPairs = new List<(int, int)> { (1, 1), (4, 4) };
        }

        List<(int, int)> availableNormalPairs = normalPairs;
        List<(int, int)> availableSpecialPairs = specialPairs;

        if (currentDiceMode == DiceMode.Tiny)
        {
            // Filter the lists to only include pairs where BOTH dice are 1, 2, or 3.
            availableNormalPairs = normalPairs.Where
                (pair => pair.Item1 <= 3 && pair.Item2 <= 3).ToList();
            availableSpecialPairs = specialPairs.Where
                (pair => pair.Item1 <= 3 && pair.Item2 <= 3).ToList();
        }
        else if (currentDiceMode == DiceMode.Giant)
        {
            // Filter the lists to only include pairs where BOTH dice are 4, 5, or 6.
            availableNormalPairs = normalPairs.Where
                (pair => pair.Item1 >= 4 && pair.Item2 >= 4).ToList();
            availableSpecialPairs = specialPairs.Where
                (pair => pair.Item1 >= 4 && pair.Item2 >= 4).ToList();
        }


        // Pick a roll from the correct list

        // - Special roll
        if (isSpecial && specialPairs.Any())
        {
            // Pick a random pair from the special list
            var chosenPair = specialPairs[Random.Range(0, specialPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, true);
        }
        else // Normal roll
        {
            var chosenPair = normalPairs[Random.Range(0, normalPairs.Count)];
            return (chosenPair.Item1, chosenPair.Item2, false);
        }
    }

    // 3. DetermineDuelWinner : Given two players' choices, determine the winner.
    private (int winner, bool isSpecialWin) DetermineDuelWinner(DuelChoice p1, DuelChoice p2)
    {
        // (Pacifist Exclusive) If either player is a Pacifist and both chose the same type, it's a tie.
        bool p1IsPacifist = players[0].characterData?.passiveAbility == CharacterPassive.ThePacifist;
        bool p2IsPacifist = players[1].characterData?.passiveAbility == CharacterPassive.ThePacifist;

        if ((p1IsPacifist || p2IsPacifist) && p1.type == p2.type)
        {
            return (-1, false);
        }

        // (Overwhelming Power Exclusive) Check if either player has the Overwhelming Power buff.
        PlayerController player1 = players[0];
        PlayerController player2 = players[1];

        if (player1.passiveState.hasOverwhelmingPower && p1.isSpecial && !p2.isSpecial)
        {
            player1.passiveState.hasOverwhelmingPower = false;
            return (0, true);
        }

        if (player2.passiveState.hasOverwhelmingPower && p2.isSpecial && !p1.isSpecial)
        {
            player2.passiveState.hasOverwhelmingPower = false;
            return (1, true);
        }

        if (p1.type != p2.type)
        {
            if ((p1.type == RPS_Choice.Rock && p2.type == RPS_Choice.Scissors) ||
                (p1.type == RPS_Choice.Paper && p2.type == RPS_Choice.Rock) ||
                (p1.type == RPS_Choice.Scissors && p2.type == RPS_Choice.Paper))
            {
                return (0, p1.isSpecial); 
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

    // 4. ProcessWinStreaks : Manage win streaks and apply bonuses.
    private void ProcessWinStreaks(PlayerController winner,
        PlayerController loser)
    {
        if (loser.passiveState.winStreak >= 2)
        {
            uiManager.LogDuelMessage($"{loser.name}'s win streak of {loser.passiveState.winStreak} was broken!");
        }
        loser.passiveState.winStreak = 0;

        winner.passiveState.winStreak++;

        if (winner.passiveState.winStreak >= 2)
        {
            int bonusAmount = (winner.passiveState.winStreak - 1) * 50;
            // (Duelist Exclusive) All money gained (including bonus)
            // is multiplied based on win streak
            if (winner.characterData.passiveAbility ==
                CharacterPassive.TheDuelist)
            {
                if (winner.passiveState.winStreak >= 5) bonusAmount *= 5;
                else if (winner.passiveState.winStreak >= 3) bonusAmount *= 2;
            }

            winner.money += bonusAmount;
            uiManager.UpdatePlayerMoney(winner);
            uiManager.LogDuelMessage($"{winner.name} is on a {winner.passiveState.winStreak} win streak! +${bonusAmount} bonus!");
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }
    }

    // 4. ProcessPostDuelPassives : Handle passive abilities that trigger after a duel.
    // (Helper for RunDuelAndTurns)
    private void ProcessPostDuelPassives(PlayerController winner, PlayerController loser,
        ref (int winner, bool isSpecialWin) duelResult)
    {
        if (winner.passiveState.tiesAreLosses && duelResult.isSpecialWin)
        {
            winner.passiveState.tiesAreLosses = false; // Lift the debuff!
            uiManager.LogDuelMessage($"Misfortune has been lifted from {winner.name}!");
        }
        ProcessWinStreaks(winner, loser);
        ProcessGamblerPassive(winner, loser, duelResult);
        ProcessLuckyOnePassive(winner, duelResult.isSpecialWin);
        ProcessNegotiatorPassive(winner, loser, ref duelResult);

        if (duelResult.isSpecialWin)
        {
            CheckQuestProgress(winner, QuestType.GetSpecialRolls);

            RPS_Choice winnerChoice = lastDuelChoices[winner.playerId].type;

            if (winnerChoice == RPS_Choice.Rock) LiftCurse(winner, CurseType.CurseOfRock);
            if (winnerChoice == RPS_Choice.Paper) LiftCurse(winner, CurseType.CurseOfPaper);
            if (winnerChoice == RPS_Choice.Scissors) LiftCurse(winner, CurseType.CurseOfScissors);
        }
    }

    // 5. ProcessPostMovementPassives : Handle passive abilities that trigger after movement.
    private void ProcessPostMovementPassives(PlayerController player, int movementAmount)
    {
        int goTileIndex = (player.playerId == 0) ? 0 : boardManager.GetPathLength() / 2;
        // "Landed on Go" quest check
        if (player.currentPathIndex == goTileIndex)
        {
            CheckQuestProgress(player, QuestType.LandOnGo);
        }

        // (Architect & Thief) Did the player pass their Go tile? (this logic is identical for both)
        if (player.characterData?.passiveAbility == CharacterPassive.TheArchitect ||
            player.characterData?.passiveAbility == CharacterPassive.TheThief)
        {
            int previousPathIndex = (player.currentPathIndex - movementAmount + boardManager.GetPathLength()) % boardManager.GetPathLength();

            bool passedGo = (previousPathIndex > player.currentPathIndex);

            if (passedGo)
            {
                player.passiveState.lapsCompleted++;

                if (player.characterData.passiveAbility == CharacterPassive.TheArchitect)
                {
                    UpgradeRandomBuildingForPlayer(player);
                }

                if (player.characterData.passiveAbility == CharacterPassive.TheThief)
                {
                    if (player.passiveState.thiefLapIsClean)
                    {
                        uiManager.LogDuelMessage("Thief Passive: Clean lap completed! Heist successful!");
                        player.passiveState.hasTaxImmunity = true;
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

    //6. Chance Card Logic
    // 6.1. ChooseRandomCategory : Randomly choose a card category based on defined probabilities.
    private CardCategory ChooseRandomCategory(PlayerController player)
    {
        float goodChance = 0.4f;
        float badChance = 0.2f;

        // (Eventer Exclusive) Eventer passive modifies chances based on cards drawn this game.
        if (player.characterData?.passiveAbility == CharacterPassive.TheEventer)
        {
            if (player.passiveState.eventerCardsDrawn >= 15) { goodChance = 0.6f; badChance = 0.0f; }
            else if (player.passiveState.eventerCardsDrawn >= 5) { goodChance = 0.6f; badChance = 0.2f; }
        }

        float randomRoll = Random.value;
        if (randomRoll < goodChance) return CardCategory.Good;
        if (randomRoll < goodChance + badChance) return CardCategory.Bad;
        return CardCategory.Unknown;
    }

    // 6.2. DrawChanceCard : Draw a chance card from the deck, given a category.
    private ChanceCardData DrawChanceCard(CardCategory category)
    {
        List<ChanceCardData> chosenDeck = goodChanceCards;
        switch (category)
        {
            case CardCategory.Good:
                if (goodChanceCards.Any()) chosenDeck = goodChanceCards;
                break;
            case CardCategory.Bad:
                if (badChanceCards.Any()) chosenDeck = badChanceCards;
                else Debug.LogWarning("Tried to draw a Bad card, but the deck is empty!");
                break;
            case CardCategory.Unknown:
                if (unknownChanceCards.Any()) chosenDeck = unknownChanceCards;
                else Debug.LogWarning("Tried to draw an Unknown card, but the deck is empty!");
                break;
        }

        return chosenDeck[Random.Range(0, chosenDeck.Count)];
    }

    //6.3. DrawAndApplyCardOfCategory : Draw a card of a specific category and apply its effect.
    public void DrawAndApplyCardOfCategory(PlayerController player, CardCategory category)
    {
        ChanceCardData drawnCard = DrawChanceCard(category);
        if (drawnCard != null)
        {
            drawnCard.effectLogic.Apply(this, player);
        }
    }

    // MovePlayerSmoothly : Coroutine to move a player smoothly to the next tile.
    // (Helper for MovePlayerTileByTile)

    // 7. TriggerChanceCardEvent : Draw a card and apply the effect.
    private void TriggerChanceCardEvent(PlayerController player)
    {
        UpdateGameState(GameState.DrawingChanceCard);

        // (Eventer Exclusive) This counts as a card drawn for Eventer passive.
        if (player.characterData?.passiveAbility == CharacterPassive.TheEventer)
        {
            player.passiveState.eventerCardsDrawn++;
        }

        CardCategory chosenCategory = ChooseRandomCategory(player);
        currentlyDrawnCard = DrawChanceCard(chosenCategory);

        playerDrawingCard = player;

        if (currentlyDrawnCard.effectLogic != null)
        {
            currentlyDrawnCard.effectLogic.Apply(this, playerDrawingCard);
        }
        StartCoroutine(uiManager.ShowChanceCardCoroutine(currentlyDrawnCard));
    }

    // 7.1. GrantBlessing : Grant a blessing to a player (branch of Chance Card)
    public void GrantBlessing(PlayerController player, BlessingType blessing)
    {
        // Check for duplicates
        if (player.passiveState.acquiredBlessings.Contains(blessing))
        {
            GrantRandomUnownedBlessing(player);
            return;
        }

        // Grant the new blessing
        player.passiveState.acquiredBlessings.Add(blessing);
        uiManager.LogDuelMessage($"{player.name} has been granted the {blessing.ToString()}!");

        if (player.passiveState.acquiredBlessings.Count(b => b != BlessingType.None) >= 6)
        {
            if (!player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfSeven))
            {
                uiManager.LogDuelMessage($"All Blessings acquired! {player.name} receives the ultimate blessing!");
                player.passiveState.acquiredBlessings.Add(BlessingType.BlessingOfSeven);

                ApplyBlessingOfSeven(player);
            }
        }
    }

    // 7.2. GrantRandomUnownedBlessing : Grant a random blessing that the player doesn't already have.
    public void GrantRandomUnownedBlessing(PlayerController player)
    {
        List<BlessingType> allPossibleBlessings = new List<BlessingType>
        {
            BlessingType.BlessingOfOne,
            BlessingType.BlessingOfTwo,
            BlessingType.BlessingOfThree,
            BlessingType.BlessingOfFour,
            BlessingType.BlessingOfFive,
            BlessingType.BlessingOfSix
        };

        // Find all the blessings the player doesn't have.
        List<BlessingType> unownedBlessings = allPossibleBlessings
            .Except(player.passiveState.acquiredBlessings)
            .ToList();

        // Grant a new blessing if possible
        if (unownedBlessings.Any())
        {
            // Pick a random blessing
            BlessingType newBlessing = unownedBlessings[Random.Range(0, unownedBlessings.Count)];
            GrantBlessing(player, newBlessing);
            uiManager.LogDuelMessage($"{player.name} has been granted the {newBlessing.ToString()}!");
        }
        else
        {
            uiManager.LogDuelMessage("You have already acquired all Blessings!");
        }
        return;
    }

    // 7.3. CheckForBlessingTriggers : Check if any blessings trigger based on the dice roll.
    private void CheckForBlessingTriggers(PlayerController player,
        int dice1, int dice2)
    {
        if (dice1 != dice2) return;

        // --- Blessing of One Trigger (Double 1) ---
        if (dice1 == 1 && player.passiveState.acquiredBlessings.
            Contains(BlessingType.BlessingOfOne))
        {
            player.passiveState.blessingOfOneTaxMultiplier += 1.0f;
            uiManager.LogDuelMessage($"Blessing of One! {player.name}'s tax multiplier is now x{player.passiveState.blessingOfOneTaxMultiplier}!");
        }

        // --- Blessing of Two Trigger (Double 2) ---
        if (dice1 == 2 && player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfTwo))
        {
            player.passiveState.winStreak += 2;
            uiManager.LogDuelMessage($"Blessing of Two! {player.name}'s win streak boosted to {player.passiveState.winStreak}!");
        }

        // --- Blessing of Three Trigger (Double 3) ---
        if (dice1 == 3 && player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfThree))
        {
            BuildingData rewardBuilding = shopData; // to-do : random building

            uiManager.LogDuelMessage($"Blessing of Three! {player.name} gets a free Level 3 {rewardBuilding.buildingName}!");

            AttemptFallbackBuild(player, rewardBuilding, 3);
        }

        // --- Blessing of Four Trigger (Double 4) ---
        if (dice1 == 4 && player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfFour))
        {
            player.passiveState.blessingOfFourExtraSteps += 4;
            uiManager.LogDuelMessage($"Blessing of Four! Gained 4 bonus steps! (Total: {player.passiveState.blessingOfFourExtraSteps})");
        }

        // --- Blessing of Five Trigger (Double 5) ---
        if (dice1 == 5 && player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfFive))
        {
            int moneyMultiplier = (int)Mathf.Pow(5, player.passiveState.blessingOfFiveCounter);
            int moneyGained = 50 * moneyMultiplier;

            // Apply the cap.
            moneyGained = Mathf.Min(moneyGained, 31250);

            player.money += moneyGained;
            player.passiveState.blessingOfFiveCounter++;

            uiManager.UpdatePlayerMoney(player);
            uiManager.LogDuelMessage($"Blessing of Five! Gained ${moneyGained}!");
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }

        // --- Blessing of Six Trigger (Double 6) ---
        if (dice1 == 6 && player.passiveState.acquiredBlessings.Contains(BlessingType.BlessingOfSix))
        {
            SetNextDuelDiceMode(DiceMode.Rigged);
            SetRiggedPlayer(player);
            uiManager.LogDuelMessage($"Blessing of Six! Your first dice will be 6 next turn!");
        }
    }
    // 7.4. ApplyBlessingOfSeven : Apply the effects of the Blessing of Seven.
    private void ApplyBlessingOfSeven(PlayerController blessedPlayer)
    {
        uiManager.LogDuelMessage("THE BLESSING OF SEVEN DESCENDS! THE BOARD IS REMADE!");

        // Wipe the entire board
        boardManager.ResetAllTilesToDefault();

        // Get all possible places to build
        List<TileNode> allBuildableTiles = boardManager.GetAllBuildableTiles();

        BuildingData buildingToGrant = shopData; // Grant the best building type
        int levelToSet = MAX_BUILDING_LEVEL; // Grant max-level buildings

        Material ownerColor = GetPlayerColorMaterial(blessedPlayer.playerId);
        Material buildingColor = GetPlayerBuildingMaterial(blessedPlayer.playerId);
        GameObject buildingPrefab = GetBuildingVisualPrefab();

        foreach (TileNode tile in allBuildableTiles)
        {
            tile.owner = blessedPlayer;
            tile.currentBuilding = buildingToGrant;
            tile.buildingLevel = levelToSet;

            // Update all the visuals
            boardManager.UpdateTileVisual(tile, ownerColor);
            boardManager.UpdatePlotVisual(tile, ownerColor);
            boardManager.UpdateBuildingVisual(tile, buildingColor, buildingPrefab);
        }
    }

    // 7.5. InflictCurse : Inflict a curse on a player (branch of Chance Card)
    public void InflictCurse(PlayerController player, CurseType curse)
    {
        if (player.passiveState.activeCurses.Contains(curse))
        {
            InflictRandomCurse(player);
        }

        player.passiveState.activeCurses.Add(curse);
        uiManager.LogDuelMessage($" {player.name} is inflicted with {curse.ToString()}");

        // Apply the immediate effect of Curse of Scissors
        if (curse == CurseType.CurseOfScissors)
        {
            uiManager.LogDuelMessage("All buildings are temporarily considered Level 2 or lower!");
        }

        if (player.passiveState.activeCurses.Count >= 3)
        {
            // Unleash the ultimate curse!
            ApplyCurseOfDoom(player);
        }
    }

    // 7.6. InflictRandomCurse : Inflict a random curse.
    public void InflictRandomCurse(PlayerController player)
    {
            List<CurseType> allCurses = new List<CurseType>
        {
            CurseType.CurseOfRock,
            CurseType.CurseOfPaper,
            CurseType.CurseOfScissors
        };

        List<CurseType> unownedCurses = allCurses
        .Except(player.passiveState.activeCurses)
        .ToList();

        if (unownedCurses.Any())
        {
            CurseType newCurse = unownedCurses[Random.Range(0, unownedCurses.Count)];
            InflictCurse(player, newCurse);
uiManager.LogDuelMessage($" {player.name} is inflicted with {newCurse.ToString()}");            
        }
        else
        {
            uiManager.LogDuelMessage("All Curses are already active!");
        }
        return;
    }

    // 7.7. ApplyCurseOfDoom : Apply the effects of the Curse of Doom.
    private void ApplyCurseOfDoom(PlayerController cursedPlayer)
    {
        uiManager.LogDuelMessage($"{cursedPlayer.name} receives CURSE OF DOOM!");
        
        List<TileNode> propertiesToDestroy = boardManager.GetAllPropertyNodesForPlayer(cursedPlayer);

        uiManager.LogDuelMessage($"Destroying {propertiesToDestroy.Count} properties belonging to {cursedPlayer.name}!");

        // Loop through and reset each one.
        foreach (var node in propertiesToDestroy)
        {
            boardManager.ResetTileToDefault(node);
        }

        int moneyLost = cursedPlayer.money / 2;
        cursedPlayer.money -= moneyLost;
        uiManager.UpdatePlayerMoney(cursedPlayer);
        uiManager.LogDuelMessage($"{cursedPlayer.name} loses half their money! (-${moneyLost})");

        List<CurseType> cursesToLift = new List<CurseType>(cursedPlayer.passiveState.activeCurses);
        foreach (var curse in cursesToLift)
        {
            LiftCurse(cursedPlayer, curse);
        }

        uiManager.LogDuelMessage("The Curses have been lifted... for now.");
    }

    // 7.8. LiftCurse : Remove a specific curse from a player.
    public void LiftCurse(PlayerController player, CurseType curse)
    {
        // Check if the player actually has the curse to lift.
        if (player.passiveState.activeCurses.Contains(curse))
        {
            player.passiveState.activeCurses.Remove(curse);
            uiManager.LogDuelMessage($"The {curse.ToString()} has been lifted from {player.name}!");
        }
    }

    // 7.9. StartQuest : Start a quest for a player (branch of Chance Card)
    public void StartQuest(Effect_StartQuest questEffect)
    {
        activeQuest = new Quest(questEffect.questToStart, questEffect.questReward, questEffect.questTargetValue);
        uiManager.LogDuelMessage($"A new competitive quest has begun: {questEffect.name}!");
        // TODO: Update the Quest UI to show progress for BOTH players.
    }

    // 7.10. CheckQuestProgress : Check and update quest progress for a player.
    private void CheckQuestProgress(PlayerController player, QuestType eventType, int eventValue = 1)
    {
        if (activeQuest == null || activeQuest.Type != eventType) return;

        // Increment the progress for the player who triggered the event.
        activeQuest.progress[player.playerId] += eventValue;

        // TODO: Update the Quest UI with the new progress for the player.

        // Check if THIS player has now completed the quest.
        if (activeQuest.progress[player.playerId] >= activeQuest.TargetValue)
        {
            CompleteQuest(player);
        }
    }
    
    // 7.11. CompleteQuest : Handle quest completion and reward distribution.
    private void CompleteQuest(PlayerController winner)
    {
        if (activeQuest == null) return;

        uiManager.LogDuelMessage($"Quest Complete! {winner.name} was the first to finish and claims the reward!");
        ApplyReward(winner, activeQuest.Reward);

        // The quest is over for everyone.
        activeQuest = null;

        // TODO: Hide the Quest UI.
    }

    // 8. AttemptFallbackBuild : Attempt to build a free building for a player.
    public void AttemptFallbackBuild(PlayerController player, BuildingData buildingToGrant = null, int levelToSet = 1)
    {
        if (buildingToGrant == null)
        {
            buildingToGrant = houseData;
        }

        // Find an empty, buildable tile on the board.
        TileNode emptyTile = boardManager.GetRandomEmptyBuildableTile();

        if (emptyTile != null)
        {
            // Assign ownership and the specified building/level.
            emptyTile.owner = player;
            emptyTile.currentBuilding = buildingToGrant;
            emptyTile.buildingLevel = levelToSet;

            // Update visuals.
            Material ownerMat = GetPlayerColorMaterial(player.playerId);
            Material buildingMat = GetPlayerBuildingMaterial(player.playerId);
            GameObject buildingPrefab = GetBuildingVisualPrefab();

            boardManager.UpdateTileVisual(emptyTile, ownerMat);
            boardManager.UpdateBuildingVisual(emptyTile, buildingMat, buildingPrefab);

            uiManager.LogDuelMessage(
                $"{player.name} received a free Level {levelToSet} {buildingToGrant.buildingName}!"
            );
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }
        else
        {
            uiManager.LogDuelMessage("No empty tiles available to build on!");
        }
    }

    // 9. SetNextDuelDiceMode : Set the dice mode for the next duel.
    public void SetNextDuelDiceMode(DiceMode mode)
    {
        currentDiceMode = mode;
        // Only announce if it's not normal dice
        if (mode != DiceMode.Normal)
        {
            uiManager.LogDuelMessage($"{mode} Dice active for the next duel!");
        }
    }

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

            // Add the time since the last frame to the counter.
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        player.transform.position = targetPosition;
    }

    // 10. HandleTaxPayment : Perform tax calculation to transfer from one player to other
    private void HandleTaxPayment(PlayerController payingPlayer, PlayerController receivingPlayer,
        TileNode propertyNode)
    {
        // Get the building data and level from the node
        BuildingData building = propertyNode.currentBuilding;
        int level = propertyNode.buildingLevel;
        if (propertyNode.owner.passiveState.activeCurses.Contains(CurseType.CurseOfScissors))
        {
            level = Mathf.Min(level, 2); // Temporarily cap the level
        }

        // Calculate the tax amount
        float taxRate = building.GetTaxRateForLevel(level);
        int taxAmount = Mathf.RoundToInt(building.buildingCost * taxRate);

        if (receivingPlayer.passiveState.activeCurses.Contains(CurseType.CurseOfRock))
        {
            uiManager.LogDuelMessage($"{receivingPlayer.name} cannot collect tax due to the Curse of Rock!");
        }

        if (receivingPlayer.passiveState.acquiredBlessings.
            Contains(BlessingType.BlessingOfOne))
        {
            taxAmount += Mathf.RoundToInt(taxAmount * 
                receivingPlayer.passiveState.blessingOfOneTaxMultiplier);
        }

        // (Major Exclusive) Double tax paid
        if (payingPlayer.characterData.passiveAbility == CharacterPassive.TheMajor)
        {
            taxAmount *= 2;
        }

        // (Duelist Exclusive) All money gained (including tax collected)
        // is multiplied based on win streak
        if (receivingPlayer.characterData.passiveAbility ==
            CharacterPassive.TheDuelist)
        {
            if (receivingPlayer.passiveState.winStreak >= 5) taxAmount *= 5;
            else if (receivingPlayer.passiveState.winStreak >= 3) taxAmount *= 2;
        }

        // Check for bankruptcy
        if (payingPlayer.money < taxAmount)
        {
            uiManager.LogDuelMessage($"{payingPlayer.name} is bankrupt! {receivingPlayer.name} wins!");
            UpdateGameState(GameState.GameOver);
            return;
        }

        // Check for tax immunity
        if (payingPlayer.passiveState.hasTaxImmunity)
        {
            payingPlayer.passiveState.hasTaxImmunity = false;
            uiManager.LogDuelMessage($"{payingPlayer.name} used Tax Immunity to avoid paying tax!");
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        }
        else
        {
            int extraPenalty = 0;
            if (payingPlayer.passiveState.activeCurses.Contains(CurseType.CurseOfPaper))
            {
                // The penalty is equal to the tax amount.
                extraPenalty = taxAmount;
                uiManager.LogDuelMessage($"Curse of Paper! {payingPlayer.name} loses an extra ${extraPenalty}!");
            }
            // Perform the transaction
            payingPlayer.money -= (taxAmount + extraPenalty);
            receivingPlayer.money += (receivingPlayer.passiveState.incomeIsHalved ? taxAmount / 2 : taxAmount);
            if (payingPlayer.passiveState.cannotUpgrade)
            {
                payingPlayer.passiveState.cannotUpgrade = false; // Lift the debuff
                uiManager.LogDuelMessage($"Sanctions have been lifted for {payingPlayer.name}!");
            }
            if (receivingPlayer.passiveState.cannotGainPassive)
            {
                receivingPlayer.passiveState.cannotGainPassive = false; // Lift the debuff
                uiManager.LogDuelMessage($"The Drought has ended for {receivingPlayer.name}!");
            }
            CheckQuestProgress(receivingPlayer, QuestType.CollectTaxes);
            uiManager.UpdatePlayerMoney(payingPlayer);
            uiManager.UpdatePlayerMoney(receivingPlayer);
            uiManager.LogDuelMessage
                ($"{payingPlayer.name} paid ${taxAmount} tax to {receivingPlayer.name}!");
        }

        // (Thief Exclusive) Thief pays tax, so their lap is no longer clean
        if (payingPlayer.characterData?.passiveAbility == CharacterPassive.TheThief)
        {
            payingPlayer.passiveState.thiefLapIsClean = false;
        }
    }

    // 11. CollectPassiveIncome : Called at the end of each turn to collect income from properties.
    private void CollectPassiveIncome()
    {
        int[] incomeThisTurn = new int[2] { 0, 0 };

        // Get a list of all properties on the board
        List<TileNode> allProperties = boardManager.GetAllPropertyNodes();

        foreach (TileNode property in allProperties)
        {
            if (property.owner != null && property.currentBuilding != null)
            {
                if (property.owner.passiveState.cannotGainPassive)
                {
                    continue;
                }
                int level = property.buildingLevel;
                if (property.owner.passiveState.activeCurses.Contains(CurseType.CurseOfScissors))
                {
                    level = Mathf.Min(level, 2); // Temporarily cap the level
                }

                // Calculate Gross Income -> exponential growth
                float currentIncomeRate =
                    property.currentBuilding.baseIncomeRate * Mathf.Pow(2, level - 1);
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
                    if (property.owner.passiveState.winStreak >= 5)
                    {
                        grossIncome *= 5; // Pentuple income from 5+ streak
                    }
                    else if (property.owner.passiveState.winStreak >= 3)
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

    // 12. AdvanceToNextStage : Called after every 10 duels to increase the stage and tax multiplier.
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

        // Trigger the Power-Up selection UI.
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

    // 13. ApplyReward : Applies a reward to a player
    // Helper for ProcessGamblerCashOut and potentially other places.
    private void ApplyReward(PlayerController player, RewardData reward)
    {
        uiManager.LogDuelMessage($"{player.name} got a bonus reward: {reward.rewardName}!");
        switch (reward.type)
        {
        }
    }

    // 14. MaximumLevelBuildingCheck : Check if a player has built a max-level building.
    public void MaximumLevelBuildingCheck(PlayerController player, TileNode property)
    {
        if (property.buildingLevel == MAX_BUILDING_LEVEL)
        {
            playerMakingChoice.passiveState.incomeIsHalved = false; // Lift the curse!
            uiManager.LogDuelMessage("Recession has ended by reaching a max-level building!");
            // (Major Exclusive) If Major building level is 4, upgrade to 5 instead.
            if (playerMakingChoice.characterData?.passiveAbility == CharacterPassive.TheMajor)
            {
                tileForChoice.buildingLevel++;
            }
        }
    }

    // 15. BalanceAllPlayerMoney : Evenly distribute total money among all players.
    public void BalanceAllPlayerMoney()
    {
        int totalMoney = 0;
        foreach (var player in players)
        {
            totalMoney += player.money;
        }

        int splitAmount = totalMoney / players.Count;

        foreach (var player in players)
        {
            player.money = splitAmount;
            uiManager.UpdatePlayerMoney(player);
        }
        uiManager.LogDuelMessage($"Both players now have ${splitAmount}.");
    }

    // B. Role-exclusive Logic Helpers

    // 1. (Architect Exclusive) Upgrade a random building
    private void UpgradeRandomBuildingForPlayer(PlayerController player)
    {
        int maxLevel = MAX_BUILDING_LEVEL;
        if (player.passiveState.cannotUpgrade)
        {
            uiManager.LogDuelMessage($"{player.name} (Architect) cannot upgrade buildings this turn.");
            return;
        }

        if (player.passiveState.activeCurses.Contains(CurseType.CurseOfScissors))
        {
            // Override the max level.
            maxLevel = 2;
        }

        List<TileNode> ownedProperties = boardManager.GetAllPropertyNodes().
            FindAll(node => node.owner == player);
        if (ownedProperties.Any())
        {
            // Find a random property and "level it up" (we'll define what that means later)
            TileNode randomProperty = ownedProperties[Random.Range(0, ownedProperties.Count)];
            randomProperty.buildingLevel = Mathf.Min(randomProperty.buildingLevel + 1, maxLevel);
            MaximumLevelBuildingCheck(player, randomProperty);
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

    // 7. (Lucky One Exclusive) Process the bonus money gain on winning a duel.
    private void ProcessLuckyOnePassive(PlayerController winner, bool isSpecialWin)
    {
        // Check if the winner is The Lucky One.
        if (winner.characterData?.passiveAbility != CharacterPassive.TheLuckyOne) return;

        // --- Calculate the Bonus ---
        // The base bonus increases with each stage.
        int baseBonus = 50 * (currentStage + 1); // Stage 0 = $50, Stage 1 = $100, etc.

        int finalBonus = baseBonus;

        // The bonus triples on a special win.
        if (isSpecialWin)
        {
            finalBonus *= 3;
            uiManager.LogDuelMessage("Lucky One Passive: Special win bonus tripled!");
        }

        // --- Grant the Reward ---
        winner.money += finalBonus;
        uiManager.LogDuelMessage($"{winner.name} (Lucky One) gained a bonus of ${finalBonus}!");
        SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);
        uiManager.UpdatePlayerMoney(winner);
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

    private int stepsUsedThisTurn = 0;

    public void PlayerConfirmedBonusSteps(int stepsToUse)
    {
        if (currentState != GameState.WaitingForBonusStepsChoice) return;

        PlayerController player = players[0];

        // We still clamp the value as a safety measure.
        stepsToUse = Mathf.Clamp(stepsToUse, 0, player.passiveState.bonusStepsPool);

        stepsUsedThisTurn = stepsToUse;
        player.passiveState.bonusStepsPool -= stepsToUse;

        UpdateGameState(GameState.PlayerMoving);
    }

    // PlayerChoseToUpgrade : Handles the case when the player chooses to upgrade their own building.
    public void PlayerChoseToUpgrade(bool accepted)
    {
        if (currentState != GameState.WaitingForUpgradeChoice) return;

        if (accepted)
        {
            int upgradeCost = tileForChoice.currentBuilding.upgradeCosts[tileForChoice.buildingLevel - 1];
            playerMakingChoice.money -= upgradeCost;
            tileForChoice.buildingLevel++;
            MaximumLevelBuildingCheck(playerMakingChoice, tileForChoice);

            uiManager.UpdatePlayerMoney(playerMakingChoice);
            uiManager.LogDuelMessage($"{playerMakingChoice.name} upgraded their building to Level {tileForChoice.buildingLevel}!");
        }

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