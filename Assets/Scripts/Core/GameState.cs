public enum GameState
{
    WaitingForPlayerTurnStart,   // The game is ready, waiting for the "Roll" button.
    DuelRolling,                       // The dice are rolling.
    PlayerMoving,                    // A player piece is moving from one tile to another.
    ProcessingTile,                   // What to do on the landed tile?
    DrawingChanceCard,           // A chance card is being drawn and shown to the player.
    WaitingForAthleteChoice,     // Athlete's exclusive state
    WaitingForPlayerChoice,       // The "Build?" panel is on screen, waiting for a click.
    WaitingForBonusStepsChoice, 
    WaitingForUpgradeChoice,    // The "Upgrade?" panel is on screen
    ProcessingComplete,           // The player has made their choice
    BotTurn,                          // The bot is "thinking" and taking its turn.
    TurnEnd,                          // The turn is over, preparing for the next.
    GameOver
}