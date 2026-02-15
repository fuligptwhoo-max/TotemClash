using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TotemClash.Combat;
using TotemClash.Classes; // Для MagicianClass
using TotemClash.Network; // Для GameSettings

namespace TotemClash.AI
{
    public class BotSpawner : MonoBehaviour
    {
        private static BotSpawner _instance;
        public static BotSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<BotSpawner>();
                }
                return _instance;
            }
        }

        [Header("Bot Settings")]
        [Tooltip("Префаб бота (без PlayerController)")]
        [SerializeField] public GameObject botPrefab;

        [Tooltip("Количество ботов для спавна")]
        [SerializeField] public int botCount = 5;

        [Tooltip("Задержка между спавнами (в секундах)")]
        [SerializeField] private float spawnDelay = 0.5f;

        [Tooltip("Точки спавна ботов")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Header("Bot Configuration")]
        [Tooltip("Слой для ботов")]
        [SerializeField] private int botLayer = 7;

        [Tooltip("Тег для ботов")]
        [SerializeField] private string botTag = "Enemy";

        [Tooltip("Случайные имена для ботов")]
        [SerializeField] private List<string> botNames = new List<string>()
        {
            "Shadow Walker", "Mystic Flame", "Dark Sorcerer", "Arcane Hunter", 
            "Void Seeker", "Crimson Mage", "Storm Bringer", "Frost Warden"
        };

        [Header("Events")]
        public UnityEvent<GameObject> OnBotSpawned = new UnityEvent<GameObject>();
        public UnityEvent OnAllBotsSpawned = new UnityEvent();

        private List<GameObject> spawnedBots = new List<GameObject>();
        private Dictionary<GameObject, int> botSpawnPointIndex = new Dictionary<GameObject, int>();
        private bool isSpawning = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[BotSpawner] Multiple BotSpawner instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                FindSpawnPoints();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void SpawnBots()
        {
            if (isSpawning)
            {
                Debug.LogWarning("[BotSpawner] Already spawning bots.");
                return;
            }

            if (botPrefab == null)
            {
                Debug.LogError("[BotSpawner] Bot prefab is not assigned!");
                return;
            }

            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[BotSpawner] No spawn points available!");
                return;
            }

            StartCoroutine(SpawnBotsCoroutine());
        }

        public GameObject SpawnBot(int spawnPointIndex)
        {
            if (botPrefab == null)
            {
                Debug.LogError("[BotSpawner] Bot prefab is not assigned!");
                return null;
            }

            if (spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Count)
            {
                Debug.LogError($"[BotSpawner] Invalid spawn point index: {spawnPointIndex}");
                return null;
            }

            Transform spawnPoint = spawnPoints[spawnPointIndex];
            if (spawnPoint == null)
            {
                Debug.LogError($"[BotSpawner] Spawn point at index {spawnPointIndex} is null!");
                return null;
            }

            GameObject bot = Instantiate(botPrefab, spawnPoint.position, spawnPoint.rotation);
            bot.name = $"Bot_{spawnedBots.Count + 1}_{GetRandomBotName()}";

            SetupBot(bot, spawnPointIndex);

            spawnedBots.Add(bot);
            botSpawnPointIndex[bot] = spawnPointIndex;
            
            OnBotSpawned?.Invoke(bot);

            Debug.Log($"[BotSpawner] Spawned bot: {bot.name} at {spawnPoint.position}");

            return bot;
        }

        public GameObject SpawnBotAtRandomPoint()
        {
            if (spawnPoints.Count == 0) return null;
            int randomIndex = Random.Range(0, spawnPoints.Count);
            return SpawnBot(randomIndex);
        }

        public void ClearBots()
        {
            foreach (var bot in spawnedBots)
            {
                if (bot != null)
                {
                    Destroy(bot);
                }
            }
            spawnedBots.Clear();
            botSpawnPointIndex.Clear();
            Debug.Log("[BotSpawner] All bots cleared.");
        }

        public List<GameObject> GetSpawnedBots()
        {
            spawnedBots.RemoveAll(bot => bot == null);
            return new List<GameObject>(spawnedBots);
        }

        public int GetAliveBotCount()
        {
            int aliveCount = 0;
            foreach (var bot in spawnedBots)
            {
                if (bot != null)
                {
                    HealthSystem health = bot.GetComponent<HealthSystem>();
                    if (health == null || !health.IsDead)
                    {
                        aliveCount++;
                    }
                }
            }
            return aliveCount;
        }

        public bool IsSpawning()
        {
            return isSpawning;
        }

        public bool HasFinishedSpawning()
        {
            return !isSpawning && spawnedBots.Count >= botCount;
        }

        private IEnumerator SpawnBotsCoroutine()
        {
            isSpawning = true;

            for (int i = 0; i < botCount; i++)
            {
                int spawnPointIndex = i % spawnPoints.Count;
                SpawnBot(spawnPointIndex);

                if (spawnDelay > 0 && i < botCount - 1)
                {
                    yield return new WaitForSeconds(spawnDelay);
                }
            }

            isSpawning = false;
            OnAllBotsSpawned?.Invoke();
            Debug.Log($"[BotSpawner] Finished spawning {botCount} bots.");
        }

        private void SetupBot(GameObject bot, int spawnPointIndex)
        {
            bot.layer = botLayer;
            bot.tag = botTag;

            AIBotController aiController = bot.GetComponent<AIBotController>();
            if (aiController == null)
            {
                aiController = bot.AddComponent<AIBotController>();
            }

            HealthSystem healthSystem = bot.GetComponent<HealthSystem>();
            if (healthSystem == null)
            {
                healthSystem = bot.AddComponent<HealthSystem>();
            }
            
            // Боты респавнятся (если нужно чтобы уничтожались - поставьте false)
            healthSystem.autoRespawn = true;
            
            if (spawnPointIndex >= 0 && spawnPointIndex < spawnPoints.Count)
            {
                Transform spawnPoint = spawnPoints[spawnPointIndex];
                healthSystem.SetSpawnPosition(spawnPoint.position, spawnPoint.rotation);
            }

            PlayerCombat playerCombat = bot.GetComponent<PlayerCombat>();
            if (playerCombat == null)
            {
                playerCombat = bot.AddComponent<PlayerCombat>();
            }

            PlayerScore playerScore = bot.GetComponent<PlayerScore>();
            if (playerScore == null)
            {
                playerScore = bot.AddComponent<PlayerScore>();
            }
            playerScore.SetIsBot(true);
            playerScore.SetPlayerName(GetRandomBotName());

            // ИСПРАВЛЕНО: Применяем настройки из GameSettings
            if (GameSettings.Instance != null)
            {
                aiController.moveSpeed = GameSettings.Instance.GetPlayerSpeed();
                
                MagicianClass magician = bot.GetComponent<MagicianClass>();
                if (magician != null)
                {
                    magician.fireballSpeed = GameSettings.Instance.GetProjectileSpeed();
                    magician.fireballDamage = GameSettings.Instance.GetDamagePerHit();
                    Debug.Log($"[BotSpawner] Applied settings to bot: Speed={aiController.moveSpeed}, ProjectileSpeed={magician.fireballSpeed}, Damage={magician.fireballDamage}");
                }
            }

            aiController.healthSystem = healthSystem;
            aiController.playerCombat = playerCombat;

            if (aiController.animator == null)
            {
                aiController.animator = bot.GetComponentInChildren<Animator>();
            }
            if (aiController.characterController == null)
            {
                aiController.characterController = bot.GetComponent<CharacterController>();
            }
        }

        private string GetRandomBotName()
        {
            if (botNames == null || botNames.Count == 0)
            {
                return $"Bot_{Random.Range(1000, 9999)}";
            }
            int randomIndex = Random.Range(0, botNames.Count);
            string baseName = botNames[randomIndex];
            return $"{baseName}_{Random.Range(1, 99)}";
        }

        private void FindSpawnPoints()
        {
            spawnPoints.Clear();
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (var obj in spawnPointObjects)
            {
                spawnPoints.Add(obj.transform);
            }

            if (spawnPoints.Count == 0)
            {
                Debug.LogWarning("[BotSpawner] No spawn points found with tag 'SpawnPoint'.");
            }
            else
            {
                Debug.Log($"[BotSpawner] Auto-found {spawnPoints.Count} spawn points.");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (botCount < 0) botCount = 0;
            if (spawnDelay < 0) spawnDelay = 0;
        }
#endif
    }
}