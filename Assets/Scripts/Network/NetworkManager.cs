using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    [Header("Player Settings")]
    public new GameObject playerPrefab; // Используем new для скрытия родительского поля
    
    [Header("Spawn Points")]
    public Transform[] spawnPoints;
    
    [Header("Totem")]
    public GameObject totemPrefab;
    
    private List<GameObject> players = new List<GameObject>();
    private GameObject currentTotem;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Сервер запущен!");
        
        if (totemPrefab != null)
        {
            Invoke(nameof(SpawnTotem), 1f);
        }
    }
    
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        
        GameObject player = conn.identity.gameObject;
        players.Add(player);
        
        Debug.Log($"Игрок подключился: {conn.connectionId}, всего игроков: {numPlayers}");
        
        if (spawnPoints.Length > 0)
        {
            int spawnIndex = (numPlayers - 1) % spawnPoints.Length;
            player.transform.position = spawnPoints[spawnIndex].position;
        }
        
        if (currentTotem != null)
        {
            NetworkServer.Spawn(currentTotem);
        }
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"Игрок отключился: {conn.connectionId}");
        
        foreach (var player in players)
        {
            if (player.GetComponent<NetworkIdentity>().connectionToClient == conn)
            {
                PlayerTotemInteraction totemInteraction = player.GetComponent<PlayerTotemInteraction>();
                if (totemInteraction != null && totemInteraction.IsCarrying)
                {
                    totemInteraction.OnPlayerDeath();
                }
                
                players.Remove(player);
                break;
            }
        }
        
        base.OnServerDisconnect(conn);
    }
    
    private void SpawnTotem()
    {
        if (totemPrefab != null)
        {
            Vector3 spawnPosition = new Vector3(0, 1, 0);
            currentTotem = Instantiate(totemPrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(currentTotem);
            Debug.Log("Тотем заспавнен на сервере");
        }
    }
    
    public void RespawnPlayer(GameObject player)
    {
        if (spawnPoints.Length > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Length);
            player.transform.position = spawnPoints[spawnIndex].position;
            
            HealthSystem health = player.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.Respawn(); // Метод Respawn должен быть public
            }
            
            Debug.Log($"{player.name} возродился в точке {spawnIndex}");
        }
    }
}