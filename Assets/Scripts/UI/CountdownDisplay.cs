using UnityEngine;
using TMPro;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Обратный отсчёт перед началом игры (3, 2, 1, GO!)
/// </summary>
public class CountdownDisplay : NetworkBehaviour
{
    public static CountdownDisplay Instance { get; private set; }
    
    [Header("UI")]
    public TMP_Text countdownText;
    public GameObject countdownPanel;
    
    [Header("Settings")]
    public float countdownDuration = 3f;
    public string finalText = "GO!";
    public Color[] countdownColors = new Color[] { Color.red, Color.yellow, Color.green, Color.white };
    
    // Синхронизация
    public readonly SyncVar<float> syncCountdown = new SyncVar<float>(-1f);
    public readonly SyncVar<bool> syncCountdownActive = new SyncVar<bool>(false);
    
    private float localCountdown = -1f;
    private bool countdownFinished = false;
    
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
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        syncCountdown.OnChange += OnCountdownChanged;
        syncCountdownActive.OnChange += OnCountdownActiveChanged;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        syncCountdown.OnChange -= OnCountdownChanged;
        syncCountdownActive.OnChange -= OnCountdownActiveChanged;
    }
    
    private void OnCountdownChanged(float prev, float next, bool asServer)
    {
        localCountdown = next;
        UpdateDisplay();
    }
    
    private void OnCountdownActiveChanged(bool prev, bool next, bool asServer)
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(next);
        
        if (next)
        {
            // Сбрасываем состояние при начале отсчёта
            countdownFinished = false;
            localCountdown = syncCountdown.Value > 0 ? syncCountdown.Value : countdownDuration;
            UpdateDisplay();
            FreezeAllPlayers(true);
        }
        else if (!next && !countdownFinished)
        {
            // Отсчёт завершился
            countdownFinished = true;
            FreezeAllPlayers(false);
        }
    }
    
    private void Update()
    {
        // Локальное обновление отображения
        if (syncCountdownActive.Value && localCountdown > 0)
        {
            localCountdown -= Time.deltaTime;
            UpdateDisplay();
        }
    }
    
    private void UpdateDisplay()
    {
        if (countdownText == null) return;
        
        int displayNumber = Mathf.CeilToInt(localCountdown);
        
        if (displayNumber > 0)
        {
            countdownText.text = displayNumber.ToString();
            
            // Меняем цвет
            int colorIndex = Mathf.Clamp(3 - displayNumber, 0, countdownColors.Length - 1);
            countdownText.color = countdownColors[colorIndex];
        }
        else if (localCountdown > -0.5f)
        {
            countdownText.text = finalText;
            countdownText.color = countdownColors[countdownColors.Length - 1];
        }
    }
    
    /// <summary>
    /// Запускает обратный отсчёт (только сервер)
    /// </summary>
    [Server]
    public void StartCountdown()
    {
        // Сбрасываем состояние перед новым отсчётом
        localCountdown = countdownDuration;
        countdownFinished = false;
        
        syncCountdown.Value = countdownDuration;
        syncCountdownActive.Value = true;
        
        // Показываем панель если была скрыта
        if (countdownPanel != null)
            countdownPanel.SetActive(true);
        
        // Обновляем отображение сразу
        UpdateDisplay();
        
        Debug.Log("[CountdownDisplay] Countdown started!");
        
        // Запускаем корутину завершения
        StartCoroutine(EndCountdownCoroutine());
    }
    
    private System.Collections.IEnumerator EndCountdownCoroutine()
    {
        yield return new WaitForSeconds(countdownDuration + 1f);
        
        syncCountdownActive.Value = false;
        
        // Скрываем панель через секунду
        yield return new WaitForSeconds(1f);
        
        if (countdownPanel != null)
            countdownPanel.SetActive(false);
        
        Debug.Log("[CountdownDisplay] Countdown finished!");
    }
    
    /// <summary>
    /// Замораживает/размораживает всех игроков
    /// </summary>
    private void FreezeAllPlayers(bool freeze)
    {
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            player.EnableControls(!freeze);
        }
        
        Debug.Log($"[CountdownDisplay] All players {(freeze ? "frozen" : "unfrozen")}");
    }
}
