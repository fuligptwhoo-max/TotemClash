using UnityEngine;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using FishNet.Connection;
using FishNet.Object;
using System.Collections.Generic;

/// <summary>
/// Network Manager для FishNet
/// В FishNet NetworkManager sealed - используем композицию вместо наследования
/// </summary>
public class MyNetworkManager : MonoBehaviour
{
    [Header("Spawn Points")]
    public List<Transform> manualSpawnPoints = new List<Transform>();
    
    [Header("Player Prefab")]
    public NetworkObject playerPrefab;
    
    public static MyNetworkManager Instance { get; private set; }
    
    // Ссылка на FishNet NetworkManager
    private NetworkManager _networkManager;
    
    // Отслеживание спавненных игроков
    private HashSet<int> spawnedPlayers = new HashSet<int>();
    
    // Очередь игроков ожидающих спавна (если подключились в MainMenu)
    private List<NetworkConnection> pendingSpawnConnections = new List<NetworkConnection>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _networkManager = GetComponent<NetworkManager>();
            
            // Убедимся что мы DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MyNetworkManager] Instance created and marked DontDestroyOnLoad");
            
            // Подписываемся на смену сцены чтобы очищать спавн-поинты
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Debug.LogWarning("[MyNetworkManager] Another instance exists, destroying this one");
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    /// <summary>
    /// Вызывается при загрузке новой сцены
    /// </summary>
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        Debug.Log($"[MyNetworkManager] Scene loaded: {scene.name}, clearing spawn points");
        
        // Очищаем спавн-поинты при загрузке новой сцены
        // Они будут заново найдены при первом запросе
        manualSpawnPoints.Clear();
        
