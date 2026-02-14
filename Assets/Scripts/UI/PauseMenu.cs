using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;

/// <summary>
/// Меню паузы (вызывается по ESC)
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    
    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    public GameObject serverSettingsPanel;
    
    [Header("Pause Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button serverSettingsButton;
    public Button quitToMenuButton;
    
    [Header("Settings Buttons")]
    public Button settingsBackButton;
    
    [Header("Server Settings Buttons")]
    public Button serverSettingsBackButton;
    public Button applyServerSettingsButton;
    
    [Header("Server Settings UI")]
    public Slider gameTimeSlider;
    public TMP_Text gameTimeValue;
    public Slider playerSpeedSlider;
    public TMP_Text playerSpeedValue;
    public Slider projectileSpeedSlider;
    public TMP_Text projectileSpeedValue;
    public Slider damageSlider;
    public TMP_Text damageValue;
    
    [Header("Network")]
    public NetworkManager networkManager;
    
    private bool isPaused = false;
    private bool isHost = false;
    
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
        
        // Проверяем Canvas
        SetupCanvas();
        
        // Кнопки меню паузы
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettings);
        
        if (serverSettingsButton != null)
            serverSettingsButton.onClick.AddListener(ShowServerSettings);
        
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(QuitToMenu);
        
        // Кнопки настроек
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(ShowPause);
        
        // Кнопки настроек сервера
        if (serverSettingsBackButton != null)
            serverSettingsBackButton.onClick.AddListener(ShowPause);
        
        if (applyServerSettingsButton != null)
            applyServerSettingsButton.onClick.AddListener(ApplyServerSettings);
        
        // Слайдеры
        SetupSliders();
        
        // Скрываем все панели
        HideAllPanels();
        
        Debug.Log("[PauseMenu] Initialized");
    }
    
    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(Resume);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettings);
        if (serverSettingsButton != null) serverSettingsButton.onClick.RemoveListener(ShowServerSettings);
        if (quitToMenuButton != null) quitToMenuButton.onClick.RemoveListener(QuitToMenu);
        if (settingsBackButton != null) settingsBackButton.onClick.RemoveListener(ShowPause);
        if (serverSettingsBackButton != null) serverSettingsBackButton.onClick.RemoveListener(ShowPause);
        if (applyServerSettingsButton != null) applyServerSettingsButton.onClick.RemoveListener(ApplyServerSettings);
    }
    
    private void SetupCanvas()
    {
        // Находим Canvas в иерархии
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        
        if (canvas != null)
        {
            // Добавляем GraphicRaycaster если нет
            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[PauseMenu] Added GraphicRaycaster to Canvas");
            }
            
            // Устанавливаем высокий sort order
            canvas.sortingOrder = 100;
        }
    }
    
    private void SetupSliders()
    {
        if (gameTimeSlider != null)
            gameTimeSlider.onValueChanged.AddListener(v => { if (gameTimeValue != null) gameTimeValue.text = $"{v:F0} сек"; });
        
        if (playerSpeedSlider != null)
            playerSpeedSlider.onValueChanged.AddListener(v => { if (playerSpeedValue != null) playerSpeedValue.text = $"{v:F1}"; });
        
        if (projectileSpeedSlider != null)
            projectileSpeedSlider.onValueChanged.AddListener(v => { if (projectileSpeedValue != null) projectileSpeedValue.text = $"{v:F1}"; });
        
        if (damageSlider != null)
            damageSlider.onValueChanged.AddListener(v => { if (damageValue != null) damageValue.text = $"{v:F0}"; });
    }
    
    private void CheckIfHost()
    {
        isHost = networkManager != null && networkManager.ServerManager.Started;
        
        // Показываем кнопку настроек сервера только хосту
        if (serverSettingsButton != null)
            serverSettingsButton.gameObject.SetActive(isHost);
    }
    
    private void Update()
    {
        // ESC для открытия/закрытия меню
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }
    
    public void Pause()
    {
        isPaused = true;
        
        CheckIfHost();
        ShowPause();
        
        // Замораживаем локального игрока
        FreezeLocalPlayer(true);
        
        // Скрываем прицел
        ShowCrosshair(false);
        
        // Показываем курсор
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log("[PauseMenu] Game paused");
    }
    
    public void Resume()
    {
        isPaused = false;
        
        HideAllPanels();
        
        // Размораживаем локального игрока
        FreezeLocalPlayer(false);
        
        // Показываем прицел
        ShowCrosshair(true);
        
        // Скрываем курсор
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        Debug.Log("[PauseMenu] Game resumed");
    }
    
    private void FreezeLocalPlayer(bool freeze)
    {
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                player.EnableControls(!freeze);
                break;
            }
        }
    }
    
    private void ShowCrosshair(bool show)
    {
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.IsOwner)
            {
                var aiming = player.GetComponent<AimingSystem>();
                if (aiming != null)
                {
                    aiming.ShowCrosshair(show);
                }
                break;
            }
        }
    }
    
    private void ShowPause()
    {
        HideAllPanels();
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            // Ставим на передний план
            pausePanel.transform.SetAsLastSibling();
        }
    }
    
    private void ShowSettings()
    {
        HideAllPanels();
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            settingsPanel.transform.SetAsLastSibling();
        }
    }
    
    private void ShowServerSettings()
    {
        if (!isHost) return;
        
        HideAllPanels();
        if (serverSettingsPanel != null)
        {
            serverSettingsPanel.SetActive(true);
            serverSettingsPanel.transform.SetAsLastSibling();
            UpdateServerSettingsUI();
        }
    }
    
    private void UpdateServerSettingsUI()
    {
        if (GameSettings.Instance == null) return;
        
        if (gameTimeSlider != null)
            gameTimeSlider.value = GameSettings.Instance.GetGameTime();
        
        if (playerSpeedSlider != null)
            playerSpeedSlider.value = GameSettings.Instance.GetPlayerSpeed();
        
        if (projectileSpeedSlider != null)
            projectileSpeedSlider.value = GameSettings.Instance.GetProjectileSpeed();
        
        if (damageSlider != null)
            damageSlider.value = GameSettings.Instance.GetDamage();
        
        if (gameTimeValue != null) gameTimeValue.text = $"{GameSettings.Instance.GetGameTime():F0} сек";
        if (playerSpeedValue != null) playerSpeedValue.text = $"{GameSettings.Instance.GetPlayerSpeed():F1}";
        if (projectileSpeedValue != null) projectileSpeedValue.text = $"{GameSettings.Instance.GetProjectileSpeed():F1}";
        if (damageValue != null) damageValue.text = $"{GameSettings.Instance.GetDamage():F0}";
    }
    
    private void ApplyServerSettings()
    {
        if (!isHost || GameSettings.Instance == null) return;
        
        if (gameTimeSlider != null)
            GameSettings.Instance.SetGameTime(gameTimeSlider.value);
        
        if (playerSpeedSlider != null)
            GameSettings.Instance.SetPlayerSpeed(playerSpeedSlider.value);
        
        if (projectileSpeedSlider != null)
            GameSettings.Instance.SetProjectileSpeed(projectileSpeedSlider.value);
        
        if (damageSlider != null)
            GameSettings.Instance.SetDamage((int)damageSlider.value);
        
        Debug.Log("[PauseMenu] Server settings applied");
    }
    
    private void QuitToMenu()
    {
        Resume();
        
        if (networkManager != null)
        {
            if (networkManager.ClientManager.Started)
                networkManager.ClientManager.StopConnection();
            if (networkManager.ServerManager.Started)
                networkManager.ServerManager.StopConnection(true);
        }
        
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    private void HideAllPanels()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (serverSettingsPanel != null) serverSettingsPanel.SetActive(false);
    }
    
    public bool IsPaused()
    {
        return isPaused;
    }
}
