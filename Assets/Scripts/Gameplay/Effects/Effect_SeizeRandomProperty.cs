using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Effect_SeizeRandomProperty", menuName = "HyperRoll/Effects/Seize Random Property")]
public class Effect_SeizeRandomProperty : EffectLogic
{
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        PlayerController opponent = gameManager.GetOpponentOf(targetPlayer);
        if (opponent == null) return;

        List<TileNode> opponentProperties = gameManager.GetBoardManager().GetAllPropertyNodesForPlayer(opponent);

        if (opponentProperties.Any())
        {
            TileNode propertyToSteal = opponentProperties[Random.Range(0, opponentProperties.Count)];

            // 5. Seize ownership!
            propertyToSteal.owner = targetPlayer;

            gameManager.GetUIManager().LogDuelMessage(
                $"{targetPlayer.name} has seized the {propertyToSteal.currentBuilding.buildingName} at tile {propertyToSteal.pathIndex} from {opponent.name}!"
            );
            SoundManager.Instance.PlaySound(SoundManager.Instance.buildPropertySfx);

            Material newOwnerColor = gameManager.GetPlayerColorMaterial(targetPlayer.playerId);
            Material newBuildingColor = gameManager.GetPlayerBuildingMaterial(targetPlayer.playerId);
            GameObject buildingPrefab = gameManager.GetBuildingVisualPrefab(); // This is needed for the upgrade case

            gameManager.GetBoardManager().UpdateTileVisual(propertyToSteal, newOwnerColor);
            gameManager.GetBoardManager().UpdatePlotVisual(propertyToSteal, newOwnerColor);
            gameManager.GetBoardManager().UpdateBuildingVisual(propertyToSteal, newBuildingColor, buildingPrefab);
        }
        else
        {
            gameManager.GetUIManager().LogDuelMessage("Hostile Takeover failed: Opponent has no properties!");
        }
    }
}