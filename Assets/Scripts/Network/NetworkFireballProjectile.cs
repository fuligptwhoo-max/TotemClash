using UnityEngine;
using Mirror;

public class NetworkFireballProjectile : NetworkBehaviour
{
    [Header("НАСТРОЙКИ УРОНА")]
    public float damage = 20f;
    public float explosionRadius = 2f;
    public float lifetime = 4f;
    
    [Header("КОЛЛИЗИЯ")]
    public float ignoreCollisionTime = 0.2f;
    
    [Header("ССЫЛКИ")]
    public GameObject explosionEffect;
    public GameObject owner;
    
    [Header("ФИЗИКА")]
    public bool useGravity = false;
    public float linearDamping = 0.5f;
    
    [SyncVar]
    private Vector3 moveDirection;
    
    [SyncVar]
    private float speed = 25f;
    
    private bool hasExploded = false;
    private float spawnTime;
    private bool collisionsIgnored = false;
    private Rigidbody rb;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.useGravity = useGravity;
        rb.linearDamping = linearDamping;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        spawnTime = Time.time;
        
        if (lifetime > 0)
        {
            Invoke(nameof(DestroyProjectile), lifetime);
        }
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Запускаем движение на клиенте
        if (rb != null && moveDirection != Vector3.zero)
        {
            rb.linearVelocity = moveDirection * speed;
        }
    }
    
    [Server]
    public void Initialize(Vector3 direction, float fireballSpeed, GameObject ownerObject)
    {
        moveDirection = direction.normalized;
        speed = fireballSpeed;
        owner = ownerObject;
        
        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
        
        IgnoreCollisionWithOwner();
    }
    
    [Server]
    private void IgnoreCollisionWithOwner()
    {
        if (owner != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    Physics.IgnoreCollision(projCollider, ownerCollider, true);
                }
            }
            
            collisionsIgnored = true;
            
            // Через время включаем коллизии обратно
            Invoke(nameof(EnableCollisions), ignoreCollisionTime);
        }
    }
    
    [Server]
    private void EnableCollisions()
    {
        if (owner != null && collisionsIgnored)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    Physics.IgnoreCollision(projCollider, ownerCollider, false);
                }
            }
            
            collisionsIgnored = false;
        }
    }
    
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        if (collision.gameObject == owner) return;
        
        Explode();
    }
    
    [Server]
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        RpcPlayExplosionEffect(transform.position);
        
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject != owner && hit.CompareTag("Player"))
            {
                HealthSystem health = hit.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner);
                }
            }
        }
        
        DestroyProjectile();
    }
    
    [ClientRpc]
    private void RpcPlayExplosionEffect(Vector3 position)
    {
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    [Server]
    private void DestroyProjectile()
    {
        NetworkServer.Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}