using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TotemClash.Combat.Projectiles;
using TotemClash.Network;
using TotemClash.Combat;

namespace TotemClash.Classes
{
    public class MagicianClass : MonoBehaviour
    {
        [Header("Fireball Settings")]
        public GameObject fireballPrefab;
        public Transform castPoint;
        public float fireballCooldown = 1f;
        public float fireballSpeed = 40f;
        public int fireballDamage = 50;
        
        [Header("Animation Settings")]
        public float attackAnimationDelay = 0.3f;
        
        [Header("Targeting")]
        public float autoAimRange = 15f;
        public bool enableAutoAim = true;
        public float autoAimAngle = 45f;
        public LayerMask obstacleLayers;
        public float heightTolerance = 2f;
        
        [Header("Height Constraint")]
        public float minFlightHeight = -0.5f;
        [Tooltip("Минимальное расстояние до точки прицеливания (если ближе - использовать направление камеры)")]
        public float minAimDistance = 2.0f;
        
        private static int nextPlayerId = 1;
        private int playerId = -1;
        
        private float lastFireballTime;
        private bool isAttacking = false;
        private Animator animator;
        private AimingSystem aimingSystem;
        private Camera mainCamera;
        
        private static Dictionary<int, Transform> playerTransforms = new Dictionary<int, Transform>();
        
