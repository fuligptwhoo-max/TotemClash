using UnityEngine;
using TotemClash.Combat;

namespace TotemClash.Combat.Projectiles
{
    public class IceSpikeProjectile : MonoBehaviour
    {
        [Header("Settings")]
        public float damage = 25f;
        public float slowAmount = 0.5f;
        public float slowDuration = 3f;
        public float lifeTime = 3f;
        
        [Header("Ignore Owner")]
        public float ignoreCollisionTime = 0.2f;
        
        // Local references
        private GameObject owner;
        private float spawnTime;
        private bool collisionsIgnored = false;
        
        private void Awake()
        {
            spawnTime = Time.time;
        }
        
        private void Start()
        {
            // Ignore collisions with owner
            if (owner != null)
            {
                IgnoreCollisionWithOwner();
            }
            
            // Auto-destroy after lifetime
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
        
        /// <summary>
        /// Initialize the projectile with owner
        /// </summary>
        public void Initialize(GameObject owner)
        {
            this.owner = owner;
        }
        
        private void Update()
        {
            if (collisionsIgnored && Time.time - spawnTime > ignoreCollisionTime)
            {
                EnableCollisions();
            }
        }
        
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
            if (owner != null && collisionsIgnored)
            {
                Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
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
            if (collision.gameObject != owner && collision.gameObject.CompareTag("Player"))
            {
                Debug.Log($"{owner?.name} dealt {damage} ice damage to {collision.gameObject.name}");
                
                HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner);
                }
            }
            
            // Destroy projectile
            DestroyProjectile();
        }
        
        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
    }
}
