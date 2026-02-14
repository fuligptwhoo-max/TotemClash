using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;

public class FireballProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public float speed = 40f;
    public float damage = 50f;
    public float lifeTime = 3f;
    
    [Header("Rotation")]
    public float rotationSpeed = 360f;
    public Vector3 rotationAxis = Vector3.up;
    
    [Header("Player Trigger")]
    public float triggerRadius = 1.5f;
    
    [Header("Ignore Owner")]
    public float ignoreOwnerTime = 0.3f;
    
    [Header("Target")]
    public int targetPlayerId = -1;
    public bool useDirectTarget = true;
    public Vector3 initialTargetPosition;
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<GameObject> owner = new SyncVar<GameObject>();
    
    private Rigidbody rb;
    private SphereCollider triggerCollider;
    private SphereCollider physicsCollider;
    private bool hasExploded = false;
    private Vector3 currentTargetPosition;
    private float checkInterval = 0.05f;
    private float lastCheckTime = 0f;
    private bool collisionsIgnored = false;
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
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
        if (owner.Value != null)
        {
            IgnoreOwnerCollisions(true);
            StartCoroutine(EnableOwnerCollisionsAfterDelay());
        }
        
        currentTargetPosition = initialTargetPosition;
        
        // Применяем урон из настроек если доступно
        if (base.IsServerInitialized && GameSettings.Instance != null)
        {
            damage = GameSettings.Instance.GetDamage();
            Debug.Log($"[Fireball] Damage set from GameSettings: {damage}");
        }
        
        // Запускаем снаряд ТОЛЬКО на сервере (физика сервера авторитетна)
        if (rb != null && base.IsServerInitialized)
        {
            Vector3 direction = (currentTargetPosition - transform.position).normalized;
            rb.linearVelocity = direction * speed;
            Debug.Log($"[Fireball] Launched! Speed: {speed}, Damage: {damage}, Direction: {direction}");
        }
        
        if (base.IsServerInitialized)
        {
            Invoke(nameof(DestroyFireball), lifeTime);
        }
    }
    
    private void IgnoreOwnerCollisions(bool ignore)
    {
        if (owner.Value == null) return;
        
        Collider[] ownerColliders = owner.Value.GetComponentsInChildren<Collider>();
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
        
        if (owner.Value != null)
        {
            IgnoreOwnerCollisions(false);
        }
    }
    
    private void FixedUpdate()
    {
        // Коррекция траектории для автонаведения
        // Не меняем velocity если rigidbody kinematic (например, когда подобран тотем)
        if (!useDirectTarget && targetPlayerId != -1 && rb != null && !rb.isKinematic)
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
        
        // Проверка столкновений с игроками (только на сервере)
        if (base.IsServerInitialized && Time.time - lastCheckTime > checkInterval)
        {
            CheckForPlayerCollisions();
            lastCheckTime = Time.time;
        }
    }
    
    private void CheckForPlayerCollisions()
    {
        if (hasExploded) return;
        
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, triggerRadius);
        foreach (var collider in hitColliders)
        {
            if (collider.gameObject == owner.Value && collisionsIgnored) continue;
            
            if (collider.gameObject != owner.Value && collider.CompareTag("Player"))
            {
                HealthSystem health = collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner.Value);
                    Debug.Log($"Fireball hit {collider.gameObject.name} (OverlapSphere)");
                    Explode();
                    return;
                }
            }
        }
    }
    
    // Триггер для Character Controller
    private void OnTriggerEnter(Collider other)
    {
        if (!base.IsServerInitialized || hasExploded) return;
        
        if (other.gameObject == owner.Value && collisionsIgnored) return;
        
        if (other.gameObject != owner.Value && other.CompareTag("Player"))
        {
            HealthSystem health = other.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner.Value);
                Debug.Log($"Fireball hit {other.gameObject.name} (Trigger)");
            }
            Explode();
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (!base.IsServerInitialized || hasExploded) return;
        
        GameObject hitObject = collision.gameObject;
        
        // Если попали в стену или окружение
        if (hitObject != owner.Value && !hitObject.CompareTag("Player") && !hitObject.CompareTag("Projectile"))
        {
            Debug.Log($"Fireball collided with {hitObject.name}");
            Explode();
        }
        else if (hitObject == owner.Value && !collisionsIgnored)
        {
            // Если время игнорирования прошло и фаербол вернулся к владельцу
            HealthSystem health = hitObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner.Value);
                Debug.Log($"Fireball returned and hit owner {hitObject.name}");
            }
            Explode();
        }
    }
    
    private void Explode()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        if (base.IsServerInitialized)
            DestroyFireball();
    }
    
    [Server]
    private void DestroyFireball()
    {
        base.ServerManager.Despawn(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        
        if (owner.Value != null && collisionsIgnored)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, owner.Value.transform.position);
        }
    }
}
