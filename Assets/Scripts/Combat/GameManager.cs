using UnityEngine;
using TMPro;
using UnityEngine.Events;
using TotemClash.UI;
using TotemClash.Network;
using System.Linq;

namespace TotemClash.Combat
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Game Settings")]
        public float gameTime = 300f;
        
        [Header("UI")]
        public TMP_Text timerText;
        public TMP_Text globalScoreText;
        public TMP_Text warningText;
        public TMP_Text scoreLimitText; // Текст лимита очков (опционально)
        
        [Header("References")]
        public TotemController totem;
        public CountdownDisplay countdownDisplay;
        public GameOverMenu gameOverMenu;
        
        private float currentTime;
        private int totalScore = 0;
        private bool isGameActive = false;
        private bool gameEnded = false;
        private float carrierScoreAccumulator = 0f;
        private float originalGameTime = 300f;
        
        [Header("Events")]
        public UnityEvent<float> OnTimeChanged;
        public UnityEvent<int> OnScoreChanged;
        public UnityEvent OnGameStarted;
        public UnityEvent OnGameEnded;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            if (timerText == null)
                timerText = GameObject.Find("TimerText")?.GetComponent<TMP_Text>();
            
            if (countdownDisplay == null)
                countdownDisplay = FindFirstObjectByType<CountdownDisplay>();
            if (gameOverMenu == null)
                gameOverMenu = FindFirstObjectByType<GameOverMenu>();
            
            FindTotem();
            
            ApplyGameTimeSettings();
            
            currentTime = gameTime;
            originalGameTime = gameTime;
            totalScore = 0;
            isGameActive = false;
            gameEnded = false;
            
            UpdateScoreLimitUI();
            
            if (countdownDisplay != null)
            {
                countdownDisplay.StartCountdown();
                StartCoroutine(StartGameAfterCountdown());
            }
            else
            {
                StartGame();
            }
            
            UpdateTimerUI();
            UpdateScoreUI();
            
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnGameTimeChanged.AddListener(OnGameTimeSettingChanged);
                GameSettings.Instance.OnScoreToWinChanged.AddListener(OnScoreLimitChanged);
            }
        }
        
        private void OnDestroy()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnGameTimeChanged.RemoveListener(OnGameTimeSettingChanged);
                GameSettings.Instance.OnScoreToWinChanged.RemoveListener(OnScoreLimitChanged);
            }
        }
        
        private void ApplyGameTimeSettings()
        {
            if (GameSettings.Instance != null)
            {
                gameTime = GameSettings.Instance.GetGameTime();
                originalGameTime = gameTime;
                Debug.Log($"[GameManager] Applied game time from settings: {gameTime}");
            }
        }
        
        private void OnGameTimeSettingChanged(float newTime)
        {
            if (isGameActive && !gameEnded)
            {
                float elapsedTime = originalGameTime - currentTime;
                
                if (newTime < elapsedTime)
                {
                    ShowWarning($"Введенное время ({FormatTime(newTime)}) меньше прошедшего ({FormatTime(elapsedTime)})!");
                    Debug.LogWarning($"[GameManager] Cannot set time to {newTime}, already elapsed {elapsedTime}");
                    return;
                }
                
                gameTime = newTime;
                currentTime = newTime - elapsedTime;
                originalGameTime = newTime;
                
                Debug.Log($"[GameManager] Game time updated to: {newTime}, elapsed: {elapsedTime}, current: {currentTime}");
            }
            else if (!isGameActive && !gameEnded)
            {
                gameTime = newTime;
                currentTime = newTime;
                originalGameTime = newTime;
                Debug.Log($"[GameManager] Game time set to: {newTime} (game not started)");
            }
            
            UpdateTimerUI();
        }
        
        private void OnScoreLimitChanged(int newLimit)
        {
            UpdateScoreLimitUI();
        }
        
        private void UpdateScoreLimitUI()
        {
            if (scoreLimitText != null && GameSettings.Instance != null)
            {
                if (GameSettings.Instance.UseScoreLimit())
                {
                    scoreLimitText.text = $"Цель: {GameSettings.Instance.GetScoreToWin()} очков";
                    scoreLimitText.gameObject.SetActive(true);
                }
                else
                {
                    scoreLimitText.gameObject.SetActive(false);
                }
            }
        }
        
        private void ShowWarning(string message)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.gameObject.SetActive(true);
                
                CancelInvoke(nameof(HideWarning));
                Invoke(nameof(HideWarning), 3f);
            }
            else
            {
                Debug.LogWarning($"[GameManager] Warning: {message}");
            }
        }
        
        private void HideWarning()
        {
            if (warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }
        
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60);
            return $"{minutes:00}:{seconds:00}";
        }
        
        private System.Collections.IEnumerator StartGameAfterCountdown()
        {
            yield return new WaitForSeconds(countdownDisplay.countdownDuration + 1f);
            StartGame();
            Debug.Log("[GameManager] Game started!");
        }
        
        private void Update()
        {
            if (isGameActive && !gameEnded)
            {
                UpdateGameTime();
                UpdateScoreFromTotem();
                CheckWinCondition(); // НОВАЯ ПРОВЕРКА
            }
        }
        
        // НОВЫЙ МЕТОД: Проверка условия победы
        private void CheckWinCondition()
        {
            if (GameSettings.Instance == null || !GameSettings.Instance.UseScoreLimit())
                return;
                
            int scoreLimit = GameSettings.Instance.GetScoreToWin();
            
            // Проверяем всех игроков
            PlayerScore[] allPlayers = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                if (player.GetScore() >= scoreLimit)
                {
                    Debug.Log($"[GameManager] {player.GetPlayerName()} reached score limit ({player.GetScore()}/{scoreLimit})!");
                    EndGame(player); // Заканчиваем игру с победителем
                    return;
                }
            }
        }
        
        private void UpdateGameTime()
        {
            float previousTime = currentTime;
            currentTime -= Time.deltaTime;
            
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                EndGame();
            }
            
            if (Mathf.FloorToInt(previousTime) != Mathf.FloorToInt(currentTime))
            {
                OnTimeChanged?.Invoke(currentTime);
                UpdateTimerUI();
            }
        }
        
        private void UpdateScoreFromTotem()
        {
            if (totem == null || !totem.IsBeingCarried())
            {
                carrierScoreAccumulator = 0f;
                return;
            }
            
            carrierScoreAccumulator += totem.GetCarryMultiplier() * Time.deltaTime;
            
            if (carrierScoreAccumulator >= 1f)
            {
                int pointsToAdd = Mathf.FloorToInt(carrierScoreAccumulator);
                carrierScoreAccumulator -= pointsToAdd;
                AddScore(pointsToAdd);
            }
        }
        
        public void StartGame()
        {
            isGameActive = true;
            gameEnded = false;
            currentTime = gameTime;
            originalGameTime = gameTime;
            totalScore = 0;
            carrierScoreAccumulator = 0f;
            
            OnGameStarted?.Invoke();
            UpdateTimerUI();
            UpdateScoreUI();
            UpdateScoreLimitUI();
            
            Debug.Log("[GameManager] Game started!");
        }
        
        // ПЕРЕГРУЗКА: Закончить игру с конкретным победителем
        public void EndGame(PlayerScore winner = null)
        {
            if (gameEnded) return;
            
            isGameActive = false;
            gameEnded = true;
            
            FreezeAllPlayers(true);
            
            OnGameEnded?.Invoke();
            
            if (gameOverMenu != null)
            {
                gameOverMenu.ShowGameOver();
            }
            
            if (winner != null)
            {
                Debug.Log($"[GameManager] Game Ended! Winner: {winner.GetPlayerName()} with {winner.GetScore()} points!");
            }
            else
            {
                Debug.Log("[GameManager] Game Ended! Time limit reached.");
            }
        }
        
        private void FreezeAllPlayers(bool freeze)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.EnableControls(!freeze);
                }
            }
            
            AIBotController[] bots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot != null)
                {
                    bot.Freeze(freeze);
                }
            }
        }
        
        public void RestartGame()
        {
            if (gameOverMenu != null)
            {
                if (gameOverMenu.gameOverPanel != null)
                    gameOverMenu.gameOverPanel.SetActive(false);
            }
            
            gameEnded = false;
            totalScore = 0;
            carrierScoreAccumulator = 0f;
            
            ApplyGameTimeSettings();
            currentTime = gameTime;
            originalGameTime = gameTime;
            
            if (totem != null)
            {
                totem.ResetTotem();
            }
            
            if (countdownDisplay != null)
            {
                countdownDisplay.StartCountdown();
                StartCoroutine(StartGameAfterCountdown());
            }
            else
            {
                StartGame();
            }
            
            UpdateTimerUI();
            UpdateScoreUI();
            
            Debug.Log("[GameManager] Game restarted!");
        }
        
        public void AddScore(int points)
        {
            totalScore += points;
            OnScoreChanged?.Invoke(totalScore);
            UpdateScoreUI();
        }
        
        public float GetCurrentTime() => currentTime;
        public bool IsGameActive() => isGameActive && !gameEnded;
        public int GetTotalScore() => totalScore;
        
        public void SetGameTime(float time)
        {
            gameTime = time;
            if (!isGameActive && !gameEnded)
            {
                currentTime = time;
                originalGameTime = time;
                UpdateTimerUI();
            }
        }
        
        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                timerText.text = FormatTime(currentTime);
            }
        }
        
        private void UpdateScoreUI()
        {
            if (globalScoreText != null)
            {
                globalScoreText.text = $"Score: {totalScore}";
            }
        }
        
        private void FindTotem()
        {
            var totemObject = FindFirstObjectByType<TotemController>();
            if (totemObject != null)
            {
                totem = totemObject;
            }
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