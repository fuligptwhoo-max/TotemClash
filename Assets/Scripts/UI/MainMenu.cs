using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;

/// <summary>
/// Главное меню игры + Network меню
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Main Menu")]
    public GameObject mainMenuPanel;
    public Button playButton;
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
    
    [Header("Settings")]
    public string gameSceneName = "SampleScene";
    
    private bool connectionInProgress = false;

    private void Start()
    {
        // Показываем курсор в меню
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Находим NetworkManager если не назначен
        if (networkManager == null)
        {
            networkManager = FindFirstObjectByType<NetworkManager>();
        }
        
        // КРИТИЧЕСКИ ВАЖНО: Помечаем NetworkManager как DontDestroyOnLoad сразу!
        if (networkManager != null)
        {
            DontDestroyOnLoad(networkManager.gameObject);
            Debug.Log("[MainMenu] NetworkManager marked as DontDestroyOnLoad");
        }
        else
        {
            Debug.LogError("[MainMenu] NetworkManager not found!");
        }
        
        // Назначаем кнопки главного меню
        if (playButton != null)
            playButton.onClick.AddListener(ShowNetworkMenu);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
        
        // Назначаем кнопки сетевого меню
        if (hostButton != null)
            hostButton.onClick.AddListener(OnHostClicked);
        
        if (clientButton != null)
            clientButton.onClick.AddListener(OnClientClicked);
        
        if (serverButton != null)
            serverButton.onClick.AddListener(OnServerClicked);
        
        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);
        
        // Устанавливаем IP по умолчанию
        if (ipInputField != null)
            ipInputField.text = "localhost";
        
        // Устанавливаем заголовок
        if (titleText != null)
            titleText.text = "TOTEM CLASH";
        
        // Показываем главное меню, скрываем остальное
        ShowMainMenu();
        
        Debug.Log("[MainMenu] Started");
    }
    
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (playButton != null)
            playButton.onClick.RemoveListener(ShowNetworkMenu);
        
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
        
        if (hostButton != null)
            hostButton.onClick.RemoveListener(OnHostClicked);
        
        if (clientButton != null)
            clientButton.onClick.RemoveListener(OnClientClicked);
        
        if (serverButton != null)
            serverButton.onClick.RemoveListener(OnServerClicked);
        
        if (backButton != null)
            backButton.onClick.RemoveListener(ShowMainMenu);
    }
    
    #region Menu Navigation
    
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(false);
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    
    public void ShowNetworkMenu()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(true);
        
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
    
    public void ShowLoading()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (networkMenuPanel != null)
            networkMenuPanel.SetActive(false);
        
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }
    
    #endregion
    
    #region Network Buttons
    
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
        
        // Настраиваем сервер на приём подключений со всех интерфейсов
        if (networkManager.TransportManager?.Transport != null)
        {
            var transport = networkManager.TransportManager.Transport;
            transport.SetServerBindAddress("0.0.0.0", FishNet.Transporting.IPAddressType.IPv4);
            Debug.Log($"[MainMenu] Server bind address set to: 0.0.0.0 (all interfaces)");
            Debug.Log($"[MainMenu] Server will listen on port: {transport.GetPort()}");
        }
        else
        {
            Debug.LogError("[MainMenu] Transport not found! Cannot configure server.");
        }
        
        // Запускаем сервер
        bool serverStarted = networkManager.ServerManager.StartConnection();
        Debug.Log($"[MainMenu] Server start result: {serverStarted}");
        Debug.Log($"[MainMenu] ServerManager.Started: {networkManager.ServerManager.Started}");
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
        
        Debug.Log("[MainMenu] Host started successfully, loading game scene...");
        
        // Ждём пока клиент подключится, затем загружаем сцену
        StartCoroutine(LoadGameSceneWhenReady());
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
        
        // Проверяем доступность IP через Ping
        StartCoroutine(CheckServerAvailability(ip, 7770));
    }
    
    /// <summary>
    /// Проверяет доступность сервера перед подключением
    /// </summary>
    private System.Collections.IEnumerator CheckServerAvailability(string ip, int port)
    {
        Debug.Log($"[MainMenu] Checking if server {ip}:{port} is reachable...");
        
        // Пробуем пинг
        System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
        System.Net.NetworkInformation.PingReply reply = null;
        
        try
        {
            reply = ping.Send(ip, 1000);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MainMenu] Ping failed: {e.Message}");
        }
        
        if (reply != null && reply.Status == System.Net.NetworkInformation.IPStatus.Success)
        {
            Debug.Log($"[MainMenu] Ping successful! Roundtrip: {reply.RoundtripTime}ms");
        }
        else
        {
            Debug.LogWarning($"[MainMenu] Ping failed or timed out. Status: {(reply?.Status.ToString() ?? "null")}");
            Debug.LogWarning($"[MainMenu] Server may be behind firewall or unreachable.");
        }
        
        // Логируем настройки транспорта
        if (networkManager.TransportManager?.Transport != null)
        {
            var transport = networkManager.TransportManager.Transport;
            Debug.Log($"[MainMenu] Transport: {transport.GetType().Name}");
            Debug.Log($"[MainMenu] Target port: {transport.GetPort()}");
        }
        
        // Запускаем клиент
        Debug.Log($"[MainMenu] Starting client connection to {ip}...");
        bool clientStarted = networkManager.ClientManager.StartConnection(ip);
        Debug.Log($"[MainMenu] Client start result: {clientStarted}");
        if (!clientStarted)
        {
            UpdateStatus("Failed to connect!");
            connectionInProgress = false;
            yield break;
        }
        
        Debug.Log("[MainMenu] Client started, waiting for connection...");
        
        // Клиент ждёт подключения - сцена загрузится автоматически от сервера
        StartCoroutine(WaitForClientConnection());
        
        yield break;
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
        
        // Настраиваем сервер на приём подключений со всех интерфейсов
        if (networkManager.TransportManager?.Transport != null)
        {
            var transport = networkManager.TransportManager.Transport;
            transport.SetServerBindAddress("0.0.0.0", FishNet.Transporting.IPAddressType.IPv4);
            Debug.Log($"[MainMenu] Server bind address set to: 0.0.0.0 (all interfaces)");
        }
        
        // Запускаем сервер
        bool serverStarted = networkManager.ServerManager.StartConnection();
        if (!serverStarted)
        {
            UpdateStatus("Failed to start server!");
            connectionInProgress = false;
            return;
        }
        
        Debug.Log("[MainMenu] Server started, loading game scene...");
        
        // Загружаем сцену
        StartCoroutine(LoadGameSceneWhenReady());
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
    
    /// <summary>
    /// Ждёт пока клиент подключится к серверу (только для клиента, не загружает сцену)
    /// </summary>
    private System.Collections.IEnumerator WaitForClientConnection()
    {
        float timeout = 15f;
        float elapsed = 0f;
        float lastLogTime = 0f;
        
        Debug.Log("[MainMenu] Waiting for client connection...");
        
        while (networkManager.ClientManager != null && 
               !networkManager.ClientManager.Connection.IsValid && 
               elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            
            // Логируем каждые 3 секунды
            if (elapsed - lastLogTime >= 3f)
            {
                lastLogTime = elapsed;
                Debug.Log($"[MainMenu] Still waiting for connection... ({elapsed:F1}s / {timeout}s)");
            }
            
            yield return null;
        }
        
        if (networkManager.ClientManager != null && networkManager.ClientManager.Connection.IsValid)
        {
            Debug.Log("[MainMenu] Client connected! Waiting for server to load scene...");
            Debug.Log($"[MainMenu] Connection ID: {networkManager.ClientManager.Connection.ClientId}");
            UpdateStatus("Connected! Waiting for server...");
            // Сцена загрузится автоматически от сервера через FishNet SceneManager
        }
        else
        {
            Debug.LogError("[MainMenu] Failed to connect to server!");
            if (networkManager.ClientManager != null)
            {
                Debug.LogError($"[MainMenu] ClientManager.Started: {networkManager.ClientManager.Started}");
                // ClientManager не имеет свойства ConnectionState в FishNet
                if (networkManager.ClientManager.Connection != null)
                {
                    Debug.LogError($"[MainMenu] Connection.IsValid: {networkManager.ClientManager.Connection.IsValid}");
                    Debug.LogError($"[MainMenu] Connection.ClientId: {networkManager.ClientManager.Connection.ClientId}");
                }
            }
            UpdateStatus("Connection failed! Check firewall.");
            connectionInProgress = false;
            ShowNetworkMenu();
        }
    }
    
    /// <summary>
    /// Ждёт пока клиент подключится, затем загружает игровую сцену (только для Host/Server!)
    /// </summary>
    private System.Collections.IEnumerator LoadGameSceneWhenReady()
    {
        // Ждём 2 секунды для стабилизации соединения
        yield return new WaitForSeconds(2f);
        
        if (networkManager == null)
        {
            Debug.LogError("[MainMenu] NetworkManager is null! Cannot load scene.");
            connectionInProgress = false;
            yield break;
        }
        
        // ТОЛЬКО СЕРВЕР может загружать глобальные сцены!
        if (!networkManager.ServerManager.Started)
        {
            Debug.LogError("[MainMenu] Cannot load scene - not running as server!");
            connectionInProgress = false;
            yield break;
        }
        
        // Проверяем, подключен ли клиент (если мы хост)
        if (networkManager.ClientManager != null && networkManager.ClientManager.Started)
        {
            // Ждём пока клиент будет готов
            float timeout = 5f;
            float elapsed = 0f;
            
            while (!networkManager.ClientManager.Connection.IsValid && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!networkManager.ClientManager.Connection.IsValid)
            {
                Debug.LogWarning("[MainMenu] Client connection timeout!");
            }
        }
        
        Debug.Log("[MainMenu] Loading game scene via FishNet SceneManager...");
        
        // Загружаем сцену через FishNet SceneManager
        // Используем ReplaceOption.All чтобы заменить меню на игру
        SceneLoadData sld = new SceneLoadData(gameSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        
        networkManager.SceneManager.LoadGlobalScenes(sld);
        
        Debug.Log("[MainMenu] Scene load initiated!");
    }
    
    #endregion
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        
        Debug.Log($"[MainMenu] {message}");
    }
}
