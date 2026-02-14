using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;

/// <summary>
/// Главное меню игры
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button playButton;
    public Button settingsButton;
    public Button quitButton;
    public TMP_Text titleText;
    
    [Header("Network Menu")]
    public GameObject networkMenuPanel;
    public Button hostButton;
    public Button clientButton;
    public Button serverButton;
    public Button backButton;
    public TMP_InputField ipInputField;
    public TMP_Text statusText;
    
    [Header("Loading")]
    public GameObject loadingPanel;
    
    [Header("Network")]
    public NetworkManager networkManager;
    
    [Header("Lobby")]
    public LobbyManager lobbyManager;
    
    private bool connectionInProgress = false;

    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
        
        if (lobbyManager == null)
            lobbyManager = FindFirstObjectByType<LobbyManager>();
        
        // Не делаем DontDestroyOnLoad здесь - это будет делать LobbyManager
        
        // Кнопки главного меню
        if (playButton != null)
            playButton.onClick.AddListener(ShowNetworkMenu);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        // Кнопки сетевого меню
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);
        
        if (clientButton != null)
            clientButton.onClick.AddListener(OnClientClicked);
        
        if (serverButton != null)
            serverButton.onClick.AddListener(OnServerClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);
        
        // IP по умолчанию
        if (ipInputField != null)
            ipInputField.text = "localhost";
        
        if (titleText != null)
            titleText.text = "TOTEM CLASH";
        
        ShowMainMenu();
        
        Debug.Log("[MainMenu] Started");
    }
    
    private void OnDestroy()
    {
        if (playButton != null) playButton.onClick.RemoveListener(ShowNetworkMenu);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
        if (clientButton != null) clientButton.onClick.RemoveListener(OnClientClicked);
        if (serverButton != null) serverButton.onClick.RemoveListener(OnServerClicked);
        if (backButton != null) backButton.onClick.RemoveListener(ShowMainMenu);
    }
    
    #region Menu Navigation
    
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (networkMenuPanel != null) networkMenuPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    public void ShowNetworkMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (networkMenuPanel != null) networkMenuPanel.SetActive(true);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    
    public void ShowLoading()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (networkMenuPanel != null) networkMenuPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }
    
    #endregion
    
    #region Button Handlers
    
    private void OnSettingsClicked()
    {
        Debug.Log("[MainMenu] Settings clicked - заглушка");
        // ЗАГЛУШКА: Настройки игры
    }
    
    public void OnHostClicked()
    {
        if (connectionInProgress) return;
        
        if (networkManager == null)
        {
            UpdateStatus("NetworkManager not found!");
            return;
        }
        
        connectionInProgress = true;
        ShowLoading();
        UpdateStatus("Starting as Host...");
        
        Debug.Log("[MainMenu] Starting as Host...");
        
        // Настраиваем сервер
        if (networkManager.TransportManager?.Transport != null)
        {
            var transport = networkManager.TransportManager.Transport;
            transport.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        }
        
        // Запускаем сервер
        bool serverStarted = networkManager.ServerManager.StartConnection();
        if (!serverStarted)
        {
            UpdateStatus("Failed to start server!");
            connectionInProgress = false;
            return;
        }
        
        // Запускаем клиент
        bool clientStarted = networkManager.ClientManager.StartConnection("localhost");
        if (!clientStarted)
        {
            UpdateStatus("Failed to start client!");
            connectionInProgress = false;
            return;
        }
        
        Debug.Log("[MainMenu] Host started, showing lobby...");
        
        // Показываем лобби вместо загрузки сцены
        StartCoroutine(ShowLobbyWhenReady(true));
    }
    
    public void OnClientClicked()
    {
        if (connectionInProgress) return;
        
        if (networkManager == null)
        {
            UpdateStatus("NetworkManager not found!");
            return;
        }
        
        string ip = ipInputField != null ? ipInputField.text : "localhost";
        
        connectionInProgress = true;
        ShowLoading();
        UpdateStatus($"Connecting to {ip}...");
        
        Debug.Log($"[MainMenu] Connecting to {ip}...");
        
        // Запускаем клиент
        bool clientStarted = networkManager.ClientManager.StartConnection(ip);
        if (!clientStarted)
        {
            UpdateStatus("Failed to connect!");
            connectionInProgress = false;
            return;
        }
        
        // Ждём подключения и показываем лобби
        StartCoroutine(ShowLobbyWhenReady(false));
    }
    
    public void OnServerClicked()
    {
        if (connectionInProgress) return;
        
        if (networkManager == null)
        {
            UpdateStatus("NetworkManager not found!");
            return;
        }
        
        connectionInProgress = true;
        ShowLoading();
        UpdateStatus("Starting as Server only...");
        
        Debug.Log("[MainMenu] Starting as Server only...");
        
        // Настраиваем сервер
        if (networkManager.TransportManager?.Transport != null)
        {
            var transport = networkManager.TransportManager.Transport;
            transport.SetServerBindAddress("0.0.0.0", IPAddressType.IPv4);
        }
        
        // Запускаем сервер
        bool serverStarted = networkManager.ServerManager.StartConnection();
        if (!serverStarted)
        {
            UpdateStatus("Failed to start server!");
            connectionInProgress = false;
            return;
        }
        
        Debug.Log("[MainMenu] Server started, loading lobby scene...");
        
        // Сервер-only загружает лобби сцену (если она отдельная)
        // Или показывает панель ожидания
        StartCoroutine(ShowLobbyWhenReady(true));
    }
    
    public void OnQuitClicked()
    {
        Debug.Log("[MainMenu] Quit clicked");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    
    #endregion
    
    #region Coroutines
    
    private System.Collections.IEnumerator ShowLobbyWhenReady(bool asHost)
    {
        float timeout = 10f;
        float elapsed = 0f;
        
        // Ждём подключения
        while (networkManager.ClientManager != null && 
               !networkManager.ClientManager.Connection.IsValid && 
               elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (networkManager.ClientManager != null && networkManager.ClientManager.Connection.IsValid)
        {
            Debug.Log("[MainMenu] Connected! Showing lobby...");
            
            // Показываем лобби
            if (lobbyManager != null)
            {
                // Передаём ссылки на панели перед показом лобби
                lobbyManager.networkMenuPanel = networkMenuPanel;
                lobbyManager.loadingPanel = loadingPanel;
                lobbyManager.ShowLobby(asHost);
            }
            else
            {
                Debug.LogError("[MainMenu] LobbyManager not found!");
                UpdateStatus("Error: LobbyManager not found!");
            }
        }
        else
        {
            Debug.LogError("[MainMenu] Failed to connect!");
            UpdateStatus("Connection failed!");
            connectionInProgress = false;
            ShowNetworkMenu();
        }
    }
    
    #endregion
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        Debug.Log($"[MainMenu] {message}");
    }
}
