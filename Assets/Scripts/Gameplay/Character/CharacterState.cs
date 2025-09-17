using System.Collections.Generic;

[System.Serializable]
public class CharacterState
{
    public int lapsCompleted = 0;
    public bool isStunned = false;
    public int gamblerTreasures = 0;
    public int negotiatorTokens = 0;
    public int duelsSinceLastSpecial = 0;
    public int winStreak = 0;
    public bool thiefLapIsClean = true;
    public bool willStealNextIncome = false;
    public int pacifistTieBonus = 50;
    public int eventerCardsDrawn = 0;

    // Blessings tracker
    public List<BlessingType> acquiredBlessings = new List<BlessingType>();
    // Specific blessing effects
    public float blessingOfOneTaxMultiplier = 0f;
    public int blessingOfFourExtraSteps = 0;
    public int blessingOfFiveCounter = 0;

    // Curses tracker
    public List<CurseType> activeCurses = new List<CurseType>();

    // Status effect flags (from Chance cards)
    public int chaosRunTurns = 0;
    public bool incomeIsHalved = false;
    public bool cannotUpgrade = false;
    public bool cannotGainPassive = false;
    public bool tiesAreLosses = false;
    public int singleDiceTurns = 0;
    public bool isAFK = false;
    public int bonusStepsPool = 0;
    public bool hasGuaranteedWin = false;
    public bool hasTaxImmunity = false;
    public bool hasOverwhelmingPower = false;
}