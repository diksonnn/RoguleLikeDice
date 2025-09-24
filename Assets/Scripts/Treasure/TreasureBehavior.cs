using UnityEngine;

public class TreasureBehavior : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("����� �������� �� ������!");

        var player = other.GetComponent<PlayerController>();
        if (player != null && !player.cubePool.Contains("LongAttack"))
        {
            player.cubePool.Add("LongAttack");
            Debug.Log("�������� ����� �����: LongAttack");
        }
        
        Destroy(gameObject);
    }
}
