using UnityEngine;

[CreateAssetMenu(fileName = "New TileData", menuName = "HyperRoll/Tile Data")]
public class TileData : ScriptableObject
{
    public string tileName;
    public TileType tileType;

    public enum TileType
    {
        Buildable,       // A buildable tile
        Go,          // A starting tile
        Chance,      // A chance tile
        PlayerProperty // A tile that has been built on
    }
}
