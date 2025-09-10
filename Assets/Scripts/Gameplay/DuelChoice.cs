public struct DuelChoice
{
    public RPS_Choice type; // Rock, Paper, or Scissors
    public bool isSpecial;  // Is it a special version?

    public DuelChoice(RPS_Choice type, bool isSpecial)
    {
        this.type = type;
        this.isSpecial = isSpecial;
    }
}