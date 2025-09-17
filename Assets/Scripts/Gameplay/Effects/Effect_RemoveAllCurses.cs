using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect_RemoveAllCurses", menuName = "HyperRoll/Effects/Remove All Curses")]
public class Effect_RemoveAllCurses : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        if (targetPlayer.passiveState.activeCurses.Any())
        {
            gameManager.GetUIManager().LogDuelMessage($"{targetPlayer} feels a wave of clarity... their curses are lifted!");
            List<CurseType> cursesToLift = new List<CurseType>(targetPlayer.passiveState.activeCurses);
            foreach (var curse in cursesToLift)
            {
                GameManager.Instance.LiftCurse(targetPlayer, curse);
            }
        }
        else
        {
            gameManager.GetUIManager().LogDuelMessage("You feel a sense of purity, but you were not cursed.");
        }
    }
}