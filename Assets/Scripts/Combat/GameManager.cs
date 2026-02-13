using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;

/// <summary>
/// GameManager - центральный менеджер игры
/// Синхронизирует время и счёт между всеми клиентами
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    public float gameTime = 300f;
    
    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public GameObject gameOverPanel;
    public GameObject pauseMenu;
    
    [Header("Totem")]
    public TotemController totem;
    
    // SyncVar для синхронизации времени и счёта
    public readonly SyncVar<float> syncCurrentTime = new SyncVar<float>(300f);
    public readonly SyncVar<int> syncTotalScore = new SyncVar<int>(0);
    public readonly SyncVar<bool> syncGameActive = new SyncVar<bool>(false);
    
    private bool isPaused = false;
    private float currentTime;
    private int totalScore = 0;
    private bool isGameActive = false;
    private bool gameStarted = false;
    private float gameStartTime = 0f;
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
        
        // Подписываемся на изменения SyncVar
        syncCurrentTime.OnChange += OnTimeChanged;
        syncTotalScore.OnChange += OnScoreChanged;
        syncGameActive.OnChange += OnGameActiveChanged;
        
        if (base.IsServerInitialized)
        {
            // Сервер инициализирует значения
            syncCurrentTime.Value = gameTime;
            syncTotalScore.Value = 0;
            syncGameActive.Value = false;
        }
        
        // Устанавливаем локальные значения из SyncVar
        currentTime = syncCurrentTime.Value;
        totalScore = syncTotalScore.Value;
        isGameActive = syncGameActive.Value;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        syncCurrentTime.OnChange -= OnTimeChanged;
        syncTotalScore.OnChange -= OnScoreChanged;
        syncGameActive.OnChange -= OnGameActiveChanged;
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
    
    private void Start()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        // Находим UI элементы если не назначены
        if (timerText == null)
            timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
        if (scoreText == null)
            scoreText = GameObject.Find("ScoreText")?.GetComponent<TMP_Text>();
        
        Debug.Log($"[GameManager] TimerText: {(timerText != null ? "found" : "NOT FOUND")}");
        Debug.Log($"[GameManager] ScoreText: {(scoreText != null ? "found" : "NOT FOUND")}");
        
        FindTotem();
        
        // Включаем управление у всех игроков сразу
        EnableAllPlayerControls();
        
        // Запускаем игру через 2 секунды (только сервер)
        gameStartTime = Time.time + 2f;
        if (base.IsServerInitialized)
        {
            syncGameActive.Value = false;
            syncCurrentTime.Value = gameTime;
            syncTotalScore.Value = 0;
        }
        
        // Инициализируем UI
        UpdateTimerUI();
        UpdateScoreUI();
        
        Debug.Log("[GameManager] Game will start in 2 seconds...");
    }
    
    private void Update()
    {
        HandlePauseInput();
        
        // Задержка перед стартом игры
        if (!gameStarted && Time.time >= gameStartTime)
        {
            gameStarted = true;
            if (base.IsServerInitialized)
            {
                syncGameActive.Value = true;
            }
            Debug.Log("[GameManager] Game started!");
        }
        
        // Обновляем время только на сервере или в одиночной игре
        if (gameStarted && isGameActive && !isPaused)
        {
            UpdateGameTime();
            UpdateScoreFromTotem();
        }
        
        UpdateTimerUI();
        UpdateScoreUI();
    }
    
    private void UpdateGameTime()
    {
        // Только сервер обновляет время
        if (!base.IsServerInitialized) return;
        
        syncCurrentTime.Value -= Time.deltaTime;
        
        if (syncCurrentTime.Value <= 0f)
        {
            syncCurrentTime.Value = 0f;
            syncGameActive.Value = false;
            OnGameEnded();
        }
    }
    
    private bool IsServerCheck()
    {
        // Проверяем есть ли NetworkManager и запущен ли сервер
        var nm = FindFirstObjectByType<NetworkManager>();
        if (nm != null && nm.IsServerStarted)
            return true;
        
        // Если нет сети - считаем что мы "сервер" (локальная игра)
        if (nm == null)
            return true;
            
        return false;
    }
    
    private void UpdateScoreFromTotem()
    {
        if (!base.IsServerInitialized) return;
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
            
            // Находим игрока который несёт тотем и даём ему очки
            int carrierId = totem.GetCarrierId();
            AddScoreToPlayer(carrierId, pointsToAdd);
        }
    }
    
    /// <summary>
    /// Добавляет очки конкретному игроку
    /// </summary>
    [Server]
    private void AddScoreToPlayer(int playerId, int points)
    {
        // Находим игрока по ID
        var players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        foreach (var playerScore in players)
        {
            if (playerScore.OwnerId == playerId)
            {
                playerScore.AddScore(points);
                Debug.Log($"[GameManager] Добавлено {points} очков игроку {playerId}");
                return;
            }
        }
        
        // Если не нашли по OwnerId, пробуем найти по ObjectId
        foreach (var playerScore in players)
        {
            if (playerScore.ObjectId == playerId)
            {
                playerScore.AddScore(points);
                Debug.Log($"[GameManager] Добавлено {points} очков игроку (ObjectId) {playerId}");
                return;
            }
        }
    }
    
    private void HandlePauseInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }
    
    private void EnableAllPlayerControls()
    {
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                player.EnableControls(true);
            }
        }
    }
    
    /// <summary>
    /// Добавляет очки (только сервер)
    /// </summary>
    public void AddScore(int points)
    {
        if (!base.IsServerInitialized) return;
        
        syncTotalScore.Value += points;
        Debug.Log($"[SERVER] Score added: {points}, Total: {syncTotalScore.Value}");
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
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {totalScore}";
        }
    }
    
    private void OnGameEnded()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            TMP_Text resultText = gameOverPanel.GetComponentInChildren<TMP_Text>();
            if (resultText != null)
            {
                resultText.text = $"Игра окончена!\nВаш счет: {totalScore}";
            }
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    private void FindTotem()
    {
        var totemObject = FindFirstObjectByType<TotemController>();
        if (totemObject != null)
        {
            totem = totemObject;
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
        
        Time.timeScale = isPaused ? 0f : 1f;
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Confined;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        
        if (MyNetworkManager.Instance != null)
        {
            MyNetworkManager.Instance.RestartGame();
        }
    }
    
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
