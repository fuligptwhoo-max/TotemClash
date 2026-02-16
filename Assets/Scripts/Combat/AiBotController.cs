using UnityEngine;
using System.Collections;
using TotemClash.UI;

namespace TotemClash.Combat
{
    public class AIBotController : MonoBehaviour
    {
        [Header("AI Settings")]
        public float moveSpeed = 8f;
        public float rotationSpeed = 8f;
        public float attackRange = 20f;
        public float detectionRange = 25f;
        
        [Header("Strafing Settings")]
        public float strafeSpeed = 6f;
        public float strafeChangeInterval = 0.8f;
        
        [Header("Survival Settings")]
        [Range(0,1)] public float lowHealthThreshold = 0.3f;
        public int maxEnemiesNearby = 2;
        public float dangerRadius = 12f;
        
        [Header("Combat")]
        public float attackCooldown = 1.2f;
        public float minAttackDistance = 3f;
        public float preferredAttackDistance = 12f;
        public float dodgeChance = 0.6f;
        public float dodgeCooldown = 1.5f;
        [Range(0,1)] public float attackProbability = 0.7f;
        
        [Header("Target Selection")]
        public float targetSwitchDelay = 0.5f; // Задержка перед сменой цели
        public bool prioritizeTotemCarrier = true; // Приоритет тому кто несет тотем
        
        [Header("Totem Behavior")]
        public float totemPickupRange = 2.5f;
        public float fleeDistance = 18f;
        public float minFleeDistance = 8f;
        public float wallDetectionDistance = 4f;
        
        [Header("Wall Avoidance")]
        public LayerMask wallLayers;
        public float wallAvoidanceAngle = 45f;
        public float stuckCheckTime = 1f;
        public float minMoveDistance = 0.5f;
        
        [Header("References")]
        public Animator animator;
        public CharacterController characterController;
        public HealthSystem healthSystem;
        public CombatSystem combatSystem;
        public PlayerTotemInteraction totemInteraction;
        public AimingSystem aiming;
        
        public enum BotRole { Hunter, Interceptor, Defender, Berserker }
        private BotRole role;
        private bool roleAssigned = false;
        
        private enum BotState
        {
            Idle, SeekingTotem, FleeingWithTotem, ChasingTotemCarrier, 
            AttackingEnemy, Dodging, Roaming, AvoidingWall, Retreating, Survival
        }
        
        private BotState currentState = BotState.Idle;
        private Transform target;
        private HealthSystem targetHealthSystem; // ССЫЛКА НА ЗДОРОВЬЕ ЦЕЛИ
        private TotemController totem;
        private float lastAttackTime = -999f;
        private float lastDodgeTime = -999f;
        private float stateChangeTime;
        private Vector3 lastPosition;
        private float stuckTimer = 0f;
        private Vector3 wallAvoidanceDirection;
        private float totemCheckTimer = 0f;
        private Vector3 roamTarget;
        
        private float strafeTimer = 0f;
        private float currentStrafeDirection = 1f;
        private float nextStrafeChangeTime = 0f;
        private Vector3 dodgeDirection;
        
        private Transform playerTransform;
        private bool isInitialized = false;
        private bool isFrozen = false;
        private float lastHealthCheck = 100f;
        private float lastTargetSwitchTime = 0f; // Время последней смены цели
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            if (isInitialized) return;
            
            if (!roleAssigned) AssignRandomRole();
            
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            if (healthSystem == null)
                healthSystem = GetComponent<HealthSystem>();
            if (combatSystem == null)
                combatSystem = GetComponent<CombatSystem>();
            if (totemInteraction == null)
                totemInteraction = GetComponent<PlayerTotemInteraction>();
            if (aiming == null)
                aiming = GetComponent<AimingSystem>();
            
