using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Managing;

/// <summary>
/// GameManager - центральный менеджер игры
/// Работает локально, время управляется сервером через SyncVar если есть сетевой объект
/// </summary>
public class GameManager : MonoBehaviour
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
            currentTime = gameTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        FindTotem();
        
        // Включаем управление у всех игроков сразу
        EnableAllPlayerControls();
        
        // Запускаем игру через 2 секунды
        gameStartTime = Time.time + 2f;
        Debug.Log("[GameManager] Game will start in 2 seconds...");
    }
    
    private void Update()
    {
        HandlePauseInput();
        
        // Задержка перед стартом игры
        if (!gameStarted && Time.time >= gameStartTime)
        {
            gameStarted = true;
            isGameActive = true;
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
        // Только сервер обновляет время (или локальная игра)
        if (!IsServer()) return;
        
        currentTime -= Time.deltaTime;
        
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            isGameActive = false;
            OnGameEnded();
        }
    }
    
    private bool IsServer()
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
        if (!IsServer()) return;
        if (totem == null || !totem.IsBeingCarried())
        {
            carrierScoreAccumulator = 0f;
            return;
        }
        
        carrierScoreAccumulator += totem.GetCarryMultiplier() * Time.deltaTime;
        
        if (carrierScoreAccumulator >= 1f)
        {
            int pointsToAdd = Mathf.FloorToInt(carrierScoreAccumulator);
            AddScore(pointsToAdd);
            carrierScoreAccumulator -= pointsToAdd;
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
        if (!IsServer()) return;
        
        totalScore += points;
        Debug.Log($"[SERVER] Score added: {points}, Total: {totalScore}");
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
