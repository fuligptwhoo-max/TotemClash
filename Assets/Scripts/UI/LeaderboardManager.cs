using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Управляет таблицей лидеров (Leaderboard)
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    
    [Header("UI")]
    public GameObject leaderboardPanel; // Панель таблицы лидеров
    public Transform entriesContainer;  // Контейнер для записей
    public GameObject entryPrefab;      // Префаб записи (TextMeshPro)
    
    [Header("Settings")]
    public int maxEntries = 10;         // Максимум записей в таблице
    public bool showOnStart = true;     // Показывать ли сразу
    
    private List<PlayerScore> players = new List<PlayerScore>();
    private Dictionary<PlayerScore, GameObject> entries = new Dictionary<PlayerScore, GameObject>();
    
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
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(showOnStart);
        }
        
        // Создаём стартовые записи
        UpdateLeaderboard();
    }
    
    /// <summary>
    /// Регистрирует игрока в таблице лидеров
    /// </summary>
    public void RegisterPlayer(PlayerScore player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            CreateEntry(player);
            UpdateLeaderboard();
        }
    }
    
    /// <summary>
    /// Удаляет игрока из таблицы
    /// </summary>
    public void UnregisterPlayer(PlayerScore player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
            RemoveEntry(player);
            UpdateLeaderboard();
        }
    }
    
    /// <summary>
    /// Обновляет очки игрока и пересортировывает таблицу
    /// </summary>
    public void UpdatePlayerScore(PlayerScore player)
    {
        UpdateEntry(player);
        UpdateLeaderboard();
    }
    
    /// <summary>
    /// Переключает видимость таблицы (можно назначить на клавишу Tab)
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleLeaderboard();
        }
    }
    
    public void ToggleLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(!leaderboardPanel.activeSelf);
        }
    }
    
    public void ShowLeaderboard(bool show)
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(show);
        }
    }
    
    private void CreateEntry(PlayerScore player)
    {
        if (entryPrefab == null || entriesContainer == null) return;
        
        GameObject entry = Instantiate(entryPrefab, entriesContainer);
        entry.name = $"Entry_{player.GetPlayerName()}";
        entries[player] = entry;
        
        UpdateEntry(player);
    }
    
    private void RemoveEntry(PlayerScore player)
    {
        if (entries.ContainsKey(player))
        {
            Destroy(entries[player]);
            entries.Remove(player);
        }
    }
    
    private void UpdateEntry(PlayerScore player)
    {
        if (!entries.ContainsKey(player)) return;
        
        TMP_Text text = entries[player].GetComponent<TMP_Text>();
        if (text != null)
        {
            text.text = $"{player.GetPlayerName()}: {player.GetScore()}";
        }
    }
    
    private void UpdateLeaderboard()
    {
        // Сортируем игроков по очкам (убывание)
        var sortedPlayers = players.OrderByDescending(p => p.GetScore()).Take(maxEntries).ToList();
        
        // Обновляем позиции в UI
        for (int i = 0; i < sortedPlayers.Count; i++)
        {
            PlayerScore player = sortedPlayers[i];
            if (entries.ContainsKey(player))
            {
                // Устанавливаем позицию в иерархии (сортировка сверху вниз)
                entries[player].transform.SetSiblingIndex(i);
                
                // Выделяем цветом лидера
                TMP_Text text = entries[player].GetComponent<TMP_Text>();
                if (text != null)
                {
                    if (i == 0)
                        text.color = Color.yellow; // Первое место - золото
                    else if (i == 1)
                        text.color = Color.gray;   // Второе - серебро
                    else if (i == 2)
                        text.color = new Color(0.8f, 0.5f, 0.2f); // Бронза
                    else
                        text.color = Color.white;
                }
            }
        }
    }
}
