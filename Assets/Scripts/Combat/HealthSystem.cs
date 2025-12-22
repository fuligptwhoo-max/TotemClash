using UnityEngine;
using Mirror;
using System.Collections;

public class HealthSystem : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))]
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    private NetworkPlayerController playerController;
    public GameObject deathEffect;
    
    // Для избежания двойного урона
    private float lastDamageTime = 0f;
    private const float damageCooldown = 0.1f;

    private void Start()
    {
        playerController = GetComponent<NetworkPlayerController>();
        if (isServer)
        {
            currentHealth = maxHealth;
        }
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (currentHealth <= 0) return;
        
        Debug.Log($"TakeDamage вызван! Источник: {source?.name}, Урон: {damage}, isServer: {isServer}, isLocalPlayer: {isLocalPlayer}");
        
        // Проверка кулдауна
        if (Time.time - lastDamageTime < damageCooldown) return;
        
        if (isServer)
        {
            ApplyDamage(damage, source);
        }
        else if (isLocalPlayer)
        {
            CmdTakeDamage(damage, source);
        }
    }

    [Command]
    void CmdTakeDamage(float damage, GameObject source)
    {
        Debug.Log($"CmdTakeDamage получен на сервере для {gameObject.name}, урон: {damage}");
        ApplyDamage(damage, source);
    }

    [Server]
    void ApplyDamage(float damage, GameObject source)
    {
        Debug.Log($"ApplyDamage на сервере: {gameObject.name} получает {damage} урона");
        
        lastDamageTime = Time.time;
        currentHealth -= damage;
        
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            RpcDie(source);
        }
        
        Debug.Log($"Здоровье {gameObject.name}: {currentHealth}");
    }

    [ClientRpc]
    void RpcDie(GameObject killer)
    {
        Debug.Log($"RpcDie вызван для {gameObject.name}");
        
        if (deathEffect != null) 
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        if (playerController != null) 
        {
            playerController.OnPlayerDeath();
        }
        
        // Сброс тотема при смерти
        PlayerTotemInteraction totemInteraction = GetComponent<PlayerTotemInteraction>();
        if (totemInteraction != null)
        {
            totemInteraction.OnPlayerDeath();
        }
        
        if (isServer)
        {
            StartCoroutine(ServerRespawn(3f));
        }
    }

    IEnumerator ServerRespawn(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    [Server]
    public void Respawn()
    {
        currentHealth = maxHealth;
        lastDamageTime = 0f;
        
        MyNetworkManager networkManager = FindFirstObjectByType<MyNetworkManager>();
        
        if (networkManager != null)
        {
            Transform spawnPoint = networkManager.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
                Debug.Log($"{gameObject.name} возрожден на спавн-поинте: {spawnPoint.name}");
            }
            else
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-10f, 10f), 
                    2f, 
                    Random.Range(-10f, 10f)
                );
                transform.position = randomPos;
            }
        }
        else
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-10f, 10f), 
                2f, 
                Random.Range(-10f, 10f)
            );
            transform.position = randomPos;
        }
        
        RpcRespawn();
    }

    [ClientRpc]
    void RpcRespawn()
    {
        if (playerController != null)
        {
            playerController.OnPlayerRespawn();
        }
        
        Debug.Log($"{gameObject.name} возродился!");
    }

    private void OnHealthChanged(float oldHealth, float newHealth)
    {
        Debug.Log($"{gameObject.name}: Здоровье изменилось с {oldHealth} на {newHealth}");
    }
}