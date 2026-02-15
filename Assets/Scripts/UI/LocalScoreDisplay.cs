using UnityEngine;
using TMPro;
using System.Collections;
using TotemClash.Combat;

namespace TotemClash.UI
{
    public class LocalScoreDisplay : MonoBehaviour
    {
        [Header("UI")]
        public TMP_Text scoreText;
        
        [Header("Format")]
        public string format = "Очки: {0}";
        
        private PlayerScore localPlayerScore;
        
        private void Start()
        {
            if (scoreText == null)
                scoreText = GetComponent<TMP_Text>();
            
            if (scoreText == null)
            {
                Debug.LogError("[LocalScoreDisplay] TMP_Text не найден!");
                enabled = false;
                return;
            }
            
            UpdateDisplay(0);
            StartCoroutine(SearchForPlayerCoroutine());
        }
        
        private IEnumerator SearchForPlayerCoroutine()
        {
            yield return new WaitForSeconds(0.5f); // Ждём спавн
            
            FindLocalPlayer();
            
            if (localPlayerScore == null)
            {
                yield return new WaitForSeconds(1f);
                FindLocalPlayer();
            }
        }
        
        private void FindLocalPlayer()
        {
            if (localPlayerScore != null) return; // Уже нашли
            
            // Ищем по тегу
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerScore score = player.GetComponent<PlayerScore>();
                if (score != null && !score.IsBot())
                {
                    SetPlayerScore(score);
                    return;
                }
            }
            
            // Fallback - ищем среди всех
            PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
            foreach (PlayerScore playerScore in allPlayers)
            {
                if (!playerScore.IsBot())
                {
                    SetPlayerScore(playerScore);
                    return;
                }
            }
        }
        
        private void SetPlayerScore(PlayerScore score)
        {
            if (score == null || score == localPlayerScore) return;
            
            // Отписываемся от старого если был
            if (localPlayerScore != null)
            {
                localPlayerScore.onScoreChanged.RemoveListener(OnScoreChanged);
            }
            
            localPlayerScore = score;
            localPlayerScore.onScoreChanged.AddListener(OnScoreChanged);
            
            // Обновляем сразу
            OnScoreChanged(localPlayerScore.GetScore());
            
            Debug.Log($"[LocalScoreDisplay] Connected to {localPlayerScore.GetPlayerName()}, score: {localPlayerScore.GetScore()}");
        }
        
        private void OnScoreChanged(int newScore)
        {
            UpdateDisplay(newScore);
        }
        
        private void UpdateDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = string.Format(format, score);
            }
        }
        
        private void OnDestroy()
        {
            if (localPlayerScore != null)
            {
                localPlayerScore.onScoreChanged.RemoveListener(OnScoreChanged);
            }
        }
    }
}