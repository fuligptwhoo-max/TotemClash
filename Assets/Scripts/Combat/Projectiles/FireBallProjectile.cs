using UnityEngine;

public class FireballProjectile : MonoBehaviour
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
    
    private void Start()
    {
        spawnTime = Time.time;
        
        if (lifetime > 0)
        {
            Destroy(gameObject, lifetime);
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
        }
    }
    
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
    
    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        if (collision.gameObject == owner) return;
        
        Explode();
    }
    
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        if (explosionEffect != null)
        {
            GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.gameObject != owner && hit.CompareTag("Player"))
            {
                Debug.Log($"[ФАЕРБОЛ] Попадание по {hit.name}, урон: {damage}");
                HealthSystem health = hit.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner);
                }
            }
        }
        
        Destroy(gameObject);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}