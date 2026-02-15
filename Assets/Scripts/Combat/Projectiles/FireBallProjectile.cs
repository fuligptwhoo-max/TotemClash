using UnityEngine;
using System.Collections;
using TotemClash.Combat;
using TotemClash.Classes;

namespace TotemClash.Combat.Projectiles
{
    public class FireballProjectile : MonoBehaviour
    {
        [Header("Settings")]
        public float speed = 40f;
        public float damage = 50f;
        public float lifeTime = 3f;
        
        [Header("Rocket Homing")]
        public bool useHoming = true;
        public float rotationSpeed = 2000f;
        public bool usePrediction = true;
        public bool usePerfectIntercept = true;
        public float aggressiveChase = 1.5f;
        
        [Header("Height Constraint")]
        [Tooltip("Минимальная высота полета (не втыкается в землю)")]
        public float minAimHeight = 0.5f;
        [Tooltip("Максимальный угол падения (0 = горизонтально, -1 = вниз)")]
        public float minDirectionY = -0.3f; // Не лететь слишком круто вниз
        
        [Header("Ignore Owner")]
        public float ignoreOwnerTime = 0.3f;
        
        [Header("Effects")]
        public GameObject impactEffect;
        
        private GameObject owner;
        private float projectileSpeed;
        private int projectileDamage;
        private Rigidbody rb;
        private bool hasExploded = false;
        private bool collisionsIgnored = false;
        private Transform targetTransform;
        private bool hasTarget = false;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            
            var trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.5f;
            
            var phys = gameObject.AddComponent<SphereCollider>();
            phys.isTrigger = false;
            phys.radius = 0.5f;
        }
        
        private void Start()
        {
            if (owner != null)
            {
                IgnoreOwnerCollisions(true);
                StartCoroutine(EnableOwnerCollisionsAfterDelay());
            }
            
            if (rb != null)
                rb.linearVelocity = transform.forward * projectileSpeed;
            
            Invoke(nameof(DestroyProjectile), lifeTime);
        }
        
        public void Initialize(float speed, int damage, GameObject owner)
        {
            this.projectileSpeed = speed;
            this.projectileDamage = damage;
            this.owner = owner;
        }
        
        public void SetTarget(Transform target)
        {
            this.targetTransform = target;
            this.hasTarget = target != null;
        }
        
        private void FixedUpdate()
        {
            if (hasExploded) return;
            
            if (hasTarget && targetTransform != null && useHoming)
            {
                UpdateRocketHoming();
            }
            
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = transform.forward * projectileSpeed;
            }
        }
        
        private void UpdateRocketHoming()
        {
            if (targetTransform == null) 
            {
                hasTarget = false;
                return;
            }
            
            Vector3 targetPos = targetTransform.position + Vector3.up * 1.2f;
            Vector3 targetVel = GetTargetVelocity();
            
            Vector3 aimPoint;
            
            if (usePerfectIntercept && usePrediction)
            {
                aimPoint = CalculateInterceptPoint(targetPos, targetVel);
            }
            else if (usePrediction)
            {
                float distance = Vector3.Distance(transform.position, targetPos);
                float timeToTarget = distance / projectileSpeed;
                aimPoint = targetPos + targetVel * timeToTarget;
            }
            else
            {
                aimPoint = targetPos;
            }
            
            // ИСПРАВЛЕНО: Ограничение высоты - не целиться ниже minAimHeight
            if (aimPoint.y < minAimHeight)
            {
                aimPoint.y = minAimHeight;
            }
            
            Vector3 direction = (aimPoint - transform.position).normalized;
            
            // ИСПРАВЛЕНО: Дополнительная защита - не лететь слишком круто вниз
            if (direction.y < minDirectionY)
            {
                direction.y = minDirectionY;
                direction.Normalize();
            }
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                
                float angleToTarget = Vector3.Angle(transform.forward, direction);
                float actualRotationSpeed = (angleToTarget > 90f) ? rotationSpeed * aggressiveChase : rotationSpeed;
                
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    actualRotationSpeed * Time.fixedDeltaTime
                );
            }
            
            Debug.DrawLine(transform.position, aimPoint, Color.cyan, 0.1f);
            Debug.DrawRay(transform.position, transform.forward * 5f, Color.red, 0.1f);
        }
        
        private Vector3 CalculateInterceptPoint(Vector3 targetPos, Vector3 targetVel)
        {
            Vector3 toTarget = targetPos - transform.position;
            
            if (targetVel.sqrMagnitude < 0.01f)
                return targetPos;
            
            float a = Vector3.Dot(targetVel, targetVel) - (projectileSpeed * projectileSpeed);
            float b = 2f * Vector3.Dot(toTarget, targetVel);
            float c = Vector3.Dot(toTarget, toTarget);
            
            float discriminant = b * b - 4f * a * c;
            
            if (discriminant < 0)
            {
                float distance = toTarget.magnitude;
                float timeToTarget = distance / projectileSpeed;
                return targetPos + targetVel * timeToTarget * aggressiveChase;
            }
            
            float sqrtDisc = Mathf.Sqrt(discriminant);
            float t1 = (-b + sqrtDisc) / (2f * a);
            float t2 = (-b - sqrtDisc) / (2f * a);
            
            float t = Mathf.Max(t1, t2);
            
            if (t < 0)
                return targetPos;
            
            return targetPos + targetVel * t;
        }
        
        private Vector3 GetTargetVelocity()
        {
            if (targetTransform == null) return Vector3.zero;
            
            Rigidbody targetRb = targetTransform.GetComponent<Rigidbody>();
            if (targetRb != null) return targetRb.linearVelocity;
            
            CharacterController cc = targetTransform.GetComponent<CharacterController>();
            if (cc != null) return cc.velocity;
            
            return Vector3.zero;
        }
        
        private IEnumerator EnableOwnerCollisionsAfterDelay()
        {
            yield return new WaitForSeconds(ignoreOwnerTime);
            if (owner != null) IgnoreOwnerCollisions(false);
        }
        
        private void IgnoreOwnerCollisions(bool ignore)
        {
            if (owner == null) return;
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var oc in ownerColliders)
            {
                if (oc == null || oc.isTrigger) continue;
                foreach (var pc in projectileColliders)
                {
                    if (pc == null) continue;
                    Physics.IgnoreCollision(pc, oc, ignore);
                }
            }
            collisionsIgnored = ignore;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (hasExploded) return;
            if (other.gameObject == owner && collisionsIgnored) return;
            
            if (other.gameObject != owner && (other.CompareTag("Player") || other.CompareTag("Enemy")))
            {
                ApplyDamage(other.gameObject);
                Explode();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (hasExploded) return;
            GameObject hit = collision.gameObject;
            
            if (hit != owner && !hit.CompareTag("Player") && !hit.CompareTag("Enemy") && !hit.CompareTag("Projectile"))
            {
                Explode();
            }
            else if (hit == owner && !collisionsIgnored)
            {
                ApplyDamage(hit);
                Explode();
            }
        }
        
        private void ApplyDamage(GameObject target)
        {
            HealthSystem health = target.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(projectileDamage, owner);
                if (health.IsDead && owner != null)
                {
                    var score = owner.GetComponent<PlayerScore>();
                    if (score != null) score.OnKillEnemy(target);
                }
            }
        }
        
        private void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;
            if (impactEffect != null)
                Instantiate(impactEffect, transform.position, Quaternion.identity);
            DestroyProjectile();
        }
        
        private void DestroyProjectile()
        {
            Destroy(gameObject);
        }
    }
}