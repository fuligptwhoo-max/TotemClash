using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TotemClash.Combat;

namespace TotemClash.UI
{
    public class GameOverMenu : MonoBehaviour
    {
        public static GameOverMenu Instance { get; private set; }
        
        [Header("Panels")]
        public GameObject gameOverPanel;
        
        [Header("UI")]
        public TMP_Text winnerText;
        public Transform scoresContainer;
        public GameObject scoreEntryPrefab;
        public TMP_Text localScoreText;
        
        [Header("Buttons")]
        public Button playAgainButton;
        public Button quitToMenuButton;
        
        private List<GameObject> createdEntries = new List<GameObject>();
        
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
            
            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgain);
            
            if (quitToMenuButton != null)
                quitToMenuButton.onClick.AddListener(OnQuitToMenu);
            
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            Debug.Log("[GameOverMenu] Initialized (Single Player)");
        }
        
        private void OnDestroy()
        {
            if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
            if (quitToMenuButton != null) quitToMenuButton.onClick.RemoveListener(OnQuitToMenu);
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
                    Debug.Log("[GameOverMenu] Added GraphicRaycaster to Canvas");
                }
                
                canvas.sortingOrder = 200;
                canvas.enabled = true;
            }
        }
        
        public void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverPanel.transform.SetAsLastSibling();
            }
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            PopulateResults();
            
            Debug.Log("[GameOverMenu] Game Over shown");
        }
        
        private void PopulateResults()
        {
            // Очищаем старые записи
            foreach (var entry in createdEntries)
            {
                if (entry != null) Destroy(entry);
            }
            createdEntries.Clear();
            
            var players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
            var sortedPlayers = players.OrderByDescending(p => p.GetScore()).ToList();
            
            Debug.Log($"[GameOverMenu] Found {sortedPlayers.Count} players");
            
            PlayerScore localPlayer = null;
            foreach (var player in sortedPlayers)
            {
                if (!player.IsBot())
                {
                    localPlayer = player;
                    break;
                }
            }
            
            if (sortedPlayers.Count > 0 && winnerText != null)
            {
                var winner = sortedPlayers[0];
                string winnerType = winner.IsBot() ? "[БОТ] " : "";
                winnerText.text = $"Победитель: {winnerType}{winner.GetPlayerName()} - {winner.GetScore()} очков";
                
                if (winner == localPlayer)
                {
                    winnerText.color = Color.yellow;
                }
                else
                {
                    winnerText.color = Color.red;
                }
            }
            else
            {
                if (winnerText != null)
                    winnerText.text = "Нет победителя";
            }
            
            if (scoreEntryPrefab != null && scoresContainer != null)
            {
                int place = 1;
                foreach (var player in sortedPlayers)
                {
                    CreateScoreEntry(place, player, player == localPlayer);
                    place++;
                }
            }
            
            if (localScoreText != null && localPlayer != null)
            {
                int playerPlace = sortedPlayers.IndexOf(localPlayer) + 1;
                localScoreText.text = $"Ваш счет: {localPlayer.GetScore()} (место #{playerPlace})";
            }
        }
        
        private void CreateScoreEntry(int place, PlayerScore player, bool isLocalPlayer)
        {
            if (scoreEntryPrefab == null || scoresContainer == null) return;
            
            GameObject entry = Instantiate(scoreEntryPrefab, scoresContainer);
            entry.name = $"Entry_{place}_{player.GetPlayerName()}";
            
            TMP_Text text = entry.GetComponent<TMP_Text>();
            if (text == null)
                text = entry.GetComponentInChildren<TMP_Text>();
            
            if (text != null)
            {
                string botPrefix = player.IsBot() ? "[БОТ] " : "";
                string playerPrefix = isLocalPlayer ? "(Вы) " : "";
                text.text = $"#{place} {playerPrefix}{botPrefix}{player.GetPlayerName()}: {player.GetScore()}";
                
                if (place == 1)
                    text.color = Color.yellow;
                else if (place == 2)
                    text.color = new Color(0.7f, 0.7f, 0.7f);
                else if (place == 3)
                    text.color = new Color(0.8f, 0.5f, 0.2f);
                else
                    text.color = Color.white;
                
                if (isLocalPlayer)
                {
                    text.fontStyle = FontStyles.Bold;
                }
            }
            
            createdEntries.Add(entry);
        }
        
        // ИСПРАВЛЕНО: Полная перезагрузка сцены для корректного рестарта
        private void OnPlayAgain()
        {
            Debug.Log("[GameOverMenu] Play Again clicked - reloading scene");
            
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            
            Time.timeScale = 1f;
            
            // Полная перезагрузка сцены вместо попытки рестарта внутри игры
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            UnityEngine.SceneManagement.SceneManager.LoadScene(currentScene);
        }
        
        private void OnQuitToMenu()
        {
            Debug.Log("[GameOverMenu] Quit to Menu clicked");
            
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
            
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}