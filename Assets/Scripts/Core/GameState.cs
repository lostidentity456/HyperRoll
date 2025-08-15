public enum GameState
{
    WaitingToStart,             // The game is ready, waiting for the "Roll" button.
    DuelRolling,                  // The dice are rolling.
    DuelCompare,               // The results are shown, winner is determined.
    PlayerMoving,               // A player piece is moving from one tile to another.
    ProcessingTile,             // What to do on the landed tile?
    WaitingForPlayerChoice, // The "Build?" panel is on screen, waiting for a click.
    ProcessingComplete,   // The player has made their choice
    BotTurn,                    // The bot is "thinking" and taking its turn.
    TurnEnd                     // The turn is over, preparing for the next.
}