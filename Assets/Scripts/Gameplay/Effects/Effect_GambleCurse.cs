using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_GambleCurse", menuName = "HyperRoll/Effects/Gamble Curse")]
public class Effect_GambleCurse : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        if (targetPlayer.passiveState.activeCurses.Any())
        {
            // Player has curses, remove one at random.
            CurseType toRemove = targetPlayer.passiveState.activeCurses[Random.Range(0, targetPlayer.passiveState.activeCurses.Count)];
            gameManager.LiftCurse(targetPlayer, toRemove);
        }
        else // No curses, inflict a random curse.
        {
            GameManager.Instance.InflictRandomCurse(targetPlayer);
        }
    }
}