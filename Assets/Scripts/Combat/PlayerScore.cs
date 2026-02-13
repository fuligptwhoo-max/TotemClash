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
    
    [Header("Local Player UI")]
    public string localScoreTextName = "LocalScoreText"; // Имя объекта под таймером
    
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
        
        // Если это локальный игрок - находим UI для отображения очков
        // Проверяем в Start, т.к. в OnStartNetwork IsOwner недоступен
        StartCoroutine(FindLocalScoreUICoroutine());
    }
    
    /// <summary>
    /// Корутина для поиска UI (нужно подождать пока сеть инициализируется)
    /// </summary>
    private System.Collections.IEnumerator FindLocalScoreUICoroutine()
    {
        // Ждём один кадр чтобы сеть инициализировалась
        yield return null;
        
        // Теперь можно проверять IsOwner
        if (base.IsOwner)
        {
            FindLocalScoreUI();
        }
    }
    
    /// <summary>
    /// Находит UI элемент для отображения очков локального игрока
    /// </summary>
    private void FindLocalScoreUI()
    {
        // Ищем по имени (можно назначить в инспекторе или найти автоматически)
        if (scoreText == null && !string.IsNullOrEmpty(localScoreTextName))
        {
            GameObject uiObj = GameObject.Find(localScoreTextName);
            if (uiObj != null)
            {
                scoreText = uiObj.GetComponent<TMP_Text>();
            }
        }
        
        // Если не нашли по имени, ищем по тегу
        if (scoreText == null)
        {
            GameObject uiObj = GameObject.FindWithTag("LocalScore");
            if (uiObj != null)
            {
                scoreText = uiObj.GetComponent<TMP_Text>();
            }
        }
        
        // Если нашли - сразу обновляем
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {score.Value}";
            Debug.Log($"[PlayerScore] Найден UI для очков: {scoreText.name}");
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
        // Если это локальный игрок - обновляем его личный UI
        // base.Owner.IsLocalClient можно использовать вместо IsOwner в хуках SyncVar
        if ((base.IsOwner || base.Owner.IsLocalClient) && scoreText != null)
        {
            scoreText.text = $"Очки: {next}";
        }
        
        // Уведомляем таблицу лидеров об изменении (для всех игроков)
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.UpdatePlayerScore(this);
        }
    }
}
