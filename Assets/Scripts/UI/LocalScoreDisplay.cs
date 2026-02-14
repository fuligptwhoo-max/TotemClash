using UnityEngine;
using TMPro;
using FishNet.Object;
using System.Collections;

/// <summary>
/// Отображает очки локального игрока под таймером
/// </summary>
public class LocalScoreDisplay : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;
    
    [Header("Format")]
    public string format = "Очки: {0}";
    
    [Header("Debug")]
    public bool debugMode = false;
    
    private PlayerScore localPlayerScore;
    private int lastScore = -1;
    
    private void Start()
    {
        if (scoreText == null)
            scoreText = GetComponent<TMP_Text>();
        
        if (scoreText == null)
        {
            Debug.LogError("[LocalScoreDisplay] TMP_Text не найден!");
            enabled = false;
            return;
        }
        
        // Показываем начальное значение
        UpdateDisplay(0);
        
        // Запускаем поиск игрока с задержкой
        StartCoroutine(SearchForPlayerCoroutine());
    }
    
    private IEnumerator SearchForPlayerCoroutine()
    {
        // Ждём пока сеть инициализируется и игрок спавнится
        float waitTime = 0f;
        float maxWaitTime = 10f; // Максимум 10 секунд ждём
        
        while (waitTime < maxWaitTime)
        {
            FindLocalPlayer();
            
            if (localPlayerScore != null)
            {
                yield break; // Нашли - выходим
            }
            
            waitTime += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
        
        Debug.LogWarning("[LocalScoreDisplay] Could not find local player after 10 seconds");
    }
    
    private void Update()
    {
        if (localPlayerScore == null) return;
        
        int currentScore = localPlayerScore.GetScore();
        if (currentScore != lastScore)
        {
            lastScore = currentScore;
            UpdateDisplay(currentScore);
            
            if (debugMode)
                Debug.Log($"[LocalScoreDisplay] Score updated: {currentScore}");
        }
    }
    
    private void FindLocalPlayer()
    {
        PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        
        if (debugMode)
            Debug.Log($"[LocalScoreDisplay] Found {allPlayers.Length} players total");
        
        foreach (PlayerScore playerScore in allPlayers)
        {
            NetworkObject netObj = playerScore.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                localPlayerScore = playerScore;
                Debug.Log($"[LocalScoreDisplay] Found local player: {playerScore.GetPlayerName()}, Score: {playerScore.GetScore()}");
                UpdateDisplay(localPlayerScore.GetScore());
                return;
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
