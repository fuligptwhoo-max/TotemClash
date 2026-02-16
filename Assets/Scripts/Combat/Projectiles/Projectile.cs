using UnityEngine;

namespace TotemClash.Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Flight Settings")]
        public float minHeightAboveGround = 0.8f;
        public float groundCheckDistance = 2f;
        public float terrainLiftStrength = 0.15f;
        public LayerMask groundLayers;
        public bool bounceOffGround = true; // НОВОЕ: отскакивать от земли
        
        float damage;
        float speed;
        float maxRange;
        Transform target;
        GameObject owner;
        GameObject hitEffect;
        
        Vector3 startPosition;
        bool initialized = false;
        float spawnTime;
        Rigidbody rb;
        Vector3 lastPosition;
        
        public void Initialize(float dmg, float spd, float range, Transform tgt, GameObject own, GameObject hitFx)
        {
            damage = dmg;
            speed = spd;
            maxRange = range;
            target = tgt;
            owner = own;
            hitEffect = hitFx;
            
            startPosition = transform.position;
            lastPosition = startPosition;
            spawnTime = Time.time;
            initialized = true;
            
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
            
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Ground");
                
            if (owner != null)
            {
                Collider[] ownerCols = owner.GetComponentsInChildren<Collider>();
                Collider[] myCols = GetComponents<Collider>();
                foreach (var oc in ownerCols)
                    foreach (var mc in myCols)
                        if (oc != null && mc != null)
                            Physics.IgnoreCollision(mc, oc, true);
            }
        }
        
        void FixedUpdate()
        {
            if (!initialized) return;
            
            if (Vector3.Distance(startPosition, transform.position) > maxRange)
            {
                Destroy(gameObject);
                return;
            }
            
            // Проверка застревания (если не двигаемся)
            if (Vector3.Distance(transform.position, lastPosition) < 0.01f && Time.time - spawnTime > 0.5f)
            {
                Explode();
                return;
            }
            lastPosition = transform.position;
            
            if (target != null)
            {
                UpdateHoming();
            }
            
            CheckHeight();
            
            rb.linearVelocity = transform.forward * speed;
        }
        
        void UpdateHoming()
        {
            if (target == null) return;
            
            Vector3 targetPos = target.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - transform.position).normalized;
            
            if (direction.y < -0.2f)
            {
                direction.y = -0.2f;
                direction.Normalize();
            }
            
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, 
                targetRot, 
                2000f * Time.fixedDeltaTime
            );
        }
        
        void CheckHeight()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckDistance, groundLayers))
            {
                float height = hit.distance;
                
                if (height < minHeightAboveGround)
                {
                    if (bounceOffGround)
                    {
                        // Плавный подъем с сохранением скорости
                        Vector3 currentForward = transform.forward;
                        float lift = (minHeightAboveGround - height) * terrainLiftStrength * 2f;
                        
                        Vector3 newDirection = new Vector3(
                            currentForward.x * 0.95f, 
                            Mathf.Max(currentForward.y + lift, 0.2f), 
                            currentForward.z * 0.95f
                        ).normalized;
                        
                        transform.rotation = Quaternion.LookRotation(newDirection);
                        
                        // Форсируем подъем если совсем низко
                        if (height < 0.3f)
                        {
                            transform.position += Vector3.up * 0.1f;
                        }
                    }
                }
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (!initialized) return;
            if (other.gameObject == owner) return;
            if (other.isTrigger) return;
            
            // Игнорируем землю первые 0.1 сек
            bool isGround = ((1 << other.gameObject.layer) & groundLayers) != 0;
            if (isGround && Time.time - spawnTime < 0.1f) return;
            
            if (other.CompareTag("Player") || other.CompareTag("Enemy"))
            {
                ApplyDamage(other.gameObject);
                Explode();
            }
            else if (isGround && !bounceOffGround)
            {
                Explode();
            }
        }
        
        void OnCollisionEnter(Collision collision)
        {
            if (!initialized) return;
            
            GameObject hit = collision.gameObject;
            if (hit == owner) return;
            
            bool isGround = ((1 << hit.layer) & groundLayers) != 0;
            
            if (isGround && Time.time - spawnTime < 0.1f) return;
            
            if (hit.CompareTag("Player") || hit.CompareTag("Enemy"))
            {
                ApplyDamage(hit);
                Explode();
            }
            else if (isGround)
            {
                if (!bounceOffGround)
                {
                    Explode();
                }
                else
                {
                    // Рикошет от земли
                    Vector3 reflectDir = Vector3.Reflect(transform.forward, collision.contacts[0].normal);
                    transform.rotation = Quaternion.LookRotation(reflectDir);
                }
            }
            else
            {
                Explode();
            }
        }
        
        void ApplyDamage(GameObject target)
        {
            HealthSystem health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner);
                if (health.IsDead && owner != null)
                {
                    var score = owner.GetComponent<PlayerScore>();
                    if (score != null) score.OnKillEnemy(target);
                }
            }
        }
        
        void Explode()
        {
            if (hitEffect != null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}