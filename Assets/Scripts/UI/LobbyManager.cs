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
        
        // Автопоиск loadingPanel если не назначен
        if (loadingPanel == null)
        {
            // Ищем по имени (обычно LoadingPanel или Loading)
            GameObject found = GameObject.Find("LoadingPanel");
            if (found == null)
                found = GameObject.Find("Loading");
            if (found == null)
                found = GameObject.Find("Loading Screen");
            
            if (found != null)
            {
                loadingPanel = found;
                Debug.Log($"[LobbyManager] Auto-found loadingPanel: {found.name}");
            }
            else
            {
                Debug.LogWarning("[LobbyManager] loadingPanel not assigned and could not be found automatically!");
            }
        }
        
        // Автопоиск networkMenuPanel если не назначен
        if (networkMenuPanel == null)
        {
            GameObject found = GameObject.Find("NetworkMenuPanel");
            if (found == null)
                found = GameObject.Find("Network Menu");
            
            if (found != null)
            {
                networkMenuPanel = found;
                Debug.Log($"[LobbyManager] Auto-found networkMenuPanel: {found.name}");
            }
        }
        
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
        
        // Проверяем назначены ли слайдеры и тексты
        ValidateSettingsUI();
        
        Debug.Log("[LobbyManager] Started");
    }
    
    private void ValidateSettingsUI()
    {
        Debug.Log("[LobbyManager] Validating Settings UI...");
        
        if (gameTimeSlider == null) Debug.LogError("[LobbyManager] gameTimeSlider is NOT assigned!");
        else Debug.Log($"[LobbyManager] gameTimeSlider assigned: {gameTimeSlider.name}, min={gameTimeSlider.minValue}, max={gameTimeSlider.maxValue}, value={gameTimeSlider.value}");
        
        if (gameTimeValue == null) Debug.LogError("[LobbyManager] gameTimeValue is NOT assigned!");
        else Debug.Log($"[LobbyManager] gameTimeValue assigned: {gameTimeValue.name}, text={gameTimeValue.text}");
        
        if (playerSpeedSlider == null) Debug.LogError("[LobbyManager] playerSpeedSlider is NOT assigned!");
        if (playerSpeedValue == null) Debug.LogError("[LobbyManager] playerSpeedValue is NOT assigned!");
        
        if (projectileSpeedSlider == null) Debug.LogError("[LobbyManager] projectileSpeedSlider is NOT assigned!");
        if (projectileSpeedValue == null) Debug.LogError("[LobbyManager] projectileSpeedValue is NOT assigned!");
        
        if (damageSlider == null) Debug.LogError("[LobbyManager] damageSlider is NOT assigned!");
        if (damageValue == null) Debug.LogError("[LobbyManager] damageValue is NOT assigned!");
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
        // Отписываемся от старых событий
        if (gameTimeSlider != null)
        {
            gameTimeSlider.onValueChanged.RemoveAllListeners();
            gameTimeSlider.minValue = 60f;
            gameTimeSlider.maxValue = 600f;
            gameTimeSlider.wholeNumbers = true;
            gameTimeSlider.onValueChanged.AddListener(OnGameTimeChanged);
        }
        
        if (playerSpeedSlider != null)
        {
            playerSpeedSlider.onValueChanged.RemoveAllListeners();
            playerSpeedSlider.minValue = 4f;
            playerSpeedSlider.maxValue = 15f;
            playerSpeedSlider.onValueChanged.AddListener(OnPlayerSpeedChanged);
        }
        
        if (projectileSpeedSlider != null)
        {
            projectileSpeedSlider.onValueChanged.RemoveAllListeners();
            projectileSpeedSlider.minValue = 10f;
            projectileSpeedSlider.maxValue = 40f;
            projectileSpeedSlider.onValueChanged.AddListener(OnProjectileSpeedChanged);
        }
        
        if (damageSlider != null)
        {
            damageSlider.onValueChanged.RemoveAllListeners();
            damageSlider.minValue = 10f;
            damageSlider.maxValue = 100f;
            damageSlider.wholeNumbers = true;
            damageSlider.onValueChanged.AddListener(OnDamageChanged);
        }
        
        Debug.Log("[LobbyManager] Settings sliders setup complete");
    }
    
    /// <summary>
    /// Показывает лобби
    /// </summary>
    public void ShowLobby(bool asHost)
    {
        isHost = asHost;
        isInSettings = false;
        
        Debug.Log($"[LobbyManager] ShowLobby called, isHost={isHost}");
        Debug.Log($"[LobbyManager] loadingPanel reference: {(loadingPanel != null ? loadingPanel.name : "NULL")}");
        Debug.Log($"[LobbyManager] networkMenuPanel reference: {(networkMenuPanel != null ? networkMenuPanel.name : "NULL")}");
        
        // Скрываем загрузку и меню
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
            Debug.Log("[LobbyManager] Loading panel hidden");
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Cannot hide loadingPanel - reference is NULL!");
        }
        
        if (networkMenuPanel != null)
        {
            networkMenuPanel.SetActive(false);
            Debug.Log("[LobbyManager] Network menu hidden");
        }
        
        // Показываем лобби
        if (lobbyPanel != null)
        {
            lobbyPanel.SetActive(true);
            Debug.Log("[LobbyManager] Lobby panel shown");
        }
        
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
        
        // Обновляем UI настроек (с задержкой чтобы GameSettings успел инициализироваться)
        StartCoroutine(UpdateSettingsUIDelayed());
        
        Debug.Log($"[LobbyManager] Lobby shown as {(isHost ? "Host" : "Client")}");
    }
    
    private System.Collections.IEnumerator UpdateSettingsUIDelayed()
    {
        // Ждём пока GameSettings инициализируется
        yield return null;
        
        // Пробуем несколько раз
        int attempts = 0;
        while (GameSettings.Instance == null && attempts < 10)
        {
            yield return null;
            attempts++;
        }
        
        if (GameSettings.Instance != null)
        {
            Debug.Log($"[LobbyManager] GameSettings found after {attempts} attempts");
            UpdateSettingsUI();
        }
        else
        {
            Debug.LogError("[LobbyManager] GameSettings.Instance is still null after waiting!");
        }
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
        Debug.Log("[LobbyManager] UpdateSettingsUI called");
        
        if (GameSettings.Instance == null) 
        {
            Debug.LogError("[LobbyManager] GameSettings.Instance is null! Убедись что GameSettings добавлен в сцену MainMenu на активный объект с NetworkObject компонентом.");
            return;
        }
        
        Debug.Log($"[LobbyManager] GameSettings.Instance found! IsServerInitialized={GameSettings.Instance.IsServerInitialized}");
        Debug.Log($"[LobbyManager] Current GameSettings values: Time={GameSettings.Instance.GetGameTime()}, Speed={GameSettings.Instance.GetPlayerSpeed()}, Projectile={GameSettings.Instance.GetProjectileSpeed()}, Damage={GameSettings.Instance.GetDamage()}");
        
        // Отписываемся от событий временно чтобы не вызывать изменения при установке значений
        if (gameTimeSlider != null)
        {
            gameTimeSlider.onValueChanged.RemoveAllListeners();
            float newValue = GameSettings.Instance.GetGameTime();
            gameTimeSlider.value = newValue;
            gameTimeSlider.onValueChanged.AddListener(OnGameTimeChanged);
            Debug.Log($"[LobbyManager] Set gameTimeSlider.value = {newValue}, actual={gameTimeSlider.value}");
        }
        else
        {
            Debug.LogError("[LobbyManager] gameTimeSlider is null in UpdateSettingsUI!");
        }
        
        if (playerSpeedSlider != null)
        {
            playerSpeedSlider.onValueChanged.RemoveAllListeners();
            playerSpeedSlider.value = GameSettings.Instance.GetPlayerSpeed();
            playerSpeedSlider.onValueChanged.AddListener(OnPlayerSpeedChanged);
        }
        
        if (projectileSpeedSlider != null)
        {
            projectileSpeedSlider.onValueChanged.RemoveAllListeners();
            projectileSpeedSlider.value = GameSettings.Instance.GetProjectileSpeed();
            projectileSpeedSlider.onValueChanged.AddListener(OnProjectileSpeedChanged);
        }
        
        if (damageSlider != null)
        {
            damageSlider.onValueChanged.RemoveAllListeners();
            damageSlider.value = GameSettings.Instance.GetDamage();
            damageSlider.onValueChanged.AddListener(OnDamageChanged);
        }
        
        UpdateSettingsText();
    }
    
    private void UpdateSettingsText()
    {
        Debug.Log("[LobbyManager] Updating settings text...");
        
        if (gameTimeValue != null && gameTimeSlider != null)
        {
            string newText = $"{gameTimeSlider.value:F0} сек";
            gameTimeValue.text = newText;
            Debug.Log($"[LobbyManager] gameTimeValue.text set to: {newText}");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot update gameTimeValue: value={(gameTimeValue != null)}, slider={(gameTimeSlider != null)}");
        }
        
        if (playerSpeedValue != null && playerSpeedSlider != null)
        {
            string newText = $"{playerSpeedSlider.value:F1}";
            playerSpeedValue.text = newText;
            Debug.Log($"[LobbyManager] playerSpeedValue.text = {newText}");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot update playerSpeedValue: value={(playerSpeedValue != null)}, slider={(playerSpeedSlider != null)}");
        }
        
        if (projectileSpeedValue != null && projectileSpeedSlider != null)
        {
            string newText = $"{projectileSpeedSlider.value:F1}";
            projectileSpeedValue.text = newText;
            Debug.Log($"[LobbyManager] projectileSpeedValue.text = {newText}");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot update projectileSpeedValue: value={(projectileSpeedValue != null)}, slider={(projectileSpeedSlider != null)}");
        }
        
        if (damageValue != null && damageSlider != null)
        {
            string newText = $"{damageSlider.value:F0}";
            damageValue.text = newText;
            Debug.Log($"[LobbyManager] damageValue.text = {newText}");
        }
        else
        {
            Debug.LogWarning($"[LobbyManager] Cannot update damageValue: value={(damageValue != null)}, slider={(damageSlider != null)}");
        }
    }
    
    #region Settings Handlers
    
    private void OnGameTimeChanged(float value)
    {
        Debug.Log($"[LobbyManager] OnGameTimeChanged: {value}, isHost={isHost}, GameSettings.Instance={GameSettings.Instance != null}");
        
        if (!isHost) 
        {
            Debug.LogWarning("[LobbyManager] Cannot change settings - not host!");
            return;
        }
        
        if (GameSettings.Instance == null) 
        {
            Debug.LogError("[LobbyManager] GameSettings.Instance is null!");
            return;
        }
        
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
