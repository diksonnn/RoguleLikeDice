using UnityEngine;

public class DiceBehavior : MonoBehaviour
{
    public string actionType;
    public bool isSelected = false;

    private SpriteRenderer sr;
    private Color defaultColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        defaultColor = sr.color;
    }

    void OnMouseDown()
    {
        // Снимаем выделение со всех кубов
        foreach (DiceBehavior other in FindObjectsOfType<DiceBehavior>())
        {
            other.Deselect();
        }

        // Выбираем текущий
        isSelected = true;
        sr.color = Color.yellow; // или любая подсветка
    }

    public void Deselect()
    {
        isSelected = false;
        if (sr != null)
            sr.color = defaultColor;
    }
}
