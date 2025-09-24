using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject cellPrefab;
    public int cellCount = 30;
    public GameObject gridContainer;  // �������� ������ (Background)
    public BoxCollider2D targetArea;  // �������, ���� ������ ��������� ����

    public float spacingFactor = 0.25f;  // ��� ����� �������� ��� ���� �� ������� (0.25 = 1/4 ������)

    private HashSet<Vector2Int> gridCells = new HashSet<Vector2Int>();
    private Vector2 cellWorldSize;

    public GameObject playerPrefab;
    public GameObject enemyPrefab;
    public GameObject treasurePrefab;
    private GameManager gameManager;

    private Vector2Int[] directions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    private void Awake()
    {
        // ���������� �������� ������ ������� ������
        cellWorldSize = cellPrefab.GetComponent<SpriteRenderer>().bounds.size;
    }

    void Start()
    {
        GenerateMap();
    }

    public void GenerateMap()
    {
        GameManager gameManager = FindObjectOfType<GameManager>();
        // �� ������� ����� ����� - ��� ������ GameManager
        // ������ ���� ��� ������ ������ (�� Start)
        if (Application.isPlaying && FindObjectOfType<GameManager>() != null)
        {
            // ���� GameManager � �������� ��������, �� �������
            if (!gameManager.IsTransitioning())
            {
                ClearMap();
            }
        }
        else
        {
            ClearMap(); // ������ ������ ��� ��������
        }

        // ����������� ��������� � ������ �������
        int currentLevel = gameManager != null ? gameManager.GetCurrentLevel() : 1;

        // ����������� ������ ����� � ���������� ������ � �������
        int levelCellCount = cellCount + (currentLevel - 1) * 5; // +5 ������ �� �������

        gridCells.Clear();
        Vector2Int currentPos = Vector2Int.zero;
        gridCells.Add(currentPos);

        List<Vector2Int> frontier = new List<Vector2Int> { currentPos };

        while (gridCells.Count < levelCellCount)
        {
            if (frontier.Count == 0) break;

            int idx = Random.Range(0, frontier.Count);
            Vector2Int baseCell = frontier[idx];

            List<Vector2Int> freeNeighbors = new List<Vector2Int>();
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = baseCell + dir;
                if (!gridCells.Contains(neighbor))
                {
                    freeNeighbors.Add(neighbor);
                }
            }

            if (freeNeighbors.Count > 0)
            {
                Vector2Int newCell = freeNeighbors[Random.Range(0, freeNeighbors.Count)];
                gridCells.Add(newCell);
                frontier.Add(newCell);

                if (Random.value < 0.3f)
                    frontier.Add(newCell);
            }
            else
            {
                frontier.RemoveAt(idx);
            }
        }

        foreach (var pos in gridCells)
        {
            Vector3 cellPosition = new Vector3(
                pos.x * cellWorldSize.x * spacingFactor,
                pos.y * cellWorldSize.y * spacingFactor,
                0
            );

            GameObject spawnedCell = Instantiate(cellPrefab, cellPosition, Quaternion.identity, gridContainer.transform);
            CellClick cclick = spawnedCell.GetComponent<CellClick>();
            cclick.cellGridPos = pos;
        }

        FitFieldToTargetArea();
        SpawnObjectsOnMap();
    }

    private void ClearMap()
    {
        // ������� ��� ������ �������
        ClearObjectsWithTag("Cell");
        ClearObjectsWithTag("Player");
        ClearObjectsWithTag("Enemy");
        ClearObjectsWithTag("Treasure");

        // ����� ������� ����� gridContainer
        foreach (Transform child in gridContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void ClearObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }

    public void FitFieldToTargetArea()
    {
        gridContainer.transform.localScale = Vector3.one; // ����� �������� ����� ���������

        if (gridContainer.transform.childCount == 0)
            return;

        // ������� Bounds ���� ������ (�� �� Transform-��������)
        Bounds fieldBounds = new Bounds(gridContainer.transform.GetChild(0).localPosition, Vector3.zero);

        foreach (Transform cell in gridContainer.transform)
        {
            fieldBounds.Encapsulate(cell.localPosition);
        }

        // ��������� ���������� ������ ������ (�������)
        Vector2 halfCellSize = new Vector2(
            cellPrefab.GetComponent<SpriteRenderer>().bounds.size.x * 0.5f,
            cellPrefab.GetComponent<SpriteRenderer>().bounds.size.y * 0.5f
        );

        // ��������� ������� ���� � ������ ������� ������
        fieldBounds.Expand(new Vector3(halfCellSize.x * 2, halfCellSize.y * 2, 0));

        // ��������� Bounds � ������� ����������
        Vector3 fieldMin = gridContainer.transform.TransformPoint(fieldBounds.min);
        Vector3 fieldMax = gridContainer.transform.TransformPoint(fieldBounds.max);

        Bounds worldFieldBounds = new Bounds();
        worldFieldBounds.SetMinMax(fieldMin, fieldMax);

        Bounds targetBounds = targetArea.bounds;

        // ������� ������� ���, ����� ���� �������������� ������ � TargetArea
        float scaleX = targetBounds.size.x / worldFieldBounds.size.x;
        float scaleY = targetBounds.size.y / worldFieldBounds.size.y;

        float finalScale = Mathf.Min(scaleX, scaleY);

        gridContainer.transform.localScale = Vector3.one * finalScale;

        // ���������� ���� � TargetArea
        Vector3 fieldCenterWorld = gridContainer.transform.TransformPoint(fieldBounds.center);
        Vector3 offset = targetBounds.center - fieldCenterWorld;

        gridContainer.transform.position += offset;
    }

    private void SpawnObjectsOnMap()
    {
        List<Vector3> availablePositions = new List<Vector3>();

        // �������� ������� ���� ������ (��� ������ spacing � ������� gridContainer)
        foreach (Transform cell in gridContainer.transform)
        {
            availablePositions.Add(cell.position);
        }

        // ����������� ������
        Vector3 playerPos = GetAndRemoveRandomPosition(availablePositions);
        GameObject newPlayer = Instantiate(playerPrefab, playerPos, Quaternion.identity, gridContainer.transform);

        PlayerController playerController = newPlayer.GetComponent<PlayerController>();

        foreach (var cell in GameObject.FindGameObjectsWithTag("Cell"))
        {
            if (Vector3.Distance(cell.transform.position, newPlayer.transform.position) < 0.01f)
            {
                CellClick cellClick = cell.GetComponent<CellClick>();
                playerController.playerGridPosition = cellClick.cellGridPos;
            }
        }
        playerController.cubePool = new List<string> { "Movement", "Attack" };

        // ����������� ���������� ������ � �������
        GameManager gameManager = FindObjectOfType<GameManager>();
        int currentLevel = gameManager != null ? gameManager.GetCurrentLevel() : 1;

        int baseEnemyCount = Mathf.Max(1, gridCells.Count / 10);
        int levelEnemyCount = baseEnemyCount + (currentLevel - 1); // +1 ���� �� �������
        int enemyCount = levelEnemyCount + Random.Range(-1, 2);

        for (int i = 0; i < enemyCount && availablePositions.Count > 0; i++)
        {
            Vector3 enemyPos = GetAndRemoveRandomPosition(availablePositions);
            Instantiate(enemyPrefab, enemyPos, Quaternion.identity, gridContainer.transform);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var cell in GameObject.FindGameObjectsWithTag("Cell"))
        {
            foreach (var enemy in enemies)
            {
                if (Vector3.Distance(cell.transform.position, enemy.transform.position) < 0.01f)
                {
                    CellClick cellClick = cell.GetComponent<CellClick>();
                    EnemyController enemyController = enemy.GetComponent<EnemyController>();
                    enemyController.enemyGridPosition = cellClick.cellGridPos;
                }
            }
        }

        // ���������� �������� ���� ����� �������������
        int treasureCount = Mathf.Max(1, gridCells.Count / 20 + (currentLevel - 1) / 3);

        for (int i = 0; i < treasureCount && availablePositions.Count > 0; i++)
        {
            Vector3 treasurePos = GetAndRemoveRandomPosition(availablePositions);
            Instantiate(treasurePrefab, treasurePos, Quaternion.identity, gridContainer.transform);
        }

        Debug.Log($"������� {currentLevel}: ������� {enemyCount} ������ � {treasureCount} ��������");
    }

    // ��������������� �������, ����� �� ��������� �������
    private Vector3 GetAndRemoveRandomPosition(List<Vector3> positions)
    {
        int idx = Random.Range(0, positions.Count);
        Vector3 pos = positions[idx];
        positions.RemoveAt(idx);
        return pos;
    }
}