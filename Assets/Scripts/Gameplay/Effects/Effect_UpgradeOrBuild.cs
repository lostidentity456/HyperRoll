using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "Effect_UpgradeOrBuild", menuName = "HyperRoll/Effects/Upgrade Or Build")]
public class Effect_UpgradeOrBuild : EffectLogic
{
    [Header("Fallback Settings")]
    [SerializeField] private BuildingData fallbackBuilding;
    public override void Apply(GameManager gameManager, PlayerController targetPlayer)
    {
        const int MAX_BUILDING_LEVEL = 4;

        List<TileNode> allOwnedProperties = gameManager.GetBoardManager().
            GetAllPropertyNodesForPlayer(targetPlayer);

        // Find upgradable properties only (level < max).
        List<TileNode> upgradableProperties = allOwnedProperties
            .Where(node => node.buildingLevel < MAX_BUILDING_LEVEL)
            .ToList();

        if (upgradableProperties.Any())
        {
            TileNode propertyToUpgrade = upgradableProperties[Random.
                Range(0, upgradableProperties.Count)];

            propertyToUpgrade.buildingLevel++;
            GameManager.Instance.MaximumLevelBuildingCheck(targetPlayer, propertyToUpgrade);

            gameManager.GetUIManager().LogDuelMessage(
                $"{targetPlayer.name} upgraded their {propertyToUpgrade.currentBuilding.buildingName} to Level {propertyToUpgrade.buildingLevel}!"
            );

        }
        else
        {
            // No properties to upgrade, attempt to build a lvl 1 house
            gameManager.AttemptFallbackBuild(targetPlayer, fallbackBuilding, 1);
        }
    }
}
