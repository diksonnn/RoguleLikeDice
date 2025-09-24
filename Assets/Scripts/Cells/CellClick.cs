using UnityEngine;

public class CellClick : MonoBehaviour
{
    public Vector2Int cellGridPos = new Vector2Int(0,0);
    void OnMouseDown()
    {
        Debug.Log(cellGridPos);
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        PlayerController controller = player.GetComponent<PlayerController>();
        Debug.Log($"������� ������: {controller.playerGridPosition}");
        // ���� ��������� �����
        DiceBehavior selectedDie = null;
        foreach (DiceBehavior die in FindObjectsOfType<DiceBehavior>())
        {
            if (die.isSelected)
            {
                selectedDie = die;
                break;
            }
        }

        if (selectedDie == null)
        {
            Debug.Log("��� ���������� ������ ��������.");
            return;
        }

        string actionType = selectedDie.actionType;

        switch (actionType)
        {
            case "Movement":
                controller.MoveTo(cellGridPos, transform.position);
                break;

            case "Jump":
                controller.JumpTo(cellGridPos, transform.position);
                break;

            case "Attack":
                controller.AttackEnemy(cellGridPos, "Attack");
                break;

            case "LongAttack":
                controller.AttackEnemy(cellGridPos, "LongAttack");
                break;            

            default:
                Debug.Log("����������� �������� ������: " + actionType);
                break;
        }
    }
}
