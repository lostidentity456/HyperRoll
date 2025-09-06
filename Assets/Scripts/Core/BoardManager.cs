using System.Collections.Generic;
using UnityEngine;
using static TileData;

public class BoardManager : MonoBehaviour
{
    [Header("Board Dimensions")]
    // The board is 12x12, but there is a layer of building outside every buildable tile
    [SerializeField] private int width = 14;  
    [SerializeField] private int height = 14;
    [SerializeField] private float cellSize = 10f;

    [Header("Visual Prefabs")]
    // Prefab for all board pieces
    [SerializeField] private GameObject tilePrefab;

    [Header("Tile Materials")]
    [SerializeField] private Material Player1GoMaterial;
    [SerializeField] private Material Player2GoMaterial;
    [SerializeField] private Material chanceMaterial;
    [SerializeField] private Material availablePlotMaterial;

    // Dictionaries to store references to the created visuals for later changes.
    private Dictionary<Vector2Int, GameObject> tileVisuals = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> plotVisuals = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> buildingVisuals = new Dictionary<Vector2Int, GameObject>();

    private Grid<TileNode> grid;
    private List<Vector2Int> pathCoordinates;

    void Start()
    {
        InitializeBoard();
        DrawBoardVisuals();
        DrawPlotVisuals();
    }

    void InitializeBoard()
    {
        grid = new Grid<TileNode>(width, height, cellSize, Vector3.zero, (g, x, y) => new TileNode(g, x, y));

        pathCoordinates = new List<Vector2Int>();
        int min = 1; 
        int max = 12; 

        // Bottom row (from left to right)
        for (int x = min; x < max; x++) pathCoordinates.Add(new Vector2Int(x, min));
        // Right col (from bottom to top)
        for (int y = min; y < max; y++) pathCoordinates.Add(new Vector2Int(max, y));
        // Top row (from right to left)
        for (int x = max; x > min; x--) pathCoordinates.Add(new Vector2Int(x, max));
        // Left col (from top to bottom)
        for (int y = max; y > min; y--) pathCoordinates.Add(new Vector2Int(min, y));

        Debug.Log($"Path generation complete. Total path tiles: {pathCoordinates.Count}"); // Should be 44

        for (int i = 0; i < pathCoordinates.Count; i++)
        {
            Vector2Int pos = pathCoordinates[i];
            TileNode node = grid.GetGridObject(pos.x, pos.y);

            // We need to give each node its own path index so it knows where it is in the sequence.
            if (node != null)
            {
                node.pathIndex = i;
            }
        }

        grid.GetGridObject(min, min).initialTileType = TileType.Go;         // Bottom-left (1, 1)
        grid.GetGridObject(max, min).initialTileType = TileType.Chance;    // Bottom-right (12, 1)
        grid.GetGridObject(max, max).initialTileType = TileType.Go;         // Top-right (12, 12)
        grid.GetGridObject(min, max).initialTileType = TileType.Chance;    // Top-left (1, 12) - THIS IS THE FIX.
    }

    void DrawBoardVisuals()
    {
        foreach (Vector2Int pos in pathCoordinates)
        {
            Vector3 worldPos = grid.GetWorldPosition(pos.x, pos.y);
            GameObject tileObject = Instantiate(tilePrefab, worldPos, Quaternion.identity, this.transform);
            tileVisuals[pos] = tileObject;

            TileNode node = grid.GetGridObject(pos.x, pos.y);
            Renderer tileRenderer = tileObject.GetComponent<Renderer>();
            if (tileRenderer == null) continue; // Safety check

            if (node.initialTileType == TileData.TileType.Chance)
            {
                tileRenderer.material = chanceMaterial;
            }
            else if (node.initialTileType == TileData.TileType.Go)
            {
                if (node.x == 1 && node.y == 1)
                {
                    tileRenderer.material = Player1GoMaterial;
                }
                else 
                {
                    tileRenderer.material = Player2GoMaterial;
                }
            }
        }
    }

