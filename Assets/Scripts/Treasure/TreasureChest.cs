
using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour
{
    public List<string> treasureActions; // ��������: "LongAttack", "Shield", "Fireball"

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var controller = other.GetComponent<PlayerController>();
            if (controller == null) return;

            string randomTreasure = treasureActions[Random.Range(0, treasureActions.Count)];

            if (!controller.cubePool.Contains(randomTreasure))
            {
                controller.cubePool.Add(randomTreasure);
                Debug.Log("�������� ����� �� ������������: " + randomTreasure);
                RemoveFromAllTreasures(randomTreasure);
            }
            else
            {
                Debug.Log("����� ��� ���� � ����.");
            }

            // ���������� ��� ��������� ������
            Destroy(gameObject);
        }
    }

    private void RemoveFromAllTreasures(string RandomAction)
    {
        GameObject[] TreasuresList = GameObject.FindGameObjectsWithTag("Treasure");
        foreach(var Treasure in TreasuresList)
        {
            Treasure.GetComponent<TreasureChest>().treasureActions.Remove(RandomAction);
        }
    }
}
