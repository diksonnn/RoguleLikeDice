using UnityEngine;
using UnityEngine.UI;

public class LevelUI : MonoBehaviour
{
    public Text levelText;
    private GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        if (gameManager != null && levelText != null)
        {
            levelText.text = $"LEVEL: {gameManager.GetCurrentLevel()}";
        }
    }
}