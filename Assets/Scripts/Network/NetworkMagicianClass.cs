using UnityEngine;
using Mirror;
using System.Collections;

public class NetworkMagicianClass : PlayerClass
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
    
    private float lastFireballTime = -100f;
    private bool isAttacking = false;
    
    private NetworkPlayerController networkPlayerController;
    
    public override void Initialize(PlayerController controller, PlayerCombat combat, Animator anim)
    {
        base.Initialize(controller, combat, anim);
        
        networkPlayerController = controller.GetComponent<NetworkPlayerController>();
        
        Debug.Log($"NetworkMagicianClass инициализирован для {gameObject.name}");
        
        CreateCastPointIfNeeded();
    }
    
    private void CreateCastPointIfNeeded()
    {
        if (castPoint == null)
        {
            GameObject castPointObj = new GameObject("CastPoint");
            castPointObj.transform.SetParent(transform);
            castPointObj.transform.localPosition = new Vector3(0, 1.5f, 2f);
            castPoint = castPointObj.transform;
        }
    }
    
    public override bool PrimaryAttack(Vector3 targetPosition)
    {
        if (Time.time - lastFireballTime < fireballCooldown || isAttacking)
        {
            return false;
        }
        
        // Только сервер создает фаербол
        if (NetworkServer.active)
        {
            StartCoroutine(PerformAttack(targetPosition));
        }
        
        lastFireballTime = Time.time;
        return true;
    }
    
    private IEnumerator PerformAttack(Vector3 targetPosition)
    {
        isAttacking = true;
        
        yield return new WaitForSeconds(attackAnimationDelay);
        
        Vector3 direction = CalculateDirection(targetPosition);
        SpawnFireball(direction);
        
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
    
    [Server]
    private void SpawnFireball(Vector3 direction)
    {
        if (fireballPrefab == null)
        {
            Debug.LogError("Нет префаба фаербола!");
            return;
        }
        
        Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(fireballSpawnRotation);
        Vector3 spawnPosition = castPoint.position + direction * spawnDistanceFromPlayer;
        
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, rotation);
        
        // Инициализируем фаербол
        NetworkFireballProjectile projectile = fireball.GetComponent<NetworkFireballProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(direction, fireballSpeed, gameObject);
        }
        
        // Спавним на всех клиентах
        NetworkServer.Spawn(fireball);
        
        Debug.Log($"Сервер: выпущен фаербол от {gameObject.name}");
    }
    
    // Остальные способности можно адаптировать аналогично
    public override bool Ability1(Vector3 targetPosition) { return false; }
    public override bool Ability2(Vector3 targetPosition) { return false; }
    public override bool UltimateAbility(Vector3 targetPosition) { return false; }
    
    public override void UpdateClass() { }
    
    public override float GetCooldownProgress(int abilityIndex)
    {
        if (abilityIndex == 0)
            return Mathf.Clamp01((Time.time - lastFireballTime) / fireballCooldown);
        return 0f;
    }
    
    public override bool IsAbilityReady(int abilityIndex)
    {
        if (abilityIndex == 0)
            return Time.time - lastFireballTime >= fireballCooldown && !isAttacking;
        return false;
    }
}