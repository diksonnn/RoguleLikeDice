using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Vector2Int enemyGridPosition;
    public float moveSpeed = 3f;

    private Vector3 targetWorldPosition;
    private bool isMoving = false;

    void Start()
    {
        targetWorldPosition = transform.position;
    }

    void Update()
    {
        // Плавное движение к целевой позиции
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
            {
                isMoving = false;
            }
        }
    }

    public void MoveTowardsPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        PlayerController playerController = player.GetComponent<PlayerController>();
        Vector2Int playerPos = playerController.playerGridPosition;

        // Находим лучший ход к игроку
        Vector2Int bestMove = GetBestMoveTowardsPlayer(playerPos);

        if (bestMove != enemyGridPosition && IsCellFree(bestMove))
        {
            // Обновляем позицию в гриде
            enemyGridPosition = bestMove;

            // Находим мировую позицию клетки
            Vector3 worldPos = GetWorldPositionFromGrid(bestMove);
            if (worldPos != Vector3.zero)
            {
                targetWorldPosition = worldPos;
                isMoving = true;
                Debug.Log($"Враг {gameObject.name} движется к {bestMove}");
            }
        }
    }

    private Vector2Int GetBestMoveTowardsPlayer(Vector2Int playerPos)
    {
        Vector2Int currentPos = enemyGridPosition;

        // Возможные направления движения (только по вертикали и горизонтали)
        Vector2Int[] possibleMoves = new Vector2Int[]
        {
            currentPos + Vector2Int.up,
            currentPos + Vector2Int.down,
            currentPos + Vector2Int.left,
            currentPos + Vector2Int.right
        };

        Vector2Int bestMove = currentPos;
        float bestScore = float.MaxValue;

        foreach (Vector2Int move in possibleMoves)
        {
            // Проверяем, существует ли такая клетка на карте и свободна ли она
            if (DoesCellExist(move) && IsCellFree(move))
            {
                float distanceToPlayer = Vector2Int.Distance(move, playerPos);

                // Бонус за движение в правильном направлении
                Vector2Int directionToPlayer = playerPos - currentPos;
                Vector2Int moveDirection = move - currentPos;

                float directionBonus = 0f;
                if ((directionToPlayer.x > 0 && moveDirection.x > 0) ||
                    (directionToPlayer.x < 0 && moveDirection.x < 0) ||
                    (directionToPlayer.y > 0 && moveDirection.y > 0) ||
                    (directionToPlayer.y < 0 && moveDirection.y < 0))
                {
                    directionBonus = -0.1f; // Небольшой бонус за правильное направление
                }

                float totalScore = distanceToPlayer + directionBonus;

                if (totalScore < bestScore)
                {
                    bestScore = totalScore;
                    bestMove = move;
                }
            }
        }

        // Если не нашли свободного хода, попробуем найти путь в обход
        if (bestMove == currentPos)
        {
            bestMove = FindPathAroundObstacles(currentPos, playerPos);
        }

        return bestMove;
    }

    // Простой алгоритм поиска пути в обход препятствий
    private Vector2Int FindPathAroundObstacles(Vector2Int startPos, Vector2Int targetPos)
    {
        // Используем простой BFS для поиска кратчайшего пути
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startPos);
        visited.Add(startPos);
        cameFrom[startPos] = startPos;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Если достигли игрока, восстанавливаем путь
            if (current == targetPos)
            {
                return ReconstructFirstStep(cameFrom, startPos, targetPos);
            }

            foreach (Vector2Int direction in directions)
            {
                Vector2Int neighbor = current + direction;

                if (!visited.Contains(neighbor) && DoesCellExist(neighbor))
                {
                    // Для поиска пути позволяем проходить через клетку игрока
                    bool canMove = IsCellFree(neighbor) || neighbor == targetPos;

                    if (canMove)
                    {
                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            // Ограничиваем поиск разумным радиусом
            if (Vector2Int.Distance(current, startPos) > 10)
                break;
        }

        // Если путь не найден, остаемся на месте
        return startPos;
    }

    // Восстанавливает первый шаг из найденного пути
    private Vector2Int ReconstructFirstStep(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int end)
    {
        Vector2Int current = end;
        Vector2Int firstStep = end;

        while (cameFrom.ContainsKey(current) && cameFrom[current] != start)
        {
            firstStep = current;
            current = cameFrom[current];
        }

        return cameFrom.ContainsKey(current) ? firstStep : start;
    }

    private bool IsCellFree(Vector2Int gridPos)
    {
        // Проверяем, не занята ли клетка другим врагом
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            if (enemy == this.gameObject) continue; // Пропускаем себя

            EnemyController otherEnemy = enemy.GetComponent<EnemyController>();
            if (otherEnemy != null && otherEnemy.enemyGridPosition == gridPos)
            {
                return false;
            }
        }

        // Проверяем, не занята ли клетка игроком
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController.playerGridPosition == gridPos)
            {
                return false;
            }
        }

        // Проверяем, нет ли сокровищницы на этой клетке
        if (IsTreasureOnCell(gridPos))
        {
            return false;
        }

        return true;
    }

    // Проверка, есть ли сокровищница на указанной клетке
    private bool IsTreasureOnCell(Vector2Int gridPos)
    {
        GameObject[] treasures = GameObject.FindGameObjectsWithTag("Treasure");
        Vector3 targetWorldPos = GetWorldPositionFromGrid(gridPos);

        if (targetWorldPos == Vector3.zero) return false;

        foreach (GameObject treasure in treasures)
        {
            // Сравниваем позиции с небольшой погрешностью
            if (Vector3.Distance(treasure.transform.position, targetWorldPos) < 0.1f)
            {
                return true;
            }
        }
        return false;
    }

    private bool DoesCellExist(Vector2Int gridPos)
    {
        // Проверяем, существует ли клетка с такими координатами
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");
        foreach (GameObject cell in cells)
        {
            CellClick cellClick = cell.GetComponent<CellClick>();
            if (cellClick != null && cellClick.cellGridPos == gridPos)
            {
                return true;
            }
        }
        return false;
    }

    private Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
    {
        // Находим клетку с нужными координатами и возвращаем её мировую позицию
        GameObject[] cells = GameObject.FindGameObjectsWithTag("Cell");
        foreach (GameObject cell in cells)
        {
            CellClick cellClick = cell.GetComponent<CellClick>();
            if (cellClick != null && cellClick.cellGridPos == gridPos)
            {
                return cell.transform.position;
            }
        }
        return Vector3.zero;
    }
}