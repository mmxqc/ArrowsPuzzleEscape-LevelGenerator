using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    private static LevelGenerator _instance;
    public static LevelGenerator Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<LevelGenerator>();
            return _instance;
        }
    }

    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public int levelWidth = 9;
    public int levelHeight = 9;

    private float spacing = 1.0f;

    private Tile[,] gridList;

    private void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        GenerateLevel();
    }

    private void GenerateLevel()
    {
        gridList = new Tile[levelWidth, levelHeight];

        float startPositionX = -levelWidth / 2.0f + 0.5f;
        float startPositionY = -levelHeight / 2.0f + 0.5f;
        Vector3 startPosition = new Vector3(startPositionX, startPositionY, 0);
        for (int y = 0; y < levelHeight; y++)
        {
            for (int x = 0; x < levelWidth; x++)
            {
                Vector3 position = new Vector3(x * spacing, y * spacing, 0) + startPosition;
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";

                Tile tileComponent = tile.GetComponent<Tile>();
                tileComponent.gridPosition = new Vector2Int(x, y);
                tileComponent.type = TileType.Empty;

                gridList[x, y] = tileComponent;
            }
        }
    }

    public Tile[,] GetGridList()
    {
        return gridList;
    }

    /// <summary>
    /// Destroys old tiles and rebuilds the grid. Can be called from Editor.
    /// </summary>
    public void RebuildGrid()
    {
        // Destroy existing tiles
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        gridList = new Tile[levelWidth, levelHeight];

        float startPositionX = -levelWidth / 2.0f + 0.5f;
        float startPositionY = -levelHeight / 2.0f + 0.5f;
        Vector3 startPosition = new Vector3(startPositionX, startPositionY, 0);

        for (int y = 0; y < levelHeight; y++)
        {
            for (int x = 0; x < levelWidth; x++)
            {
                Vector3 position = new Vector3(x * spacing, y * spacing, 0) + startPosition;
                GameObject tile = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                tile.name = $"Tile_{x}_{y}";

                Tile tileComponent = tile.GetComponent<Tile>();
                tileComponent.gridPosition = new Vector2Int(x, y);
                tileComponent.type = TileType.Empty;

                gridList[x, y] = tileComponent;
            }
        }
    }

    public void CheckWin()
    {
        bool allEmpty = gridList.Cast<Tile>().All(tile => tile.type == TileType.Empty);
        if (allEmpty)
        {
            UnityEngine.Debug.Log("You Win!");
        }
    }
}
