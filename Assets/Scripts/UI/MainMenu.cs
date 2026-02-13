using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using FishNet.Managing;
using FishNet.Managing.Scened;

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
        
        // Запускаем клиент
        bool clientStarted = networkManager.ClientManager.StartConnection(ip);
        if (!clientStarted)
        {
            UpdateStatus("Failed to connect!");
            connectionInProgress = false;
            return;
        }
        
        Debug.Log("[MainMenu] Client started, waiting for connection...");
        
        // Ждём подключения к серверу
        StartCoroutine(LoadGameSceneWhenReady());
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
    /// Ждёт пока клиент подключится, затем загружает игровую сцену
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
        
        // Проверяем, подключен ли клиент (если мы хост или клиент)
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
