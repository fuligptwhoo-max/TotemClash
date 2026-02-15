using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace TotemClash.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("Main Menu")]
        public GameObject mainMenuPanel;
        public Button playButton;
        public Button settingsButton; // Кнопка для открытия настроек (Lobby)
        public Button quitButton;
        public TMP_Text titleText;
        
        [Header("Loading")]
        public GameObject loadingPanel;

        private void Start()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (playButton != null)
                playButton.onClick.AddListener(StartGame);
            
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            
            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);
            
            if (titleText != null)
                titleText.text = "TOTEM CLASH";
            
            ShowMainMenu();
        }
        
        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(StartGame);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }
        
        // Показывает главное меню, скрывает остальное
        public void ShowMainMenu()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (loadingPanel != null) loadingPanel.SetActive(false);
            
            // Скрываем Lobby/Settings если они есть
            LobbyManager lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                if (lobby.lobbyPanel != null) lobby.lobbyPanel.SetActive(false);
                if (lobby.settingsPanel != null) lobby.settingsPanel.SetActive(false);
            }
        }
        
        // Открывает настройки (через LobbyManager)
        public void OpenSettings()
        {
            LobbyManager lobby = FindFirstObjectByType<LobbyManager>();
            if (lobby != null)
            {
                lobby.ShowLobby(); // Показывает панель настроек
                if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            }
        }
        
        public void StartGame()
        {
            ShowLoading();
            SceneManager.LoadScene("SampleScene");
        }
        
        public void ShowLoading()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(true);
        }
        
        public void QuitGame()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}