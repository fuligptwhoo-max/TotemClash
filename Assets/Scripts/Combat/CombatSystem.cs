using UnityEngine;
using System.Collections;
using UnityEngine.Events;

namespace TotemClash.Combat
{
    public enum AttackType { Projectile, Hitscan, Melee }
    public enum AimStrength { None, Weak, Medium, Strong, Perfect }
    
    [System.Serializable]
    public class WeaponConfig
    {
        public string weaponName = "Fireball";
        public AttackType attackType = AttackType.Projectile;
        public GameObject projectilePrefab;
        public float damage = 50f;
        public float range = 20f;
        public float cooldown = 1f;
        public float projectileSpeed = 40f;
        public bool useHoming = true;
        public float spawnOffset = 0.3f;
        public float castDelay = 0.3f;
        public bool isMelee = false;
        public float meleeArc = 120f;
        public bool usePrediction = true;
        public AimStrength aimAssist = AimStrength.Medium; // НОВОЕ: степень помощи прицеливания
        
        [Header("Effects")]
        public GameObject muzzleEffect;
        public GameObject hitEffect;
    }
    
    public class CombatSystem : MonoBehaviour
    {
        [Header("Setup")]
        public Transform castPoint;
        public Animator animator;
        public AimingSystem aiming;
        
        [Header("AI Settings")]
        public bool isPlayerControlled = true; // КРИТИЧНО: true для игрока, false для ботов
        public float aiAttackRange = 25f; // Дальность атаки бота
        public float aiAttackCooldown = 1f; // Переопределение кулдауна для бота
        
        [Header("Weapons")]
        public WeaponConfig primaryAttack;
        public WeaponConfig abilityQ;
        public WeaponConfig abilityR;
        public WeaponConfig ultimate;
        
        [Header("Events")]
        public UnityEvent<GameObject> OnHitTarget;
        public UnityEvent OnAttack;
        
        private float[] lastAttackTimes = new float[4];
        private bool isAttacking = false;
        private Transform currentTarget;
        
        void Start()
        {
            if (aiming == null)
                aiming = GetComponent<AimingSystem>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (castPoint == null)
                FindCastPoint();
                
            // Боты не используют Input
            if (!isPlayerControlled)
            {
                enabled = true; // Оставляем включенным для AI метода AttackAt
            }
        }
        
        void FindCastPoint()
        {
            castPoint = transform.Find("CastPoint");
            if (castPoint == null)
            {
                GameObject cp = new GameObject("CastPoint");
                cp.transform.SetParent(transform);
                cp.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
                castPoint = cp.transform;
            }
        }
        
        void Update()
        {
            // ТОЛЬКО игрок использует Input!
            if (!isPlayerControlled) return;
            
            if (Input.GetMouseButtonDown(0) && CanAttack(0))
                StartAttack(primaryAttack, 0);
            
            if (Input.GetKeyDown(KeyCode.Q) && CanAttack(1))
                StartAttack(abilityQ, 1);
            
            if (Input.GetKeyDown(KeyCode.R) && CanAttack(2))
                StartAttack(abilityR, 2);
            
            if (Input.GetKeyDown(KeyCode.F) && CanAttack(3))
                StartAttack(ultimate, 3);
        }
        
        bool CanAttack(int index)
        {
            return Time.time >= lastAttackTimes[index] && !isAttacking;
        }
        
        void StartAttack(WeaponConfig weapon, int index)
        {
            if (weapon == null) return;
            
            isAttacking = true;
            lastAttackTimes[index] = Time.time + weapon.cooldown;
            
            if (animator != null)
            {
                animator.SetTrigger(weapon.isMelee ? "Melee" : "Attack");
                animator.SetBool("IsAttacking", true);
            }
            
            StartCoroutine(ExecuteAttack(weapon));
            OnAttack?.Invoke();
        }
        
        IEnumerator ExecuteAttack(WeaponConfig weapon)
        {
            yield return new WaitForSeconds(weapon.castDelay);
            
            if (weapon.isMelee)
                PerformMeleeAttack(weapon);
            else if (weapon.attackType == AttackType.Projectile)
                PerformProjectileAttack(weapon);
            else if (weapon.attackType == AttackType.Hitscan)
                PerformHitscanAttack(weapon);
            
            yield return new WaitForSeconds(0.2f);
            isAttacking = false;
            if (animator != null)
                animator.SetBool("IsAttacking", false);
        }
        
