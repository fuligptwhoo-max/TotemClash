using UnityEngine;
using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class MagicianClass : NetworkBehaviour
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform castPoint;
    public float fireballCooldown = 1f;
    public float fireballSpeed = 40f;
    
    [Header("Fireball Rotation")]
    public float fireballRotationSpeed = 360f;
    public Vector3 fireballRotationAxis = Vector3.up;
    
    [Header("Animation Settings")]
    public float attackAnimationDelay = 0.3f;
    
    [Header("Targeting")]
    public float autoAimRange = 15f;
    public bool enableAutoAim = true;
    public float autoAimAngle = 45f;
    public LayerMask obstacleLayers;
    public float heightTolerance = 2f;
    
    private float lastFireballTime;
    private bool isAttacking = false;
    private NetworkPlayerController playerController;
    private Animator animator;
    private AimingSystem aimingSystem;
    
    // Статический словарь для трекинга игроков
    private static Dictionary<int, Transform> playerTransforms = new Dictionary<int, Transform>();
    
    public void Initialize(NetworkPlayerController controller, PlayerCombat combat, Animator anim)
    {
        playerController = controller;
        animator = anim;
        aimingSystem = GetComponent<AimingSystem>();
        
        if (castPoint == null)
        {
            FindCastPoint();
        }
        
        if (base.IsServerInitialized && base.NetworkObject != null)
        {
            RegisterPlayer(base.NetworkObject.ObjectId, transform);
        }
        
        Debug.Log($"MagicianClass initialized for {gameObject.name}, ObjectId: {base.NetworkObject?.ObjectId}");
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
        
        if (playerController != null && base.IsOwner)
        {
            int targetPlayerId = -1;
            bool useDirectTarget = true;
            Vector3 finalTargetPosition = targetPosition;
            
            // ПРИОРИТЕТ 1: Прицел на игрока
            if (aimingSystem != null && aimingSystem.IsAimingAtPlayer())
            {
                GameObject aimedPlayer = aimingSystem.GetAimedPlayer();
                if (aimedPlayer != null)
                {
                    NetworkObject netObj = aimedPlayer.GetComponent<NetworkObject>();
                    if (netObj != null && netObj.ObjectId != base.NetworkObject.ObjectId)
                    {
                        targetPlayerId = netObj.ObjectId;
                        finalTargetPosition = aimedPlayer.transform.position + Vector3.up * 1f;
                        useDirectTarget = true;
                        Debug.Log($"Shot at targeted player: {aimedPlayer.name}, height: {finalTargetPosition.y}");
                    }
                }
            }
            
            // ПРИОРИТЕТ 2: Автонаведение
            if (targetPlayerId == -1 && enableAutoAim && !IsAimingAtGround(targetPosition))
            {
                targetPlayerId = FindAutoAimTarget();
                if (targetPlayerId != -1)
                {
                    if (playerTransforms.TryGetValue(targetPlayerId, out Transform targetTransform))
                    {
                        finalTargetPosition = targetTransform.position + Vector3.up * 1f;
                        useDirectTarget = false;
                        Debug.Log($"Auto-aim at player ID: {targetPlayerId}, height: {finalTargetPosition.y}");
                    }
                }
            }
            
            StartCoroutine(PerformFireballAttack(finalTargetPosition, targetPlayerId, useDirectTarget));
            return true;
        }
        
        return false;
    }
    
    IEnumerator PerformFireballAttack(Vector3 targetPosition, int targetPlayerId, bool useDirectTarget)
    {
        isAttacking = true;
        lastFireballTime = Time.time;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsAttacking", true);
        }
        
        yield return new WaitForSeconds(attackAnimationDelay);
        
        if (base.IsServerInitialized)
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
    
    [ServerRpc]
    private void CmdSpawnFireball(Vector3 targetPosition, int targetPlayerId, bool useDirectTarget)
    {
        SpawnFireballServer(targetPosition, targetPlayerId, useDirectTarget);
    }
    
    [Server]
    private void SpawnFireballServer(Vector3 targetPosition, int targetPlayerId, bool useDirectTarget)
    {
        Vector3 spawnPosition = castPoint != null ? 
            castPoint.position : transform.position + Vector3.up * 1.5f;
        
        Vector3 direction = (targetPosition - spawnPosition).normalized;
        
        // НЕ обнуляем Y для автонаведения
        if (!useDirectTarget)
        {
            float heightDifference = Mathf.Abs(targetPosition.y - spawnPosition.y);
            if (heightDifference > heightTolerance)
            {
                direction = (targetPosition - spawnPosition).normalized;
            }
            else
            {
                direction.y *= 0.5f;
                direction.Normalize();
            }
        }
        
        if (direction == Vector3.zero)
        {
            direction = transform.forward;
            direction.Normalize();
        }
        
        // Компенсация поворота префаба (-90 по X)
        Quaternion compensation = Quaternion.Euler(270, 0, 0);
        Quaternion finalRotation = Quaternion.LookRotation(direction) * compensation;
        
        Debug.Log($"=== SPAWNING FIREBALL ===");
        Debug.Log($"Type: {(useDirectTarget ? "Direct" : "Auto-aim")}");
        Debug.Log($"Direction: {direction}");
        Debug.Log($"Target height: {targetPosition.y}, Spawn height: {spawnPosition.y}");
        
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, finalRotation);
        
        FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
        if (projectile != null)
        {
            projectile.owner.Value = gameObject;
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
            rb.linearVelocity = fireball.transform.forward * fireballSpeed;
        }
        
        // Спавним на сервере - FishNet автоматически синхронизирует с клиентами
        base.ServerManager.Spawn(fireball);
    }
    
    private bool IsAimingAtGround(Vector3 targetPosition)
    {
        if (aimingSystem == null) return true;
        
        if (aimingSystem.IsAimingAtPlayer()) return false;
        
        return Mathf.Abs(targetPosition.y) < 0.5f;
    }
    
    private int FindAutoAimTarget()
    {
        int bestTargetId = -1;
        float bestScore = 0f;
        
        Vector3 playerForward = transform.forward;
        Vector3 playerPosition = transform.position;
        
        foreach (var kvp in playerTransforms)
        {
            if (kvp.Key == base.NetworkObject.ObjectId) continue;
            
            Vector3 toTarget = (kvp.Value.position - playerPosition);
            float distance = toTarget.magnitude;
            
            if (distance > autoAimRange) continue;
            
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
    
    public static void RegisterPlayer(int playerId, Transform playerTransform)
    {
        if (!playerTransforms.ContainsKey(playerId))
        {
            playerTransforms[playerId] = playerTransform;
            Debug.Log($"Player registered in tracker: ID {playerId}");
        }
    }
    
    public static void UnregisterPlayer(int playerId)
    {
        if (playerTransforms.ContainsKey(playerId))
        {
            playerTransforms.Remove(playerId);
            Debug.Log($"Player removed from tracker: ID {playerId}");
        }
    }
    
    public static Transform GetPlayerTransform(int playerId)
    {
        playerTransforms.TryGetValue(playerId, out Transform transform);
        return transform;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        if (base.NetworkObject != null)
        {
            UnregisterPlayer(base.NetworkObject.ObjectId);
        }
    }
    
    // Заглушки для будущих способностей
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
