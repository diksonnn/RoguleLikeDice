using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceThrower : MonoBehaviour
{
    public BoxCollider2D diceArea;
    public int diceCount = 5;

    public List<DiceDefinition> allDiceDefinitions; //  Префабы и имена
    private Dictionary<string, GameObject> actionPrefabs;

    private List<GameObject> dicePool = new();

    void Start()
    {
        // Инициализируем словарь: actionType  префаб
        actionPrefabs = new Dictionary<string, GameObject>();
        foreach (var def in allDiceDefinitions)
        {
            if (!actionPrefabs.ContainsKey(def.actionType))
                actionPrefabs.Add(def.actionType, def.prefab);
        }
    }

    public void ThrowDice()
    {
        GameObject player = GameObject.FindWithTag("Player");
        var controller = player.GetComponent<PlayerController>();
        controller.CheckAdjacentEnemies();

        ClearDice();

        for (int i = 0; i < diceCount; i++)
        {
            string actionType = controller.cubePool[Random.Range(0, controller.cubePool.Count)];

            if (!actionPrefabs.ContainsKey(actionType))
            {
                Debug.LogWarning("Нет префаба для действия: " + actionType);
                continue;
            }

            GameObject prefab = actionPrefabs[actionType];
            Vector2 spawnPos = GetRandomPointInBounds(diceArea.bounds);
            GameObject die = Instantiate(prefab, spawnPos, Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
            die.GetComponent<DiceBehavior>().actionType = actionType;
            dicePool.Add(die);
        }

        // После броска кубиков - двигаем всех врагов
        StartCoroutine(MoveEnemiesAfterDelay());
    }

    private IEnumerator MoveEnemiesAfterDelay()
    {
        // Проверяем, не в процессе ли перехода между уровнями
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null && gameManager.IsTransitioning())
        {
            yield break; // Не двигаем врагов во время перехода
        }

        // Небольшая задержка для визуального эффекта
        yield return new WaitForSeconds(0.5f);

        // Получаем всех врагов и двигаем их к игроку
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (GameObject enemy in enemies)
        {
            EnemyController enemyController = enemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.MoveTowardsPlayer();
            }
        }

        Debug.Log($"Moved {enemies.Length} enemies towards player");
    }

    private Vector2 GetRandomPointInBounds(Bounds area)
    {
        float x = Random.Range(area.min.x, area.max.x);
        float y = Random.Range(area.min.y, area.max.y);
        return new Vector2(x, y);
    }

    private void ClearDice()
    {
        foreach (var die in dicePool)
            Destroy(die);
        dicePool.Clear();
    }
}