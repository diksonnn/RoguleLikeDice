using UnityEngine;

public class PortalBehavior : MonoBehaviour
{
    public float rotationSpeed = 50f; // Скорость вращения портала для визуального эффекта

    void Update()
    {
        // Вращаем портал для красивого эффекта
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Игрок вошел в портал!");

            // Находим GameManager и переходим на следующий уровень
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.NextLevel();
            }
            else
            {
                Debug.LogError("GameManager не найден!");
            }
        }
    }
}