using UnityEngine;
using UnityEngine.UI;
using TotemClash.UI;
using TotemClash.Network; // Для GameSettings

namespace TotemClash.Combat
{
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerTotemInteraction))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float rotationSpeed = 10f;
        
        [Header("Combat Settings")]
        public float attackCooldown = 0.5f;
        
        [Header("Totem Pickup Settings")]
        public float totemPickupTime = 1.5f;
        
        [Header("Totem Pickup UI")]
        public GameObject totemPickupUIPrefab;
        
        [Header("References")]
        public Animator animator;
        public CharacterController characterController;
        public AimingSystem aimingSystem;
        public PlayerCombat playerCombat;
        public HealthSystem healthSystem;
        
        [Header("Input Settings")]
        public KeyCode attackKey = KeyCode.Mouse0;
        public KeyCode ability1Key = KeyCode.Q;
        public KeyCode ability2Key = KeyCode.R;
        public KeyCode ultimateKey = KeyCode.F;
        public KeyCode pickupKey = KeyCode.E;
        public KeyCode dropKey = KeyCode.G;
        
        private PlayerTotemInteraction totemInteraction;
        private Vector2 inputVector = Vector2.zero;
        private Vector3 moveDirection = Vector3.zero;
        private bool isMovementEnabled = true;
        private bool isAttackEnabled = true;
        private bool controlsEnabled = true;
        private float lastAttackTime = 0f;
        private float spawnTime = 0f;
        private TotemPickupUI totemPickupUI;
        private GameObject totemPickupUIInstance;
        private bool isCarryingTotem = false;
        
        private void Awake()
        {
            Debug.Log($"[PlayerController] Awake on {gameObject.name}");
            
            totemInteraction = GetComponent<PlayerTotemInteraction>();
            
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            
            if (playerCombat == null)
                playerCombat = GetComponent<PlayerCombat>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            if (healthSystem == null)
                healthSystem = GetComponent<HealthSystem>();
            
            if (aimingSystem == null)
                aimingSystem = GetComponent<AimingSystem>();
                
            // ИСПРАВЛЕНО: Применяем настройки скорости из GameSettings
            ApplySpeedSettings();
        }
        
        private void Start()
        {
            Debug.Log($"[PlayerController] Start - {gameObject.name}");
            
            spawnTime = Time.time;
            InitializeTotemPickupUI();
            
            if (aimingSystem != null)
                aimingSystem.enabled = true;
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
            
            SetupCamera();
            EnableControls(true);
            
            if (healthSystem != null)
            {
                healthSystem.OnDeath.AddListener(OnPlayerDeath);
                healthSystem.OnRespawn.AddListener(OnPlayerRespawn);
            }
            
            // ИСПРАВЛЕНО: Подписываемся на изменение настроек скорости
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnPlayerSpeedChanged.AddListener(OnSpeedSettingChanged);
            }
        }
        
        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDeath.RemoveListener(OnPlayerDeath);
                healthSystem.OnRespawn.RemoveListener(OnPlayerRespawn);
            }
            
            // ИСПРАВЛЕНО: Отписываемся от событий
            if (GameSettings.Instance != null)
            {
                GameSettings.Instance.OnPlayerSpeedChanged.RemoveListener(OnSpeedSettingChanged);
            }
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (totemPickupUIInstance != null)
            {
                Destroy(totemPickupUIInstance);
            }
        }
        
        // ИСПРАВЛЕНО: Применение начальных настроек
        private void ApplySpeedSettings()
        {
            if (GameSettings.Instance != null)
            {
                moveSpeed = GameSettings.Instance.GetPlayerSpeed();
                Debug.Log($"[PlayerController] Applied speed from settings: {moveSpeed}");
            }
        }
        
        // ИСПРАВЛЕНО: Обработчик изменения скорости
        private void OnSpeedSettingChanged(float newSpeed)
        {
            moveSpeed = newSpeed;
            Debug.Log($"[PlayerController] Speed updated to: {newSpeed}");
        }
        
        public void SetupCamera()
        {
            Debug.Log("[PlayerController] SetupCamera called");
            
            CameraController cameraController = FindFirstObjectByType<CameraController>();
            
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
                Debug.Log("[PlayerController] Camera set to follow player");
            }
            else
            {
                Debug.LogWarning("[PlayerController] CameraController not found! Creating fallback...");
                CreateFallbackCamera();
            }
        }
        
        private void CreateFallbackCamera()
        {
            GameObject camObj = new GameObject("FallbackCamera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 90f;
            
            CameraController cc = camObj.AddComponent<CameraController>();
            cc.SetTarget(transform);
            
            Debug.Log("[PlayerController] Fallback camera created");
        }
        
        private void InitializeTotemPickupUI()
        {
            if (totemPickupUIPrefab == null) return;
            
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("PlayerCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            totemPickupUIInstance = Instantiate(totemPickupUIPrefab, canvas.transform);
            totemPickupUI = totemPickupUIInstance.GetComponent<TotemPickupUI>();
            
            if (totemPickupUI != null)
            {
                totemPickupUI.Hide();
                totemPickupUI.ResetProgress();
            }
        }
        
        private void Update()
        {
            if (!controlsEnabled) return;
            
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused()) return;
            
            if (Time.time - spawnTime < 2f) return;
            
            GetInput();
            
            if (isMovementEnabled)
            {
                MovePlayer();
            }
            
            UpdateAnimations();
            HandleInput();
        }
        
        private void GetInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            inputVector = new Vector2(horizontal, vertical);
        }
        
        private void HandleInput()
        {
            if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused()) return;
            
            if (Input.GetKeyDown(attackKey) && isAttackEnabled)
            {
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    PerformAttack();
                }
            }
            
            if (Input.GetKeyDown(ability1Key) && isAttackEnabled)
            {
                UseAbility(0);
            }
            
            if (Input.GetKeyDown(ability2Key) && isAttackEnabled)
            {
                UseAbility(1);
            }
            
            if (Input.GetKeyDown(ultimateKey) && isAttackEnabled)
            {
                UseUltimate();
            }
            
            if (Input.GetKeyDown(pickupKey))
            {
                if (IsCarryingTotem())
                {
                    DropTotem();
                }
                else
                {
                    TryPickUpTotem();
                }
            }
            
            if (Input.GetKeyDown(dropKey))
            {
                DropTotem();
            }
        }
        
        private void PerformAttack()
        {
            if (playerCombat != null && aimingSystem != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                bool attackPerformed = playerCombat.PrimaryAttack(aimPosition);
                
                if (attackPerformed)
                {
                    lastAttackTime = Time.time;
                }
            }
        }
        
        private void UseAbility(int abilityIndex)
        {
            if (playerCombat != null && aimingSystem != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                playerCombat.UseAbility(abilityIndex, aimPosition);
            }
        }
        
        private void UseUltimate()
        {
            if (playerCombat != null && aimingSystem != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                playerCombat.UseUltimate(aimPosition);
            }
        }
        
        private void MovePlayer()
        {
            moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);
            
            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                
                Vector3 movement = transform.forward * moveSpeed * Time.deltaTime;
                
                if (characterController != null && characterController.enabled)
                {
                    movement.y = -9.81f * Time.deltaTime;
                    characterController.Move(movement);
                }
                else
                {
                    transform.position += movement;
                }
            }
        }
        
        private void TryPickUpTotem()
        {
            if (isCarryingTotem) return;
            
            if (totemInteraction != null)
            {
                totemInteraction.TryPickUp();
            }
        }
        
        public void DropTotem()
        {
            if (totemInteraction != null)
            {
                totemInteraction.DropTotem();
            }
        }
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            float moveSpeedAnimation = Mathf.Clamp01(moveDirection.magnitude);
            animator.SetFloat("Speed", moveSpeedAnimation);
            
            bool carrying = IsCarryingTotem();
            animator.SetBool("IsCarrying", carrying);
        }
        
        public void UpdateCarryingAnimation(bool carrying)
        {
            if (animator != null)
            {
                animator.SetBool("IsCarrying", carrying);
            }
        }
        
        public void SetCarryingState(bool carrying)
        {
            isCarryingTotem = carrying;
            UpdateCarryingAnimation(carrying);
            
            if (totemPickupUI != null)
            {
                if (carrying)
                {
                    totemPickupUI.Show();
                    totemPickupUI.UpdateProgress(1f, 0f);
                }
                else
                {
                    totemPickupUI.Hide();
                }
            }
            
            Debug.Log($"[PlayerController] Carrying state set to: {carrying}");
        }
        
        public void OnPlayerDeath()
        {
            EnableControls(false);
            
            if (playerCombat != null)
                playerCombat.enabled = false;
                
            if (aimingSystem != null)
                aimingSystem.enabled = false;
            
            if (totemInteraction != null)
            {
                totemInteraction.OnPlayerDeath();
            }
            
            if (characterController != null)
                characterController.enabled = false;
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (totemPickupUI != null)
            {
                totemPickupUI.Hide();
            }
        }
        
        public void OnPlayerRespawn()
        {
            EnableControls(true);
            
            if (playerCombat != null)
                playerCombat.enabled = true;
                
            if (aimingSystem != null)
                aimingSystem.enabled = true;
            
            if (characterController != null)
                characterController.enabled = true;
            
            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
            
            spawnTime = Time.time;
            
            SetupCamera();
            
            // ИСПРАВЛЕНО: Применяем настройки снова после респавна
            ApplySpeedSettings();
        }
        
        public void EnableControls(bool enable)
        {
            controlsEnabled = enable;
            isMovementEnabled = enable;
            isAttackEnabled = enable;
            
            if (characterController != null)
            {
                characterController.enabled = enable;
            }
            
            if (playerCombat != null)
            {
                playerCombat.enabled = enable;
            }
        }
        
        public bool IsCarryingTotem()
        {
            return isCarryingTotem;
        }
        
        public PlayerTotemInteraction GetTotemInteraction()
        {
            return totemInteraction;
        }
        
        public TotemPickupUI GetTotemPickupUI()
        {
            return totemPickupUI;
        }
    }
}