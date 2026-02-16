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
        public float totemPriorityRange = 50f;
        
        [Header("Combat")]
        public float attackCooldown = 1.2f;
        public float minAttackDistance = 3f;
        public float preferredAttackDistance = 12f;
        public float dodgeChance = 0.4f;
        public float dodgeCooldown = 2f;
        [Range(0,1)] public float attackProbability = 0.6f;
        
        [Header("Totem Behavior")]
        public float totemPickupRange = 2.5f;
        public float fleeDistance = 20f;
        public float minFleeDistance = 8f;
        public float aggressionRange = 10f;
        
        [Header("Wall Avoidance")]
        public LayerMask wallLayers;
        public float wallDetectionDistance = 3f;
        public float wallAvoidanceAngle = 45f;
        public float stuckCheckTime = 2f;
        public float minMoveDistance = 0.5f;
        
        [Header("References")]
        public Animator animator;
        public CharacterController characterController;
        public HealthSystem healthSystem;
        public CombatSystem combatSystem;
        public PlayerTotemInteraction totemInteraction;
        public AimingSystem aiming;
        
        private enum BotState
        {
            Idle, SeekingTotem, FleeingWithTotem, ChasingTotemCarrier, 
            AttackingEnemy, Dodging, Roaming, AvoidingWall, Retreating
        }
        
        private BotState currentState = BotState.Idle;
        private Transform target;
        private TotemController totem;
        private float lastAttackTime = -999f;
        private float lastDodgeTime = -999f;
        private float stateChangeTime;
        private Vector3 lastKnownTotemPosition;
        private Vector3 dodgeDirection;
        private Vector3 roamTarget;
        private Vector3 lastPosition;
        private float stuckTimer = 0f;
        private Vector3 wallAvoidanceDirection;
        private float totemCheckTimer = 0f;
        private float randomAttackTimer = 0f;
        
        private Transform playerTransform;
        private bool isInitialized = false;
        private bool isFrozen = false;
        
        private void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            if (isInitialized) return;
            
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
            }
            
            if (wallLayers == 0)
            {
                wallLayers = LayerMask.GetMask("Default", "Wall", "Obstacle");
            }
            
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            
            FindTotem();
            ChangeState(BotState.SeekingTotem);
            
            roamTarget = transform.position;
            lastPosition = transform.position;
            
            isInitialized = true;
        }
        
        public void Freeze(bool freeze)
        {
            isFrozen = freeze;
            
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
            }
        }
        
        private void Update()
        {
            if (CountdownDisplay.IsCountdownActive || isFrozen)
            {
                if (animator != null)
                {
                    animator.SetFloat("Speed", 0f);
                }
                return;
            }
            
            if (!isInitialized || healthSystem == null || healthSystem.IsDead)
                return;
            
            CheckIfStuck();
            
            totemCheckTimer += Time.deltaTime;
            if (totemCheckTimer > 0.5f)
            {
                FindTotem();
                totemCheckTimer = 0f;
            }
            
            if (currentState != BotState.AvoidingWall && IsWallAhead())
            {
                StartWallAvoidance();
            }
            
            // === ЛОГИКА ПРИОРИТЕТОВ ===
            
            // 1. Если несу тотем - бежать И стрелять на ходу + уворачиваться
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                // УВОРАЧИВАЕМСЯ ВСЕГДА (с тотемом)
                CheckAndDodge();
                
                // Стреляем на ходу, не останавливаясь
                ShootWhileMoving();
                
                if (currentState != BotState.FleeingWithTotem)
                {
                    ChangeState(BotState.FleeingWithTotem);
                }
                UpdateFleeingWithTotem();
                UpdateAnimations();
                return;
            }
            
            // 2. Если враг несет тотем - преследовать
            GameObject carrier = GetTotemCarrier();
            if (carrier != null && carrier != gameObject)
            {
                // УВОРАЧИВАЕМСЯ ВСЕГДА (без тотема)
                CheckAndDodge();
                
                if (target != carrier.transform || currentState != BotState.ChasingTotemCarrier)
                {
                    target = carrier.transform;
                    ChangeState(BotState.ChasingTotemCarrier);
                }
                UpdateChasingTotemCarrier();
                UpdateAnimations();
                return;
            }
            
            // 3. Если тотем свободен - идти к нему
            if (totem != null && !totem.IsBeingCarried())
            {
                CheckAndDodge(); // Уворот даже при беге к тотему
                
                if (currentState != BotState.SeekingTotem)
                {
                    ChangeState(BotState.SeekingTotem);
                }
                UpdateSeekingTotem();
                UpdateAnimations();
                return;
            }
            
            // 4. Случайные атаки (реже)
            randomAttackTimer += Time.deltaTime;
            if (randomAttackTimer > 2f)
            {
                Transform nearestEnemy = GetNearestThreat();
                if (nearestEnemy != null)
                {
                    float dist = Vector3.Distance(transform.position, nearestEnemy.position);
                    if (dist <= detectionRange && dist > minAttackDistance)
                    {
                        if (Random.value < 0.2f)
                        {
                            target = nearestEnemy;
                            ChangeState(BotState.AttackingEnemy);
                        }
                    }
                }
                randomAttackTimer = 0f;
            }
            
            // УВОРАЧИВАЕМСЯ ВСЕГДА (в любом состоянии)
            CheckAndDodge();
            
            // Выполняем текущее состояние
            switch (currentState)
            {
                case BotState.Idle:
                    UpdateIdle();
                    break;
                case BotState.SeekingTotem:
                    UpdateSeekingTotem();
                    break;
                case BotState.ChasingTotemCarrier:
                    UpdateChasingTotemCarrier();
                    break;
                case BotState.AttackingEnemy:
                    UpdateAttackingEnemy();
                    break;
                case BotState.Retreating:
                    UpdateRetreating();
                    break;
                case BotState.Dodging:
                    UpdateDodging();
                    break;
                case BotState.Roaming:
                    UpdateRoaming();
                    break;
                case BotState.AvoidingWall:
                    UpdateAvoidingWall();
                    break;
                case BotState.FleeingWithTotem:
                    UpdateFleeingWithTotem();
                    break;
            }
            
            UpdateAnimations();
        }
        
        // НОВЫЙ МЕТОД: Стрельба на ходу (без остановки)
        private void ShootWhileMoving()
        {
            Transform nearestThreat = GetNearestThreat();
            if (nearestThreat == null) return;
            
            float dist = Vector3.Distance(transform.position, nearestThreat.position);
            
            // Стреляем если враг в зоне досягаемости и не слишком близко
            if (dist <= attackRange && dist > minAttackDistance && CanAttack())
            {
                if (Random.value < attackProbability)
                {
                    // Плавный поворот на врага БЕЗ остановки движения
                    Vector3 directionToTarget = (nearestThreat.position - transform.position).normalized;
                    directionToTarget.y = 0;
                    
                    // Плавно поворачиваемся лицом к врагу, но продолжаем двигаться
                    if (directionToTarget.magnitude > 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                    }
                    
                    // Стреляем без остановки
                    TryAttack(nearestThreat.position);
                }
            }
        }
        
        // НОВЫЙ МЕТОД: Проверка и выполнение уворота
        private void CheckAndDodge()
        {
            if (Time.time - lastDodgeTime < dodgeCooldown) return;
            
            // Проверяем incoming projectiles (если есть система определения)
            // Или уворачиваемся случайно при бое
            if (target != null && Random.value < dodgeChance * 0.5f) // 50% от базового шанса
            {
                StartDodge();
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
                    roamTarget = GetRandomRoamPoint();
                    ChangeState(BotState.Roaming);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            
            lastPosition = transform.position;
        }
        
        private bool IsWallAhead()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            for (int i = -1; i <= 1; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 20f, 0) * forward;
                if (Physics.Raycast(origin, direction, wallDetectionDistance, wallLayers))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void StartWallAvoidance()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            for (int i = 1; i <= 4; i++)
            {
                Vector3 rightDir = Quaternion.Euler(0, i * wallAvoidanceAngle, 0) * forward;
                if (!Physics.Raycast(origin, rightDir, wallDetectionDistance, wallLayers))
                {
                    wallAvoidanceDirection = rightDir;
                    ChangeState(BotState.AvoidingWall);
                    return;
                }
                
                Vector3 leftDir = Quaternion.Euler(0, -i * wallAvoidanceAngle, 0) * forward;
                if (!Physics.Raycast(origin, leftDir, wallDetectionDistance, wallLayers))
                {
                    wallAvoidanceDirection = leftDir;
                    ChangeState(BotState.AvoidingWall);
                    return;
                }
            }
            
            wallAvoidanceDirection = -forward;
            ChangeState(BotState.AvoidingWall);
        }
        
        private void UpdateAvoidingWall()
        {
            MoveInDirection(wallAvoidanceDirection);
            
            if (!IsWallAhead())
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void MoveInDirection(Vector3 direction)
        {
            if (characterController == null) return;
            
            direction.y = 0;
            direction.Normalize();
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
                movement.y = -9.81f * Time.deltaTime;
                characterController.Move(movement);
            }
        }
        
        private void UpdateIdle()
        {
            if (Time.time - stateChangeTime > 0.5f)
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateSeekingTotem()
        {
            if (totem == null || totem.IsBeingCarried())
            {
                ChangeState(BotState.Roaming);
                return;
            }
            
            Vector3 directionToTotem = (totem.transform.position - transform.position).normalized;
            MoveInDirection(directionToTotem);
            
            float distanceToTotem = Vector3.Distance(transform.position, totem.transform.position);
            if (distanceToTotem <= totemPickupRange)
            {
                if (totemInteraction != null)
                {
                    totemInteraction.TryPickUp();
                    if (totemInteraction.IsCarrying)
                    {
                        ChangeState(BotState.FleeingWithTotem);
                    }
                }
            }
        }
        
        private void UpdateFleeingWithTotem()
        {
            if (totemInteraction == null || !totemInteraction.IsCarrying)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            Transform nearestThreat = GetNearestThreat();
            
            if (nearestThreat != null)
            {
                float distanceToThreat = Vector3.Distance(transform.position, nearestThreat.position);
                
                if (distanceToThreat < fleeDistance)
                {
                    // Бежим от угрозы
                    Vector3 fleeDirection = (transform.position - nearestThreat.position).normalized;
                    
                    Vector3 origin = transform.position + Vector3.up * 0.5f;
                    if (Physics.Raycast(origin, fleeDirection, wallDetectionDistance, wallLayers))
                    {
                        StartWallAvoidance();
                        return;
                    }
                    
                    // ДВИЖЕНИЕ без остановки - никаких FaceTarget здесь!
                    MoveInDirection(fleeDirection);
                }
                else
                {
                    RoamAroundSafe();
                }
            }
            else
            {
                RoamAroundSafe();
            }
        }
        
        private void UpdateChasingTotemCarrier()
        {
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                ChangeState(BotState.FleeingWithTotem);
                return;
            }
            
            if (totem != null && !totem.IsBeingCarried())
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            if (target != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);
                
                if (distanceToTarget > attackRange)
                {
                    Vector3 directionToTarget = (target.position - transform.position).normalized;
                    MoveInDirection(directionToTarget);
                }
                else
                {
                    // Близко - атакуем на ходу, не останавливаясь
                    if (CanAttack() && Random.value < attackProbability)
                    {
                        TryAttack(target.position);
                    }
                    // Продолжаем двигаться к цели даже вблизи
                    FaceTarget(target.position);
                    Vector3 moveDir = (target.position - transform.position).normalized;
                    MoveInDirection(moveDir);
                }
            }
            else
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateAttackingEnemy()
        {
            if (totem != null && !totem.IsBeingCarried())
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            GameObject carrier = GetTotemCarrier();
            if (carrier != null && carrier == target?.gameObject)
            {
                ChangeState(BotState.ChasingTotemCarrier);
                return;
            }
            
            if (target == null)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (aiming != null)
            {
                aiming.SetLockedTarget(target);
            }
            
            if (distanceToTarget > attackRange)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            if (distanceToTarget < minAttackDistance)
            {
                ChangeState(BotState.Retreating);
                return;
            }
            
            // СТРЕЛЬБА НА ХОДУ - не останавливаемся!
            if (CanAttack() && Random.value < attackProbability)
            {
                TryAttack(target.position);
            }
            
            // Продолжаем двигаться (страфимся), не стоим на месте
            StrafeAround(target.position);
            
            if (Time.time - stateChangeTime > 5f)
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateRetreating()
        {
            if (target == null)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (aiming != null)
            {
                aiming.SetLockedTarget(target);
            }
            
            if (distanceToTarget < preferredAttackDistance)
            {
                Vector3 retreatDir = (transform.position - target.position).normalized;
                MoveInDirection(retreatDir);
                
                // Стреляем на ходу при отступлении
                if (CanAttack() && Random.value < 0.5f)
                {
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
            if (Time.time - lastDodgeTime > 0.5f)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            if (characterController != null)
            {
                Vector3 movement = dodgeDirection * moveSpeed * 1.5f * Time.deltaTime; // Уворот быстрее обычного бега
                movement.y = -9.81f * Time.deltaTime;
                characterController.Move(movement);
            }
        }
        
        private void UpdateRoaming()
        {
            if (totem != null && !totem.IsBeingCarried())
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            if (Time.time - stateChangeTime > 3f)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            float distanceToRoam = Vector3.Distance(transform.position, roamTarget);
            if (distanceToRoam < 1f)
            {
                roamTarget = GetRandomRoamPoint();
            }
            
            Vector3 directionToRoam = (roamTarget - transform.position).normalized;
            MoveInDirection(directionToRoam);
        }
        
        private Vector3 GetRandomRoamPoint()
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                randomDirection.Normalize();
                
                float randomDistance = Random.Range(5f, 15f);
                Vector3 potentialTarget = transform.position + randomDirection * randomDistance;
                
                if (!Physics.CheckSphere(potentialTarget, 1f, wallLayers))
                {
                    if (Physics.Raycast(potentialTarget + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                    {
                        return hit.point;
                    }
                }
            }
            
            return transform.position + Random.insideUnitSphere * 5f;
        }
        
        private void RoamAroundSafe()
        {
            float distanceToRoam = Vector3.Distance(transform.position, roamTarget);
            if (distanceToRoam < 1f || IsWallAhead())
            {
                roamTarget = GetRandomRoamPoint();
            }
            
            Vector3 directionToRoam = (roamTarget - transform.position).normalized;
            MoveInDirection(directionToRoam);
        }
        
        private void FaceTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        
        private void StrafeAround(Vector3 center)
        {
            if (characterController == null) return;
            
            Vector3 toTarget = (center - transform.position).normalized;
            Vector3 strafeDirection = Vector3.Cross(toTarget, Vector3.up);
            
            if (Mathf.Sin(Time.time * 2f) > 0)
                strafeDirection = -strafeDirection;
            
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, strafeDirection, wallDetectionDistance * 0.5f, wallLayers))
            {
                strafeDirection = -strafeDirection;
            }
            
            Vector3 movement = strafeDirection * moveSpeed * 0.6f * Time.deltaTime;
            movement.y = -9.81f * Time.deltaTime;
            characterController.Move(movement);
        }
        
        private bool CanAttack()
        {
            return Time.time - lastAttackTime >= attackCooldown;
        }
        
        private void TryAttack(Vector3 targetPosition)
        {
            if (combatSystem == null) return;
            if (!CanAttack()) return;
            
            lastAttackTime = Time.time;
            combatSystem.AttackAt(targetPosition);
            
            // Уворот сразу после атаки (как в старых играх: shoot & dodge)
            if (Random.value < dodgeChance && Time.time - lastDodgeTime > dodgeCooldown)
            {
                StartDodge();
            }
        }
        
        private void StartDodge()
        {
            lastDodgeTime = Time.time;
            
            if (target != null)
            {
                Vector3 toThreat = (target.position - transform.position).normalized;
                dodgeDirection = Vector3.Cross(toThreat, Vector3.up);
                if (Random.value > 0.5f)
                    dodgeDirection = -dodgeDirection;
            }
            else
            {
                dodgeDirection = Random.insideUnitSphere;
                dodgeDirection.y = 0;
                dodgeDirection.Normalize();
            }
            
            ChangeState(BotState.Dodging);
        }
        
        private void ChangeState(BotState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            stateChangeTime = Time.time;
            
            if (newState == BotState.Roaming)
            {
                roamTarget = GetRandomRoamPoint();
            }
        }
        
        private void FindTotem()
        {
            totem = FindFirstObjectByType<TotemController>();
            if (totem != null)
            {
                lastKnownTotemPosition = totem.transform.position;
            }
        }
        
        private GameObject GetTotemCarrier()
        {
            if (totem == null) return null;
            return totem.GetCarrier();
        }
        
        private Transform GetNearestThreat()
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;
            
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance < nearestDistance && distance < detectionRange)
                {
                    nearestDistance = distance;
                    nearest = playerTransform;
                }
            }
            
            AIBotController[] bots = FindObjectsByType<AIBotController>(FindObjectsSortMode.None);
            foreach (var bot in bots)
            {
                if (bot == this || bot.gameObject == gameObject) continue;
                
                float distance = Vector3.Distance(transform.position, bot.transform.position);
                if (distance < nearestDistance && distance < detectionRange)
                {
                    nearestDistance = distance;
                    nearest = bot.transform;
                }
            }
            
            return nearest;
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            float speed = 0f;
            if (characterController != null && !isFrozen)
            {
                speed = characterController.velocity.magnitude / moveSpeed;
            }
            
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsCarrying", totemInteraction != null && totemInteraction.IsCarrying);
        }
        
        public void OnHitByFireball()
        {
            if (Random.value < 0.7f && Time.time - lastDodgeTime > dodgeCooldown)
            {
                StartDodge();
            }
        }
        
        public void OnFireballIncoming(Vector3 fireballPosition, Vector3 fireballDirection)
        {
            float distance = Vector3.Distance(transform.position, fireballPosition);
            if (distance < 10f && Random.value < dodgeChance)
            {
                dodgeDirection = Vector3.Cross(fireballDirection, Vector3.up);
                if (Random.value > 0.5f)
                    dodgeDirection = -dodgeDirection;
                
                lastDodgeTime = Time.time;
                ChangeState(BotState.Dodging);
            }
        }
    }
}