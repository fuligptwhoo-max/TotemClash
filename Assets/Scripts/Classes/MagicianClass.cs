using UnityEngine;
using System.Collections;

public class MagicianClass : PlayerClass
{
    [Header("ОСНОВНЫЕ НАСТРОЙКИ")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform castPoint;
    [SerializeField] private float fireballSpeed = 25f;
    [SerializeField] private float fireballCooldown = 1f;
    
    [Header("ВРАЩЕНИЕ ФАЕРБОЛА")]
    [SerializeField] private Vector3 fireballSpawnRotation = new Vector3(0, 90, 0);
    
    [Header("КОЛЛИЗИЯ")]
    [SerializeField] private float spawnDistanceFromPlayer = 2f;
    
    [Header("АНИМАЦИЯ")]
    [SerializeField] private float attackAnimationDelay = 0.5f;
    
    [Header("ДРУГИЕ СПОСОБНОСТИ")]
    [SerializeField] private GameObject iceSpikePrefab;
    [SerializeField] private GameObject lightningPrefab;
    [SerializeField] private GameObject meteorPrefab;
    
    [Header("КУЛДАУНЫ")]
    [SerializeField] private float iceSpikeCooldown = 3f;
    [SerializeField] private float lightningCooldown = 5f;
    [SerializeField] private float meteorCooldown = 10f;
    
    // Кэшированные ссылки
    private GameObject validatedFireballPrefab;
    private GameObject validatedIceSpikePrefab;
    private GameObject validatedLightningPrefab;
    private GameObject validatedMeteorPrefab;
    
    // Таймеры
    private float lastFireballTime = -100f;
    private float lastIceSpikeTime = -100f;
    private float lastLightningTime = -100f;
    private float lastMeteorTime = -100f;
    
    // Состояние атаки
    private bool isAttacking = false;
    private Vector3 pendingAttackTarget;
    
    public override void Initialize(PlayerController controller, PlayerCombat combat, Animator anim)
    {
        base.Initialize(controller, combat, anim);
        
        Debug.Log($"MagicianClass инициализирован для {gameObject.name}");
        
        CreateCastPointIfNeeded();
        ValidateAndCachePrefabs();
    }
    
    private void CreateCastPointIfNeeded()
    {
        if (castPoint == null)
        {
            GameObject castPointObj = new GameObject("CastPoint");
            castPointObj.transform.SetParent(transform);
            castPointObj.transform.localPosition = new Vector3(0, 1.5f, 2f);
            castPoint = castPointObj.transform;
            Debug.Log("Создана точка каста");
        }
    }
    
    private void ValidateAndCachePrefabs()
    {
        validatedFireballPrefab = ValidateProjectilePrefab(fireballPrefab, "Fireball");
        validatedIceSpikePrefab = ValidateProjectilePrefab(iceSpikePrefab, "Ice Spike");
        validatedLightningPrefab = ValidateProjectilePrefab(lightningPrefab, "Lightning");
        validatedMeteorPrefab = ValidateProjectilePrefab(meteorPrefab, "Meteor");
    }
    
    private GameObject ValidateProjectilePrefab(GameObject prefab, string prefabName)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"{prefabName} префаб не назначен");
            return null;
        }
        
        if (prefab.scene.IsValid())
        {
            Debug.LogError($"ОШИБКА: {prefabName} - это экземпляр на сцене! Используйте префаб из папки Resources/Prefabs");
            return null;
        }
        
        return prefab;
    }
    
    public override bool PrimaryAttack(Vector3 targetPosition)
    {
        if (Time.time - lastFireballTime < fireballCooldown || isAttacking)
        {
            return false;
        }
        
        pendingAttackTarget = targetPosition;
        StartCoroutine(PerformAttackWithAnimation());
        
        return true;
    }
    
    private IEnumerator PerformAttackWithAnimation()
    {
        isAttacking = true;
        
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsAttacking", true);
        }
        
        yield return new WaitForSeconds(attackAnimationDelay);
        
        if (castPoint == null)
        {
            CreateCastPointIfNeeded();
        }
        
        Vector3 direction = CalculateDirection(pendingAttackTarget);
        CreateFireball(direction);
        
        lastFireballTime = Time.time;
        
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
        }
        
        isAttacking = false;
    }
    
    private Vector3 CalculateDirection(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        
        if (direction.magnitude < 0.1f)
        {
            direction = transform.forward;
        }
        
        return direction;
    }
    
    private void CreateFireball(Vector3 direction)
    {
        if (validatedFireballPrefab == null)
        {
            Debug.LogError("Нет валидного префаба фаербола!");
            return;
        }
        
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(fireballSpawnRotation);
        Vector3 spawnPosition = castPoint.position + direction * spawnDistanceFromPlayer;
        
        GameObject fireball = Instantiate(validatedFireballPrefab, spawnPosition, rotation);
        
        FireballProjectile projectile = fireball.GetComponent<FireballProjectile>();
        if (projectile != null)
        {
            projectile.owner = gameObject;
            projectile.IgnoreCollisionWithOwner();
        }
        
        Rigidbody rb = fireball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * fireballSpeed;
        }
        else
        {
            Debug.LogError("У фаербола нет Rigidbody!");
        }
        
        Debug.Log($"Выпущен фаербол! Скорость: {fireballSpeed}, Направление: {direction}");
    }
    
    public override bool Ability1(Vector3 targetPosition)
    {
        if (Time.time - lastIceSpikeTime < iceSpikeCooldown) return false;
        
        if (validatedIceSpikePrefab == null)
        {
            Debug.LogWarning("Ice Spike префаб не назначен или невалиден!");
            return false;
        }
        
        Vector3 direction = CalculateDirection(targetPosition);
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(fireballSpawnRotation);
        
        Vector3 spawnPosition = castPoint.position + direction * spawnDistanceFromPlayer;
        
        GameObject iceSpike = Instantiate(validatedIceSpikePrefab, spawnPosition, rotation);
        
        IceSpikeProjectile iceScript = iceSpike.GetComponent<IceSpikeProjectile>();
        if (iceScript != null)
        {
            iceScript.owner = gameObject;
            iceScript.IgnoreCollisionWithOwner();
        }
        
        Rigidbody rb = iceSpike.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * fireballSpeed;
        }
        
        lastIceSpikeTime = Time.time;
        return true;
    }
    
    public override bool Ability2(Vector3 targetPosition)
    {
        if (Time.time - lastLightningTime < lightningCooldown) return false;
        
        if (validatedLightningPrefab == null)
        {
            Debug.LogWarning("Lightning префаб не назначен или невалиден!");
            return false;
        }
        
        Vector3 direction = CalculateDirection(targetPosition);
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(fireballSpawnRotation);
        
        Vector3 spawnPosition = castPoint.position + direction * spawnDistanceFromPlayer;
        
        GameObject lightning = Instantiate(validatedLightningPrefab, spawnPosition, rotation);
        
        LightningProjectile lightningScript = lightning.GetComponent<LightningProjectile>();
        if (lightningScript != null)
        {
            lightningScript.owner = gameObject;
        }
        
        Rigidbody rb = lightning.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = direction * fireballSpeed;
        }
        
        lastLightningTime = Time.time;
        return true;
    }
    
    public override bool UltimateAbility(Vector3 targetPosition)
    {
        if (Time.time - lastMeteorTime < meteorCooldown) return false;
        
        if (validatedMeteorPrefab == null)
        {
            Debug.LogWarning("Meteor префаб не назначен или невалиден!");
            return false;
        }
        
        Vector3 spawnPosition = targetPosition + Vector3.up * 25f;
        GameObject meteor = Instantiate(validatedMeteorPrefab, spawnPosition, Quaternion.identity);
        
        MeteorProjectile meteorScript = meteor.GetComponent<MeteorProjectile>();
        if (meteorScript != null)
        {
            meteorScript.targetPosition = targetPosition;
            meteorScript.owner = gameObject;
        }
        
        lastMeteorTime = Time.time;
        return true;
    }
    
    public override void UpdateClass()
    {
        // Логика обновления класса
    }
    
    public override float GetCooldownProgress(int abilityIndex)
    {
        switch (abilityIndex)
        {
            case 0: return Mathf.Clamp01((Time.time - lastFireballTime) / fireballCooldown);
            case 1: return Mathf.Clamp01((Time.time - lastIceSpikeTime) / iceSpikeCooldown);
            case 2: return Mathf.Clamp01((Time.time - lastLightningTime) / lightningCooldown);
            case 3: return Mathf.Clamp01((Time.time - lastMeteorTime) / meteorCooldown);
            default: return 0f;
        }
    }
    
    public override bool IsAbilityReady(int abilityIndex)
    {
        switch (abilityIndex)
        {
            case 0: return Time.time - lastFireballTime >= fireballCooldown && !isAttacking;
            case 1: return Time.time - lastIceSpikeTime >= iceSpikeCooldown;
            case 2: return Time.time - lastLightningTime >= lightningCooldown;
            case 3: return Time.time - lastMeteorTime >= meteorCooldown;
            default: return false;
        }
    }
}