using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Connection;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Лобби - меню ожидания игроков перед началом игры
/// Работает как Leaderboard - просто список подключенных игроков
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    
    [Header("Panels")]
    public GameObject lobbyPanel;
    public GameObject settingsPanel;
    public GameObject networkMenuPanel;  // Ссылка на NetworkMenuPanel
    public GameObject loadingPanel;      // Ссылка на LoadingPanel
    
    [Header("Lobby UI - как Leaderboard")]
    public Transform playersListContainer;
    public GameObject playerEntryPrefab;
    public TMP_Text lobbyStatusText;
    public Button startGameButton;
    public Button backButton;
    public Button settingsButton;
    
    [Header("Settings UI")]
    public Slider gameTimeSlider;
    public TMP_Text gameTimeValue;
    public Slider playerSpeedSlider;
    public TMP_Text playerSpeedValue;
    public Slider projectileSpeedSlider;
    public TMP_Text projectileSpeedValue;
    public Slider damageSlider;
    public TMP_Text damageValue;
    public Button settingsBackButton;
    public Button resetDefaultsButton;
    
    [Header("Network")]
    public NetworkManager networkManager;
    
    [Header("Scene")]
    public string gameSceneName = "SampleScene";
    
    private Dictionary<int, GameObject> playerEntries = new Dictionary<int, GameObject>();
    private bool isHost = false;
    private bool isInSettings = false;
    
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
    
    private void Start()
    {
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
        
        // Кнопки лобби
        if (startGameButton != null)
            startGameButton.onClick.AddListener(OnStartGameClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        // Кнопки настроек
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
        
        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.AddListener(ResetSettings);
        
        // Слайдеры настроек
        SetupSettingsSliders();
        
        // Изначально скрываем лобби и настройки
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (startGameButton != null)
            startGameButton.onClick.RemoveListener(OnStartGameClicked);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (settingsBackButton != null)
            settingsBackButton.onClick.RemoveListener(OnSettingsBackClicked);
        if (resetDefaultsButton != null)
            resetDefaultsButton.onClick.RemoveListener(ResetSettings);
        
        if (networkManager != null)
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
    }
    
    private void SetupSettingsSliders()
    {
        if (gameTimeSlider != null)
        {
            gameTimeSlider.minValue = 60f;
            gameTimeSlider.maxValue = 600f;
            gameTimeSlider.onValueChanged.AddListener(OnGameTimeChanged);
        }
        
        if (playerSpeedSlider != null)
        {
            playerSpeedSlider.minValue = 4f;
            playerSpeedSlider.maxValue = 15f;
            playerSpeedSlider.onValueChanged.AddListener(OnPlayerSpeedChanged);
        }
        
        if (projectileSpeedSlider != null)
        {
            projectileSpeedSlider.minValue = 10f;
            projectileSpeedSlider.maxValue = 40f;
            projectileSpeedSlider.onValueChanged.AddListener(OnProjectileSpeedChanged);
        }
        
        if (damageSlider != null)
        {
            damageSlider.minValue = 10f;
            damageSlider.maxValue = 100f;
            damageSlider.onValueChanged.AddListener(OnDamageChanged);
        }
    }
    
    /// <summary>
    /// Показывает лобби
    /// </summary>
    public void ShowLobby(bool asHost)
    {
        isHost = asHost;
        isInSettings = false;
        
        Debug.Log("[LobbyManager] ShowLobby called");
        
        // Скрываем загрузку и меню
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(false);
        
        // Показываем лобби
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
        
        // Скрываем настройки
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        // Только хост видит кнопку "Начать игру" и настройки
        if (startGameButton != null)
            startGameButton.gameObject.SetActive(isHost);
        
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(isHost);
        
        // Подписываемся на события сети
        if (networkManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
            networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionStateChanged;
        }
        
        // Обновляем список игроков
        UpdatePlayersList();
        
        // Обновляем UI настроек
        UpdateSettingsUI();
        
        Debug.Log($"[LobbyManager] Lobby shown as {(isHost ? "Host" : "Client")}");
    }
    
    private void OnSettingsClicked()
    {
        if (!isHost) 
        {
            Debug.LogWarning("[LobbyManager] Only host can access settings!");
            return;
        }
        
        Debug.Log("[LobbyManager] Opening settings...");
        isInSettings = true;
        
        // Скрываем лобби, показываем настройки
        if (lobbyPanel != null)
            lobbyPanel.SetActive(false);
        
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateSettingsUI();
            Debug.Log("[LobbyManager] Settings panel activated");
        }
    }
    
    private void OnSettingsBackClicked()
    {
        Debug.Log("[LobbyManager] Returning to lobby from settings");
        isInSettings = false;
        
        // Скрываем настройки, показываем лобби
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);
    }
    
    private void UpdateSettingsUI()
    {
        if (GameSettings.Instance == null) 
        {
            Debug.LogWarning("[LobbyManager] GameSettings.Instance is null!");
            return;
        }
        
        Debug.Log($"[LobbyManager] Updating settings UI: Time={GameSettings.Instance.GetGameTime()}");
        
        if (gameTimeSlider != null)
            gameTimeSlider.value = GameSettings.Instance.GetGameTime();
        
        if (playerSpeedSlider != null)
            playerSpeedSlider.value = GameSettings.Instance.GetPlayerSpeed();
        
        if (projectileSpeedSlider != null)
            projectileSpeedSlider.value = GameSettings.Instance.GetProjectileSpeed();
        
        if (damageSlider != null)
            damageSlider.value = GameSettings.Instance.GetDamage();
        
        UpdateSettingsText();
    }
    
    private void UpdateSettingsText()
    {
        if (gameTimeValue != null && gameTimeSlider != null)
            gameTimeValue.text = $"{gameTimeSlider.value:F0} сек";
        
        if (playerSpeedValue != null && playerSpeedSlider != null)
            playerSpeedValue.text = $"{playerSpeedSlider.value:F1}";
        
        if (projectileSpeedValue != null && projectileSpeedSlider != null)
            projectileSpeedValue.text = $"{projectileSpeedSlider.value:F1}";
        
        if (damageValue != null && damageSlider != null)
            damageValue.text = $"{damageSlider.value:F0}";
    }
    
    #region Settings Handlers
    
    private void OnGameTimeChanged(float value)
    {
        if (!isHost || GameSettings.Instance == null) return;
        GameSettings.Instance.SetGameTime(value);
        UpdateSettingsText();
    }
    
    private void OnPlayerSpeedChanged(float value)
    {
        if (!isHost || GameSettings.Instance == null) return;
        GameSettings.Instance.SetPlayerSpeed(value);
        UpdateSettingsText();
    }
    
    private void OnProjectileSpeedChanged(float value)
    {
        if (!isHost || GameSettings.Instance == null) return;
        GameSettings.Instance.SetProjectileSpeed(value);
        UpdateSettingsText();
    }
    
    private void OnDamageChanged(float value)
    {
        if (!isHost || GameSettings.Instance == null) return;
        GameSettings.Instance.SetDamage((int)value);
        UpdateSettingsText();
    }
    
    private void ResetSettings()
    {
        if (!isHost || GameSettings.Instance == null) return;
        GameSettings.Instance.ResetToDefaults();
        UpdateSettingsUI();
    }
    
    #endregion
    
    private void OnRemoteConnectionStateChanged(NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        UpdatePlayersList();
    }
    
    private void UpdatePlayersList()
    {
        // Очищаем старые записи
        foreach (var entry in playerEntries.Values)
        {
            if (entry != null) Destroy(entry);
        }
        playerEntries.Clear();
        
        if (networkManager == null) return;
        
        int playerCount = 0;
        
        // Добавляем хоста (если это мы)
        if (networkManager.ClientManager.Connection.IsHost)
        {
            CreatePlayerEntry("Host (You)", true);
            playerCount++;
        }
        
        // Добавляем подключенных клиентов
        if (networkManager.ServerManager.Started)
        {
            int clientNumber = 1;
            foreach (var conn in networkManager.ServerManager.Clients.Values)
            {
                if (conn.IsHost) continue;
                
                CreatePlayerEntry($"Player {clientNumber}", false);
                clientNumber++;
                playerCount++;
            }
        }
        
        UpdateLobbyStatus(playerCount);
    }
    
    private void CreatePlayerEntry(string playerName, bool isLocal)
    {
        if (playerEntryPrefab == null || playersListContainer == null) return;
        
        GameObject entry = Instantiate(playerEntryPrefab, playersListContainer);
        entry.name = $"Entry_{playerName}";
        
        TMP_Text text = entry.GetComponent<TMP_Text>();
        if (text == null)
            text = entry.GetComponentInChildren<TMP_Text>();
        
        if (text != null)
        {
            text.text = isLocal ? $"> {playerName}" : playerName;
            text.color = isLocal ? Color.yellow : Color.white;
        }
        
        playerEntries[entry.GetInstanceID()] = entry;
    }
    
    private void UpdateLobbyStatus(int count)
    {
        if (lobbyStatusText != null)
        {
            lobbyStatusText.text = $"Игроков: {count}";
        }
    }
    
    private void OnStartGameClicked()
    {
        if (!isHost || networkManager == null) 
        {
            Debug.LogWarning("[LobbyManager] Cannot start game - not host or no network!");
            return;
        }
        
        if (isInSettings)
        {
            Debug.LogWarning("[LobbyManager] Cannot start game while in settings!");
            return;
        }
        
        Debug.Log("[LobbyManager] Starting game...");
        
        // Отписываемся от событий
        networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
        
        // Загружаем игровую сцену
        SceneLoadData sld = new SceneLoadData(gameSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        networkManager.SceneManager.LoadGlobalScenes(sld);
    }
    
    private void OnBackClicked()
    {
        Debug.Log("[LobbyManager] Back clicked - disconnecting and returning to network menu");
        
        // Отписываемся от событий
        if (networkManager != null)
        {
            networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionStateChanged;
        }
        
        // Отключаемся от сети
        if (networkManager != null)
        {
            if (networkManager.ClientManager.Started)
                networkManager.ClientManager.StopConnection();
            if (networkManager.ServerManager.Started)
                networkManager.ServerManager.StopConnection(true);
        }
        
        // Скрываем лобби и настройки
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        
        // Показываем NetworkMenu обратно
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(true);
    }
}
