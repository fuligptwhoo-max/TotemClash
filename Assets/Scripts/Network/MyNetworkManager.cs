using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    [Header("Spawn Points")]
    public List<Transform> manualSpawnPoints = new List<Transform>();
    
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        
        GameObject player = conn.identity.gameObject;
        NetworkIdentity netId = player.GetComponent<NetworkIdentity>();
        
        // Регистрируем игрока в трекере MagicianClass
        if (netId != null)
        {
            MagicianClass.RegisterPlayer(netId.netId, player.transform);
            Debug.Log($"Игрок зарегистрирован в трекере: {player.name}, netId: {netId.netId}");
        }
        
        // Инициализируем GameManager если нужно
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && gameManager.isServer)
        {
            // Можно добавить игрока в список
        }
        
        Debug.Log($"Player added: {player.name}, connectionId: {conn.connectionId}, netId: {netId?.netId}");
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Удаляем игрока из трекера MagicianClass
        if (conn.identity != null)
        {
            MagicianClass.UnregisterPlayer(conn.identity.netId);
            Debug.Log($"Игрок удален из трекера: {conn.identity.name}, netId: {conn.identity.netId}");
        }
        
        base.OnServerDisconnect(conn);
        Debug.Log($"Player disconnected: {conn.connectionId}");
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Очищаем трекер игроков при старте сервера
        System.Reflection.FieldInfo field = typeof(MagicianClass).GetField("playerTransforms", 
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        
        if (field != null)
        {
            System.Collections.Generic.Dictionary<uint, Transform> playerTransforms = 
                field.GetValue(null) as System.Collections.Generic.Dictionary<uint, Transform>;
            if (playerTransforms != null)
            {
                playerTransforms.Clear();
                Debug.Log("Player tracker cleared on server start!");
            }
        }
        
        // Инициализируем спавн-поинты
        InitializeSpawnPoints();
        
        Debug.Log("Server started!");
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Server stopped!");
    }
    
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("Client connected to server!");
    }
    
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("Client disconnected from server!");
    }
    
    public void RespawnPlayer(GameObject player)
    {
        if (!NetworkServer.active) 
        {
            Debug.LogWarning("Cannot respawn player: not running as server");
            return;
        }
        
        HealthSystem healthSystem = player.GetComponent<HealthSystem>();
        if (healthSystem != null)
        {
            healthSystem.Respawn();
        }
        else
        {
            Debug.LogWarning($"Cannot respawn player: HealthSystem not found on {player.name}");
        }
    }
    
    private void InitializeSpawnPoints()
    {
        if (manualSpawnPoints.Count == 0)
        {
            FindSpawnPointsInScene();
        }
        
        Debug.Log($"Initialized {startPositions.Count} spawn points");
    }
    
    private void FindSpawnPointsInScene()
    {
        NetworkStartPosition[] sceneSpawnPoints = FindObjectsByType<NetworkStartPosition>(FindObjectsSortMode.None);
        
        foreach (NetworkStartPosition spawnPoint in sceneSpawnPoints)
        {
            startPositions.Add(spawnPoint.transform);
            manualSpawnPoints.Add(spawnPoint.transform);
        }
        
        if (sceneSpawnPoints.Length == 0)
        {
            Debug.LogWarning("No NetworkStartPosition objects found in scene!");
            CreateDefaultSpawnPoints();
        }
    }
    
    private void CreateDefaultSpawnPoints()
    {
        Debug.Log("Creating default spawn points...");
        
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
            
            spawnPointObj.AddComponent<NetworkStartPosition>();
            
            startPositions.Add(spawnPointObj.transform);
            manualSpawnPoints.Add(spawnPointObj.transform);
        }
    }
    
    public Transform GetRandomSpawnPoint()
    {
        if (startPositions.Count == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return null;
        }
        
        int randomIndex = Random.Range(0, startPositions.Count);
        return startPositions[randomIndex];
    }
    
    public Transform GetSpawnPoint(int index)
    {
        if (index >= 0 && index < startPositions.Count)
        {
            return startPositions[index];
        }
        
        return GetRandomSpawnPoint();
    }
}