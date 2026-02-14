using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using System.Collections.Generic;

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
            // Спавним ожидающих игроков (которые подключились в лобби)
            if (MyNetworkManager.Instance != null)
            {
                MyNetworkManager.Instance.SpawnPendingPlayers();
            }
            
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
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // На клиенте применяем настройки когда они изменяются
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.GameTime.OnChange += OnGameTimeSettingChanged;
            GameSettings.Instance.PlayerSpeed.OnChange += OnPlayerSpeedSettingChanged;
            GameSettings.Instance.ProjectileSpeed.OnChange += OnProjectileSpeedSettingChanged;
            GameSettings.Instance.DamagePerHit.OnChange += OnDamageSettingChanged;
        }
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.GameTime.OnChange -= OnGameTimeSettingChanged;
            GameSettings.Instance.PlayerSpeed.OnChange -= OnPlayerSpeedSettingChanged;
            GameSettings.Instance.ProjectileSpeed.OnChange -= OnProjectileSpeedSettingChanged;
            GameSettings.Instance.DamagePerHit.OnChange -= OnDamageSettingChanged;
        }
    }
    
    private void OnGameTimeSettingChanged(float prev, float next, bool asServer)
    {
        if (asServer)
        {
            // На сервере обновляем текущее время игры
            gameTime = next;
            // Если игра активна, добавляем разницу к текущему времени
            if (isGameActive && !gameEnded)
            {
                float diff = next - prev;
                syncCurrentTime.Value += diff;
                Debug.Log($"[GameManager] GameTime setting changed on server: {prev} -> {next}, adjusted current time by {diff}");
            }
            else
            {
                // Если игра не активна, просто обновляем начальное время
                syncCurrentTime.Value = next;
                Debug.Log($"[GameManager] GameTime setting updated on server: {next}");
            }
        }
        else
        {
            gameTime = next;
            Debug.Log($"[GameManager] GameTime setting updated on client: {next}");
        }
    }
    
    private void OnPlayerSpeedSettingChanged(float prev, float next, bool asServer)
    {
        if (asServer)
        {
            // На сервере применяем к всем игрокам
            ApplyGameSettings();
            Debug.Log($"[GameManager] PlayerSpeed setting updated on server: {next}");
        }
        else
        {
            // Применяем скорость к локальному игроку
            ApplyGameSettings();
            Debug.Log($"[GameManager] PlayerSpeed setting updated on client: {next}");
        }
    }
    
    private void OnProjectileSpeedSettingChanged(float prev, float next, bool asServer)
    {
        Debug.Log($"[GameManager] ProjectileSpeed setting changed: {prev} -> {next} (asServer: {asServer})");
        // ProjectileSpeed применяется к новым снарядам при их создании
        // Нет необходимости обновлять существующие снаряды
    }
    
    private void OnDamageSettingChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[GameManager] Damage setting changed: {prev} -> {next} (asServer: {asServer})");
        // Damage применяется при попадании снаряда
        // Нет необходимости обновлять существующие снаряды
    }
    
    /// <summary>
    /// Применяет настройки из GameSettings к игрокам
    /// </summary>
    private void ApplyGameSettings()
    {
        if (GameSettings.Instance == null) 
        {
            Debug.LogWarning("[GameManager] Cannot apply settings - GameSettings.Instance is null!");
            return;
        }
        
        float playerSpeed = GameSettings.Instance.GetPlayerSpeed();
        float gameTimeSetting = GameSettings.Instance.GetGameTime();
        
        Debug.Log($"[GameManager] Applying settings: PlayerSpeed={playerSpeed}, GameTime={gameTimeSetting}");
        
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        Debug.Log($"[GameManager] Found {players.Length} players to apply settings");
        
        foreach (var player in players)
        {
            player.moveSpeed = playerSpeed;
            Debug.Log($"[GameManager] Applied speed {playerSpeed} to player {player.name}");
        }
        
        // Обновляем время игры
        if (base.IsServerInitialized)
        {
            syncCurrentTime.Value = gameTimeSetting;
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
        
        // Очищаем список спавненных игроков в NetworkManager
        if (MyNetworkManager.Instance != null)
        {
            MyNetworkManager.Instance.ClearSpawnedPlayers();
        }
        
        // Сбрасываем состояние
        syncGameEnded.Value = false;
        syncTotalScore.Value = 0;
        syncCurrentTime.Value = gameTime;
        
        // Сбрасываем тотем
        if (totem != null)
        {
            totem.ResetTotem();
        }
        
        // Переспавниваем всех игроков на новых позициях
        RespawnAllPlayers();
        
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
    
    /// <summary>
    /// Переспавнивает всех игроков на случайных spawn point'ах
    /// </summary>
    private void RespawnAllPlayers()
    {
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        
        // Создаём список занятых точек для этого цикла респавна
        List<Transform> usedSpawnPoints = new List<Transform>();
        
        foreach (var player in players)
        {
            // Сбрасываем очки
            var playerScore = player.GetComponent<PlayerScore>();
            if (playerScore != null)
            {
                playerScore.ResetScore();
            }
            
            // Сбрасываем здоровье
            var healthSystem = player.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.ResetHealth();
            }
            
            // Телепортируем на случайный spawn point
            if (MyNetworkManager.Instance != null)
            {
                Transform spawnPoint = GetUniqueSpawnPoint(usedSpawnPoints);
                if (spawnPoint != null)
                {
                    usedSpawnPoints.Add(spawnPoint);
                    TeleportPlayer(player, spawnPoint.position, spawnPoint.rotation);
                }
            }
        }
        
        Debug.Log($"[GameManager] Respawned {players.Length} players on unique spawn points");
    }
    
    /// <summary>
    /// Получает уникальную точку спавна, которую ещё не использовали в этом раунде
    /// </summary>
    private Transform GetUniqueSpawnPoint(List<Transform> usedPoints)
    {
        if (MyNetworkManager.Instance == null) return null;
        
        // Получаем все доступные точки
        var allPoints = new List<Transform>();
        for (int i = 0; i < 10; i++) // Пробуем 10 раз
        {
            var point = MyNetworkManager.Instance.GetRandomSpawnPoint();
            if (point != null && !allPoints.Contains(point))
            {
                allPoints.Add(point);
            }
        }
        
        // Ищем неиспользованную точку
        foreach (var point in allPoints)
        {
            if (!usedPoints.Contains(point))
            {
                return point;
            }
        }
        
        // Если все заняты, возвращаем любую
        return allPoints.Count > 0 ? allPoints[0] : null;
    }
    
    /// <summary>
    /// Телепортирует игрока на новую позицию
    /// </summary>
    private void TeleportPlayer(NetworkPlayerController player, Vector3 position, Quaternion rotation)
    {
        // Отключаем CharacterController перед телепортацией
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.transform.position = position;
            player.transform.rotation = rotation;
            cc.enabled = true;
        }
        else
        {
            player.transform.position = position;
            player.transform.rotation = rotation;
        }
        
        // Сбрасываем скорость если есть Rigidbody
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"[GameManager] Teleported player {player.name} to {position}");
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
