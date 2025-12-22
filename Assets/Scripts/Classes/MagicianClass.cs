using UnityEngine;
using System.Collections;
using Mirror;
using System.Collections.Generic;

public class MagicianClass : NetworkBehaviour
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform castPoint;
    public float fireballCooldown = 1f;
    public float fireballSpeed = 40f;
    
    [Header("Вращение фаербола")]
    public float fireballRotationSpeed = 360f;
    public Vector3 fireballRotationAxis = Vector3.up;
    
    [Header("Animation Settings")]
    public float attackAnimationDelay = 0.3f;
    
    [Header("Наведение")]
    public float autoAimRange = 15f;
    public bool enableAutoAim = true;
    public float autoAimAngle = 45f;
    public LayerMask obstacleLayers;
    public float heightTolerance = 2f; // Допустимая разница высот
    
    private float lastFireballTime;
    private bool isAttacking = false;
    private NetworkPlayerController playerController;
    private Animator animator;
    private AimingSystem aimingSystem;
    
    private static Dictionary<uint, Transform> playerTransforms = new Dictionary<uint, Transform>();
    
    public void Initialize(NetworkPlayerController controller, PlayerCombat combat, Animator anim)
    {
        playerController = controller;
        animator = anim;
        aimingSystem = GetComponent<AimingSystem>();
        
        if (castPoint == null)
        {
            FindCastPoint();
        }
        
        if (isServer && netIdentity != null)
        {
            RegisterPlayer(netIdentity.netId, transform);
        }
        
        Debug.Log($"MagicianClass initialized for {gameObject.name}, netId: {netIdentity?.netId}");
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
            castPointObj.transform.localPosition = new Vector3(0, 1.5f, 1f);
            castPoint = castPointObj.transform;
        }
    }
    
    public bool PrimaryAttack(Vector3 targetPosition)
    {
        if (Time.time - lastFireballTime < fireballCooldown || isAttacking)
            return false;
        
        if (playerController != null && playerController.isLocalPlayer)
        {
            uint targetPlayerId = 0;
            bool useDirectTarget = true;
            Vector3 finalTargetPosition = targetPosition;
            
            // ПРИОРИТЕТ 1: Прицел на игрока
            if (aimingSystem != null && aimingSystem.IsAimingAtPlayer())
            {
                GameObject aimedPlayer = aimingSystem.GetAimedPlayer();
                if (aimedPlayer != null)
                {
                    NetworkIdentity identity = aimedPlayer.GetComponent<NetworkIdentity>();
                    if (identity != null && identity.netId != netIdentity.netId)
                    {
                        targetPlayerId = identity.netId;
                        // Берем позицию игрока на его высоте
                        finalTargetPosition = aimedPlayer.transform.position + Vector3.up * 1f;
                        useDirectTarget = true;
                        Debug.Log($"Выстрел по цели в прицеле: {aimedPlayer.name}, высота: {finalTargetPosition.y}");
                    }
                }
            }
            
            // ПРИОРИТЕТ 2: Автонаведение
            if (targetPlayerId == 0 && enableAutoAim && !IsAimingAtGround(targetPosition))
            {
                targetPlayerId = FindAutoAimTarget();
                if (targetPlayerId != 0)
                {
                    if (playerTransforms.TryGetValue(targetPlayerId, out Transform targetTransform))
                    {
                        // Берем позицию цели на её высоте
                        finalTargetPosition = targetTransform.position + Vector3.up * 1f;
                        useDirectTarget = false;
                        Debug.Log($"Автонаведение на игрока ID: {targetPlayerId}, высота: {finalTargetPosition.y}");
                    }
                }
            }
            
            StartCoroutine(PerformFireballAttack(finalTargetPosition, targetPlayerId, useDirectTarget));
            return true;
        }
        
        return false;
    }
    
    IEnumerator PerformFireballAttack(Vector3 targetPosition, uint targetPlayerId, bool useDirectTarget)
    {
        isAttacking = true;
        lastFireballTime = Time.time;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsAttacking", true);
        }
        
        yield return new WaitForSeconds(attackAnimationDelay);
        
        if (isServer)
        {
            SpawnFireballServer(targetPosition, targetPlayerId, useDirectTarget);
        }
        else
        {
            CmdSpawnFireball(targetPosition, targetPlayerId, useDirectTarget);
        }
        
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }
        
        isAttacking = false;
    }
    
    [Command]
    private void CmdSpawnFireball(Vector3 targetPosition, uint targetPlayerId, bool useDirectTarget)
    {
        SpawnFireballServer(targetPosition, targetPlayerId, useDirectTarget);
    }
    
    [Server]
    private void SpawnFireballServer(Vector3 targetPosition, uint targetPlayerId, bool useDirectTarget)
    {
        Vector3 spawnPosition = castPoint != null ? 
            castPoint.position : transform.position + Vector3.up * 1.5f;
        
        Vector3 direction = (targetPosition - spawnPosition).normalized;
        
        // ИСПРАВЛЕНИЕ: НЕ обнуляем Y для автонаведения!
        // Если это автонаведение - проверяем высоту
        if (!useDirectTarget)
        {
            // Проверяем разницу высот
            float heightDifference = Mathf.Abs(targetPosition.y - spawnPosition.y);
            if (heightDifference > heightTolerance)
            {
                // Если цель слишком высоко/низко, стреляем с учетом высоты
                direction = (targetPosition - spawnPosition).normalized;
            }
            else
            {
                // Если разница высот небольшая, можно немного сгладить траекторию
                direction.y *= 0.5f; // Уменьшаем вертикальную компоненту, но не обнуляем
                direction.Normalize();
            }
        }
        
        // Если направление нулевое - стреляем вперед
        if (direction == Vector3.zero)
        {
            direction = transform.forward;
            direction.Normalize();
        }
        
        // Компенсация поворота префаба (-90 по X)
        Quaternion compensation = Quaternion.Euler(270, 0, 0);
        Quaternion finalRotation = Quaternion.LookRotation(direction) * compensation;
        
        Debug.Log($"=== СОЗДАНИЕ ФАЕРБОЛА ===");
        Debug.Log($"Тип: {(useDirectTarget ? "Прямой" : "Автонаведение")}");
        Debug.Log($"Направление: {direction}");
        Debug.Log($"Высота цели: {targetPosition.y}, высота спавна: {spawnPosition.y}");
        
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, finalRotation);
        
        FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
        if (projectile != null)
        {
            projectile.owner = gameObject;
            projectile.speed = fireballSpeed;
            projectile.rotationSpeed = fireballRotationSpeed;
            projectile.rotationAxis = fireballRotationAxis;
            projectile.targetPlayerId = targetPlayerId;
            projectile.useDirectTarget = useDirectTarget;
            projectile.initialTargetPosition = targetPosition;
        }
        
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Используем transform.forward фаербола
            rb.linearVelocity = fireball.transform.forward * fireballSpeed;
        }
        
        NetworkServer.Spawn(fireball);
    }
    
    private bool IsAimingAtGround(Vector3 targetPosition)
    {
        if (aimingSystem == null) return true;
        
        if (aimingSystem.IsAimingAtPlayer()) return false;
        
        // Проверяем высоту цели
        return Mathf.Abs(targetPosition.y) < 0.5f;
    }
    
    private uint FindAutoAimTarget()
    {
        uint bestTargetId = 0;
        float bestScore = 0f;
        
        Vector3 playerForward = transform.forward;
        Vector3 playerPosition = transform.position;
        
        foreach (var kvp in playerTransforms)
        {
            if (kvp.Key == netIdentity.netId) continue;
            
            Vector3 toTarget = (kvp.Value.position - playerPosition);
            float distance = toTarget.magnitude;
            
            if (distance > autoAimRange) continue;
            
            // Проверяем разницу высот
            float heightDifference = Mathf.Abs(kvp.Value.position.y - playerPosition.y);
            if (heightDifference > heightTolerance) continue;
            
            float angle = Vector3.Angle(playerForward, toTarget.normalized);
            if (angle > autoAimAngle) continue;
            
            // Проверка на препятствия
            if (obstacleLayers != 0)
            {
                RaycastHit hit;
                Vector3 rayOrigin = playerPosition + Vector3.up * 1f;
                if (Physics.Raycast(rayOrigin, toTarget.normalized, out hit, distance, obstacleLayers))
                {
                    if (!hit.collider.CompareTag("Player"))
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
    
    public static void RegisterPlayer(uint playerId, Transform playerTransform)
    {
        if (!playerTransforms.ContainsKey(playerId))
        {
            playerTransforms[playerId] = playerTransform;
            Debug.Log($"Игрок зарегистрирован в трекере: ID {playerId}");
        }
    }
    
    public static void UnregisterPlayer(uint playerId)
    {
        if (playerTransforms.ContainsKey(playerId))
        {
            playerTransforms.Remove(playerId);
            Debug.Log($"Игрок удален из трекера: ID {playerId}");
        }
    }
    
    public static Transform GetPlayerTransform(uint playerId)
    {
        playerTransforms.TryGetValue(playerId, out Transform transform);
        return transform;
    }
    
    private void OnDestroy()
    {
        if (netIdentity != null)
        {
            UnregisterPlayer(netIdentity.netId);
        }
    }
    
    public bool Ability1(Vector3 targetPosition) { return false; }
    public bool Ability2(Vector3 targetPosition) { return false; }
    public bool UltimateAbility(Vector3 targetPosition) { return false; }
    
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