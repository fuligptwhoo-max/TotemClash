using UnityEngine;
using TMPro;
using TotemClash.Combat;

namespace TotemClash.UI
{
    public class CountdownDisplay : MonoBehaviour
    {
        public static CountdownDisplay Instance { get; private set; }
        
        [Header("UI")]
        public TMP_Text countdownText;
        public GameObject countdownPanel;
        
        [Header("Settings")]
        public float countdownDuration = 3f;
        public string finalText = "GO!";
        public Color[] countdownColors = new Color[] { Color.red, Color.yellow, Color.green, Color.white };
        
        private float countdownTimer = -1f;
        private bool countdownActive = false;
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
        
        private void Update()
        {
            if (countdownActive && countdownTimer > 0)
            {
                countdownTimer -= Time.deltaTime;
                UpdateDisplay();
                
                if (countdownTimer <= 0)
                {
                    EndCountdown();
                }
            }
        }
        
        private void UpdateDisplay()
        {
            if (countdownText == null) return;
            
            int displayNumber = Mathf.CeilToInt(countdownTimer);
            
            if (displayNumber > 0)
            {
                countdownText.text = displayNumber.ToString();
                
                int colorIndex = Mathf.Clamp(3 - displayNumber, 0, countdownColors.Length - 1);
                countdownText.color = countdownColors[colorIndex];
            }
            else if (countdownTimer > -0.5f)
            {
                countdownText.text = finalText;
                countdownText.color = countdownColors[countdownColors.Length - 1];
            }
        }
        
        public void StartCountdown()
        {
            countdownTimer = countdownDuration;
            countdownActive = true;
            countdownFinished = false;
            
            if (countdownPanel != null)
                countdownPanel.SetActive(true);
            
            UpdateDisplay();
            FreezeAllPlayers(true);
            
            Debug.Log("[CountdownDisplay] Countdown started!");
        }
        
        private void EndCountdown()
        {
            countdownActive = false;
            countdownFinished = true;
            
            FreezeAllPlayers(false);
            
            Invoke(nameof(HidePanel), 1f);
            
            Debug.Log("[CountdownDisplay] Countdown finished!");
        }
        
        private void HidePanel()
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);
        }
        
        // ИСПРАВЛЕНО: Правильный фриз ботов
        private void FreezeAllPlayers(bool freeze)
        {
            // Фризим игрока
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.EnableControls(!freeze);
                }
            }
            
            // ИСПРАВЛЕНО: Фризим ботов через их метод Freeze
            AIBotController[] bots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot != null)
                {
                    bot.Freeze(freeze);
                }
            }
            
            Debug.Log($"[CountdownDisplay] All players {(freeze ? "frozen" : "unfrozen")}");
        }
        
        public bool IsCountdownFinished()
        {
            return countdownFinished;
        }
    }
}