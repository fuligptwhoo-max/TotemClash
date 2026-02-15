using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TotemClash.Combat;
using TotemClash.Network;

namespace TotemClash.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }
        
        [Header("Panels")]
        public GameObject pausePanel;
        public GameObject settingsPanel;
        
        [Header("Pause Buttons")]
        public Button resumeButton;
        public Button settingsButton;
        public Button quitToMenuButton;
        
        [Header("Settings Buttons")]
        public Button settingsBackButton;
        public Button applyButton; // ИСПРАВЛЕНО: Кнопка Apply
        
        [Header("Settings UI")]
        public Slider gameTimeSlider;
        public TMP_Text gameTimeValue;
        public Slider playerSpeedSlider;
        public TMP_Text playerSpeedValue;
        public Slider projectileSpeedSlider;
        public TMP_Text projectileSpeedValue;
        public Slider damageSlider;
        public TMP_Text damageValue;
        
        private bool isPaused = false;
        
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
            SetupCanvas();
            
            if (resumeButton != null)
                resumeButton.onClick.AddListener(Resume);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(ShowSettings);
            
            if (quitToMenuButton != null)
                quitToMenuButton.onClick.AddListener(QuitToMenu);
            
            if (settingsBackButton != null)
                settingsBackButton.onClick.AddListener(ShowPause); // Back просто закрывает
            
            // ИСПРАВЛЕНО: Apply применяет настройки
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            
            SetupSettingsSliders();
            UpdateSettingsUI();
            HideAllPanels();
            
            Debug.Log("[PauseMenu] Initialized");
        }
        
        private void SetupCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }
            
            if (canvas != null)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                }
                
                canvas.sortingOrder = 100;
                canvas.enabled = true;
            }
        }
        
        private void SetupSettingsSliders()
        {
            if (gameTimeSlider != null)
            {
                gameTimeSlider.minValue = 60f;
                gameTimeSlider.maxValue = 600f;
                gameTimeSlider.wholeNumbers = true;
                gameTimeSlider.onValueChanged.AddListener(OnGameTimeSliderChanged);
            }
            
            if (playerSpeedSlider != null)
            {
                playerSpeedSlider.minValue = 4f;
                playerSpeedSlider.maxValue = 15f;
                playerSpeedSlider.onValueChanged.AddListener(OnPlayerSpeedSliderChanged);
            }
            
            if (projectileSpeedSlider != null)
            {
                projectileSpeedSlider.minValue = 10f;
                projectileSpeedSlider.maxValue = 40f;
                projectileSpeedSlider.onValueChanged.AddListener(OnProjectileSpeedSliderChanged);
            }
            
            if (damageSlider != null)
            {
                damageSlider.minValue = 10f;
                damageSlider.maxValue = 100f;
                damageSlider.wholeNumbers = true;
                damageSlider.onValueChanged.AddListener(OnDamageSliderChanged);
            }
        }
        
        // ИСПРАВЛЕНО: Метод Apply настроек
        public void ApplySettings()
        {
            if (GameSettings.Instance == null) return;
            
            if (gameTimeSlider != null)
                GameSettings.Instance.SetGameTime(gameTimeSlider.value);
            
            if (playerSpeedSlider != null)
                GameSettings.Instance.SetPlayerSpeed(playerSpeedSlider.value);
            
            if (projectileSpeedSlider != null)
                GameSettings.Instance.SetProjectileSpeed(projectileSpeedSlider.value);
            
            if (damageSlider != null)
                GameSettings.Instance.SetDamage((int)damageSlider.value);
                
            Debug.Log("[PauseMenu] Settings Applied!");
            
            // Опционально: показать сообщение или закрыть меню
            // ShowPause(); // Раскомментируй если хочешь закрывать меню после Apply
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
            
            // ИСПРАВЛЕНО: Текст берем со слайдеров (текущие значения), а не из GameSettings
            if (gameTimeValue != null) 
                gameTimeValue.text = $"{gameTimeSlider.value:F0} сек";
            if (playerSpeedValue != null) 
                playerSpeedValue.text = $"{playerSpeedSlider.value:F1}";
            if (projectileSpeedValue != null) 
                projectileSpeedValue.text = $"{projectileSpeedSlider.value:F1}";
            if (damageValue != null) 
                damageValue.text = $"{damageSlider.value:F0}";
        }
        
        // ИСПРАВЛЕНО: Слайдеры только обновляют текст, не применяют настройки
        private void OnGameTimeSliderChanged(float value)
        {
            UpdateSettingsText();
        }
        
        private void OnPlayerSpeedSliderChanged(float value)
        {
            UpdateSettingsText();
        }
        
        private void OnProjectileSpeedSliderChanged(float value)
        {
            UpdateSettingsText();
        }
        
        private void OnDamageSliderChanged(float value)
        {
            UpdateSettingsText();
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isPaused && settingsPanel != null && settingsPanel.activeSelf)
                {
                    ShowPause(); // Back поведение
                }
                else if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
        
        public void Pause()
        {
            if (isPaused) return;
            
            isPaused = true;
            ShowPause();
            Time.timeScale = 0f;
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.EnableControls(false);
                }
                
                AimingSystem aiming = player.GetComponent<AimingSystem>();
                if (aiming != null)
                {
                    aiming.ShowCrosshair(false);
                }
            }
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        public void Resume()
        {
            if (!isPaused) return;
            
            isPaused = false;
            HideAllPanels();
            Time.timeScale = 1f;
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.EnableControls(true);
                }
                
                AimingSystem aiming = player.GetComponent<AimingSystem>();
                if (aiming != null)
                {
                    aiming.ShowCrosshair(true);
                }
            }
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }
        
        public void ShowSettings()
        {
            if (pausePanel != null) 
                pausePanel.SetActive(false);
            
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                settingsPanel.transform.SetAsLastSibling();
                UpdateSettingsUI(); // Загружаем текущие настройки
            }
        }
        
        // ИСПРАВЛЕНО: Back сбрасывает значения слайдеров к примененным настройкам
        private void ShowPause()
        {
            if (settingsPanel != null) 
                settingsPanel.SetActive(false);
            
            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
                pausePanel.transform.SetAsLastSibling();
            }
            
            // Сбрасываем слайдеры к реальным значениям (отменяем непримененные изменения)
            UpdateSettingsUI();
        }
        
        private void QuitToMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        
        private void HideAllPanels()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }
        
        public bool IsPaused()
        {
            return isPaused;
        }
    }
}