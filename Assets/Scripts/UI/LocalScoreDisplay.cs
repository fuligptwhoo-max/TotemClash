using UnityEngine;
using TMPro;
using FishNet.Object;

/// <summary>
/// Отображает очки локального игрока под таймером
/// </summary>
public class LocalScoreDisplay : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText; // Текст для отображения очков (если не назначен, берётся с этого объекта)
    
    [Header("Format")]
    public string format = "Очки: {0}";
    
    private PlayerScore localPlayerScore;
    private int lastScore = -1;
    
    private void Start()
    {
        // Если текст не назначен, берём с этого объекта
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();
            
        if (scoreText == null)
        {
            Debug.LogError("[LocalScoreDisplay] Не найден компонент TMP_Text!");
            enabled = false;
            return;
        }
        
        // Ищем локального игрока
        FindLocalPlayer();
    }
    
    private void Update()
    {
        // Если игрок ещё не найден, продолжаем искать
        if (localPlayerScore == null)
        {
            FindLocalPlayer();
            return;
        }
        
        // Проверяем изменение очков
        int currentScore = localPlayerScore.GetScore();
        if (currentScore != lastScore)
        {
            lastScore = currentScore;
            UpdateDisplay(currentScore);
        }
    }
    
    private void FindLocalPlayer()
    {
        // Ищем всех игроков с PlayerScore
        PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        
        foreach (PlayerScore playerScore in allPlayers)
        {
            // Проверяем, является ли этот игрок локальным
            NetworkObject netObj = playerScore.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localPlayerScore = playerScore;
                Debug.Log($"[LocalScoreDisplay] Найден локальный игрок: {playerScore.GetPlayerName()}");
                
                // Сразу обновляем отображение
                UpdateDisplay(localPlayerScore.GetScore());
                break;
            }
        }
    }
    
    private void UpdateDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(format, score);
        }
    }
}