    private void DrawPlotVisuals()
    {
        foreach (Vector2Int pathPos in pathCoordinates)
        {
            TileNode nodeOnPath = grid.GetGridObject(pathPos.x, pathPos.y);
            if (nodeOnPath != null && nodeOnPath.initialTileType == TileData.TileType.Buildable)
            {
                Vector2Int plotPos = CalculatePlotPosition(pathPos);
                Vector3 plotWorldPos = grid.GetWorldPosition(plotPos.x, plotPos.y);

                // Use the single, consolidated prefab.
                GameObject plotObject = Instantiate(tilePrefab, plotWorldPos, Quaternion.identity, this.transform);

                // Apply the "Available" green material.
                plotObject.GetComponent<Renderer>().material = availablePlotMaterial;

                // Store a reference to it, using the path tile's position as the key.
                plotVisuals[pathPos] = plotObject;
            }
        }
    }

    public void UpdateTileVisual(TileNode node, Material ownerMaterial)
    {
        Vector2Int pos = new Vector2Int(node.x, node.y);
        if (tileVisuals.ContainsKey(pos))
        {
            tileVisuals[pos].GetComponent<Renderer>().material = ownerMaterial;
        }
    }

    public void UpdatePlotVisual(TileNode node, Material ownerMaterial)
    {
        Vector2Int pathPos = new Vector2Int(node.x, node.y);
        if (plotVisuals.ContainsKey(pathPos))
        {
            plotVisuals[pathPos].GetComponent<Renderer>().material = ownerMaterial;
        }
    }

    public void UpdateBuildingVisual(TileNode nodeOnPath, Material buildingMat, GameObject buildingPrefab)
    {
        Vector2Int pathPos = new Vector2Int(nodeOnPath.x, nodeOnPath.y);

        // This method now ONLY handles the small building object itself.
        if (buildingVisuals.ContainsKey(pathPos))
        {
            // Logic for upgrading an existing building's material.
            buildingVisuals[pathPos].GetComponent<Renderer>().material = buildingMat;
        }
        else
        {
            // Create a new building object on the plot.
            Vector2Int plotPos = CalculatePlotPosition(pathPos);
            Vector3 buildingWorldPos = grid.GetWorldPosition(plotPos.x, plotPos.y);

            // We still need a height offset so it sits ON TOP of the plot visual.
            buildingWorldPos.y += 0.8f; // Adjust as needed

            GameObject buildingObj = Instantiate(buildingPrefab, buildingWorldPos, Quaternion.identity, this.transform);
            buildingObj.GetComponent<Renderer>().material = buildingMat;

            buildingVisuals[pathPos] = buildingObj;
        }
    }

    private Vector2Int CalculatePlotPosition(Vector2Int pathPos)
    {
        Vector2Int plotPos = pathPos;
        if (pathPos.y == 1) plotPos.y--;       // Bottom edge
        else if (pathPos.x == 12) plotPos.x++; // Right edge
        else if (pathPos.y == 12) plotPos.y++; // Top edge
        else if (pathPos.x == 1) plotPos.x--;  // Left edge
        return plotPos;
    }

    public int GetBuildingCountForPlayer(PlayerController player)
    {
        int count = 0;
        foreach (Vector2Int pos in pathCoordinates)
        {
            TileNode node = grid.GetGridObject(pos.x, pos.y);
            if (node != null && node.owner == player)
            {
                count++;
            }
        }
        return count;
    }

    public List<TileNode> GetAllPropertyNodes()
    {
        List<TileNode> propertyNodes = new List<TileNode>();
        foreach (Vector2Int pos in pathCoordinates)
        {
            TileNode node = grid.GetGridObject(pos.x, pos.y);
            // A property is any tile that has an owner.
            if (node != null && node.owner != null)
            {
                propertyNodes.Add(node);
            }
        }
        return propertyNodes;
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
        if (pathCoordinates == null || pathCoordinates.Count == 0)
        {
            Debug.LogError("Path Coordinates list has not been initialized!");
            return null;
        }
        if (pathIndex < 0 || pathIndex >= pathCoordinates.Count)
        {
            Debug.LogError($"Path Index {pathIndex} is out of bounds! Path size is {pathCoordinates.Count}.");
            return null;
        }
        Vector2Int gridPos = pathCoordinates[pathIndex];
        return GetNodeAtPosition(gridPos);
    }

    public int GetPathLength()
    {
        return pathCoordinates.Count;
    }
}