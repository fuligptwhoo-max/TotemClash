using UnityEngine;
using UnityEngine.Events;
using System.Collections;

namespace TotemClash.Combat
{
    public class HealthSystem : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Death Animation")]
        [SerializeField] private bool useRigidbodyDeath = true;
        [SerializeField] private float deathAnimationDuration = 1f;
        [SerializeField] private float respawnDelay = 3f;
        
        [Header("Respawn")]
        [Tooltip("If true, will automatically respawn after death delay. If false, destroys the object.")]
        public bool autoRespawn = true;

        [Header("Spawn Protection (Roblox Style)")]
        [Tooltip("If true, grants temporary invincibility after respawn")]
        [SerializeField] private bool useSpawnProtection = true;
        [Tooltip("Duration of spawn protection in seconds")]
        [SerializeField] private float spawnProtectionDuration = 2f;
        [Tooltip("Visual effect for spawn protection (optional)")]
        [SerializeField] private GameObject spawnProtectionEffect;
        
        [Header("References")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Animator animator;
        [SerializeField] private Collider hitCollider;
        [SerializeField] private Behaviour[] componentsToDisableOnDeath;

        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent<float> OnDamaged;
        public UnityEvent OnDeath;
        public UnityEvent OnRespawn;

        private bool isDead = false;
        private bool isSpawnProtected = false;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 originalScale;
        private Coroutine spawnProtectionCoroutine;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool IsSpawnProtected => isSpawnProtected;

        private void Awake()
        {
            currentHealth = maxHealth;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            originalScale = transform.localScale;

            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (animator == null)
                animator = GetComponent<Animator>();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(GetHealthPercent());
            
            // Применяем защиту при старте (начальный спавн)
            if (useSpawnProtection)
            {
                StartSpawnProtection();
            }
        }

        public void TakeDamage(float damage, GameObject source)
        {
            if (isDead || damage <= 0)
                return;

            // ПРОВЕРКА НА SPAWN PROTECTION
            if (isSpawnProtected)
            {
                Debug.Log($"[HealthSystem] {gameObject.name} ignored {damage} damage due to spawn protection!");
                return;
            }

            currentHealth -= damage;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

            OnHealthChanged?.Invoke(GetHealthPercent());
            OnDamaged?.Invoke(damage);

            if (currentHealth <= 0)
            {
                Die(source);
            }
        }

        private void Die(GameObject killer)
        {
            if (isDead)
                return;

            isDead = true;
            OnDeath?.Invoke();

            StartCoroutine(DeathAnimationCoroutine());
        }

        private IEnumerator DeathAnimationCoroutine()
        {
            DropTotemOnDeath();
            SetComponentsEnabled(false);

            if (useRigidbodyDeath && rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Vector3 randomForce = new Vector3(
                    Random.Range(-2f, 2f),
                    Random.Range(3f, 5f),
                    Random.Range(-2f, 2f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);

                Vector3 randomTorque = new Vector3(
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f),
                    Random.Range(-5f, 5f)
                );
                rb.AddTorque(randomTorque, ForceMode.Impulse);
            }
            else
            {
                yield return StartCoroutine(SimpleDeathAnimation());
            }
            
            yield return new WaitForSeconds(deathAnimationDuration);

            if (autoRespawn)
            {
                yield return new WaitForSeconds(respawnDelay - deathAnimationDuration);
                Respawn();
            }
            else
            {
                yield return new WaitForSeconds(1f);
                Destroy(gameObject);
            }
        }

        private IEnumerator SimpleDeathAnimation()
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition + Vector3.down * 0.5f;
            Quaternion startRotation = transform.rotation;
            Quaternion endRotation = Quaternion.Euler(
                startRotation.eulerAngles.x + 90f,
                startRotation.eulerAngles.y,
                startRotation.eulerAngles.z
            );

            float elapsed = 0f;
            while (elapsed < deathAnimationDuration)
            {
                float t = elapsed / deathAnimationDuration;
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                transform.rotation = Quaternion.Lerp(startRotation, endRotation, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        public void Respawn()
        {
            ResetHealth();

            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
            transform.localScale = originalScale;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            SetComponentsEnabled(true);

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            isDead = false;
            
            // ЗАПУСКАЕМ SPAWN PROTECTION ПОСЛЕ РЕСПАВНА
            if (useSpawnProtection)
            {
                StartSpawnProtection();
            }
            
            OnRespawn?.Invoke();
        }
        
        private void StartSpawnProtection()
        {
            // Останавливаем предыдущую корутину если есть
            if (spawnProtectionCoroutine != null)
            {
                StopCoroutine(spawnProtectionCoroutine);
            }
            
            spawnProtectionCoroutine = StartCoroutine(SpawnProtectionCoroutine());
        }

        private IEnumerator SpawnProtectionCoroutine()
        {
            isSpawnProtected = true;
            
            // Включаем визуальный эффект если есть
            if (spawnProtectionEffect != null)
            {
                spawnProtectionEffect.SetActive(true);
            }
            
            Debug.Log($"[HealthSystem] {gameObject.name} has spawn protection for {spawnProtectionDuration} seconds");
            
            // Можно добавить мигание или другой визуальный эффект здесь
            float timer = 0f;
            while (timer < spawnProtectionDuration)
            {
                timer += Time.deltaTime;
                
                // Пример мигания (опционально)
                /*
                if (animator != null)
                {
                    float blink = Mathf.PingPong(timer * 10f, 1f);
                    // Применить прозрачность или эмиссию
                }
                */
                
                yield return null;
            }
            
            isSpawnProtected = false;
            
            if (spawnProtectionEffect != null)
            {
                spawnProtectionEffect.SetActive(false);
            }
            
            Debug.Log($"[HealthSystem] {gameObject.name} spawn protection ended");
        }

        public void ResetHealth()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(GetHealthPercent());
        }

        public float GetHealthPercent()
        {
            return maxHealth > 0 ? currentHealth / maxHealth : 0f;
        }

        private void DropTotemOnDeath()
        {
            PlayerTotemInteraction totemInteraction = GetComponent<PlayerTotemInteraction>();
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                totemInteraction.OnPlayerDeath();
                Debug.Log($"[HealthSystem] {gameObject.name} dropped totem on death.");
            }
        }

        public void SetSpawnPosition(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        private void SetComponentsEnabled(bool enabled)
        {
            if (hitCollider != null)
                hitCollider.enabled = enabled;

            if (componentsToDisableOnDeath != null)
            {
                foreach (var component in componentsToDisableOnDeath)
                {
                    if (component != null)
                        component.enabled = enabled;
                }
            }

            if (animator != null && !enabled)
                animator.enabled = false;
            else if (animator != null && enabled)
                animator.enabled = true;
                
            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = enabled;
        }

        public void Heal(float amount)
        {
            if (isDead || amount <= 0)
                return;

            currentHealth += amount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            OnHealthChanged?.Invoke(GetHealthPercent());
        }

        public void Kill(GameObject killer = null)
        {
            TakeDamage(maxHealth, killer);
        }
    }
}