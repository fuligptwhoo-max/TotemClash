using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float gameTime = 300f;
    
    [Header("UI")]
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public GameObject gameOverPanel;
    public GameObject pauseMenu;
    
    [Header("Totem")]
    public TotemController totem;
    
    private float currentTime;
    private int totalScore = 0;
    private bool gameActive = false;
    private float scoreAccumulator = 0f;
    private bool isPaused = false;
    
    private void Start()
    {
        currentTime = gameTime;
        gameActive = true;
        
        if (totem == null)
        {
            FindTotem();
        }
        
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        
        UpdateTimerUI();
        UpdateScoreUI();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        Debug.Log("Game started!");
    }
    
    private void Update()
    {
        if (!gameActive) return;
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
        
        if (isPaused) return;
        
        currentTime -= Time.deltaTime;
        UpdateTimerUI();
        
        UpdateScoreFromTotem();
        
        if (currentTime <= 0f)
        {
            EndGame();
        }
    }
    
    private void FindTotem()
    {
        // ИСПРАВЛЕНО: используем FindFirstObjectByType
        var totemObject = FindFirstObjectByType<TotemController>();
        if (totemObject != null)
        {
            totem = totemObject;
            Debug.Log($"Found totem: {totem.name}");
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(isPaused);
        }
        
        Time.timeScale = isPaused ? 0f : 1f;
        
        Cursor.visible = isPaused;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
    }
    
    private void UpdateScoreFromTotem()
    {
        if (totem != null && totem.IsBeingCarried())
        {
            scoreAccumulator += totem.CalculateScore() * Time.deltaTime;
            
            if (scoreAccumulator >= 1f)
            {
                int pointsToAdd = Mathf.FloorToInt(scoreAccumulator);
                AddScore(pointsToAdd);
                scoreAccumulator -= pointsToAdd;
            }
        }
    }
    
    public void AddScore(int points)
    {
        totalScore += points;
        UpdateScoreUI();
    }
    
    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Очки: {totalScore}";
        }
    }
    
    private void EndGame()
    {
        gameActive = false;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            TMP_Text resultText = gameOverPanel.GetComponentInChildren<TMP_Text>();
            if (resultText != null)
            {
                resultText.text = $"Игра окончена!\nВаш счет: {totalScore}";
            }
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
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