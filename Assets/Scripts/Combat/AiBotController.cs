using UnityEngine;
using System.Collections;
using TotemClash.Combat;
using TotemClash.Classes;

namespace TotemClash.Combat
{
    public class AIBotController : MonoBehaviour
    {
        [Header("AI Settings")]
        public float moveSpeed = 8f;
        public float rotationSpeed = 8f;
        public float attackRange = 20f;
        public float detectionRange = 25f;
        public float totemPriorityRange = 30f;
        
        [Header("Combat")]
        public float attackCooldown = 1.5f;
        public float attackDelay = 0.3f;
        public float dodgeChance = 0.4f;
        public float dodgeCooldown = 2f;
        
        [Header("Totem Behavior")]
        public float totemPickupRange = 2.5f;
        public float fleeDistance = 15f;
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
        public PlayerCombat playerCombat;
        public PlayerTotemInteraction totemInteraction;
        
        private enum BotState
        {
            Idle,
            SeekingTotem,
            FleeingWithTotem,
            ChasingTotemCarrier,
            AttackingEnemy,
            Dodging,
            Roaming,
            AvoidingWall
        }
        
        private BotState currentState = BotState.Idle;
        private Transform target;
        private TotemController totem;
        private float lastAttackTime;
        private float lastDodgeTime;
        private float stateChangeTime;
        private Vector3 lastKnownTotemPosition;
        private Vector3 dodgeDirection;
        private Vector3 roamTarget;
        private Vector3 lastPosition;
        private float stuckTimer = 0f;
        private Vector3 wallAvoidanceDirection;
        
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
            if (playerCombat == null)
                playerCombat = GetComponent<PlayerCombat>();
            if (totemInteraction == null)
                totemInteraction = GetComponent<PlayerTotemInteraction>();
            
            // ИСПРАВЛЕНО: Настраиваем слои стен по умолчанию
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
            
            Debug.Log($"[AIBotController] {gameObject.name} {(freeze ? "frozen" : "unfrozen")}");
        }
        
        private void Update()
        {
            if (!isInitialized || healthSystem == null || healthSystem.IsDead)
                return;
            
            if (isFrozen) return;
            
            // ИСПРАВЛЕНО: Проверка на застревание
            CheckIfStuck();
            
            if (totem == null)
                FindTotem();
            
            // ИСПРАВЛЕНО: Проверка стен перед обновлением состояния
            if (currentState != BotState.AvoidingWall && IsWallAhead())
            {
                StartWallAvoidance();
            }
            
            switch (currentState)
            {
                case BotState.Idle:
                    UpdateIdle();
                    break;
                case BotState.SeekingTotem:
                    UpdateSeekingTotem();
                    break;
                case BotState.FleeingWithTotem:
                    UpdateFleeingWithTotem();
                    break;
                case BotState.ChasingTotemCarrier:
                    UpdateChasingTotemCarrier();
                    break;
                case BotState.AttackingEnemy:
                    UpdateAttackingEnemy();
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
            }
            
            UpdateAnimations();
        }
        
