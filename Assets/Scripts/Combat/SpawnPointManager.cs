using UnityEngine;
using Mirror;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }
    
    public Transform[] spawnPoints;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return transform;
        }
        
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }
}