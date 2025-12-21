using UnityEngine;

public class PlayerScore : MonoBehaviour
{
    [Header("Player Info")]
    public int playerId = 0; // 0 для первого игрока, 1 для второго и т.д.
    
    [Header("Score")]
    public int score = 0;
    
    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = FindAnyObjectByType<GameManager>();
    }
    
    public void AddScore(int points)
    {
        score += points;
        
        if (gameManager != null)
        {
            // Если GameManager поддерживает ID игрока
            if (gameManager.GetType().GetMethod("AddScore", new System.Type[] { typeof(int), typeof(int) }) != null)
            {
                // Через рефлексию вызываем метод с двумя параметрами
                gameManager.GetType().GetMethod("AddScore").Invoke(gameManager, new object[] { playerId, points });
            }
            else
            {
                // Иначе используем метод с одним параметром
                gameManager.GetType().GetMethod("AddScore", new System.Type[] { typeof(int) }).Invoke(gameManager, new object[] { points });
            }
        }
    }
}