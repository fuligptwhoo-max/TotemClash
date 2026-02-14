using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Managing;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Меню окончания игры (появляется когда время вышло)
/// Работает как Leaderboard - показывает список игроков с очками
/// </summary>
public class GameOverMenu : MonoBehaviour
{
    public static GameOverMenu Instance { get; private set; }
    
    [Header("Panels")]
    public GameObject gameOverPanel;
    
    [Header("UI - как Leaderboard")]
    public TMP_Text winnerText;              // Текст победителя сверху
    public Transform scoresContainer;        // Контейнер для записей (как entriesContainer)
    public GameObject scoreEntryPrefab;      // Префаб записи (как entryPrefab)
    public TMP_Text localScoreText;          // Очки локального игрока отдельно
    
    [Header("Buttons")]
    public Button playAgainButton;
    public Button changeCharacterButton;
    public Button quitToMenuButton;
    
    [Header("Network")]
    public NetworkManager networkManager;
    
    // Список созданных записей
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
        if (networkManager == null)
            networkManager = FindFirstObjectByType<NetworkManager>();
        
        // Настраиваем Canvas для кликабельности
        SetupCanvas();
        
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);
        
        if (changeCharacterButton != null)
            changeCharacterButton.onClick.AddListener(OnChangeCharacter);
        
        if (quitToMenuButton != null)
            quitToMenuButton.onClick.AddListener(OnQuitToMenu);
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
            
        Debug.Log("[GameOverMenu] Initialized");
    }
    
    private void OnDestroy()
    {
        if (playAgainButton != null) playAgainButton.onClick.RemoveListener(OnPlayAgain);
        if (changeCharacterButton != null) changeCharacterButton.onClick.RemoveListener(OnChangeCharacter);
        if (quitToMenuButton != null) quitToMenuButton.onClick.RemoveListener(OnQuitToMenu);
    }
    
    /// <summary>
    /// Настраивает Canvas для работы UI
    /// </summary>
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
                Debug.Log("[GameOverMenu] Added GraphicRaycaster to Canvas");
            }
            
            // Устанавливаем высокий sort order чтобы быть поверх всего
            canvas.sortingOrder = 200;
            
            // Убедимся что Canvas включен
            canvas.enabled = true;
            
            Debug.Log($"[GameOverMenu] Canvas setup complete: SortOrder={canvas.sortingOrder}");
        }
        else
        {
            Debug.LogWarning("[GameOverMenu] Canvas not found in parent, but UI might still work if assigned in inspector.");
        }
        
        // Проверяем наличие EventSystem
        var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("[GameOverMenu] EventSystem not found in scene! If buttons don't work, add EventSystem via GameObject -> UI -> EventSystem");
        }
    }
    
    /// <summary>
    /// Показывает меню окончания игры с результатами
    /// </summary>
    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            // Ставим на передний план
            gameOverPanel.transform.SetAsLastSibling();
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        PopulateResults();
        
        Debug.Log("[GameOverMenu] Game Over shown");
    }
    
    /// <summary>
    /// Заполняет результаты - как в Leaderboard
    /// </summary>
    private void PopulateResults()
    {
        // Очищаем старые записи
        foreach (var entry in createdEntries)
        {
            if (entry != null) Destroy(entry);
        }
        createdEntries.Clear();
        
        // Находим всех игроков и сортируем по очкам
        var players = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        var sortedPlayers = players.OrderByDescending(p => p.GetScore()).ToList();
        
        Debug.Log($"[GameOverMenu] Found {sortedPlayers.Count} players");
        
        // Показываем победителя
        if (sortedPlayers.Count > 0 && winnerText != null)
        {
            var winner = sortedPlayers[0];
            winnerText.text = $"Победитель: {winner.GetPlayerName()} - {winner.GetScore()} очков";
        }
        else
        {
            if (winnerText != null)
                winnerText.text = "Нет победителя";
        }
        
        // Заполняем таблицу результатов - как в Leaderboard
        if (scoreEntryPrefab != null && scoresContainer != null)
        {
            int place = 1;
            foreach (var player in sortedPlayers)
            {
                CreateScoreEntry(place, player);
                place++;
            }
        }
        
        // Показываем очки локального игрока отдельно
        if (localScoreText != null)
        {
            foreach (var player in sortedPlayers)
            {
                var netObj = player.GetComponent<FishNet.Object.NetworkObject>();
                if (netObj != null && netObj.IsOwner)
                {
                    localScoreText.text = $"Ваш счет: {player.GetScore()} (место #{sortedPlayers.IndexOf(player) + 1})";
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Создаёт запись результата - как CreateEntry в Leaderboard
    /// </summary>
    private void CreateScoreEntry(int place, PlayerScore player)
    {
        if (scoreEntryPrefab == null || scoresContainer == null) return;
        
        GameObject entry = Instantiate(scoreEntryPrefab, scoresContainer);
        entry.name = $"Entry_{place}_{player.GetPlayerName()}";
        
        // Получаем TMP_Text из префаба
        TMP_Text text = entry.GetComponent<TMP_Text>();
        if (text == null)
            text = entry.GetComponentInChildren<TMP_Text>();
        
        if (text != null)
        {
            text.text = $"#{place} {player.GetPlayerName()}: {player.GetScore()}";
            
            // Цвета для призовых мест
            if (place == 1)
                text.color = Color.yellow;      // Золото
            else if (place == 2)
                text.color = new Color(0.7f, 0.7f, 0.7f); // Серебро
            else if (place == 3)
                text.color = new Color(0.8f, 0.5f, 0.2f); // Бронза
            else
                text.color = Color.white;
        }
        
        createdEntries.Add(entry);
    }
    
    private void OnPlayAgain()
    {
        Debug.Log("[GameOverMenu] Play Again clicked");
        
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Вызываем рестарт через GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RestartGame();
        }
        else
        {
            Debug.LogError("[GameOverMenu] GameManager.Instance is null!");
            // Fallback: перезагружаем текущую сцену
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
    
    private void OnChangeCharacter()
    {
        Debug.Log("[GameOverMenu] Change Character clicked - заглушка");
        
        if (winnerText != null)
        {
            winnerText.text = "Смена персонажа\n(В разработке)";
        }
    }
    
    private void OnQuitToMenu()
    {
        Debug.Log("[GameOverMenu] Quit to Menu clicked");
        
        // Скрываем панель
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Очищаем списки игроков в MyNetworkManager
        if (MyNetworkManager.Instance != null)
        {
            MyNetworkManager.Instance.ClearSpawnedPlayers();
        }
        
        // Отключаем сеть и загружаем меню
        if (networkManager != null)
        {
            // Останавливаем клиента и сервер
            if (networkManager.ClientManager.Started)
                networkManager.ClientManager.StopConnection();
            if (networkManager.ServerManager.Started)
                networkManager.ServerManager.StopConnection(true);
        }
        
        // Загружаем сцену меню
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
