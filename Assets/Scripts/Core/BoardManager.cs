using System.Collections.Generic;
using UnityEngine;
using static TileData;

public class BoardManager : MonoBehaviour
{
    [Header("Board Dimensions")]
    [SerializeField] private int width = 12;  // Your 12x12 board
    [SerializeField] private int height = 12;
    [SerializeField] private float cellSize = 10f;

    [Header("Prefabs & Data")]
    [SerializeField] private GameObject tilePrefab; // A simple cube for the visual
    private Dictionary<Vector2Int, GameObject> tileVisuals = new Dictionary<Vector2Int, GameObject>();

    // We only need ScriptableObjects for our *special* corner tiles
    [SerializeField] private TileData goTileData;
    [SerializeField] private TileData chanceTileData;

    private Grid<TileNode> grid;
    private List<Vector2Int> pathCoordinates; // Stores the ordered path

    void Start()
    {
        InitializeBoard();
        DrawBoardVisuals();
    }

    void InitializeBoard()
    {
        // 1. Create the logical grid that holds our TileNode objects
        grid = new Grid<TileNode>(width, height, cellSize, Vector3.zero, (g, x, y) => new TileNode(g, x, y));
        pathCoordinates = new List<Vector2Int>();

        // 2. Generate the path coordinates in a clockwise loop
        for (int x = 0; x < width - 1; x++) 
            pathCoordinates.Add(new Vector2Int(x, 0));           // Bottom row
        for (int y = 0; y < height - 1; y++)
            pathCoordinates.Add(new Vector2Int(width - 1, y));    // Right col
        for (int x = width - 1; x > 0; x--)
            pathCoordinates.Add(new Vector2Int(x, height - 1));  // Top row
        for (int y = height - 1; y > 0; y--)
            pathCoordinates.Add(new Vector2Int(0, y));        // Left col

        // 3. Assign the special tile types to the corners
        // Get the node at each corner and set its fundamental type
        grid.GetGridObject(0, 0).initialTileType = TileType.Go;         // Player 1's Go
        grid.GetGridObject(width - 1, 0).initialTileType = TileType.Chance;
        grid.GetGridObject(width - 1, height - 1).initialTileType = TileType.Go; // Player 2's Go
        grid.GetGridObject(0, height - 1).initialTileType = TileType.Chance;
    }

    void DrawBoardVisuals()
    {
        foreach (Vector2Int pos in pathCoordinates)
        {
            Vector3 worldPos = grid.GetWorldPosition(pos.x, pos.y);

            GameObject tileObject = Instantiate(tilePrefab, worldPos, Quaternion.identity, this.transform);

            tileVisuals[pos] = tileObject;
        }
    }

    public void UpdateTileVisual(TileNode node, Material buildingMat)
    {
        if (node != null && buildingMat != null)
        {
            Vector2Int pos = new Vector2Int(node.x, node.y);

            if (tileVisuals.ContainsKey(pos))
            {
                GameObject tileObject = tileVisuals[pos];
                Renderer tileRenderer = tileObject.GetComponent<Renderer>();
                tileRenderer.material = buildingMat;
            }
        }
    }

    // --- Public Helper Methods for other scripts ---

    public TileNode GetNodeAtPosition(Vector2Int gridPosition)
    {
        return grid.GetGridObject(gridPosition.x, gridPosition.y);
    }

    public Vector3 GetWorldPositionFromPathIndex(int pathIndex)
    {
        Vector2Int gridPos = pathCoordinates[pathIndex];
        return grid.GetWorldPosition(gridPos.x, gridPos.y);
    }

    public TileNode GetNodeFromPathIndex(int pathIndex)
    {
        Vector2Int gridPos = pathCoordinates[pathIndex];
        return GetNodeAtPosition(gridPos);
    }

    public int GetPathLength()
    {
        return pathCoordinates.Count;
    }
}