using System.Collections.Generic; 

public class Quest
{
    public QuestType Type { get; private set; }
    public RewardData Reward { get; private set; }
    public int TargetValue { get; private set; }

    // Each player now has their own progress. Key is playerID (0 or 1).
    public Dictionary<int, int> progress;

    public Quest(QuestType type, RewardData reward, int target)
    {
        Type = type;
        Reward = reward;
        TargetValue = target;

        // Initialize progress for both players to 0.
        progress = new Dictionary<int, int>
        {
            { 0, 0 },
            { 1, 0 }
        };
    }
}