public enum ChanceCardEffect
{
    // Simple, instant effects
    GainMoneyFlat,              // Gain a fixed amount of money
    GainMoneyPerBuilding,       // Gain money based on number of owned properties

    // Complex, status effects (Buffs)
    NextDuelIsGuaranteedWin,    // The next duel is an automatic win
    NextRandomWinIsFreeBuild,   // If you win the next duel via "Random Roll", building is free
    TaxImmunity,                // Grants one-time immunity to paying rent

    // Complex, world-altering effects
    BuildRandomFreeBuilding,    // Instantly builds a free building on an empty tile
    UpgradeRandomBuilding       // Instantly upgrades an owned building for free
}