        // Если мы в игровой сцене, сразу ищем спавн-поинты
        if (scene.name != "MainMenu")
        {
            FindSpawnPointsInScene();
        }
    }
    
    private void OnEnable()
    {
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
            _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
            Debug.Log("[MyNetworkManager] Subscribed to network events");
        }
    }
    
    private void OnDisable()
    {
        if (_networkManager != null)
        {
            if (_networkManager.ServerManager != null)
            {
                _networkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
            }
            if (_networkManager.SceneManager != null)
            {
                _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoadedStartScenes;
                _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
            }
            Debug.Log("[MyNetworkManager] Unsubscribed from network events");
        }
    }
    
    /// <summary>
    /// Вызывается когда сцена загружена (через FishNet SceneManager)
    /// </summary>
    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        if (!_networkManager.IsServerStarted) return;
        
        Debug.Log($"[MyNetworkManager] Scene load ended. Loaded scenes: {args.LoadedScenes.Length}");
        
        // Проверяем есть ли ожидающие спавна игроки
        if (pendingSpawnConnections.Count > 0)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[MyNetworkManager] Processing {pendingSpawnConnections.Count} pending spawns in scene: {currentScene}");
            
            // Спавним всех ожидающих игроков
            List<NetworkConnection> toSpawn = new List<NetworkConnection>(pendingSpawnConnections);
            pendingSpawnConnections.Clear();
            
            foreach (var conn in toSpawn)
            {
                if (conn.IsValid && !spawnedPlayers.Contains(conn.ClientId))
                {
                    Debug.Log($"[MyNetworkManager] Spawning pending player: ClientId={conn.ClientId}");
                    SpawnPlayer(conn);
                }
            }
        }
    }
    
    private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
        if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Удаляем из списка спавненных
            if (spawnedPlayers.Contains(conn.ClientId))
            {
                spawnedPlayers.Remove(conn.ClientId);
            }
            
            // Удаляем из очереди ожидания если есть
            pendingSpawnConnections.Remove(conn);
            
            if (conn.FirstObject != null)
            {
                MagicianClass.UnregisterPlayer(conn.FirstObject.ObjectId);
                Debug.Log($"[MyNetworkManager] Player disconnected from tracker: {conn.FirstObject.ObjectId}");
            }
            
            Debug.Log($"[MyNetworkManager] Player disconnected: ClientId={conn.ClientId}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Started)
        {
            Debug.Log($"[MyNetworkManager] Player connected: ClientId={conn.ClientId}");
        }
    }
    
    /// <summary>
    /// Вызывается когда клиент загрузил стартовые сцены
    /// </summary>
    private void OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
        if (!asServer) 
        {
            Debug.Log("[MyNetworkManager] OnClientLoadedStartScenes called on client, ignoring");
            return; // Только на сервере
        }
        
        Debug.Log($"[MyNetworkManager] Client loaded start scenes: ClientId={conn.ClientId}, IsValid={conn.IsValid}");
        
        // Проверяем, не спавнили ли мы уже этого игрока
        if (spawnedPlayers.Contains(conn.ClientId))
        {
            Debug.LogWarning($"[MyNetworkManager] Player {conn.ClientId} already spawned, skipping");
            return;
        }
        
        // Проверяем, валидно ли соединение
        if (!conn.IsValid)
        {
            Debug.LogWarning($"[MyNetworkManager] Connection {conn.ClientId} is not valid, cannot spawn");
            return;
        }
        
        // Проверяем текущую сцену
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[MyNetworkManager] Current scene: {currentScene}");
        
        // Если в MainMenu - добавляем в очередь ожидания
        if (currentScene == "MainMenu")
        {
            Debug.Log("[MyNetworkManager] In MainMenu, adding player to pending spawn queue");
            if (!pendingSpawnConnections.Contains(conn))
            {
                pendingSpawnConnections.Add(conn);
            }
            return;
        }
        
        // Спавним игрока сразу если в игровой сцене
        SpawnPlayer(conn);
    }
    
    /// <summary>
    /// Спавнит игрока для подключившегося клиента
    /// </summary>
    private void SpawnPlayer(NetworkConnection conn)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("[MyNetworkManager] Player prefab not assigned!");
            return;
        }
        
        if (_networkManager == null || !_networkManager.IsServerStarted)
        {
            Debug.LogError("[MyNetworkManager] Not running as server, cannot spawn player!");
            return;
        }
        
        // Помечаем что мы спавним этого игрока
        spawnedPlayers.Add(conn.ClientId);
        
        // Получаем точку спавна с проверкой занятости
        Transform spawnPoint = GetRandomSpawnPoint(conn);
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        
        // Если нет точки спавна, используем случайную позицию с запасом
        if (spawnPoint == null)
        {
            spawnPosition = new Vector3(Random.Range(-10f, 10f), 2f, Random.Range(-10f, 10f));
        }
        
        Debug.Log($"[MyNetworkManager] Spawning player for ClientId={conn.ClientId} at position {spawnPosition}");
        
        try
        {
            NetworkObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            
            if (playerInstance == null)
            {
                Debug.LogError("[MyNetworkManager] Failed to instantiate player prefab!");
                spawnedPlayers.Remove(conn.ClientId);
                return;
            }
            
            // Спавним на сервере
            _networkManager.ServerManager.Spawn(playerInstance, conn);
            
            Debug.Log($"[MyNetworkManager] Player spawned successfully: {playerInstance.name}, ObjectId={playerInstance.ObjectId}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MyNetworkManager] Exception while spawning player: {e.Message}");
            spawnedPlayers.Remove(conn.ClientId);
        }
    }
    
    /// <summary>
    /// Принудительно спавнит всех ожидающих игроков (вызывается при старте игры)
    /// </summary>
    public void SpawnPendingPlayers()
    {
        if (!_networkManager.IsServerStarted) return;
        
        Debug.Log($"[MyNetworkManager] Force spawning {pendingSpawnConnections.Count} pending players");
        
        List<NetworkConnection> toSpawn = new List<NetworkConnection>(pendingSpawnConnections);
        pendingSpawnConnections.Clear();
        
        foreach (var conn in toSpawn)
        {
            if (conn.IsValid && !spawnedPlayers.Contains(conn.ClientId))
            {
                SpawnPlayer(conn);
            }
        }
    }
    
    public void OnPlayerSpawned(NetworkObject playerObject)
    {
        if (playerObject != null)
        {
            MagicianClass.RegisterPlayer(playerObject.ObjectId, playerObject.transform);
            Debug.Log($"[MyNetworkManager] Player registered in tracker: {playerObject.name}, ObjectId: {playerObject.ObjectId}");
        }
    }
    
    public void RespawnPlayer(GameObject player)
    {
        if (_networkManager == null || !_networkManager.IsServerStarted) 
        {
            Debug.LogWarning("[MyNetworkManager] Cannot respawn player: not running as server");
            return;
        }
        
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.Respawn();
        }
    }
    
    private void Start()
    {
        // Очищаем трекер игроков при старте
        System.Reflection.FieldInfo field = typeof(MagicianClass).GetField("playerTransforms", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        
        if (field != null)
        {
            Dictionary<int, Transform> playerTransforms = 
                field.GetValue(null) as Dictionary<int, Transform>;
            if (playerTransforms != null)
            {
                playerTransforms.Clear();
                Debug.Log("[MyNetworkManager] Player tracker cleared on start!");
            }
        }
        
        // Очищаем списки
        spawnedPlayers.Clear();
        pendingSpawnConnections.Clear();
        
        // Очищаем спавн-поинты - они будут заново найдены в текущей сцене
        manualSpawnPoints.Clear();
        
        InitializeSpawnPoints();
        
        // Логируем сетевую информацию с задержкой
        Invoke(nameof(LogNetworkInfo), 1f);
    }
    
    private void LogNetworkInfo()
    {
        if (_networkManager == null) return;
        
        Debug.Log("=== NETWORK INFO ===");
        Debug.Log($"[MyNetworkManager] IsServerStarted: {_networkManager.IsServerStarted}");
        Debug.Log($"[MyNetworkManager] IsClientStarted: {_networkManager.IsClientStarted}");
        
        if (_networkManager.ServerManager != null)
        {
            Debug.Log($"[MyNetworkManager] ServerManager.Started: {_networkManager.ServerManager.Started}");
        }
        
        if (_networkManager.TransportManager?.Transport != null)
        {
            var t = _networkManager.TransportManager.Transport;
            Debug.Log($"[MyNetworkManager] Transport: {t.GetType().Name}");
            Debug.Log($"[MyNetworkManager] Port: {t.GetPort()}");
            Debug.Log($"[MyNetworkManager] Bind Address (IPv4): '{t.GetServerBindAddress(FishNet.Transporting.IPAddressType.IPv4)}'");
        }
        Debug.Log("====================");
    }
    
    private void InitializeSpawnPoints()
    {
        Debug.Log("[MyNetworkManager] Spawn points initialized");
    }
    
    public Transform GetRandomSpawnPoint()
    {
        return GetRandomSpawnPoint(null);
    }
    
    /// <summary>
    /// Получает случайную точку спавна, избегая близости к другим игрокам
    /// </summary>
    public Transform GetRandomSpawnPoint(NetworkConnection spawningPlayer)
    {
        // Очищаем null-ссылки (уничтоженные объекты)
        manualSpawnPoints.RemoveAll(t => t == null);
        
        if (manualSpawnPoints.Count == 0)
        {
            FindSpawnPointsInScene();
        }
        
        if (manualSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[MyNetworkManager] No spawn points available!");
            return null;
        }
        
        // Создаём список доступных точек
        List<Transform> availablePoints = new List<Transform>(manualSpawnPoints);
        
        // Если есть игроки в сцене, проверяем расстояние
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        float minDistance = 3f; // Минимальное расстояние между игроками
        
        foreach (var point in manualSpawnPoints)
        {
            if (point == null) continue;
            
            // Проверяем есть ли игрок рядом с этой точкой
            bool tooClose = false;
            foreach (var player in players)
            {
                // Пропускаем самого спавнящегося игрока
                if (spawningPlayer != null && player.Owner == spawningPlayer) continue;
                
                float distance = Vector3.Distance(point.position, player.transform.position);
                if (distance < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            
            if (tooClose)
            {
                availablePoints.Remove(point);
            }
        }
        
        // Если остались доступные точки, выбираем из них
        if (availablePoints.Count > 0)
        {
            int randomIndex = Random.Range(0, availablePoints.Count);
            Debug.Log($"[MyNetworkManager] Selected spawn point {randomIndex} from {availablePoints.Count} available points");
            return availablePoints[randomIndex];
        }
        
        // Если все точки заняты, выбираем любую (но логируем)
        Debug.LogWarning("[MyNetworkManager] All spawn points have players nearby, using random point");
        int fallbackIndex = Random.Range(0, manualSpawnPoints.Count);
        return manualSpawnPoints[fallbackIndex];
    }
    
    private void FindSpawnPointsInScene()
    {
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        
        foreach (GameObject obj in spawnPointObjects)
        {
            if (obj != null && obj.transform != null)
            {
                manualSpawnPoints.Add(obj.transform);
            }
        }
        
        if (spawnPointObjects.Length == 0)
        {
            Debug.LogWarning("[MyNetworkManager] No spawn points found with tag 'SpawnPoint'! Creating default...");
            CreateDefaultSpawnPoints();
        }
        else
        {
            Debug.Log($"[MyNetworkManager] Found {spawnPointObjects.Length} spawn points in scene");
        }
    }
    
    private void CreateDefaultSpawnPoints()
    {
        Vector3[] defaultPositions = new Vector3[]
        {
            new Vector3(-10f, 2f, -10f),
            new Vector3(10f, 2f, -10f),
            new Vector3(-10f, 2f, 10f),
            new Vector3(10f, 2f, 10f)
        };
        
        for (int i = 0; i < defaultPositions.Length; i++)
        {
            GameObject spawnPointObj = new GameObject($"DefaultSpawnPoint_{i}");
            spawnPointObj.transform.position = defaultPositions[i];
            spawnPointObj.transform.rotation = Quaternion.identity;
            spawnPointObj.tag = "SpawnPoint";
            
            manualSpawnPoints.Add(spawnPointObj.transform);
        }
        
        Debug.Log($"[MyNetworkManager] Created {defaultPositions.Length} default spawn points");
    }
    
    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (_networkManager != null && _networkManager.IsServerStarted)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            SceneLoadData sld = new SceneLoadData(sceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }
    }
    
    /// <summary>
    /// Очищает список спавненных игроков (вызывается при рестарте игры)
    /// </summary>
    public void ClearSpawnedPlayers()
    {
        spawnedPlayers.Clear();
        pendingSpawnConnections.Clear();
        Debug.Log("[MyNetworkManager] Spawned players list cleared");
    }
}
