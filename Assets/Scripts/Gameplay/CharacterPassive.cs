public enum CharacterPassive
{
    TheArchitect,   // Can level up a random building whenever passing the Go tile.
    TheAthlete,      // Can move more after duel, gain money for every space moved (increases with stage)
    TheCivillian,      // a default character with no special passive ability (used for easy Bot)
    TheDuelist,         //  Three consecutive duel wins double all the money received for the rest of the game. Five consecutive duel wins pentuples instead.
    TheEconomist,  // Gains more passive income (specifically, 100% more)
    TheEventer,       // Gain a random chance whenever landing on own building. After 5 and 15 chances, less likely to get bad chances and more likely to get good chances.
    TheGambler,      // Gain a treasure with random (1-6) values on losses, cashes out on special wins. Cash out rewards increase based on treasure values.
    TheLuckyOne,   // Gains money on duel wins, triples on special wins (increases with stage)
    TheMajor,          // Has more starting money. Can't build buildings, and taxes are doubled. 10 tiles are marked randomly every game, land on all of them to win. These tile can't be built by opponent.
    TheNegotiator,   // Tie duels are considered wins, gain a token on every win this way. Tokens can avoid enemy's special win and exchange an exclusive building.
    ThePacifist,        // All duels with same choices are considered as tie. Gain an amount of gold for each tile, and each time you tied when you are supposed to win, increase this base amount.
    //TheSeer,         // Disable one of opponent's choice, and know which choice is disabled. After stage 5, disable 2 choices instead. Random choices can't be disabled. (PvP exclusive)
    TheSpecialist,     // Every 4th roll will be a double roll. After stage 3, every 3rd roll will be a double roll.
    TheThief,           // Finishing a lap without paying taxes allows stealing passive income from opponent and a one-time tax immunity. Stolen income increases with stage
}