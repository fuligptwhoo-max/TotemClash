using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;

/// <summary>
/// GameManager - центральный менеджер игры
/// Синхронизирует время и состояние игры между всеми клиентами
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    public float gameTime = 300f;
    
    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text globalScoreText; // Общий счёт команды (опционально)
    
    [Header("References")]
    public TotemController totem;
    public CountdownDisplay countdownDisplay;
    public GameOverMenu gameOverMenu;
    
    // SyncVar для синхронизации
    public readonly SyncVar<float> syncCurrentTime = new SyncVar<float>(300f);
    public readonly SyncVar<int> syncTotalScore = new SyncVar<int>(0);
    public readonly SyncVar<bool> syncGameActive = new SyncVar<bool>(false);
    public readonly SyncVar<bool> syncGameEnded = new SyncVar<bool>(false);
    
    private bool isGameActive = false;
    private bool gameEnded = false;
    private float currentTime;
    private int totalScore = 0;
    private float carrierScoreAccumulator = 0f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        syncCurrentTime.OnChange += OnTimeChanged;
        syncTotalScore.OnChange += OnScoreChanged;
        syncGameActive.OnChange += OnGameActiveChanged;
        syncGameEnded.OnChange += OnGameEndedChanged;
        
        if (base.IsServerInitialized)
        {
            // Применяем настройки из GameSettings
            if (GameSettings.Instance != null)
            {
                gameTime = GameSettings.Instance.GetGameTime();
            }
            
            syncCurrentTime.Value = gameTime;
            syncTotalScore.Value = 0;
            syncGameActive.Value = false;
            syncGameEnded.Value = false;
        }
        
        currentTime = syncCurrentTime.Value;
        totalScore = syncTotalScore.Value;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        syncCurrentTime.OnChange -= OnTimeChanged;
        syncTotalScore.OnChange -= OnScoreChanged;
        syncGameActive.OnChange -= OnGameActiveChanged;
        syncGameEnded.OnChange -= OnGameEndedChanged;
    }
    
    private void OnTimeChanged(float prev, float next, bool asServer)
    {
        currentTime = next;
        UpdateTimerUI();
    }
    
    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        totalScore = next;
        UpdateScoreUI();
    }
    
    private void OnGameActiveChanged(bool prev, bool next, bool asServer)
    {
        isGameActive = next;
    }
    
    private void OnGameEndedChanged(bool prev, bool next, bool asServer)
    {
        gameEnded = next;
        if (next)
        {
            OnGameEnded();
        }
    }
    
    private void Start()
    {
        // Находим UI если не назначены
        if (timerText == null)
            timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
        
        // Находим компоненты если не назначены
        if (countdownDisplay == null)
            countdownDisplay = FindFirstObjectByType<CountdownDisplay>();
        if (gameOverMenu == null)
            gameOverMenu = FindFirstObjectByType<GameOverMenu>();
        
        FindTotem();
        
        // Запускаем обратный отсчёт через CountdownDisplay
        if (base.IsServerInitialized && countdownDisplay != null)
        {
            countdownDisplay.StartCountdown();
            
            // Запускаем игру после отсчёта
            StartCoroutine(StartGameAfterCountdown());
        }
        
        UpdateTimerUI();
        UpdateScoreUI();
    }
    
    private System.Collections.IEnumerator StartGameAfterCountdown()
    {
        // Ждём пока закончится отсчёт
        yield return new WaitForSeconds(countdownDisplay.countdownDuration + 1f);
        
        // Запускаем игру
        syncGameActive.Value = true;
        
        // Применяем настройки к существующим игрокам
        ApplyGameSettings();
        
        Debug.Log("[GameManager] Game started!");
    }
    
    /// <summary>
    /// Применяет настройки из GameSettings к игрокам
    /// </summary>
    private void ApplyGameSettings()
    {
        if (GameSettings.Instance == null) return;
        
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.moveSpeed = GameSettings.Instance.GetPlayerSpeed();
        }
        
        Debug.Log("[GameManager] Game settings applied to players");
    }
    
    private void Update()
    {
        // Обновляем время только на сервере
        if (isGameActive && !gameEnded && base.IsServerInitialized)
        {
            UpdateGameTime();
            UpdateScoreFromTotem();
        }
    }
    
    private void UpdateGameTime()
    {
        syncCurrentTime.Value -= Time.deltaTime;
        
        if (syncCurrentTime.Value <= 0f)
        {
            syncCurrentTime.Value = 0f;
            syncGameActive.Value = false;
            syncGameEnded.Value = true;
        }
    }
    
    private void UpdateScoreFromTotem()
    {
        if (totem == null || !totem.IsBeingCarried())
        {
            carrierScoreAccumulator = 0f;
            return;
        }
        
        carrierScoreAccumulator += totem.GetCarryMultiplier() * Time.deltaTime;
        
        if (carrierScoreAccumulator >= 1f)
        {
            int pointsToAdd = Mathf.FloorToInt(carrierScoreAccumulator);
            carrierScoreAccumulator -= pointsToAdd;
            
            int carrierId = totem.GetCarrierId();
            AddScoreToPlayer(carrierId, pointsToAdd);
        }
    }
    
    [Server]
    private void AddScoreToPlayer(int playerId, int points)
    {
        var players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        foreach (var playerScore in players)
        {
            // Ищем по ObjectId (как в TotemController)
            if (playerScore.ObjectId == playerId)
            {
                playerScore.AddScore(points);
                Debug.Log($"[GameManager] Added {points} points to player {playerScore.GetPlayerName()} (ObjectId: {playerId})");
                return;
            }
        }
        Debug.LogWarning($"[GameManager] Could not find player with ObjectId: {playerId}");
    }
    
    [Server]
    public void AddScore(int points)
    {
        syncTotalScore.Value += points;
    }
    
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    private void UpdateScoreUI()
    {
        // Обновление UI общего счёта (если нужно)
    }
    
    private void OnGameEnded()
    {
        Debug.Log("[GameManager] Game Ended!");
        
        // Показываем меню окончания игры
        if (gameOverMenu != null)
        {
            gameOverMenu.ShowGameOver();
        }
    }
    
    private void FindTotem()
    {
        var totemObject = FindFirstObjectByType<TotemController>();
        if (totemObject != null)
        {
            totem = totemObject;
        }
    }
    
    public void RestartGame()
    {
        if (!base.IsServerInitialized) return;
        
        // Скрываем меню окончания игры
        if (gameOverMenu != null)
        {
            if (gameOverMenu.gameOverPanel != null)
                gameOverMenu.gameOverPanel.SetActive(false);
        }
        
        // Очищаем таблицу лидеров
        if (LeaderboardManager.Instance != null)
        {
            LeaderboardManager.Instance.ClearAll();
        }
        
        // Сбрасываем состояние
        syncGameEnded.Value = false;
        syncTotalScore.Value = 0;
        syncCurrentTime.Value = gameTime;
        
        // Сбрасываем очки игроков
        var players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.ResetScore();
        }
        
        // Снова запускаем отсчёт
        if (countdownDisplay != null)
        {
            countdownDisplay.StartCountdown();
            StartCoroutine(StartGameAfterCountdown());
        }
        else
        {
            syncGameActive.Value = true;
        }
        
        Debug.Log("[GameManager] Game restarted!");
    }
    
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    public bool IsGameActive()
    {
        return isGameActive && !gameEnded;
    }
    
    public float GetCurrentTime()
    {
        return currentTime;
    }
}
