using UnityEngine;
using TotemClash.Combat;

namespace TotemClash.Combat.Projectiles
{
    public class LightningProjectile : MonoBehaviour
    {
        [Header("Settings")]
        public float damage = 20f;
        public int chainCount = 3;
        public float chainRange = 4f;
        public float lifeTime = 3f;
        
        // Local references
        private GameObject owner;
        private bool hasHit = false;
        
        private void Start()
        {
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
        
        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;
            
            // Lightning hit logic
            if (collision.gameObject != owner && collision.gameObject.CompareTag("Player"))
            {
                hasHit = true;
                Debug.Log($"{owner?.name} dealt {damage} lightning damage to {collision.gameObject.name}");
                
                HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner);
                }
                
                // Chain lightning
                ApplyChainLightning(collision.gameObject.transform.position);
            }
            
            // Destroy projectile
            DestroyProjectile();
        }
        
        private void ApplyChainLightning(Vector3 hitPosition)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hitPosition, chainRange);
            int chainsLeft = chainCount;
            
            foreach (var collider in nearbyColliders)
            {
                if (chainsLeft <= 0) break;
                
                if (collider.gameObject != owner && 
                    collider.gameObject.CompareTag("Player") &&
                    collider.gameObject.transform.position != hitPosition)
                {
                    HealthSystem health = collider.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        health.TakeDamage(damage * 0.5f, owner); // Chain damage is less
                        chainsLeft--;
                        
                        Debug.Log($"Chain lightning hit {collider.gameObject.name}");
                    }
                }
            }
        }
        
        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
    }
}
