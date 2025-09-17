using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Effect_SellAllBuildings", menuName = "HyperRoll/Effects/Sell All Buildings")]
public class Effect_SellAllBuildings : EffectLogic
{
    [Range(0, 3)]
    public float sellMultiplier = 1.5f;

    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        // Get all properties owned by the target player
        List<TileNode> properties = gameManager.GetBoardManager().GetAllPropertyNodesForPlayer(targetPlayer);
        int totalValue = 0;

        foreach (var node in properties)
        {
            totalValue += node.currentBuilding.buildingCost;

            // Reset the node's data
            node.owner = null;
            node.currentBuilding = null;
            node.buildingLevel = 0;

            gameManager.GetBoardManager().ResetTileToDefault(node);
        }

        int moneyGained = Mathf.RoundToInt(totalValue * sellMultiplier);
        targetPlayer.money += moneyGained;

        gameManager.GetUIManager().LogDuelMessage($"Liquidated {properties.Count} properties for ${moneyGained}!");
        gameManager.GetUIManager().UpdatePlayerMoney(targetPlayer);
    }
}