        private void OnEnable()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnProjectileSpeedChanged.AddListener(OnProjectileSpeedChanged);
                GameSettings.Instance.OnDamageChanged.AddListener(OnDamageChanged);
                ApplySettings();
            }
        }
        
        private void OnDisable()
        {
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnProjectileSpeedChanged.RemoveListener(OnProjectileSpeedChanged);
                GameSettings.Instance.OnDamageChanged.RemoveListener(OnDamageChanged);
            }
        }
        
        private void ApplySettings()
        {
            if (GameSettings.Instance != null)
            {
                fireballSpeed = GameSettings.Instance.GetProjectileSpeed();
                fireballDamage = GameSettings.Instance.GetDamagePerHit();
            }
        }
        
        private void OnProjectileSpeedChanged(float newSpeed) => fireballSpeed = newSpeed;
        private void OnDamageChanged(int newDamage) => fireballDamage = newDamage;
        
        public void Initialize(Animator anim)
        {
            animator = anim;
            aimingSystem = GetComponent<AimingSystem>();
            mainCamera = Camera.main;
            
            if (castPoint == null)
            {
                FindCastPoint();
            }
            
            if (playerId == -1)
            {
                playerId = nextPlayerId++;
            }
            
            RegisterPlayer(playerId, transform);
            
            ApplySettings();
        }
        
        void FindCastPoint()
        {
            Transform foundPoint = transform.Find("SpellCastPoint");
            if (foundPoint == null) foundPoint = transform.Find("CastPoint");
            if (foundPoint != null)
            {
                castPoint = foundPoint;
            }
            else
            {
                GameObject castPointObj = new GameObject("SpellCastPoint");
                castPointObj.transform.SetParent(transform);
                castPointObj.transform.localPosition = new Vector3(0, 1.5f, 0f);
                castPoint = castPointObj.transform;
            }
        }
        
        public bool PrimaryAttack(Vector3 targetPosition)
        {
            if (Time.time - lastFireballTime < fireballCooldown || isAttacking)
                return false;
            
            Transform targetTransform = null;
            
            if (aimingSystem != null && aimingSystem.IsAimingAtPlayer())
            {
                targetTransform = aimingSystem.GetAimedTransform();
            }
            else if (enableAutoAim)
            {
                int targetId = FindAutoAimTarget();
                if (targetId != -1)
                {
                    playerTransforms.TryGetValue(targetId, out targetTransform);
                }
            }
            
            StartCoroutine(PerformFireballAttack(targetPosition, targetTransform));
            return true;
        }
        
        IEnumerator PerformFireballAttack(Vector3 targetPosition, Transform targetTransform)
        {
            isAttacking = true;
            lastFireballTime = Time.time;
            
            if (animator != null)
            {
                animator.SetTrigger("Attack");
                animator.SetBool("IsAttacking", true);
            }
            
            yield return new WaitForSeconds(attackAnimationDelay);
            
            SpawnFireball(targetPosition, targetTransform);
            
            if (animator != null)
                animator.SetBool("IsAttacking", false);
            
            isAttacking = false;
        }
        
        private void SpawnFireball(Vector3 targetPosition, Transform targetTransform)
        {
            if (fireballPrefab == null)
            {
                Debug.LogError("Fireball prefab is not assigned!");
                return;
            }
    
            Vector3 spawnPosition = castPoint != null ? 
                castPoint.position : transform.position + Vector3.up * 1.5f;
            
            Vector3 direction;
            
            if (targetTransform != null)
            {
                // Цель есть - целимся в неё
                Vector3 targetPos = targetTransform.position + Vector3.up * 1.2f;
                direction = (targetPos - spawnPosition).normalized;
            }
            else
            {
                // ИСПРАВЛЕНО: Проверяем расстояние до точки прицеливания
                Vector3 toTarget = targetPosition - spawnPosition;
                float distance = toTarget.magnitude;
                
                if (distance < minAimDistance)
                {
                    // Точка слишком близко (в упор) - используем направление камеры/мыши
                    // Стреляем туда, куда смотрит курсор, а не в точку за спиной
                    direction = GetMouseDirection(spawnPosition);
                    Debug.Log($"[Magician] Close aim corrected, using mouse direction: {direction}");
                }
                else
                {
                    // Точка далеко - используем обычное направление
                    Vector3 actualTarget = targetPosition;
                    
                    // Ограничиваем только высоту
                    float minY = spawnPosition.y + minFlightHeight;
                    if (actualTarget.y < minY)
                    {
                        actualTarget.y = minY;
                    }
                    
                    direction = (actualTarget - spawnPosition).normalized;
                }
            }
            
            if (direction == Vector3.zero)
            {
                direction = transform.forward;
            }
            
            Quaternion rotation = Quaternion.LookRotation(direction);
            
            GameObject fireball = Instantiate(fireballPrefab, spawnPosition, rotation);
            
            FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
            if (projectile != null)
            {
                projectile.Initialize(fireballSpeed, fireballDamage, gameObject);
                
                if (targetTransform != null)
                {
                    projectile.SetTarget(targetTransform);
                }
            }
            
            Rigidbody rb = fireball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = fireball.transform.forward * fireballSpeed;
            }
        }
        
        // ИСПРАВЛЕНО: Получаем направление от камеры через курсор (как в шутерах)
        private Vector3 GetMouseDirection(Vector3 spawnPosition)
        {
            if (mainCamera == null)
                mainCamera = Camera.main;
                
            if (mainCamera == null)
                return transform.forward;
            
            // Луч из камеры через курсор мыши
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            
            // Используем направление луча как направление выстрела
            // Это дает точное направление куда смотрит мышь, даже если точка близко
            return ray.direction.normalized;
        }
        
        private int FindAutoAimTarget()
        {
            int bestTargetId = -1;
            float bestScore = 0f;
            
            Vector3 playerForward = transform.forward;
            Vector3 playerPosition = transform.position;
            
            foreach (var kvp in playerTransforms)
            {
                if (kvp.Key == this.playerId) continue;
                
                Vector3 toTarget = (kvp.Value.position - playerPosition);
                float distance = toTarget.magnitude;
                
                if (distance > autoAimRange) continue;
                
                float heightDifference = Mathf.Abs(kvp.Value.position.y - playerPosition.y);
                if (heightDifference > heightTolerance) continue;
                
                float angle = Vector3.Angle(playerForward, toTarget.normalized);
                if (angle > autoAimAngle) continue;
                
                if (obstacleLayers != 0)
                {
                    RaycastHit hit;
                    Vector3 rayOrigin = playerPosition + Vector3.up * 1f;
                    if (Physics.Raycast(rayOrigin, toTarget.normalized, out hit, distance, obstacleLayers))
                    {
                        if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Enemy"))
                            continue;
                    }
                }
                
                float distanceScore = 1f - (distance / autoAimRange);
                float angleScore = 1f - (angle / autoAimAngle);
                float heightScore = 1f - (heightDifference / heightTolerance);
                float totalScore = distanceScore * 0.3f + angleScore * 0.5f + heightScore * 0.2f;
                
                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    bestTargetId = kvp.Key;
                }
            }
            
            return bestTargetId;
        }
        
        public static void RegisterPlayer(int playerId, Transform playerTransform)
        {
            if (!playerTransforms.ContainsKey(playerId))
                playerTransforms[playerId] = playerTransform;
        }
        
        public static void UnregisterPlayer(int playerId)
        {
            if (playerTransforms.ContainsKey(playerId))
                playerTransforms.Remove(playerId);
        }
        
        void OnDestroy()
        {
            if (playerId != -1) UnregisterPlayer(playerId);
        }
        
        public int GetPlayerId() => playerId;
        public bool Ability1(Vector3 targetPosition) => false;
        public bool Ability2(Vector3 targetPosition) => false;
        public bool UltimateAbility(Vector3 targetPosition) => false;
        
        public float GetCooldownProgress(int abilityIndex)
        {
            if (abilityIndex == 0)
                return Mathf.Clamp01((Time.time - lastFireballTime) / fireballCooldown);
            return 0f;
        }
        
        public bool IsAbilityReady(int abilityIndex)
        {
            if (abilityIndex == 0)
                return Time.time - lastFireballTime >= fireballCooldown && !isAttacking;
            return false;
        }
    }
}