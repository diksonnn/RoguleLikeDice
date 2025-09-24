using UnityEngine;

public class PortalBehavior : MonoBehaviour
{
    public float rotationSpeed = 50f; // �������� �������� ������� ��� ����������� �������

    void Update()
    {
        // ������� ������ ��� ��������� �������
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("����� ����� � ������!");

            // ������� GameManager � ��������� �� ��������� �������
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                gameManager.NextLevel();
            }
            else
            {
                Debug.LogError("GameManager �� ������!");
            }
        }
    }
}