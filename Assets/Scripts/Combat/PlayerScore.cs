using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;

/// <summary>
/// Хранит и синхронизирует очки игрока
/// </summary>
public class PlayerScore : NetworkBehaviour
{
    // Синхронизируемое значение очков
    public readonly SyncVar<int> score = new SyncVar<int>(0);
    
    [Header("UI")]
    public TMP_Text scoreText; // Локальный текст для этого игрока (опционально)
    
    private string playerName;
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Подписываемся на изменения очков
        score.OnChange += OnScoreChanged;
        
        // Устанавливаем имя игрока (можно брать из профиля или сгенерировать)
        playerName = $"Player {OwnerId}";
        
        // Регистрируем в таблице лидеров
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.RegisterPlayer(this);
        }
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        score.OnChange -= OnScoreChanged;
        
        // Удаляем из таблицы лидеров
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.UnregisterPlayer(this);
        }
    }
    
    /// <summary>
    /// Добавляет очки игроку (только сервер)
    /// </summary>
    [Server]
    public void AddScore(int points)
    {
        if (points <= 0) return;
        
        score.Value += points;
        Debug.Log($"[PlayerScore] {playerName} получил {points} очков. Всего: {score.Value}");
    }
    
    /// <summary>
    /// Устанавливает имя игрока
    /// </summary>
    [Server]
    public void SetPlayerName(string name)
    {
        playerName = name;
    }
    
    public string GetPlayerName()
    {
        return playerName;
    }
    
    public int GetScore()
    {
        return score.Value;
    }
    
    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        // Обновляем локальный UI если есть
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {next}";
        }
        
        // Уведомляем таблицу лидеров об изменении
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.UpdatePlayerScore(this);
        }
    }
}
