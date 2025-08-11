using UnityEngine;
using static TileData;

public class TileNode
{
    // The grid this node belongs to and its position
    private Grid<TileNode> grid;
    public readonly int x, y;

    // The fundamental type of this tile, set at the start
    public TileType initialTileType;

    // The state of this tile, which changes during the game
    public PlayerController owner; // Who owns the building on this tile? Null if nobody.
    public BuildingData currentBuilding;

    public TileNode(Grid<TileNode> grid, int x, int y)
    {
        this.grid = grid;
        this.x = x;
        this.y = y;
        this.initialTileType = TileType.Buildable; // Default to Buildable
        this.owner = null;
        currentBuilding = null;
    }

    public override string ToString()
    {
        return initialTileType.ToString(); // For debugging
    }
}