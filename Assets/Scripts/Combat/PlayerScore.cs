using UnityEngine;
using UnityEngine.Events;
using TotemClash.UI;

namespace TotemClash.Combat
{
    public class PlayerScore : MonoBehaviour
    {
        [System.Serializable]
        public class ScoreChangedEvent : UnityEvent<int> { }
        
        private int score = 0;
        private string playerName;
        private bool isBot = false;
        
        public ScoreChangedEvent onScoreChanged = new ScoreChangedEvent();
        
        // ИСПРАВЛЕНО: Ссылка на тотем для начисления очков
        private TotemController totem;
        private float carryTimeAccumulator = 0f;
        
        private void Start()
        {
            if (string.IsNullOrEmpty(playerName))
            {
                playerName = $"Player {gameObject.name}";
            }
            
            // ИСПРАВЛЕНО: Находим тотем
            totem = FindFirstObjectByType<TotemController>();
            
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.RegisterPlayer(this);
            }
        }
        
        private void Update()
        {
            // ИСПРАВЛЕНО: Начисляем очки за ношение тотема
            if (totem != null && totem.GetCarrier() == gameObject)
            {
                carryTimeAccumulator += Time.deltaTime;
                
                // Каждую секунду начисляем очки
                if (carryTimeAccumulator >= 1f)
                {
                    int points = Mathf.FloorToInt(totem.GetCarryMultiplier());
                    AddScore(points);
                    carryTimeAccumulator -= 1f;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.UnregisterPlayer(this);
            }
        }
        
        public void AddScore(int points)
        {
            if (points <= 0) return;
            
            score += points;
            onScoreChanged?.Invoke(score);
            
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.UpdatePlayerScore(this);
            }
            
            Debug.Log($"[PlayerScore] {playerName} gained {points} points. Total: {score}");
        }
        
        public void ResetScore()
        {
            score = 0;
            carryTimeAccumulator = 0f;
            onScoreChanged?.Invoke(score);
            
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.UpdatePlayerScore(this);
            }
        }
        
        public void SetPlayerName(string name)
        {
            playerName = name;
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.UpdatePlayerScore(this);
            }
        }
        
        public string GetPlayerName() => playerName;
        public int GetScore() => score;
        public void SetIsBot(bool value) => isBot = value;
        public bool IsBot() => isBot;
        
        // ИСПРАВЛЕНО: Публичный метод для начисления очков за убийство
        public void OnKillEnemy(GameObject enemy)
        {
            AddScore(100); // 100 очков за убийство
            Debug.Log($"[PlayerScore] {playerName} killed {enemy.name}, +100 points");
        }
    }
}