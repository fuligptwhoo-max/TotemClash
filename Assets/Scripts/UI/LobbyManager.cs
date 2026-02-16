using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using TotemClash.Network;

namespace TotemClash.UI
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance { get; private set; }
        
        [Header("Panels")]
        public GameObject lobbyPanel;
        public GameObject settingsPanel;
        
        [Header("Settings UI")]
        public Slider gameTimeSlider;
        public TMP_Text gameTimeValue;
        public Slider playerSpeedSlider;
        public TMP_Text playerSpeedValue;
        public Slider projectileSpeedSlider;
        public TMP_Text projectileSpeedValue;
        public Slider damageSlider;
        public TMP_Text damageValue;
        public Slider scoreLimitSlider; // НОВОЕ
        public TMP_Text scoreLimitValue; // НОВОЕ
        public Toggle useScoreLimitToggle; // НОВОЕ
        
        [Header("Buttons")]
        public Button startGameButton;
        public Button backButton;
        public Button settingsBackButton;
        public Button resetDefaultsButton;
        
        [Header("Scene")]
        public string gameSceneName = "SampleScene";
        
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
            if (startGameButton != null)
                startGameButton.onClick.AddListener(OnStartGameClicked);
            
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(OnBackClicked);
            
            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(ResetSettings);
            
            SetupSliders();
            
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (startGameButton != null)
                startGameButton.onClick.RemoveListener(OnStartGameClicked);
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
            if (settingsBackButton != null)
                settingsBackButton.onClick.RemoveListener(OnBackClicked);
        }
        
        public void ShowLobby()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            
            UpdateSettingsUI();
        }
        
        private void OnBackClicked()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            
            MainMenu mainMenu = FindFirstObjectByType<MainMenu>();
            if (mainMenu != null)
            {
                mainMenu.ShowMainMenu();
            }
        }
        
        private void SetupSliders()
        {
            if (gameTimeSlider != null)
            {
                gameTimeSlider.minValue = 60f;
                gameTimeSlider.maxValue = 600f;
                gameTimeSlider.wholeNumbers = true;
                gameTimeSlider.onValueChanged.AddListener(v => { 
                    if (GameSettings.Instance != null) GameSettings.Instance.SetGameTime(v);
                    UpdateSettingsText();
                });
            }
            
            if (playerSpeedSlider != null)
            {
                playerSpeedSlider.minValue = 4f;
                playerSpeedSlider.maxValue = 15f;
                playerSpeedSlider.onValueChanged.AddListener(v => { 
                    if (GameSettings.Instance != null) GameSettings.Instance.SetPlayerSpeed(v);
                    UpdateSettingsText();
                });
            }
            
            if (projectileSpeedSlider != null)
            {
                projectileSpeedSlider.minValue = 10f;
                projectileSpeedSlider.maxValue = 40f;
                projectileSpeedSlider.onValueChanged.AddListener(v => { 
                    if (GameSettings.Instance != null) GameSettings.Instance.SetProjectileSpeed(v);
                    UpdateSettingsText();
                });
            }
            
            if (damageSlider != null)
            {
                damageSlider.minValue = 10f;
                damageSlider.maxValue = 100f;
                damageSlider.wholeNumbers = true;
                damageSlider.onValueChanged.AddListener(v => { 
                    if (GameSettings.Instance != null) GameSettings.Instance.SetDamage((int)v);
                    UpdateSettingsText();
                });
            }
            
            // НОВОЕ: Слайдер лимита очков
            if (scoreLimitSlider != null)
            {
                scoreLimitSlider.minValue = 100f;
                scoreLimitSlider.maxValue = 5000f;
                scoreLimitSlider.wholeNumbers = true;
                scoreLimitSlider.onValueChanged.AddListener(v => { 
                    if (GameSettings.Instance != null) GameSettings.Instance.SetScoreToWin((int)v);
                    UpdateSettingsText();
                });
            }
            
            // НОВОЕ: Тоггл использования лимита
            if (useScoreLimitToggle != null)
            {
                useScoreLimitToggle.onValueChanged.AddListener(v => {
                    if (GameSettings.Instance != null) GameSettings.Instance.SetUseScoreLimit(v);
                    if (scoreLimitSlider != null) scoreLimitSlider.interactable = v;
                });
            }
        }
        
        private void UpdateSettingsUI()
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
                
            if (scoreLimitSlider != null)
                scoreLimitSlider.value = GameSettings.Instance.GetScoreToWin();
                
            if (useScoreLimitToggle != null)
            {
                useScoreLimitToggle.isOn = GameSettings.Instance.UseScoreLimit();
                if (scoreLimitSlider != null) 
                    scoreLimitSlider.interactable = useScoreLimitToggle.isOn;
            }
            
            UpdateSettingsText();
        }
        
        private void UpdateSettingsText()
        {
            if (GameSettings.Instance == null) return;
            
            if (gameTimeValue != null) 
                gameTimeValue.text = $"{GameSettings.Instance.GetGameTime():F0} сек";
            if (playerSpeedValue != null) 
                playerSpeedValue.text = $"{GameSettings.Instance.GetPlayerSpeed():F1}";
            if (projectileSpeedValue != null) 
                projectileSpeedValue.text = $"{GameSettings.Instance.GetProjectileSpeed():F1}";
            if (damageValue != null) 
                damageValue.text = $"{GameSettings.Instance.GetDamage():F0}";
            if (scoreLimitValue != null)
                scoreLimitValue.text = $"{GameSettings.Instance.GetScoreToWin():F0}";
        }
        
        private void ResetSettings()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.ResetToDefaults();
                UpdateSettingsUI();
            }
        }
        
        private void OnStartGameClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }
}