using UnityEngine;

[CreateAssetMenu(fileName = "Effect_DrawGoodAndBad", menuName = "HyperRoll/Effects/Draw Good And Bad")]
public class Effect_DrawGoodAndBad : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        gameManager.GetUIManager().LogDuelMessage("Double Dip! Drawing one good and one bad card...");

        gameManager.DrawAndApplyCardOfCategory(targetPlayer, CardCategory.Good);
        gameManager.DrawAndApplyCardOfCategory(targetPlayer, CardCategory.Bad);
    }
}