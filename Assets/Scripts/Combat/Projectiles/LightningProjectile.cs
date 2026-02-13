using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class LightningProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float damage = 20f;
    public int chainCount = 3;
    public float chainRange = 4f;
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<GameObject> owner = new SyncVar<GameObject>();
    
    private bool hasHit = false;
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!base.IsServerInitialized) return;
        if (hasHit) return;
        
        // Логика удара молнии
        if (collision.gameObject != owner.Value && collision.gameObject.CompareTag("Player"))
        {
            hasHit = true;
            Debug.Log($"{owner.Value?.name} dealt {damage} lightning damage to {collision.gameObject.name}");
            
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner.Value);
            }
            
            // Цепная молния
            ApplyChainLightning(collision.gameObject.transform.position);
        }
        
        // Уничтожаем на сервере
        base.ServerManager.Despawn(gameObject);
    }
    
    [Server]
    private void ApplyChainLightning(Vector3 hitPosition)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(hitPosition, chainRange);
        int chainsLeft = chainCount;
        
        foreach (var collider in nearbyColliders)
        {
            if (chainsLeft <= 0) break;
            
            if (collider.gameObject != owner.Value && 
                collider.gameObject.CompareTag("Player") &&
                collider.gameObject.transform.position != hitPosition)
            {
                HealthSystem health = collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage * 0.5f, owner.Value); // Цепной урон меньше
                    chainsLeft--;
                    
                    Debug.Log($"Chain lightning hit {collider.gameObject.name}");
                }
            }
        }
    }
}
