using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public int maxPlayerHealth = 3;
    private int currentPlayerHealth;

    private Text playerHealthText;

    public Vector2Int playerGridPosition;
    public float moveSpeed = 5f;

    private Vector3 targetWorldPosition;
    private bool isMoving = false;

    public List<string> cubePool = new List<string> { "Movement", "Attack" };
    public MapGenerator mapGen;

    void Start()
    {
        playerHealthText = GameObject.FindWithTag("HP")?.GetComponent<Text>();
        currentPlayerHealth = maxPlayerHealth;
        targetWorldPosition = transform.position;
    }

    void Update()
    {

        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetWorldPosition) < 0.01f)
            {
                isMoving = false;
            }
        }

        playerHealthText.text = $"{currentPlayerHealth}/{maxPlayerHealth}";

        if (currentPlayerHealth <= 0)
        {
            PlayerDeath();
        }
    }

    // Передвижение на клетку, если соседняя
    public void MoveTo(Vector2Int targetGridPos, Vector3 targetTransformPosition)
    {
        if (IsCellAdjacent(targetGridPos))
        {
            // Проверяем, нет ли врага на целевой клетке
            if (IsEnemyOnCell(targetGridPos))
            {
                Debug.Log("На клетке находится враг! Движение невозможно.");
                return;
            }

            playerGridPosition = targetGridPos;
            targetWorldPosition = targetTransformPosition;
            isMoving = true;

            UseSelectedDie("Movement");
        }
        else
        {
            Debug.Log("Клетка слишком далеко для движения");
            Debug.Log($"корд цели: {targetGridPos}, корд игрока: {playerGridPosition}");
        }
    }

    public void JumpTo(Vector2Int targetGridPos, Vector3 targetTransformPosition)
    {
        if (IsCellWithinTwoCells(targetGridPos))
        {
            // Проверяем, нет ли врага на целевой клетке
            if (IsEnemyOnCell(targetGridPos))
            {
                Debug.Log("На клетке находится враг! Прыжок невозможен.");
                return;
            }

            playerGridPosition = targetGridPos;
            targetWorldPosition = targetTransformPosition;
            isMoving = true;

            UseSelectedDie("Jump");
        }
        else
        {
            Debug.Log("Клетка слишком далеко для прыжка");
            Debug.Log($"корд цели: {targetGridPos}, корд игрока: {playerGridPosition}");
        }
    }

    // Атака врага на клетке с проверкой диапазона
    public void AttackEnemy(Vector2Int enemyGridPos, string expectedAction)
    {
        bool inRange = false;

        switch (expectedAction)
        {
            case "Attack":
                inRange = IsCellAdjacent(enemyGridPos);
                Debug.Log(enemyGridPos);
                break;
            case "LongAttack":
                inRange = IsCellWithinTwoCells(enemyGridPos);
                break;
            default:
                Debug.LogWarning("Неизвестный тип атаки: " + expectedAction);
                return;
        }

        if (!inRange)
        {
            Debug.Log("Враг слишком далеко для атаки");
            return;
        }

        // Ищем и уничтожаем врага на позиции
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null && enemyController.enemyGridPosition == enemyGridPos)
            {
                Destroy(enemy);
                Debug.Log("Враг уничтожен!");
                UseSelectedDie(expectedAction);
                return;
            }
        }

        Debug.Log("Враг на этой клетке не найден.");
    }

    // Проверка соседних врагов для урона игроку
    public void CheckAdjacentEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null && IsCellAdjacent(enemyController.enemyGridPosition))
            {
                TakeDamage();
                Debug.Log("Игрок рядом с врагом! Получен урон.");
                return;
            }
        }
    }

    // Проверка, есть ли враг на указанной клетке
    private bool IsEnemyOnCell(Vector2Int gridPos)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null && enemyController.enemyGridPosition == gridPos)
            {
                return true;
            }
        }
        return false;
    }

    // Проверка соседних клеток по горизонтали и вертикали
    private bool IsCellAdjacent(Vector2Int targetGridPos)
    {
        Vector2Int diff = playerGridPosition - targetGridPos;
        return (Mathf.Abs(diff.x) == 1 && diff.y == 0) || (Mathf.Abs(diff.y) == 1 && diff.x == 0);
    }

    // Проверка в радиусе 2 клеток (только по вертикали и горизонтали)
    private bool IsCellWithinTwoCells(Vector2Int targetGridPos)
    {
        Vector2Int diff = playerGridPosition - targetGridPos;
        return (Mathf.Abs(diff.x) <= 2 && diff.y == 0) || (Mathf.Abs(diff.y) <= 2 && diff.x == 0);
    }

    // Использование выбранного кубика с нужным действием
    private void UseSelectedDie(string expectedAction)
    {
        DiceBehavior[] dice = FindObjectsOfType<DiceBehavior>();
        foreach (DiceBehavior die in dice)
        {
            if (die.isSelected && die.actionType == expectedAction)
            {
                Destroy(die.gameObject);
                return;
            }
        }
        Debug.Log("Не выбран кубик действия: " + expectedAction);
    }

    // Получение урона
    public void TakeDamage()
    {
        currentPlayerHealth = Mathf.Max(0, currentPlayerHealth - 1);
    }

    // Восстановление здоровья (для нового уровня)
    public void RestoreHealth()
    {
        currentPlayerHealth = maxPlayerHealth;
        Debug.Log("Здоровье игрока восстановлено!");
    }

    // Смерть игрока
    private void PlayerDeath()
    {
        SceneManager.LoadScene("Game Over");
    }
}