            if (combatSystem != null) 
            {
                combatSystem.isPlayerControlled = false;
                // ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ ПОПАДАНИЯ ЧТОБЫ ОТСЛЕЖИВАТЬ УБИЙСТВО
                combatSystem.OnHitTarget.AddListener(OnHitTarget);
            }
            
            if (wallLayers == 0) wallLayers = LayerMask.GetMask("Default", "Wall", "Obstacle");
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            
            FindTotem();
            SetInitialBehaviorByRole();
            
            roamTarget = transform.position;
            lastPosition = transform.position;
            if (healthSystem != null) lastHealthCheck = healthSystem.CurrentHealth;
            
            isInitialized = true;
        }
        
        private void OnDestroy()
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЙ ЧТОБЫ ИЗБЕЖАТЬ УТЕЧЕК ПАМЯТИ
            if (combatSystem != null)
            {
                combatSystem.OnHitTarget.RemoveListener(OnHitTarget);
            }
            
            // Отписываемся от события смерти текущей цели
            UnsubscribeFromTargetDeath();
        }
        
        private void AssignRandomRole()
        {
            float rand = Random.value;
            if (rand < 0.4f) role = BotRole.Hunter;
            else if (rand < 0.7f) role = BotRole.Interceptor;
            else if (rand < 0.9f) role = BotRole.Defender;
            else role = BotRole.Berserker;
            
            roleAssigned = true;
        }
        
        private void SetInitialBehaviorByRole()
        {
            switch (role)
            {
                case BotRole.Hunter: ChangeState(BotState.SeekingTotem); break;
                case BotRole.Interceptor:
                case BotRole.Berserker:
                    Transform nearest = GetNearestThreat();
                    if (nearest != null) { SetTarget(nearest); ChangeState(BotState.AttackingEnemy); }
                    else ChangeState(BotState.SeekingTotem);
                    break;
                case BotRole.Defender:
                    if (totem != null) 
                    { 
                        roamTarget = totem.transform.position + Random.insideUnitSphere * 8f; 
                        ChangeState(BotState.Roaming); 
                    }
                    else ChangeState(BotState.SeekingTotem);
                    break;
            }
        }
        
        public void Freeze(bool freeze)
        {
            isFrozen = freeze;
            if (animator != null) animator.SetFloat("Speed", 0f);
        }
        
        private void Update()
        {
            if (CountdownDisplay.IsCountdownActive || isFrozen) return;
            if (!isInitialized || healthSystem == null || healthSystem.IsDead) return;
            
            // ПРОВЕРКА ЗДОРОВЬЯ ТЕКУЩЕЙ ЦЕЛИ - ЕСЛИ МЕРТВА, ИЩЕМ НОВУЮ
            if (target != null && !IsTargetValid())
            {
                HandleTargetDeath();
            }
            
            // ПРОВЕРКА САМОСОХРАНЕНИЯ
            if (ShouldEnterSurvivalMode())
            {
                if (currentState != BotState.Survival && currentState != BotState.FleeingWithTotem)
                {
                    ChangeState(BotState.Survival);
                }
            }
            
            CheckIfStuck();
            
            // Таймеры
            strafeTimer += Time.deltaTime;
            if (strafeTimer > nextStrafeChangeTime)
            {
                currentStrafeDirection = Random.value > 0.5f ? 1f : -1f;
                nextStrafeChangeTime = strafeChangeInterval + Random.Range(-0.2f, 0.2f);
                strafeTimer = 0f;
            }
            
            totemCheckTimer += Time.deltaTime;
            if (totemCheckTimer > 0.5f) { FindTotem(); totemCheckTimer = 0f; }
            
            // Стены - проверяем всегда
            if (currentState != BotState.AvoidingWall && IsWallAhead())
            {
                StartSmartWallAvoidance();
            }
            
            // === ПРИОРИТЕТЫ С УЧЕТОМ ВЫЖИВАНИЯ ===
            
            // 1. Режим выживания
            if (currentState == BotState.Survival)
            {
                UpdateSurvivalMode();
                UpdateAnimations();
                return;
            }
            
            // 2. Если несу тотем - УМНОЕ бегство
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                UpdateSmartFleeing();
                UpdateAnimations();
                return;
            }
            
            // 3. Если враг несет тотем - преследуем только если безопасно
            GameObject carrier = GetTotemCarrier();
            if (carrier != null && carrier != gameObject && !IsTooDangerous())
            {
                if (target != carrier.transform) SetTarget(carrier.transform);
                UpdateChasingTotemCarrier();
                UpdateAnimations();
                return;
            }
            else if (carrier != null && IsTooDangerous())
            {
                if (Random.value < 0.5f) ChangeState(BotState.Survival);
            }
            
            // 4. Если тотем свободен - идем только если безопасно
            if (totem != null && !totem.IsBeingCarried())
            {
                if (!IsTooDangerous() && healthSystem.GetHealthPercent() > lowHealthThreshold)
                {
                    UpdateSeekingTotem();
                }
                else
                {
                    UpdateSurvivalMode();
                }
                UpdateAnimations();
                return;
            }
            
            // Обычное поведение по состояниям
            switch (currentState)
            {
                case BotState.Idle: UpdateIdle(); break;
                case BotState.SeekingTotem: UpdateSeekingTotem(); break;
                case BotState.ChasingTotemCarrier: UpdateChasingTotemCarrier(); break;
                case BotState.AttackingEnemy: UpdateAttackingEnemy(); break;
                case BotState.Retreating: UpdateRetreating(); break;
                case BotState.Dodging: UpdateDodging(); break;
                case BotState.Roaming: UpdateRoaming(); break;
                case BotState.AvoidingWall: UpdateAvoidingWall(); break;
            }
            
            UpdateAnimations();
        }
        
        // === НОВЫЕ МЕТОДЫ ДЛЯ УПРАВЛЕНИЯ ЦЕЛЯМИ ===
        
        /// <summary>
        /// Устанавливает новую цель с отслеживанием её здоровья
        /// </summary>
        private void SetTarget(Transform newTarget)
        {
            if (newTarget == target) return;
            
            // Отписываемся от старой цели
            UnsubscribeFromTargetDeath();
            
            target = newTarget;
            lastTargetSwitchTime = Time.time;
            
            // Подписываемся на событие смерти новой цели
            if (target != null)
            {
                targetHealthSystem = target.GetComponent<HealthSystem>();
                if (targetHealthSystem != null)
                {
                    targetHealthSystem.OnDeath.AddListener(OnTargetDied);
                }
            }
        }
        
        /// <summary>
        /// Отписывается от события смерти текущей цели
        /// </summary>
        private void UnsubscribeFromTargetDeath()
        {
            if (targetHealthSystem != null)
            {
                targetHealthSystem.OnDeath.RemoveListener(OnTargetDied);
                targetHealthSystem = null;
            }
        }
        
        /// <summary>
        /// Проверяет валидна ли цель (существует и жива)
        /// </summary>
        private bool IsTargetValid()
        {
            if (target == null) return false;
            
            // Проверяем что объект активен
            if (!target.gameObject.activeInHierarchy) return false;
            
            // Проверяем здоровье
            if (targetHealthSystem != null && targetHealthSystem.IsDead) return false;
            
            // Дополнительная проверка - получаем HealthSystem если не был получен ранее
            if (targetHealthSystem == null)
            {
                targetHealthSystem = target.GetComponent<HealthSystem>();
                if (targetHealthSystem != null && targetHealthSystem.IsDead) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Обработчик смерти цели (вызывается через событие)
        /// </summary>
        private void OnTargetDied()
        {
            Debug.Log($"[AIBotController] {gameObject.name}: Target {target?.name} died!");
            HandleTargetDeath();
        }
        
        /// <summary>
        /// Обработка смерти цели - поиск новой цели или смена состояния
        /// </summary>
        private void HandleTargetDeath()
        {
            UnsubscribeFromTargetDeath();
            target = null;
            targetHealthSystem = null;
            
            // Ищем новую цель с задержкой чтобы не мгновенно переключаться
            StartCoroutine(FindNewTargetAfterDelay(targetSwitchDelay));
        }
        
        /// <summary>
        /// Корутина для поиска новой цели с задержкой
        /// </summary>
        private IEnumerator FindNewTargetAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (this == null || !enabled) yield break;
            
            // Приоритет: тотем -> ближайший враг -> случайное блуждание
            GameObject carrier = GetTotemCarrier();
            if (carrier != null && carrier != gameObject && !IsTooDangerous())
            {
                SetTarget(carrier.transform);
                ChangeState(BotState.ChasingTotemCarrier);
                yield break;
            }
            
            Transform nearest = GetNearestThreat();
            if (nearest != null)
            {
                SetTarget(nearest);
                ChangeState(BotState.AttackingEnemy);
                Debug.Log($"[AIBotController] {gameObject.name}: Switched to new target {nearest.name}");
            }
            else
            {
                // Нет врагов - ищем тотем
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        /// <summary>
        /// Обработчик попадания (для проверки убийства через CombatSystem)
        /// </summary>
        private void OnHitTarget(GameObject hitObject)
        {
            // Проверяем убили ли мы текущую цель
            if (hitObject != null && target != null && hitObject == target.gameObject)
            {
                HealthSystem hitHealth = hitObject.GetComponent<HealthSystem>();
                if (hitHealth != null && hitHealth.IsDead)
                {
                    // Цель убита нами, обрабатываем смерть цели
                    OnTargetDied();
                }
            }
        }
        
        // === ОСТАЛЬНЫЕ МЕТОДЫ (без изменений в логике, но с использованием SetTarget) ===
        
        private bool ShouldEnterSurvivalMode()
        {
            if (healthSystem == null) return false;
            
            float healthPercent = healthSystem.GetHealthPercent();
            
            if (healthPercent < lowHealthThreshold) return true;
            
            if (lastHealthCheck - healthSystem.CurrentHealth > 30f) 
            {
                lastHealthCheck = healthSystem.CurrentHealth;
                return true;
            }
            lastHealthCheck = healthSystem.CurrentHealth;
            
            if (IsTooDangerous()) return true;
            
            return false;
        }
        
        private bool IsTooDangerous()
        {
            int enemiesNearby = 0;
            
            if (playerTransform != null && 
                Vector3.Distance(transform.position, playerTransform.position) < dangerRadius)
            {
                // Проверяем что игрок жив
                var playerHealth = playerTransform.GetComponent<HealthSystem>();
                if (playerHealth == null || !playerHealth.IsDead)
                    enemiesNearby++;
            }
            
            AIBotController[] bots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot == this || bot.gameObject == gameObject) continue;
                if (Vector3.Distance(transform.position, bot.transform.position) < dangerRadius)
                {
                    // Проверяем что бот жив
                    if (bot.healthSystem == null || !bot.healthSystem.IsDead)
                        enemiesNearby++;
                }
            }
            
            return enemiesNearby >= maxEnemiesNearby;
        }
        
        private void UpdateSurvivalMode()
        {
            Transform nearestThreat = GetNearestThreat();
            
            if (nearestThreat == null)
            {
                if (healthSystem.GetHealthPercent() > 0.5f)
                {
                    ChangeState(BotState.SeekingTotem);
                }
                else
                {
                    RoamAroundSafe();
                }
                return;
            }
            
            float distance = Vector3.Distance(transform.position, nearestThreat.position);
            
            Vector3 fleeDirection = (transform.position - nearestThreat.position).normalized;
            fleeDirection = FindBestEscapeDirection(fleeDirection);
            
            if (distance <= attackRange && CanAttack() && Random.value < 0.8f)
            {
                FaceTarget(nearestThreat.position);
                TryAttack(nearestThreat.position);
            }
            
            Vector3 strafeDir = Vector3.Cross(Vector3.up, fleeDirection) * currentStrafeDirection;
            Vector3 finalDir = (fleeDirection * 2f + strafeDir * 0.5f).normalized;
            
            MoveInDirection(finalDir, moveSpeed * 1.2f);
            
            if (distance < 10f && Random.value < dodgeChance * Time.deltaTime * 15f)
            {
                dodgeDirection = strafeDir;
                StartDodge();
            }
            
            if (distance > fleeDistance && healthSystem.GetHealthPercent() > 0.4f)
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateSmartFleeing()
        {
            if (totemInteraction == null || !totemInteraction.IsCarrying)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            Transform nearestThreat = GetNearestThreat();
            
            if (nearestThreat == null)
            {
                MoveInDirection(transform.forward);
                return;
            }
            
            float distanceToThreat = Vector3.Distance(transform.position, nearestThreat.position);
            Vector3 awayFromEnemy = (transform.position - nearestThreat.position).normalized;
            Vector3 bestDirection = FindBestEscapeDirection(awayFromEnemy);
            
            if (Vector3.Dot(bestDirection, awayFromEnemy) < 0.3f)
            {
                bestDirection = Vector3.Cross(Vector3.up, awayFromEnemy) * (Random.value > 0.5f ? 1f : -1f);
                bestDirection = FindBestEscapeDirection(bestDirection);
            }
            
            MoveInDirection(bestDirection, moveSpeed * 1.1f);
            
            if (distanceToThreat <= attackRange && distanceToThreat > minAttackDistance)
            {
                if (CanAttack() && Random.value < 0.7f)
                {
                    FaceTarget(nearestThreat.position);
                    TryAttack(nearestThreat.position);
                }
            }
            
            if (distanceToThreat < 12f && Random.value < dodgeChance * Time.deltaTime * 10f)
            {
                Vector3 strafeDir = Vector3.Cross(Vector3.up, bestDirection);
                dodgeDirection = strafeDir;
                StartDodge();
            }
        }
        
        private Vector3 FindBestEscapeDirection(Vector3 preferredDirection)
        {
            Vector3[] directions = new Vector3[]
            {
                preferredDirection,
                Quaternion.Euler(0, 30, 0) * preferredDirection,
                Quaternion.Euler(0, -30, 0) * preferredDirection,
                Quaternion.Euler(0, 60, 0) * preferredDirection,
                Quaternion.Euler(0, -60, 0) * preferredDirection,
                Quaternion.Euler(0, 90, 0) * preferredDirection,
                Quaternion.Euler(0, -90, 0) * preferredDirection,
                Quaternion.Euler(0, 135, 0) * preferredDirection,
                Quaternion.Euler(0, -135, 0) * preferredDirection,
                -preferredDirection
            };
            
            Vector3 bestDir = preferredDirection;
            float maxDistance = 0f;
            
            foreach (var dir in directions)
            {
                RaycastHit hit;
                float dist = wallDetectionDistance * 2f;
                
                if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, out hit, wallDetectionDistance * 2f, wallLayers))
                {
                    dist = hit.distance;
                }
                
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    bestDir = dir;
                }
            }
            
            return bestDir.normalized;
        }
        
        private void StartSmartWallAvoidance()
        {
            Vector3 forward = transform.forward;
            Vector3 bestDir = FindBestEscapeDirection(forward);
            
            wallAvoidanceDirection = bestDir;
            ChangeState(BotState.AvoidingWall);
        }
        
        private void UpdateSeekingTotem()
        {
            if (totem == null || totem.IsBeingCarried())
            {
                ChangeState(BotState.Roaming);
                return;
            }
            
            if (IsTooDangerous())
            {
                ChangeState(BotState.Survival);
                return;
            }
            
            Vector3 directionToTotem = (totem.transform.position - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, totem.transform.position);
            
            if (IsWallAheadTowards(directionToTotem))
            {
                Vector3 alternative = FindBestEscapeDirection(directionToTotem);
                MoveInDirection(alternative);
            }
            else
            {
                if (dist < 8f && dist > totemPickupRange)
                {
                    Vector3 strafeDir = Vector3.Cross(Vector3.up, directionToTotem) * currentStrafeDirection;
                    directionToTotem = (directionToTotem + strafeDir * 0.3f).normalized;
                }
                
                MoveInDirection(directionToTotem);
            }
            
            if (dist <= totemPickupRange)
            {
                if (totemInteraction != null)
                {
                    totemInteraction.TryPickUp();
                    if (totemInteraction.IsCarrying)
                    {
                        Debug.Log($"{gameObject.name} picked up totem! Now fleeing...");
                    }
                }
            }
        }
        
        private void UpdateChasingTotemCarrier()
        {
            if (totemInteraction != null && totemInteraction.IsCarrying) return;
            
            // ПРОВЕРКА ВАЛИДНОСТИ ЦЕЛИ
            if (!IsTargetValid()) 
            { 
                HandleTargetDeath();
                return; 
            }
            
            // Если стало опасно - бросаем погоню
            if (IsTooDangerous() || healthSystem.GetHealthPercent() < lowHealthThreshold)
            {
                ChangeState(BotState.Survival);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            Vector3 directionToTarget = (target.position - transform.position).normalized;
            
            if (distanceToTarget < attackRange)
            {
                Vector3 strafeDir = Vector3.Cross(Vector3.up, directionToTarget) * currentStrafeDirection;
                Vector3 circleDir = (directionToTarget * 0.4f + strafeDir * 0.6f).normalized;
                
                if (!IsWallAheadTowards(circleDir))
                    MoveInDirection(circleDir);
                else
                    MoveInDirection(strafeDir);
                
                if (CanAttack() && Random.value < 0.8f)
                {
                    FaceTarget(target.position);
                    TryAttack(target.position);
                }
            }
            else
            {
                if (!IsWallAheadTowards(directionToTarget))
                    MoveInDirection(directionToTarget);
                else
                    MoveInDirection(FindBestEscapeDirection(directionToTarget));
            }
        }
        
        private void UpdateAttackingEnemy()
        {
            if (healthSystem.GetHealthPercent() < lowHealthThreshold && !totemInteraction.IsCarrying)
            {
                ChangeState(BotState.Survival);
                return;
            }
            
            // ПРОВЕРКА ВАЛИДНОСТИ ЦЕЛИ
            if (!IsTargetValid()) 
            { 
                HandleTargetDeath();
                return; 
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (aiming != null) aiming.SetLockedTarget(target);
            
            if (distanceToTarget > attackRange)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                MoveInDirection(dir);
            }
            else if (distanceToTarget < minAttackDistance)
            {
                Vector3 retreatDir = (transform.position - target.position).normalized;
                Vector3 strafeDir = Vector3.Cross(Vector3.up, retreatDir) * currentStrafeDirection;
                MoveInDirection((retreatDir + strafeDir * 0.5f).normalized);
                
                if (CanAttack()) TryAttack(target.position);
            }
            else
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                Vector3 strafeDir = Vector3.Cross(Vector3.up, toTarget) * currentStrafeDirection;
                MoveInDirection(strafeDir * 0.8f + toTarget * 0.2f);
                
                FaceTarget(target.position);
                
                if (CanAttack() && Random.value < attackProbability)
                    TryAttack(target.position);
            }
            
            if (Time.time - stateChangeTime > 6f)
                ChangeState(BotState.SeekingTotem);
        }
        
        private void UpdateRetreating()
        {
            // ПРОВЕРКА ВАЛИДНОСТИ ЦЕЛИ
            if (!IsTargetValid()) 
            { 
                HandleTargetDeath();
                return; 
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget < preferredAttackDistance)
            {
                Vector3 retreatDir = (transform.position - target.position).normalized;
                retreatDir = FindBestEscapeDirection(retreatDir);
                Vector3 strafeDir = Vector3.Cross(Vector3.up, retreatDir) * currentStrafeDirection;
                MoveInDirection((retreatDir + strafeDir * 0.4f).normalized);
                
                if (CanAttack() && Random.value < 0.4f)
                {
                    FaceTarget(target.position);
                    TryAttack(target.position);
                }
            }
            else
            {
                ChangeState(BotState.AttackingEnemy);
            }
        }
        
        private void UpdateDodging()
        {
            if (Time.time - lastDodgeTime > 0.4f)
            {
                if (healthSystem.GetHealthPercent() < lowHealthThreshold)
                    ChangeState(BotState.Survival);
                else
                    ChangeState(BotState.SeekingTotem);
                return;
            }
            
            if (characterController != null)
            {
                Vector3 movement = dodgeDirection * moveSpeed * 2f * Time.deltaTime;
                movement.y = -9.81f * Time.deltaTime;
                characterController.Move(movement);
            }
        }
        
        private void UpdateRoaming()
        {
            if (totem != null && !totem.IsBeingCarried() && !IsTooDangerous() && healthSystem.GetHealthPercent() > lowHealthThreshold)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            float distanceToRoam = Vector3.Distance(transform.position, roamTarget);
            if (distanceToRoam < 2f || IsWallAheadTowards((roamTarget - transform.position).normalized))
            {
                roamTarget = GetRandomRoamPoint();
            }
            
            Vector3 direction = (roamTarget - transform.position).normalized;
            MoveInDirection(direction);
        }
        
        private void UpdateAvoidingWall()
        {
            MoveInDirection(wallAvoidanceDirection, moveSpeed * 0.9f);
            
            if (!IsWallAhead())
            {
                ChangeState(BotState.SeekingTotem);
            }
            else
            {
                wallAvoidanceDirection = FindBestEscapeDirection(transform.forward);
            }
        }
        
        private void UpdateIdle()
        {
            Transform threat = GetNearestThreat();
            if (threat != null && Vector3.Distance(transform.position, threat.position) < detectionRange * 0.5f)
            {
                SetTarget(threat);
                ChangeState(BotState.AttackingEnemy);
                return;
            }
            
            if (Time.time - stateChangeTime > 1f)
                ChangeState(BotState.SeekingTotem);
        }
        
        private bool IsWallAheadTowards(Vector3 direction)
        {
            return Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, wallDetectionDistance, wallLayers);
        }
        
        private bool IsWallAhead()
        {
            return IsWallAheadTowards(transform.forward);
        }
        
        private void MoveInDirection(Vector3 direction, float? customSpeed = null)
        {
            if (characterController == null) return;
            direction.y = 0;
            direction.Normalize();
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                float speed = customSpeed ?? moveSpeed;
                Vector3 movement = transform.forward * speed * Time.deltaTime;
                movement.y = -9.81f * Time.deltaTime;
                characterController.Move(movement);
            }
        }
        
        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
            }
        }
        
        private void CheckIfStuck()
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            if (distanceMoved < minMoveDistance)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > stuckCheckTime)
                {
                    Vector3 escapeDir = FindBestEscapeDirection(-transform.forward);
                    wallAvoidanceDirection = escapeDir;
                    ChangeState(BotState.AvoidingWall);
                    stuckTimer = 0f;
                }
            }
            else stuckTimer = 0f;
            
            lastPosition = transform.position;
        }
        
        private void StartDodge()
        {
            if (Time.time - lastDodgeTime < dodgeCooldown) return;
            lastDodgeTime = Time.time;
            
            if (target != null && IsTargetValid())
            {
                Vector3 toTarget = (target.position - transform.position).normalized;
                dodgeDirection = Vector3.Cross(Vector3.up, toTarget) * (Random.value > 0.5f ? 1f : -1f);
            }
            else dodgeDirection = transform.right * (Random.value > 0.5f ? 1f : -1f);
            
            ChangeState(BotState.Dodging);
        }
        
        private void ChangeState(BotState newState)
        {
            if (currentState == newState) return;
            currentState = newState;
            stateChangeTime = Time.time;
            if (newState == BotState.Roaming) roamTarget = GetRandomRoamPoint();
        }
        
        private Vector3 GetRandomRoamPoint()
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector3 randomDir = Random.insideUnitSphere;
                randomDir.y = 0;
                randomDir.Normalize();
                
                float dist = Random.Range(5f, 15f);
                Vector3 potential = transform.position + randomDir * dist;
                
                if (!Physics.CheckSphere(potential, 1f, wallLayers))
                {
                    if (Physics.Raycast(potential + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                        return hit.point;
                }
            }
            return transform.position + Random.insideUnitSphere * 5f;
        }
        
        private void RoamAroundSafe()
        {
            float distanceToRoam = Vector3.Distance(transform.position, roamTarget);
            if (distanceToRoam < 2f || IsWallAheadTowards((roamTarget - transform.position).normalized))
                roamTarget = GetRandomRoamPoint();
            
            Vector3 direction = (roamTarget - transform.position).normalized;
            MoveInDirection(direction);
        }
        
        private void FindTotem()
        {
            totem = FindFirstObjectByType<TotemController>();
        }
        
        private GameObject GetTotemCarrier()
        {
            if (totem == null) return null;
            return totem.GetCarrier();
        }
        
        private Transform GetNearestThreat()
        {
            Transform nearest = null;
            float nearestDist = float.MaxValue;
            
            // Проверяем игрока
            if (playerTransform != null)
            {
                var playerHealth = playerTransform.GetComponent<HealthSystem>();
                if (playerHealth == null || !playerHealth.IsDead)
                {
                    float d = Vector3.Distance(transform.position, playerTransform.position);
                    if (d < nearestDist && d < detectionRange)
                    {
                        nearestDist = d;
                        nearest = playerTransform;
                    }
                }
            }
            
            // Проверяем других ботов
            AIBotController[] bots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot == this || bot.gameObject == gameObject) continue;
                
                // ПРОВЕРКА ЧТО БОТ ЖИВ
                if (bot.healthSystem != null && bot.healthSystem.IsDead) continue;
                
                float d = Vector3.Distance(transform.position, bot.transform.position);
                if (d < nearestDist && d < detectionRange)
                {
                    nearestDist = d;
                    nearest = bot.transform;
                }
            }
            
            return nearest;
        }
        
        private bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown;
        }
        
        private void TryAttack(Vector3 targetPos)
        {
            if (combatSystem == null || !CanAttack()) return;
            lastAttackTime = Time.time;
            combatSystem.AttackAt(targetPos);
            
            if (Random.value < dodgeChance * 0.3f)
                StartDodge();
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            float speed = 0f;
            if (characterController != null && !isFrozen)
                speed = characterController.velocity.magnitude / moveSpeed;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsCarrying", totemInteraction != null && totemInteraction.IsCarrying);
        }
        
        public void OnHitByFireball()
        {
            if (Random.value < 0.8f) StartDodge();
        }
        
        public void OnFireballIncoming(Vector3 fireballPos, Vector3 fireballDir)
        {
            float dist = Vector3.Distance(transform.position, fireballPos);
            if (dist < 12f && Random.value < dodgeChance)
            {
                dodgeDirection = Vector3.Cross(fireballDir, Vector3.up) * (Random.value > 0.5f ? 1f : -1f);
                lastDodgeTime = Time.time;
                ChangeState(BotState.Dodging);
            }
        }
    }
}