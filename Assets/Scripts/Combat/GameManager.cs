using UnityEngine;
using TMPro;
using UnityEngine.Events;
using TotemClash.UI;
using TotemClash.Network;

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
        public TMP_Text warningText; // ИСПРАВЛЕНО: Текст для предупреждений
        
        [Header("References")]
        public TotemController totem;
        public CountdownDisplay countdownDisplay;
        public GameOverMenu gameOverMenu;
        
        private float currentTime;
        private int totalScore = 0;
        private bool isGameActive = false;
        private bool gameEnded = false;
        private float carrierScoreAccumulator = 0f;
        private float originalGameTime = 300f; // ИСПРАВЛЕНО: Храним изначальное время
        
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
            originalGameTime = gameTime; // ИСПРАВЛЕНО: Сохраняем оригинальное время
            totalScore = 0;
            isGameActive = false;
            gameEnded = false;
            
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
            }
        }
        
        private void OnDestroy()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnGameTimeChanged.RemoveListener(OnGameTimeSettingChanged);
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
        
        // ИСПРАВЛЕНО: Обработка изменения времени с проверкой
        private void OnGameTimeSettingChanged(float newTime)
        {
            // Если игра активна - проверяем, не меньше ли новое время текущего
            if (isGameActive && !gameEnded)
            {
                float elapsedTime = originalGameTime - currentTime; // Сколько времени прошло
                
                if (newTime < elapsedTime)
                {
                    // Новое время меньше, чем уже прошло - показываем предупреждение
                    ShowWarning($"Введенное время ({FormatTime(newTime)}) меньше прошедшего ({FormatTime(elapsedTime)})!");
                    Debug.LogWarning($"[GameManager] Cannot set time to {newTime}, already elapsed {elapsedTime}");
                    return; // Не применяем изменение
                }
                
                // Применяем новое время: прошедшее время остается, меняется общее
                gameTime = newTime;
                currentTime = newTime - elapsedTime;
                originalGameTime = newTime; // Обновляем оригинальное время
                
                Debug.Log($"[GameManager] Game time updated to: {newTime}, elapsed: {elapsedTime}, current: {currentTime}");
            }
            else if (!isGameActive && !gameEnded)
            {
                // Игра не началась - просто меняем время
                gameTime = newTime;
                currentTime = newTime;
                originalGameTime = newTime;
                Debug.Log($"[GameManager] Game time set to: {newTime} (game not started)");
            }
            
            UpdateTimerUI();
        }
        
        // ИСПРАВЛЕНО: Показать предупреждение
        private void ShowWarning(string message)
        {
            if (warningText != null)
            {
                warningText.text = message;
                warningText.gameObject.SetActive(true);
                
                // Автоматически скрываем через 3 секунды
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
        
        // ИСПРАВЛЕНО: Форматирование времени
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
            originalGameTime = gameTime; // ИСПРАВЛЕНО: Сохраняем оригинальное время
            totalScore = 0;
            carrierScoreAccumulator = 0f;
            
            OnGameStarted?.Invoke();
            UpdateTimerUI();
            UpdateScoreUI();
            
            Debug.Log("[GameManager] Game started!");
        }
        
        public void EndGame()
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
            
            Debug.Log("[GameManager] Game Ended!");
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
                bot.Freeze(freeze);
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