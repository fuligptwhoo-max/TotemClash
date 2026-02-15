using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using TotemClash.Combat;

namespace TotemClash.UI
{
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
        public string botPrefix = "[BOT] ";
        
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
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleLeaderboard();
            }
        }
        
        public void RegisterPlayer(PlayerScore player)
        {
            if (player == null || players.Contains(player)) return;
            
            players.Add(player);
            player.onScoreChanged.AddListener(OnPlayerScoreChanged);
            
            CreateEntry(player);
            UpdateLeaderboard();
            
            Debug.Log($"[LeaderboardManager] Registered: {player.GetPlayerName()}");
        }
        
        public void UnregisterPlayer(PlayerScore player)
        {
            if (player == null || !players.Contains(player)) return;
            
            player.onScoreChanged.RemoveListener(OnPlayerScoreChanged);
            players.Remove(player);
            RemoveEntry(player);
            UpdateLeaderboard();
        }
        
        public void UpdatePlayerScore(PlayerScore player)
        {
            if (player == null) return;
            
            if (!players.Contains(player))
            {
                RegisterPlayer(player);
                return;
            }
            
            UpdateEntry(player);
            UpdateLeaderboard();
        }
        
        private void OnPlayerScoreChanged(int newScore)
        {
            // Находим игрока с этим счетом и обновляем
            foreach (var player in players)
            {
                if (player != null && player.GetScore() == newScore)
                {
                    UpdateEntry(player);
                    UpdateLeaderboard();
                    break;
                }
            }
        }
        
        private void CreateEntry(PlayerScore player)
        {
            if (entryPrefab == null || entriesContainer == null) return;
            if (entries.ContainsKey(player)) return;
            
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
            if (player == null || !entries.ContainsKey(player)) return;
            if (entries[player] == null) return;
            
            TMP_Text text = entries[player].GetComponent<TMP_Text>();
            if (text == null) text = entries[player].GetComponentInChildren<TMP_Text>();
            
            if (text != null)
            {
                string displayName = player.GetPlayerName();
                if (player.IsBot() && !displayName.StartsWith(botPrefix))
                {
                    displayName = botPrefix + displayName;
                }
                
                int rank = GetPlayerRank(player);
                text.text = $"{rank}. {displayName}: {player.GetScore()}";
            }
        }
        
        private void UpdateLeaderboard()
        {
            // Сортируем по очкам
            var sortedPlayers = players.OrderByDescending(p => p.GetScore()).Take(maxEntries).ToList();
            
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                PlayerScore player = sortedPlayers[i];
                if (entries.ContainsKey(player) && entries[player] != null)
                {
                    entries[player].transform.SetSiblingIndex(i);
                    
                    TMP_Text text = entries[player].GetComponent<TMP_Text>();
                    if (text != null)
                    {
                        // Цвета для рангов
                        if (i == 0) text.color = Color.yellow;
                        else if (i == 1) text.color = new Color(0.7f, 0.7f, 0.7f);
                        else if (i == 2) text.color = new Color(0.8f, 0.5f, 0.2f);
                        else text.color = Color.white;
                        
                        // Жирным для локального игрока
                        if (!player.IsBot())
                        {
                            text.fontStyle = FontStyles.Bold;
                        }
                    }
                    
                    UpdateEntry(player);
                }
            }
        }
        
        public int GetPlayerRank(PlayerScore player)
        {
            if (player == null || !players.Contains(player)) return 0;
            var sorted = players.OrderByDescending(p => p.GetScore()).ToList();
            return sorted.IndexOf(player) + 1;
        }
        
        public void ToggleLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(!leaderboardPanel.activeSelf);
            }
        }
        
        public void ClearAll()
        {
            foreach (var player in players)
            {
                if (player != null)
                    player.onScoreChanged.RemoveListener(OnPlayerScoreChanged);
            }
            
            foreach (var entry in entries.Values)
            {
                if (entry != null) Destroy(entry);
            }
            
            entries.Clear();
            players.Clear();
        }
    }
}