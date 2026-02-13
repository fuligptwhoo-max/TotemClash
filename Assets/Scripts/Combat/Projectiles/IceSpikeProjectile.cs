using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class IceSpikeProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float damage = 25f;
    public float slowAmount = 0.5f;
    public float slowDuration = 3f;
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<GameObject> owner = new SyncVar<GameObject>();
    
    public float ignoreCollisionTime = 0.2f;
    
    private float spawnTime;
    private bool collisionsIgnored = false;
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        spawnTime = Time.time;
        
        // Игнорируем столкновения с владельцем
        if (owner.Value != null)
        {
            IgnoreCollisionWithOwner();
        }
    }
    
    private void Update()
    {
        if (collisionsIgnored && Time.time - spawnTime > ignoreCollisionTime)
        {
            EnableCollisions();
        }
    }
    
    public void IgnoreCollisionWithOwner()
    {
        if (owner.Value != null)
        {
            Collider[] ownerColliders = owner.Value.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    if (ownerCollider != null && projCollider != null)
                    {
                        Physics.IgnoreCollision(projCollider, ownerCollider, true);
                    }
                }
            }
            
            collisionsIgnored = true;
        }
    }
    
    private void EnableCollisions()
    {
        if (owner.Value != null && collisionsIgnored)
        {
            Collider[] ownerColliders = owner.Value.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    if (ownerCollider != null && projCollider != null)
                    {
                        Physics.IgnoreCollision(projCollider, ownerCollider, false);
                    }
                }
            }
            
            collisionsIgnored = false;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!base.IsServerInitialized) return;
        
        if (collision.gameObject != owner.Value && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{owner.Value?.name} dealt {damage} ice damage to {collision.gameObject.name}");
            
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner.Value);
            }
        }
        
        // Уничтожаем на сервере
        base.ServerManager.Despawn(gameObject);
    }
}
