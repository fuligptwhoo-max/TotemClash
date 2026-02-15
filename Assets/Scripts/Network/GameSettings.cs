using UnityEngine;
using UnityEngine.Events;
using TotemClash.Combat;

namespace TotemClash.Network
{
    public class GameSettings : MonoBehaviour
    {
        public static GameSettings Instance { get; private set; }

        [SerializeField] private float gameTime = 300f;
        [SerializeField] private float playerSpeed = 8f;
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private int damagePerHit = 20;

        [Header("Events")]
        public UnityEvent<float> OnGameTimeChanged = new UnityEvent<float>();
        public UnityEvent<float> OnPlayerSpeedChanged = new UnityEvent<float>();
        public UnityEvent<float> OnProjectileSpeedChanged = new UnityEvent<float>();
        public UnityEvent<int> OnDamageChanged = new UnityEvent<int>();
        public UnityEvent<string> OnErrorMessage = new UnityEvent<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public float GetGameTime() => gameTime;
        
        public void SetGameTime(float value)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
            {
                float currentTime = GameManager.Instance.GetCurrentTime();
                
                if (value < currentTime)
                {
                    string errorMsg = $"Нельзя установить время {value:F0} сек, уже прошло {currentTime:F0} сек!";
                    Debug.LogWarning($"[GameSettings] {errorMsg}");
                    OnErrorMessage?.Invoke(errorMsg);
                    return;
                }
            }
            
            if (Mathf.Approximately(gameTime, value)) return;
            gameTime = Mathf.Max(0f, value);
            OnGameTimeChanged?.Invoke(gameTime);
        }

        public float GetPlayerSpeed() => playerSpeed;
        
        public void SetPlayerSpeed(float value)
        {
            if (Mathf.Approximately(playerSpeed, value)) return;
            playerSpeed = Mathf.Max(0f, value);
            OnPlayerSpeedChanged?.Invoke(playerSpeed);
            ApplyPlayerSpeedToAll();
        }

        public float GetProjectileSpeed() => projectileSpeed;
        
        public void SetProjectileSpeed(float value)
        {
            if (Mathf.Approximately(projectileSpeed, value)) return;
            projectileSpeed = Mathf.Max(0f, value);
            OnProjectileSpeedChanged?.Invoke(projectileSpeed);
            ApplyProjectileSpeedToAll();
        }

        public int GetDamagePerHit() => damagePerHit;
        
        public void SetDamagePerHit(int value)
        {
            if (damagePerHit == value) return;
            damagePerHit = Mathf.Max(0, value);
            OnDamageChanged?.Invoke(damagePerHit);
            ApplyDamageToAll();
        }

        public int GetDamage() => GetDamagePerHit();
        public void SetDamage(int value) => SetDamagePerHit(value);

        private void ApplyPlayerSpeedToAll()
        {
            var allControllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var controller in allControllers)
            {
                if (controller != null)
                {
                    controller.moveSpeed = playerSpeed;
                }
            }

            var allBots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in allBots)
            {
                if (bot != null)
                {
                    bot.moveSpeed = playerSpeed;
                }
            }
        }

        private void ApplyProjectileSpeedToAll()
        {
            var allMagicians = FindObjectsByType<TotemClash.Classes.MagicianClass>(FindObjectsSortMode.None);
            foreach (var magician in allMagicians)
            {
                if (magician != null)
                {
                    magician.fireballSpeed = projectileSpeed;
                }
            }
        }

        private void ApplyDamageToAll()
        {
            var allMagicians = FindObjectsByType<TotemClash.Classes.MagicianClass>(FindObjectsSortMode.None);
            foreach (var magician in allMagicians)
            {
                if (magician != null)
                {
                    magician.fireballDamage = damagePerHit;
                }
            }
        }

        public void ResetToDefaults()
        {
            SetGameTime(300f);
            SetPlayerSpeed(8f);
            SetProjectileSpeed(15f);
            SetDamagePerHit(20);
        }
    }
}