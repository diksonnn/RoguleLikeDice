using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text levelCounterText; // ����� ��� ����������� ������

    [Header("Game Objects")]
    public GameObject portalPrefab; // ������ �������
    public MapGenerator mapGenerator; // ������ �� ��������� �����
    public GameObject GridContainer;

    private int currentLevel = 1;
    private GameObject activePortal;
    private bool isTransitioning = false; // ���� ��� �������������� ������������� ���������

    void Start()
    {
        UpdateLevelUI();
        // ��������� ������ � ������ ������
        StartCoroutine(CheckForEnemiesDelayed(1f));
    }

    void Update()
    {
        // ��������� ���������, �������� �� ����� (������ ���� �� � �������� ��������)
        if (activePortal == null && !isTransitioning)
        {
            CheckForEnemies();
        }
    }

    public void NextLevel()
    {
        if (isTransitioning) return; // ������������� ������������� ������

        isTransitioning = true;
        currentLevel++;
        Debug.Log($"������� �� ������� {currentLevel}");

        // ���������� ������
        if (activePortal != null)
        {
            Destroy(activePortal);
            activePortal = null;
        }

        StartCoroutine(TransitionToNextLevel());
    }

    private IEnumerator TransitionToNextLevel()
    {
        // ��������� �������� ��� ��������� ��������
        yield return new WaitForSeconds(0.5f);

        // ������� ������� ��� �������, ����� UI
        ClearAllGameObjects();

        // ���������� ������� � ������� GridContainer
        ResetGridContainer();

        yield return new WaitForEndOfFrame();

        // ��������������� �������� ������ (���� �� ��� ����������)
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            existingPlayer.RestoreHealth();
        }

        // ���������� ����� �����
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
        }

        // ��������� UI
        UpdateLevelUI();

        yield return new WaitForSeconds(1f);

        // ��������� �������� ������ �����
        isTransitioning = false;

        // ��������� ������ �� ����� ������
        StartCoroutine(CheckForEnemiesDelayed(1f));
    }

    private void ClearAllGameObjects()
    {
        // ������� ��� ������� �������
        DestroyObjectsWithTag("Enemy");
        DestroyObjectsWithTag("Treasure");
        DestroyObjectsWithTag("Player");
        DestroyObjectsWithTag("Cell");

        // ������� ������, ���� ��� ����
        DiceBehavior[] dice = FindObjectsOfType<DiceBehavior>();
        foreach (var die in dice)
        {
            Destroy(die.gameObject);
        }

        // ������� ����� GridContainer
        foreach (Transform child in GridContainer.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void DestroyObjectsWithTag(string tag)
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }
    }

    private void ResetGridContainer()
    {
        // ���������� ��������� GridContainer � ��������� ���������
        GridContainer.transform.position = Vector3.zero;
        GridContainer.transform.rotation = Quaternion.identity;
        GridContainer.transform.localScale = Vector3.one;
    }

    private IEnumerator CheckForEnemiesDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        CheckForEnemies();
    }

    private void CheckForEnemies()
    {
        if (isTransitioning) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0 && activePortal == null)
        {
            SpawnPortal();
        }
    }

    private void SpawnPortal()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("����� �� ������ ��� ������ �������!");
            return;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        Vector2Int playerPos = playerController.playerGridPosition;

        // ���� ��������� ������ ����� � �������
        Vector2Int[] adjacentPositions = new Vector2Int[]
        {
            playerPos + Vector2Int.up,
            playerPos + Vector2Int.down,
            playerPos + Vector2Int.left,
            playerPos + Vector2Int.right
        };

        foreach (Vector2Int pos in adjacentPositions)
        {
            if (IsCellFreeForPortal(pos))
            {
                Vector3 worldPos = GetWorldPositionFromGrid(pos);
                if (worldPos != Vector3.zero)
                {
                    activePortal = Instantiate(portalPrefab, worldPos, Quaternion.identity, GridContainer.transform);
                    Debug.Log($"������ �������� �� ������� {pos}");
                    return;
                }
            }
        }

        // ���� ����� ��� ���������� �����, ���� ����� ��������� ����� �� �����
        SpawnPortalAnywhere();
    }

    private void SpawnPortalAnywhere()
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");

        if (cells.Length == 0)
        {
            Debug.LogWarning("��� ������ ��� ���������� �������!");
            return;
        }

        for (int attempts = 0; attempts < 50; attempts++) // �������� 50 �������
        {
            GameObject randomCell = cells[Random.Range(0, cells.Length)];
            CellClick cellClick = randomCell.GetComponent<CellClick>();

            if (cellClick != null && IsCellFreeForPortal(cellClick.cellGridPos))
            {
                activePortal = Instantiate(portalPrefab, randomCell.transform.position, Quaternion.identity, GridContainer.transform);
                Debug.Log($"������ �������� �� ��������� ������� {cellClick.cellGridPos}");
                return;
            }
        }

        Debug.LogWarning("�� ������� ����� ����� ��� �������!");
    }

    private bool IsCellFreeForPortal(Vector2Int gridPos)
    {
        // ���������, ��� ������ ����������
        if (!DoesCellExist(gridPos)) return false;

        // ���������, �� ������ �� ������ �������
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController.playerGridPosition == gridPos)
                return false;
        }

        // ���������, ��� �� ��� ��������
        GameObject[] treasures = GameObject.FindGameObjectsWithTag("Treasure");
        foreach (GameObject treasure in treasures)
        {
            if (Vector3.Distance(treasure.transform.position, GetWorldPositionFromGrid(gridPos)) < 0.1f)
                return false;
        }

        return true;
    }

    private bool DoesCellExist(Vector2Int gridPos)
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");
        foreach (GameObject cell in cells)
        {
            CellClick cellClick = cell.GetComponent<CellClick>();
            if (cellClick != null && cellClick.cellGridPos == gridPos)
                return true;
        }
        return false;
    }

    private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");
        foreach (GameObject cell in cells)
        {
            CellClick cellClick = cell.GetComponent<CellClick>();
            if (cellClick != null && cellClick.cellGridPos == gridPos)
                return cell.transform.position;
        }
        return Vector3.zero;
    }

    private void UpdateLevelUI()
    {
        if (levelCounterText != null)
        {
            levelCounterText.text = $"LEVEL: {currentLevel}";
        }
    }

    public int GetCurrentLevel()
    {
        return currentLevel;
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}