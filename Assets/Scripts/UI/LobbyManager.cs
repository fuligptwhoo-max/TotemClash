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
        public GameObject lobbyPanel; // Главная панель лобби (если есть)
        public GameObject settingsPanel; // ServerSettingsPanel
        
        [Header("Settings UI")]
        public Slider gameTimeSlider;
        public TMP_Text gameTimeValue;
        public Slider playerSpeedSlider;
        public TMP_Text playerSpeedValue;
        public Slider projectileSpeedSlider;
        public TMP_Text projectileSpeedValue;
        public Slider damageSlider;
        public TMP_Text damageValue;
        
        [Header("Buttons")]
        public Button startGameButton;
        public Button backButton; // Кнопка Назад
        public Button settingsBackButton; // Кнопка Назад внутри SettingsPanel (если отличается)
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
                settingsBackButton.onClick.AddListener(OnBackClicked); // Тот же метод
            
            if (resetDefaultsButton != null)
                resetDefaultsButton.onClick.AddListener(ResetSettings);
            
            SetupSliders();
            
            // Скрываем всё при старте
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
        
        // Показывает панель настроек (вызывается из MainMenu)
        public void ShowLobby()
        {
            if (lobbyPanel != null) lobbyPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(true); // Показываем сразу настройки
            
            UpdateSettingsUI();
        }
        
        // ИСПРАВЛЕНО: Back возвращает в MainMenu
        private void OnBackClicked()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(false);
            
            // Находим MainMenu и показываем его
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