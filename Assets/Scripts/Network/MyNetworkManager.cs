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
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _networkManager = GetComponent<NetworkManager>();
            
            // Убедимся что мы DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MyNetworkManager] Instance created and marked DontDestroyOnLoad");
        }
        else
        {
            Debug.LogWarning("[MyNetworkManager] Another instance exists, destroying this one");
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
            _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoadedStartScenes;
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
            }
            Debug.Log("[MyNetworkManager] Unsubscribed from network events");
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
            // НЕ спавним здесь! Ждём загрузки сцены через OnClientLoadedStartScenes
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
        
        // КРИТИЧНО: Не спавним игрока если мы всё ещё в MainMenu
        // Ждём пока загрузится игровая сцена
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu")
        {
            Debug.Log("[MyNetworkManager] Still in MainMenu, delaying player spawn until game scene loads...");
            StartCoroutine(SpawnPlayerAfterSceneLoad(conn));
            return;
        }
        
        // Спавним игрока только если мы в игровой сцене
        SpawnPlayer(conn);
    }
    
    /// <summary>
    /// Корутина для спавна игрока после загрузки игровой сцены
    /// </summary>
    private System.Collections.IEnumerator SpawnPlayerAfterSceneLoad(NetworkConnection conn)
    {
        // Ждём пока сцена изменится с MainMenu на игровую
        float timeout = 10f;
        float elapsed = 0f;
        
        while (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu" && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu")
        {
            Debug.LogError("[MyNetworkManager] Timeout waiting for scene to load!");
            yield break;
        }
        
        // Даём немного времени на инициализацию сцены
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log($"[MyNetworkManager] Scene loaded: {currentScene}, now spawning player...");
        
        // Проверяем ещё раз что игрок ещё не спавнен
        if (!spawnedPlayers.Contains(conn.ClientId) && conn.IsValid)
        {
            SpawnPlayer(conn);
        }
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
        
        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;
        
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
        
        // Очищаем список спавненных игроков
        spawnedPlayers.Clear();
        
        InitializeSpawnPoints();
    }
    
    private void InitializeSpawnPoints()
    {
        Debug.Log("[MyNetworkManager] Spawn points initialized");
    }
    
    public Transform GetRandomSpawnPoint()
    {
        if (manualSpawnPoints.Count == 0)
        {
            FindSpawnPointsInScene();
        }
        
        if (manualSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[MyNetworkManager] No spawn points available!");
            return null;
        }
        
        int randomIndex = Random.Range(0, manualSpawnPoints.Count);
        return manualSpawnPoints[randomIndex];
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
}
