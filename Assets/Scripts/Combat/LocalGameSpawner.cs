using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TotemClash.AI;
using TotemClash.UI;

namespace TotemClash.Combat
{
    /// <summary>
    /// Локальный спавнер для одиночной игры.
    /// Управляет спавном игрока, ботов и запуском игровой сессии.
    /// Заменяет сетевую логику спавна для single-player режима.
    /// </summary>
    public class LocalGameSpawner : MonoBehaviour
    {
        #region Singleton

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

        #endregion

        #region Settings

        [Header("Prefabs")]
        [Tooltip("Префаб игрока (с PlayerController)")]
        public GameObject playerPrefab;

        [Header("References")]
        [Tooltip("Спавнер ботов")]
        public BotSpawner botSpawner;

        [Tooltip("Точки спавна игрока и ботов")]
        public List<Transform> spawnPoints = new List<Transform>();

        [Tooltip("UI отображения обратного отсчета")]
        public CountdownDisplay countdownDisplay;

        [Header("Game Settings")]
        [Tooltip("Запускать игру автоматически при старте сцены")]
        [SerializeField] private bool autoStartOnAwake = true;

        [Tooltip("Задержка перед спавном ботов после игрока (в секундах)")]
        [SerializeField] private float botsSpawnDelay = 0.5f;

        [Header("Events")]
        [Tooltip("Вызывается когда игрок заспавнен")]
        public UnityEvent<GameObject> OnPlayerSpawned = new UnityEvent<GameObject>();

        [Tooltip("Вызывается когда игра полностью запущена")]
        public UnityEvent OnGameStarted = new UnityEvent();

        #endregion

        #region Private Fields

        private GameObject spawnedPlayer;
        private bool isGameStarted = false;
        private bool isSpawning = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[LocalGameSpawner] Multiple instances found. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Auto-find references if not set
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Запускает игру: спавнит игрока, ботов и запускает обратный отсчет.
        /// </summary>
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

        /// <summary>
        /// Спавнит игрока в случайной точке спавна.
        /// </summary>
        /// <returns>Заспавненный игрок или null если ошибка</returns>
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

            // Выбираем случайную точку спавна
            Transform spawnPoint = GetRandomSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogError("[LocalGameSpawner] Selected spawn point is null!");
                return null;
            }

            // Спавним игрока
            spawnedPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedPlayer.name = "LocalPlayer";

            // Настраиваем игрока
            SetupPlayer(spawnedPlayer);

            Debug.Log($"[LocalGameSpawner] Player spawned at {spawnPoint.position}");

            // Вызываем событие
            OnPlayerSpawned?.Invoke(spawnedPlayer);

            return spawnedPlayer;
        }

        /// <summary>
        /// Перезапускает текущую игру.
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("[LocalGameSpawner] Restarting game...");

            // Сбрасываем состояние
            isGameStarted = false;
            isSpawning = false;

            // Уничтожаем текущего игрока
            if (spawnedPlayer != null)
            {
                Destroy(spawnedPlayer);
                spawnedPlayer = null;
            }

            // Очищаем ботов
            if (botSpawner != null)
            {
                botSpawner.ClearBots();
            }

            // Сбрасываем GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
            else
            {
                // Если нет GameManager - просто запускаем заново
                StartGame();
            }
        }

        /// <summary>
        /// Возвращает заспавненного игрока.
        /// </summary>
        /// <returns>GameObject игрока или null</returns>
        public GameObject GetPlayer()
        {
            return spawnedPlayer;
        }

        /// <summary>
        /// Проверяет, запущена ли игра.
        /// </summary>
        /// <returns>true если игра запущена</returns>
        public bool IsGameStarted()
        {
            return isGameStarted;
        }

        /// <summary>
        /// Проверяет, идет ли процесс спавна.
        /// </summary>
        /// <returns>true если идет спавн</returns>
        public bool IsSpawning()
        {
            return isSpawning;
        }

        #endregion

        #region Private Methods

        private IEnumerator StartGameSequence()
        {
            // 1. Спавним игрока
            SpawnPlayer();

            // Небольшая задержка перед спавном ботов
            yield return new WaitForSeconds(botsSpawnDelay);

            // 2. Спавним ботов
            if (botSpawner != null)
            {
                // Если у бот-спавнера нет точек спавна - передаем наши
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

            // 3. Показываем обратный отсчет
            if (countdownDisplay != null)
            {
                // Для локальной версии используем StartCountdown если он доступен
                // или просто ждем если countdown управляется GameManager
                Debug.Log("[LocalGameSpawner] Starting countdown...");
            }

            // 4. Ждем окончания спавна ботов
            if (botSpawner != null)
            {
                while (botSpawner.IsSpawning())
                {
                    yield return null;
                }
            }

            // 5. Запускаем GameManager (игра начнется после отсчета)
            // GameManager сам управляет отсчетом и стартом игры
            isSpawning = false;
            isGameStarted = true;

            OnGameStarted?.Invoke();

            Debug.Log("[LocalGameSpawner] Game started successfully!");
        }

        private void SetupPlayer(GameObject player)
        {
            // Настраиваем слой и тег
            player.layer = LayerMask.NameToLayer("Player");
            player.tag = "Player";

            // Убеждаемся что есть PlayerController
            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogWarning("[LocalGameSpawner] Player prefab does not have PlayerController component!");
            }
            else
            {
                // Принудительно включаем компонент
                controller.enabled = true;
            }

            // Убеждаемся что есть необходимые компоненты
            EnsureComponent<PlayerCombat>(player);
            EnsureComponent<HealthSystem>(player);
            EnsureComponent<PlayerScore>(player);

            // Настраиваем HealthSystem
            HealthSystem healthSystem = player.GetComponent<HealthSystem>();
            if (healthSystem != null)
            {
                healthSystem.SetSpawnPosition(player.transform.position, player.transform.rotation);
            }

            // Настраиваем PlayerScore
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

            // Выбираем случайную точку, но не ту, где уже есть игрок
            List<Transform> availablePoints = new List<Transform>(spawnPoints);
            
            // Удаляем null точки
            availablePoints.RemoveAll(t => t == null);
            
            if (availablePoints.Count == 0)
            {
                return transform;
            }

            return availablePoints[Random.Range(0, availablePoints.Count)];
        }

        private void FindReferences()
        {
            // Ищем BotSpawner
            if (botSpawner == null)
            {
                botSpawner = FindFirstObjectByType<BotSpawner>();
                if (botSpawner != null)
                {
                    Debug.Log("[LocalGameSpawner] Auto-found BotSpawner");
                }
            }

            // Ищем CountdownDisplay
            if (countdownDisplay == null)
            {
                countdownDisplay = FindFirstObjectByType<CountdownDisplay>();
                if (countdownDisplay != null)
                {
                    Debug.Log("[LocalGameSpawner] Auto-found CountdownDisplay");
                }
            }

            // Ищем точки спавна
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

            // Проверяем SpawnPointManager
            if (SpawnPointManager.Instance != null && spawnPoints.Count == 0)
            {
                if (SpawnPointManager.Instance.spawnPoints != null)
                {
                    spawnPoints.AddRange(SpawnPointManager.Instance.spawnPoints);
                    Debug.Log($"[LocalGameSpawner] Got {spawnPoints.Count} spawn points from SpawnPointManager");
                }
            }
        }

        #endregion

        #region Editor Validation

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
            // Автоматически ищем референсы при добавлении компонента
            FindReferences();
        }
#endif

        #endregion
    }
}
