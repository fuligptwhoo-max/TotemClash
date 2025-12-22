using UnityEngine;
using TMPro;
using Mirror;
using System.Collections.Generic;

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
    
    // Синхронизированные переменные
    [SyncVar(hook = nameof(OnTimeChanged))]
    private float currentTime;
    
    [SyncVar(hook = nameof(OnScoreChanged))]
    private int totalScore = 0;
    
    [SyncVar]
    private bool gameActive = false;
    
    // Локальные переменные
    private bool isPaused = false;
    
    // Для отслеживания носителя тотема
    private uint currentCarrierId = 0;
    private float carrierScoreAccumulator = 0f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // УБРАЛ DontDestroyOnLoad - не работает для вложенных объектов
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        currentTime = gameTime;
        gameActive = true;
        
        Debug.Log("[SERVER] GameManager started!");
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (totem == null)
        {
            FindTotem();
        }
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        UpdateTimerUI();
        UpdateScoreUI();
        
        Debug.Log("[CLIENT] GameManager initialized");
    }
    
    private void Start()
    {
        // Запускаем только на сервере
        if (isServer)
        {
            currentTime = gameTime;
            gameActive = true;
        }
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }
    
    private void Update()
    {
        if (!isServer) return;
        if (!gameActive) return;
        
        // Обновляем время только на сервере
        currentTime -= Time.deltaTime;
        
        // Обновляем очки с тотема
        UpdateScoreFromTotem();
        
        if (currentTime <= 0f)
        {
            currentTime = 0f;
            gameActive = false;
            RpcEndGame();
        }
    }
    
    private void UpdateScoreFromTotem()
    {
        if (totem != null && totem.IsBeingCarried())
        {
            uint carrierId = totem.GetCarrierId();
            
            if (carrierId != 0)
            {
                carrierScoreAccumulator += totem.GetCarryMultiplier() * Time.deltaTime;
                
                if (carrierScoreAccumulator >= 1f)
                {
                    int pointsToAdd = Mathf.FloorToInt(carrierScoreAccumulator);
                    AddScore(pointsToAdd);
                    carrierScoreAccumulator -= pointsToAdd;
                }
            }
        }
        else
        {
            carrierScoreAccumulator = 0f;
        }
    }
    
    [Server]
    public void AddScore(int points)
    {
        totalScore += points;
        Debug.Log($"[SERVER] Score added: {points}, Total: {totalScore}");
        
        // Начисляем очки конкретному игроку (если нужно)
        if (currentCarrierId != 0 && NetworkServer.spawned.TryGetValue(currentCarrierId, out NetworkIdentity player))
        {
            PlayerScore playerScore = player.GetComponent<PlayerScore>();
            if (playerScore != null)
            {
                playerScore.AddScore(points);
            }
        }
    }
    
    // Hooks для синхронизации
    private void OnTimeChanged(float oldTime, float newTime)
    {
        UpdateTimerUI();
    }
    
    private void OnScoreChanged(int oldScore, int newScore)
    {
        UpdateScoreUI();
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
    
    private void FindTotem()
    {
        var totemObject = FindFirstObjectByType<TotemController>();
        if (totemObject != null)
        {
            totem = totemObject;
            Debug.Log($"Found totem: {totem.name}");
        }
    }
    
    [ClientRpc]
    private void RpcEndGame()
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
    
    public void TogglePause()
    {
        if (!isLocalPlayer) return;
        
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
        NetworkManager.singleton.ServerChangeScene(NetworkManager.singleton.onlineScene);
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