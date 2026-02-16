using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TotemClash.AI;
using TotemClash.UI;

namespace TotemClash.Combat
{
    public class LocalGameSpawner : MonoBehaviour
    {
        private static LocalGameSpawner _instance;
        public static LocalGameSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LocalGameSpawner>();
                }
                return _instance;
            }
        }

        [Header("Prefabs")]
        public GameObject playerPrefab;
        public GameObject crosshairPrefab;

        [Header("References")]
        public BotSpawner botSpawner;
        public List<Transform> spawnPoints = new List<Transform>();
        public CountdownDisplay countdownDisplay;

        [Header("Game Settings")]
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private float botsSpawnDelay = 0.5f;

        [Header("Events")]
        public UnityEvent<GameObject> OnPlayerSpawned = new UnityEvent<GameObject>();
        public UnityEvent OnGameStarted = new UnityEvent();

        private GameObject spawnedPlayer;
        private bool isGameStarted = false;
        private bool isSpawning = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[LocalGameSpawner] Multiple instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            FindReferences();
        }

        private void Start()
        {
            if (autoStartOnAwake && !isGameStarted && !isSpawning)
            {
                StartGame();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        public void StartGame()
        {
            if (isSpawning || isGameStarted)
            {
                Debug.LogWarning("[LocalGameSpawner] Game is already starting or started.");
                return;
            }

            isSpawning = true;
            Debug.Log("[LocalGameSpawner] Starting local game...");

            StartCoroutine(StartGameSequence());
        }

        public GameObject SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[LocalGameSpawner] Player prefab is not assigned!");
                return null;
            }

            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[LocalGameSpawner] No spawn points available!");
                return null;
            }

            Transform spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogError("[LocalGameSpawner] Selected spawn point is null!");
                return null;
            }

            spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedPlayer.name = "LocalPlayer";

            SetupPlayer(spawnedPlayer);

            Debug.Log($"[LocalGameSpawner] Player spawned at {spawnPoint.position}");

            OnPlayerSpawned?.Invoke(spawnedPlayer);

            return spawnedPlayer;
        }

        public void RestartGame()
        {
            Debug.Log("[LocalGameSpawner] Restarting game...");

            isGameStarted = false;
            isSpawning = false;

            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
                spawnedPlayer = null;
            }

            if (botSpawner != null)
            {
                botSpawner.ClearBots();
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                StartGame();
            }
        }

        public GameObject GetPlayer()
        {
            return spawnedPlayer;
        }

        public bool IsGameStarted()
        {
            return isGameStarted;
        }

        public bool IsSpawning()
        {
            return isSpawning;
        }

        private IEnumerator StartGameSequence()
        {
            SpawnPlayer();

            yield return new WaitForSeconds(botsSpawnDelay);

            if (botSpawner != null)
            {
                var botSpawnerType = botSpawner.GetType();
                var spawnPointsField = botSpawnerType.GetField("spawnPoints", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (spawnPointsField != null)
                {
                    var existingPoints = spawnPointsField.GetValue(botSpawner) as List<Transform>;
                    if (existingPoints == null || existingPoints.Count == 0)
                    {
                        spawnPointsField.SetValue(botSpawner, spawnPoints);
                    }
                }

                botSpawner.SpawnBots();
            }
            else
            {
                Debug.LogWarning("[LocalGameSpawner] BotSpawner not assigned. Playing without bots.");
            }

            if (countdownDisplay != null)
            {
                Debug.Log("[LocalGameSpawner] Starting countdown...");
            }

            if (botSpawner != null)
            {
                while (botSpawner.IsSpawning())
                {
                    yield return null;
                }
            }

            isSpawning = false;
            isGameStarted = true;

            OnGameStarted?.Invoke();

            Debug.Log("[LocalGameSpawner] Game started successfully!");
        }

        private void SetupPlayer(GameObject player)
        {
            player.layer = LayerMask.NameToLayer("Player");
            player.tag = "Player";

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogWarning("[LocalGameSpawner] Player prefab does not have PlayerController component!");
            }
            else
            {
                controller.enabled = true;
            }

            EnsureComponent<CombatSystem>(player);
            EnsureComponent<HealthSystem>(player);
            EnsureComponent<PlayerScore>(player);
            EnsureComponent<AimingSystem>(player);

            AimingSystem aiming = player.GetComponent<AimingSystem>();
            if (aiming != null && crosshairPrefab != null)
            {
                aiming.crosshairPrefab = crosshairPrefab;
            }

            HealthSystem healthSystem = player.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.SetSpawnPosition(player.transform.position, player.transform.rotation);
            }

            PlayerScore playerScore = player.GetComponent<PlayerScore>();
            if (playerScore != null)
            {
                playerScore.SetPlayerName("Player");
            }
        }

        private T EnsureComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
                Debug.Log($"[LocalGameSpawner] Added missing component {typeof(T).Name} to player");
            }
            return component;
        }

        private Transform GetRandomSpawnPoint()
        {
            if (spawnPoints.Count == 0)
            {
                return transform;
            }

            List<Transform> availablePoints = new List<Transform>(spawnPoints);
            availablePoints.RemoveAll(t => t == null);
            
            if (availablePoints.Count == 0)
            {
                return transform;
            }

            return availablePoints[Random.Range(0, availablePoints.Count)];
        }

        private void FindReferences()
        {
            if (botSpawner == null)
            {
                botSpawner = FindFirstObjectByType<BotSpawner>();
                if (botSpawner != null)
                {
                    Debug.Log("[LocalGameSpawner] Auto-found BotSpawner");
                }
            }

            if (countdownDisplay == null)
            {
                countdownDisplay = FindFirstObjectByType<CountdownDisplay>();
                if (countdownDisplay != null)
                {
                    Debug.Log("[LocalGameSpawner] Auto-found CountdownDisplay");
                }
            }

            if (spawnPoints.Count == 0)
            {
                GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
                foreach (var obj in spawnPointObjects)
                {
                    spawnPoints.Add(obj.transform);
                }

                if (spawnPoints.Count > 0)
                {
                    Debug.Log($"[LocalGameSpawner] Auto-found {spawnPoints.Count} spawn points");
                }
            }

            if (SpawnPointManager.Instance != null && spawnPoints.Count == 0)
            {
                if (SpawnPointManager.Instance.spawnPoints != null)
                {
                    spawnPoints.AddRange(SpawnPointManager.Instance.spawnPoints);
                    Debug.Log($"[LocalGameSpawner] Got {spawnPoints.Count} spawn points from SpawnPointManager");
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (botsSpawnDelay < 0)
            {
                botsSpawnDelay = 0;
            }
        }

        private void Reset()
        {
            FindReferences();
        }
#endif
    }
}