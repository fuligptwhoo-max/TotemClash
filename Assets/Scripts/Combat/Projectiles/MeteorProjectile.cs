using UnityEngine;
using TotemClash.Combat;

namespace TotemClash.Combat.Projectiles
{
    public class MeteorProjectile : MonoBehaviour
    {
        [Header("Settings")]
        public Vector3 targetPosition;
        public float damage = 50f;
        public float radius = 5f;
        public float fallSpeed = 15f;
        public float lifeTime = 10f;
        
        [Header("Effects")]
        public GameObject explosionEffect;
        
        // Local references
        private GameObject owner;
        private bool hasExploded = false;
        
        private void Start()
        {
            // If targetPosition is not set, use position in front
            if (targetPosition == Vector3.zero)
            {
                targetPosition = transform.position + transform.forward * 10f;
            }
            
            // Auto-destroy after lifetime as fallback
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
        
        /// <summary>
        /// Initialize the projectile with owner and target position
        /// </summary>
        public void Initialize(GameObject owner, Vector3 targetPos)
        {
            this.owner = owner;
            this.targetPosition = targetPos;
        }
        
        /// <summary>
        /// Initialize the projectile with owner only
        /// </summary>
        public void Initialize(GameObject owner)
        {
            this.owner = owner;
        }
        
        private void Update()
        {
            if (hasExploded) return;
            
            // Move towards target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, fallSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
            {
                Explode();
            }
        }
        
        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            
            // Meteor explosion
            Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider collider in colliders)
            {
                if (collider.gameObject != owner && collider.CompareTag("Player"))
                {
                    HealthSystem health = collider.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        health.TakeDamage(damage, owner);
                        Debug.Log($"{owner?.name} dealt {damage} meteor damage to {collider.name}");
                    }
                }
            }
            
            // Spawn explosion effect if assigned
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, Quaternion.identity);
            }
            
            // Destroy
            DestroyProjectile();
        }
        
        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
            
            if (targetPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetPosition);
                Gizmos.DrawWireSphere(targetPosition, 0.5f);
            }
        }
    }
}
