using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text levelCounterText; // Текст для отображения уровня

    [Header("Game Objects")]
    public GameObject portalPrefab; // Префаб портала
    public MapGenerator mapGenerator; // Ссылка на генератор карты
    public GameObject GridContainer;

    private int currentLevel = 1;
    private GameObject activePortal;
    private bool isTransitioning = false; // Флаг для предотвращения множественных переходов

    void Start()
    {
        UpdateLevelUI();
        // Проверяем врагов в начале уровня
        StartCoroutine(CheckForEnemiesDelayed(1f));
    }

    void Update()
    {
        // Постоянно проверяем, остались ли враги (только если не в процессе перехода)
        if (activePortal == null && !isTransitioning)
        {
            CheckForEnemies();
        }
    }

    public void NextLevel()
    {
        if (isTransitioning) return; // Предотвращаем множественные вызовы

        isTransitioning = true;
        currentLevel++;
        Debug.Log($"Переход на уровень {currentLevel}");

        // Уничтожаем портал
        if (activePortal != null)
        {
            Destroy(activePortal);
            activePortal = null;
        }

        StartCoroutine(TransitionToNextLevel());
    }

    private IEnumerator TransitionToNextLevel()
    {
        // Небольшая задержка для плавности перехода
        yield return new WaitForSeconds(0.5f);

        // Сначала очищаем все объекты, кроме UI
        ClearAllGameObjects();

        // Сбрасываем позицию и масштаб GridContainer
        ResetGridContainer();

        yield return new WaitForEndOfFrame();

        // Восстанавливаем здоровье игрока (если он еще существует)
        PlayerController existingPlayer = FindObjectOfType<PlayerController>();
        if (existingPlayer != null)
        {
            existingPlayer.RestoreHealth();
        }

        // Генерируем новую карту
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
        }

        // Обновляем UI
        UpdateLevelUI();

        yield return new WaitForSeconds(1f);

        // Разрешаем проверку врагов снова
        isTransitioning = false;

        // Проверяем врагов на новом уровне
        StartCoroutine(CheckForEnemiesDelayed(1f));
    }

    private void ClearAllGameObjects()
    {
        // Очищаем все игровые объекты
        DestroyObjectsWithTag("Enemy");
        DestroyObjectsWithTag("Treasure");
        DestroyObjectsWithTag("Player");
        DestroyObjectsWithTag("Cell");

        // Очищаем кубики, если они есть
        DiceBehavior[] dice = FindObjectsOfType<DiceBehavior>();
        foreach (var die in dice)
        {
            Destroy(die.gameObject);
        }

        // Очищаем детей GridContainer
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
        // Сбрасываем трансформ GridContainer к исходному состоянию
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
            Debug.LogWarning("Игрок не найден для спавна портала!");
            return;
        }

        PlayerController playerController = player.GetComponent<PlayerController>();
        Vector2Int playerPos = playerController.playerGridPosition;

        // Ищем свободную клетку рядом с игроком
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
                    Debug.Log($"Портал появился на позиции {pos}");
                    return;
                }
            }
        }

        // Если рядом нет свободного места, ищем любое свободное место на карте
        SpawnPortalAnywhere();
    }

    private void SpawnPortalAnywhere()
    {
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");

        if (cells.Length == 0)
        {
            Debug.LogWarning("Нет клеток для размещения портала!");
            return;
        }

        for (int attempts = 0; attempts < 50; attempts++) // Максимум 50 попыток
        {
            GameObject randomCell = cells[Random.Range(0, cells.Length)];
            CellClick cellClick = randomCell.GetComponent<CellClick>();

            if (cellClick != null && IsCellFreeForPortal(cellClick.cellGridPos))
            {
                activePortal = Instantiate(portalPrefab, randomCell.transform.position, Quaternion.identity, GridContainer.transform);
                Debug.Log($"Портал появился на случайной позиции {cellClick.cellGridPos}");
                return;
            }
        }

        Debug.LogWarning("Не удалось найти место для портала!");
    }

    private bool IsCellFreeForPortal(Vector2Int gridPos)
    {
        // Проверяем, что клетка существует
        if (!DoesCellExist(gridPos)) return false;

        // Проверяем, не занята ли клетка игроком
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController.playerGridPosition == gridPos)
                return false;
        }

        // Проверяем, нет ли там сокровищ
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