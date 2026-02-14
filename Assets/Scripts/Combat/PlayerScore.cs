using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Хранит и синхронизирует очки игрока
/// </summary>
public class PlayerScore : NetworkBehaviour
{
    // Синхронизируемое значение очков
    public readonly SyncVar<int> score = new SyncVar<int>(0);
    
    private string playerName;
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        score.OnChange += OnScoreChanged;
        
        // Устанавливаем имя игрока
        playerName = $"Player {OwnerId}";
        
        // Регистрируем в таблице лидеров
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.RegisterPlayer(this);
            Debug.Log($"[PlayerScore] Registered in leaderboard: {playerName}");
        }
        else
        {
            Debug.LogWarning("[PlayerScore] LeaderboardManager not found!");
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
            Debug.Log($"[PlayerScore] Unregistered from leaderboard: {playerName}");
        }
    }
    
    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        // Уведомляем таблицу лидеров
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.UpdatePlayerScore(this);
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
    }
    
    /// <summary>
    /// Сбрасывает очки (только сервер)
    /// </summary>
    [Server]
    public void ResetScore()
    {
        score.Value = 0;
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
}
