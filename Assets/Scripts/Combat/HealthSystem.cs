using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using System.Collections;

public class HealthSystem : NetworkBehaviour
{
    // FishNet 4.x SyncVar
    public readonly SyncVar<float> currentHealth = new SyncVar<float>(100f);
    
    public float maxHealth = 100f;

    private NetworkPlayerController playerController;
    public GameObject deathEffect;
    
    // Для избежания двойного урона
    private float lastDamageTime = 0f;
    private const float damageCooldown = 0.1f;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        playerController = GetComponent<NetworkPlayerController>();
        
        // Подписываемся на изменения
        currentHealth.OnChange += OnHealthChanged;
        
        if (base.IsServerInitialized)
        {
            currentHealth.Value = maxHealth;
        }
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        currentHealth.OnChange -= OnHealthChanged;
    }

    public void TakeDamage(float damage, GameObject source)
    {
        if (currentHealth.Value <= 0) return;
        
        Debug.Log($"TakeDamage called! Source: {source?.name}, Damage: {damage}, IsServer: {base.IsServerInitialized}, IsOwner: {base.IsOwner}");
        
        // Проверка кулдауна
        if (Time.time - lastDamageTime < damageCooldown) return;
        
        if (base.IsServerInitialized)
        {
            ApplyDamage(damage, source);
        }
        else if (base.IsOwner)
        {
            CmdTakeDamage(damage, source);
        }
    }

    [ServerRpc]
    void CmdTakeDamage(float damage, GameObject source)
    {
        Debug.Log($"CmdTakeDamage received on server for {gameObject.name}, damage: {damage}");
        ApplyDamage(damage, source);
    }

    [Server]
    void ApplyDamage(float damage, GameObject source)
    {
        Debug.Log($"ApplyDamage on server: {gameObject.name} takes {damage} damage");
        
        lastDamageTime = Time.time;
        currentHealth.Value -= damage;
        
        if (currentHealth.Value <= 0)
        {
            currentHealth.Value = 0;
            RpcDie(source);
        }
        
        Debug.Log($"Health of {gameObject.name}: {currentHealth.Value}");
    }

    [ObserversRpc]
    void RpcDie(GameObject killer)
    {
        Debug.Log($"RpcDie called for {gameObject.name}");
        
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
        
        if (base.IsServerInitialized)
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
        currentHealth.Value = maxHealth;
        lastDamageTime = 0f;
        
        MyNetworkManager networkManager = MyNetworkManager.Instance;
        
        if (networkManager != null)
        {
            Transform spawnPoint = networkManager.GetRandomSpawnPoint();
            if (spawnPoint != null)
            {
                Teleport(spawnPoint.position, spawnPoint.rotation);
                Debug.Log($"{gameObject.name} respawned at spawn point: {spawnPoint.name}");
            }
            else
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-10f, 10f), 
                    2f, 
                    Random.Range(-10f, 10f)
                );
                Teleport(randomPos, Quaternion.identity);
            }
        }
        else
        {
            Vector3 randomPos = new Vector3(
                Random.Range(-10f, 10f), 
                2f, 
                Random.Range(-10f, 10f)
            );
            Teleport(randomPos, Quaternion.identity);
        }
        
        RpcRespawn();
    }
    
    /// <summary>
    /// Телепортирует игрока (только на сервере)
    /// </summary>
    [Server]
    private void Teleport(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        
        // Отправляем клиентам обновление позиции
        TargetTeleport(Owner, position, rotation);
    }
    
    [TargetRpc]
    private void TargetTeleport(NetworkConnection conn, Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    [ObserversRpc]
    void RpcRespawn()
    {
        if (playerController != null)
        {
            playerController.OnPlayerRespawn();
        }
        
        Debug.Log($"{gameObject.name} respawned!");
    }

    /// <summary>
    /// Хук для SyncVar - вызывается при изменении здоровья
    /// </summary>
    private void OnHealthChanged(float prev, float next, bool asServer)
    {
        Debug.Log($"{gameObject.name}: Health changed from {prev} to {next}");
    }
}
