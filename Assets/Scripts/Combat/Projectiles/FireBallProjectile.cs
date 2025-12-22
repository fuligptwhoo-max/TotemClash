using UnityEngine;
using Mirror;
using System.Collections;

public class FireballProjectile : NetworkBehaviour
{
    [Header("Настройки")]
    public float speed = 40f;
    public float damage = 50f;
    public float lifeTime = 3f;
    
    [Header("Вращение")]
    public float rotationSpeed = 360f;
    public Vector3 rotationAxis = Vector3.up;
    
    [Header("Триггер для игроков")]
    public float triggerRadius = 1.5f;
    
    [Header("Игнорирование владельца")]
    public float ignoreOwnerTime = 0.3f; // Время, в течение которого игнорируем столкновения с владельцем
    
    [Header("Цель")]
    public uint targetPlayerId;
    public bool useDirectTarget = true;
    public Vector3 initialTargetPosition;
    
    [SyncVar]
    public GameObject owner;
    
    private Rigidbody rb;
    private SphereCollider triggerCollider;
    private SphereCollider physicsCollider;
    private bool hasExploded = false;
    private Vector3 currentTargetPosition;
    private float checkInterval = 0.05f;
    private float lastCheckTime = 0f;
    private bool collisionsIgnored = false;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Создаем триггер для игроков
        triggerCollider = gameObject.AddComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = triggerRadius;
        
        // Создаем физический коллайдер для стен
        physicsCollider = gameObject.AddComponent<SphereCollider>();
        physicsCollider.isTrigger = false;
        physicsCollider.radius = 0.5f;
        
        // Игнорируем столкновения с владельцем временно
        if (owner != null)
        {
            IgnoreOwnerCollisions(true);
            StartCoroutine(EnableOwnerCollisionsAfterDelay());
        }
        
        currentTargetPosition = initialTargetPosition;
        
        if (rb != null)
        {
            Vector3 direction = (currentTargetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;
        }
        
        if (isServer)
        {
            Invoke(nameof(DestroyFireball), lifeTime);
        }
    }
    
    private void IgnoreOwnerCollisions(bool ignore)
    {
        if (owner == null) return;
        
        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider[] projectileColliders = GetComponentsInChildren<Collider>();
        
        foreach (var ownerCollider in ownerColliders)
        {
            if (ownerCollider == null || ownerCollider.isTrigger) continue;
            
            foreach (var projectileCollider in projectileColliders)
            {
                if (projectileCollider == null) continue;
                
                Physics.IgnoreCollision(projectileCollider, ownerCollider, ignore);
            }
        }
        
        collisionsIgnored = ignore;
    }
    
    private IEnumerator EnableOwnerCollisionsAfterDelay()
    {
        yield return new WaitForSeconds(ignoreOwnerTime);
        
        if (owner != null)
        {
            IgnoreOwnerCollisions(false);
        }
    }
    
    private void FixedUpdate()
    {
        // Коррекция траектории для автонаведения
        if (!useDirectTarget && targetPlayerId != 0 && rb != null)
        {
            Transform targetTransform = MagicianClass.GetPlayerTransform(targetPlayerId);
            if (targetTransform != null)
            {
                currentTargetPosition = targetTransform.position + Vector3.up * 1f;
                
                Vector3 toTarget = (currentTargetPosition - transform.position);
                
                if (toTarget.magnitude > 0.1f)
                {
                    Vector3 desiredDirection = toTarget.normalized;
                    Vector3 currentDirection = rb.linearVelocity.normalized;
                    
                    Vector3 newDirection = Vector3.RotateTowards(
                        currentDirection, 
                        desiredDirection, 
                        0.5f * Time.fixedDeltaTime, 
                        0f
                    );
                    
                    rb.linearVelocity = newDirection * speed;
                }
            }
        }
    }
    
    private void Update()
    {
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime, Space.Self);
        
        // Проверка столкновений с игроками
        if (Time.time - lastCheckTime > checkInterval)
        {
            CheckForPlayerCollisions();
            lastCheckTime = Time.time;
        }
    }
    
    private void CheckForPlayerCollisions()
    {
        if (!isServer || hasExploded) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, triggerRadius);
        foreach (var collider in hitColliders)
        {
            if (collider.gameObject == owner && collisionsIgnored) continue;
            
            if (collider.gameObject != owner && collider.CompareTag("Player"))
            {
                HealthSystem health = collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner);
                    Debug.Log($"Фаербол попал в {collider.gameObject.name} (OverlapSphere)");
                    Explode();
                    return;
                }
            }
        }
    }
    
    // Триггер для Character Controller
    private void OnTriggerEnter(Collider other)
    {
        if (!isServer || hasExploded) return;
        
        if (other.gameObject == owner && collisionsIgnored) return;
        
        if (other.gameObject != owner && other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner);
                Debug.Log($"Фаербол попал в {other.gameObject.name} (Trigger)");
            }
            Explode();
        }
    }
    
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;
        
        GameObject hitObject = collision.gameObject;
        
        // Если попали в стену или окружение
        if (hitObject != owner && !hitObject.CompareTag("Player") && !hitObject.CompareTag("Projectile"))
        {
            Debug.Log($"Фаербол столкнулся с {hitObject.name}");
            Explode();
        }
        else if (hitObject == owner && !collisionsIgnored)
        {
            // Если время игнорирования прошло и фаербол вернулся к владельцу
            HealthSystem health = hitObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner);
                Debug.Log($"Фаербол вернулся и попал в владельца {hitObject.name}");
            }
            Explode();
        }
    }
    
    private void Explode()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        if (isServer)
            DestroyFireball();
    }
    
    [Server]
    private void DestroyFireball()
    {
        NetworkServer.Destroy(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        if (owner != null && collisionsIgnored)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, owner.transform.position);
        }
    }
}