        // ИСПРАВЛЕНО: Проверка на застревание
        private void CheckIfStuck()
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < minMoveDistance)
            {
                stuckTimer += Time.deltaTime;
                
                if (stuckTimer > stuckCheckTime)
                {
                    Debug.Log($"[AIBotController] {gameObject.name} is stuck! Finding new path...");
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
        
        // ИСПРАВЛЕНО: Проверка стены впереди
        private bool IsWallAhead()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            // Проверяем несколько лучей веером
            for (int i = -1; i <= 1; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 20f, 0) * forward;
                if (Physics.Raycast(origin, direction, wallDetectionDistance, wallLayers))
                {
                    Debug.DrawRay(origin, direction * wallDetectionDistance, Color.red, 0.5f);
                    return true;
                }
                Debug.DrawRay(origin, direction * wallDetectionDistance, Color.green, 0.1f);
            }
            
            return false;
        }
        
        // ИСПРАВЛЕНО: Начало обхода стены
        private void StartWallAvoidance()
        {
            Vector3 forward = transform.forward;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            
            // Ищем направление без стены
            for (int i = 1; i <= 4; i++)
            {
                // Пробуем вправо
                Vector3 rightDir = Quaternion.Euler(0, i * wallAvoidanceAngle, 0) * forward;
                if (!Physics.Raycast(origin, rightDir, wallDetectionDistance, wallLayers))
                {
                    wallAvoidanceDirection = rightDir;
                    ChangeState(BotState.AvoidingWall);
                    return;
                }
                
                // Пробуем влево
                Vector3 leftDir = Quaternion.Euler(0, -i * wallAvoidanceAngle, 0) * forward;
                if (!Physics.Raycast(origin, leftDir, wallDetectionDistance, wallLayers))
                {
                    wallAvoidanceDirection = leftDir;
                    ChangeState(BotState.AvoidingWall);
                    return;
                }
            }
            
            // Если не нашли - разворачиваемся назад
            wallAvoidanceDirection = -forward;
            ChangeState(BotState.AvoidingWall);
        }
        
        // ИСПРАВЛЕНО: Обновление состояния обхода стены
        private void UpdateAvoidingWall()
        {
            MoveInDirection(wallAvoidanceDirection);
            
            // Если стены больше нет - возвращаемся к предыдущему состоянию
            if (!IsWallAhead())
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        // ИСПРАВЛЕНО: Движение в заданном направлении с проверкой стен
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
            if (Time.time - stateChangeTime > 1f)
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateSeekingTotem()
        {
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                ChangeState(BotState.FleeingWithTotem);
                return;
            }
            
            GameObject carrier = GetTotemCarrier();
            if (carrier != null && carrier != gameObject)
            {
                target = carrier.transform;
                ChangeState(BotState.ChasingTotemCarrier);
                return;
            }
            
            if (totem != null && !totem.IsBeingCarried())
            {
                Vector3 directionToTotem = (totem.transform.position - transform.position).normalized;
                MoveInDirection(directionToTotem);
                
                float distanceToTotem = Vector3.Distance(transform.position, totem.transform.position);
                if (distanceToTotem <= totemPickupRange)
                {
                    if (totemInteraction != null)
                    {
                        totemInteraction.TryPickUp();
                    }
                }
            }
            else
            {
                ChangeState(BotState.Roaming);
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
                    Vector3 fleeDirection = (transform.position - nearestThreat.position).normalized;
                    
                    // ИСПРАВЛЕНО: Проверяем, не ведет ли убегание в стену
                    Vector3 origin = transform.position + Vector3.up * 0.5f;
                    if (Physics.Raycast(origin, fleeDirection, wallDetectionDistance, wallLayers))
                    {
                        // Если в стену - ищем другое направление
                        StartWallAvoidance();
                        return;
                    }
                    
                    MoveInDirection(fleeDirection);
                    
                    if (distanceToThreat < attackRange && Time.time - lastAttackTime > attackCooldown)
                    {
                        TryAttack(nearestThreat.position);
                    }
                }
                else
                {
                    RoamAround();
                }
            }
            else
            {
                RoamAround();
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
                    ChangeState(BotState.AttackingEnemy);
                }
            }
            else
            {
                ChangeState(BotState.SeekingTotem);
            }
        }
        
        private void UpdateAttackingEnemy()
        {
            if (totemInteraction != null && totemInteraction.IsCarrying)
            {
                ChangeState(BotState.FleeingWithTotem);
                return;
            }
            
            if (target == null)
            {
                ChangeState(BotState.SeekingTotem);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            if (distanceToTarget > attackRange * 1.2f)
            {
                ChangeState(BotState.ChasingTotemCarrier);
                return;
            }
            
            FaceTarget(target.position);
            
            if (Time.time - lastAttackTime > attackCooldown)
            {
                TryAttack(target.position);
            }
            
            StrafeAround(target.position);
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
                Vector3 movement = dodgeDirection * moveSpeed * Time.deltaTime;
                movement.y = -9.81f * Time.deltaTime;
                characterController.Move(movement);
            }
        }
        
        private void UpdateRoaming()
        {
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
                
                // ИСПРАВЛЕНО: Проверяем, не в стене ли точка
                if (!Physics.CheckSphere(potentialTarget, 1f, wallLayers))
                {
                    if (Physics.Raycast(potentialTarget + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
                    {
                        return hit.point;
                    }
                }
            }
            
            // Если не нашли - возвращаем текущую позицию с небольшим смещением
            return transform.position + Random.insideUnitSphere * 5f;
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
            
            // ИСПРАВЛЕНО: Проверяем стену при стрейфе
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, strafeDirection, wallDetectionDistance * 0.5f, wallLayers))
            {
                strafeDirection = -strafeDirection;
            }
            
            Vector3 movement = strafeDirection * moveSpeed * 0.7f * Time.deltaTime;
            movement.y = -9.81f * Time.deltaTime;
            characterController.Move(movement);
        }
        
        private void RoamAround()
        {
            float distanceToRoam = Vector3.Distance(transform.position, roamTarget);
            if (distanceToRoam < 1f || IsWallAhead())
            {
                roamTarget = GetRandomRoamPoint();
            }
            
            Vector3 directionToRoam = (roamTarget - transform.position).normalized;
            MoveInDirection(directionToRoam);
        }
        
        private void TryAttack(Vector3 targetPosition)
        {
            if (playerCombat == null) return;
            
            lastAttackTime = Time.time;
            playerCombat.PrimaryAttack(targetPosition);
            
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