        void PerformProjectileAttack(WeaponConfig weapon)
        {
            if (weapon.projectilePrefab == null) return;
            
            Vector3 spawnPos = castPoint.position + Vector3.up * weapon.spawnOffset;
            Vector3 targetPos = GetTargetPosition(weapon);
            
            Vector3 direction = (targetPos - spawnPos).normalized;
            
            if (direction.y < -0.3f)
            {
                direction.y = -0.3f;
                direction.Normalize();
            }
            
            Quaternion rotation = Quaternion.LookRotation(direction);
            GameObject proj = Instantiate(weapon.projectilePrefab, spawnPos, rotation);
            
            Projectile projectile = proj.GetComponent<Projectile>();
            if (projectile == null)
                projectile = proj.AddComponent<Projectile>();
            
            // Применяем Aim Assist (помощь прицеливания)
            Transform homingTarget = GetHomingTarget(weapon);
            
            projectile.Initialize(
                weapon.damage,
                weapon.projectileSpeed,
                weapon.range,
                homingTarget,
                gameObject,
                weapon.hitEffect
            );
            
            if (weapon.muzzleEffect != null)
                Instantiate(weapon.muzzleEffect, spawnPos, rotation);
        }
        
        Vector3 GetTargetPosition(WeaponConfig weapon)
        {
            if (aiming == null) return transform.position + transform.forward * 10f;
            
            Vector3 baseTarget = aiming.AimPoint;
            
            // Применяем Aim Assist (смещаем цель к врагу если близко)
            if (weapon.aimAssist != AimStrength.None && aiming.LockedTarget != null)
            {
                Vector3 targetPos = aiming.LockedTarget.position + Vector3.up * 1.2f;
                float assistFactor = weapon.aimAssist switch
                {
                    AimStrength.Weak => 0.2f,
                    AimStrength.Medium => 0.4f,
                    AimStrength.Strong => 0.7f,
                    AimStrength.Perfect => 1.0f,
                    _ => 0f
                };
                
                return Vector3.Lerp(baseTarget, targetPos, assistFactor);
            }
            
            return baseTarget;
        }
        
        Transform GetHomingTarget(WeaponConfig weapon)
        {
            if (!weapon.useHoming) return null;
            
            // Чем сильнее aim assist, тем вероятнее хоуминг
            if (weapon.aimAssist == AimStrength.Perfect)
                return aiming?.LockedTarget;
            else if (weapon.aimAssist == AimStrength.Strong && aiming?.LockedTarget != null)
                return Random.value > 0.3f ? aiming.LockedTarget : null; // 70% шанс
            else if (weapon.aimAssist == AimStrength.Medium && aiming?.LockedTarget != null)
                return Random.value > 0.5f ? aiming.LockedTarget : null; // 50% шанс
            
            return null;
        }
        
        void PerformHitscanAttack(WeaponConfig weapon)
        {
            Vector3 origin = castPoint.position;
            Vector3 targetPos = GetTargetPosition(weapon);
            Vector3 direction = (targetPos - origin).normalized;
            
            RaycastHit hit;
            if (Physics.Raycast(origin, direction, out hit, weapon.range))
            {
                if (weapon.hitEffect != null)
                    Instantiate(weapon.hitEffect, hit.point, Quaternion.identity);
                
                HealthSystem health = hit.collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(weapon.damage, gameObject);
                    OnHitTarget?.Invoke(hit.collider.gameObject);
                }
            }
            
            Debug.DrawLine(origin, origin + direction * weapon.range, Color.yellow, 0.5f);
        }
        
        void PerformMeleeAttack(WeaponConfig weapon)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, weapon.range);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (!hit.CompareTag("Player") && !hit.CompareTag("Enemy")) continue;
                
                Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);
                
                if (angle <= weapon.meleeArc * 0.5f)
                {
                    HealthSystem health = hit.GetComponent<HealthSystem>();
                    if (health != null)
                    {
                        health.TakeDamage(weapon.damage, gameObject);
                        OnHitTarget?.Invoke(hit.gameObject);
                    }
                }
            }
        }
        
        // Для ИИ - боты вызывают этот метод
        public void AttackAt(Vector3 position)
        {
            if (CanAttack(0) && primaryAttack != null)
            {
                currentTarget = null; // Сброс цели
                if (aiming != null)
                    aiming.SetTarget(position);
                StartAttack(primaryAttack, 0);
            }
        }
        
        // НОВЫЙ МЕТОД для агрессивных ботов - атака цели
        public void AttackTarget(Transform target)
        {
            if (CanAttack(0) && primaryAttack != null)
            {
                currentTarget = target;
                if (aiming != null && target != null)
                    aiming.SetTarget(target.position);
                StartAttack(primaryAttack, 0);
            }
        }
        
        public bool IsInAttackRange(float distance)
        {
            return distance <= (primaryAttack?.range ?? 10f);
        }
        
        public float GetCooldownPercent(int index)
        {
            float cooldown = index switch
            {
                0 => primaryAttack?.cooldown ?? 1f,
                1 => abilityQ?.cooldown ?? 1f,
                2 => abilityR?.cooldown ?? 1f,
                3 => ultimate?.cooldown ?? 1f,
                _ => 1f
            };
            
            float lastTime = lastAttackTimes[index] - cooldown;
            return Mathf.Clamp01((Time.time - lastTime) / cooldown);
        }
        
        public bool IsAttacking => isAttacking;
    }
}