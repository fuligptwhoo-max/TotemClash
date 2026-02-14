using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Управляет таблицей лидеров (Leaderboard)
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    
    [Header("UI")]
    public GameObject leaderboardPanel;
    public Transform entriesContainer;
    public GameObject entryPrefab;
    
    [Header("Settings")]
    public int maxEntries = 10;
    public bool showOnStart = false;
    
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
        
        // Ищем уже существующих игроков
        StartCoroutine(FindExistingPlayers());
    }
    
    private IEnumerator FindExistingPlayers()
    {
        // Ждём немного пока все игроки спавнятся
        yield return new WaitForSeconds(1f);
        
        var existingPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
        Debug.Log($"[LeaderboardManager] Found {existingPlayers.Length} existing players");
        
        foreach (var player in existingPlayers)
        {
            if (!players.Contains(player))
            {
                RegisterPlayer(player);
            }
        }
    }
    
    /// <summary>
    /// Регистрирует игрока в таблице лидеров
    /// </summary>
    public void RegisterPlayer(PlayerScore player)
    {
        if (player == null) return;
        
        if (!players.Contains(player))
        {
            players.Add(player);
            CreateEntry(player);
            UpdateLeaderboard();
            Debug.Log($"[LeaderboardManager] Player registered: {player.GetPlayerName()}, Score: {player.GetScore()}");
        }
    }
    
    /// <summary>
    /// Удаляет игрока из таблицы
    /// </summary>
    public void UnregisterPlayer(PlayerScore player)
    {
        if (player == null) return;
        
        if (players.Contains(player))
        {
            players.Remove(player);
            RemoveEntry(player);
            UpdateLeaderboard();
            Debug.Log($"[LeaderboardManager] Player unregistered: {player.GetPlayerName()}");
        }
    }
    
    /// <summary>
    /// Обновляет очки игрока и пересортировывает таблицу
    /// </summary>
    public void UpdatePlayerScore(PlayerScore player)
    {
        if (player == null) return;
        
        // Если игрок ещё не зарегистрирован - регистрируем
        if (!players.Contains(player))
        {
            RegisterPlayer(player);
            return;
        }
        
        UpdateEntry(player);
        UpdateLeaderboard();
    }
    
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
        if (entries.ContainsKey(player) && entries[player] != null)
        {
            Destroy(entries[player]);
            entries.Remove(player);
        }
    }
    
    private void UpdateEntry(PlayerScore player)
    {
        if (player == null) return;
        if (!entries.ContainsKey(player)) return;
        if (entries[player] == null) return;
        
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
            if (entries.ContainsKey(player) && entries[player] != null)
            {
                // Устанавливаем позицию в иерархии
                entries[player].transform.SetSiblingIndex(i);
                
                // Выделяем цветом
                TMP_Text text = entries[player].GetComponent<TMP_Text>();
                if (text != null)
                {
                    if (i == 0)
                        text.color = Color.yellow;
                    else if (i == 1)
                        text.color = Color.gray;
                    else if (i == 2)
                        text.color = new Color(0.8f, 0.5f, 0.2f);
                    else
                        text.color = Color.white;
                }
            }
        }
    }
    
    public void ClearAll()
    {
        foreach (var entry in entries.Values)
        {
            if (entry != null) Destroy(entry);
        }
        entries.Clear();
        players.Clear();
        Debug.Log("[LeaderboardManager] Cleared all entries");
    }
}
