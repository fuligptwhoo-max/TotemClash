using UnityEngine;
using UnityEngine.UI;
using TotemClash.UI;
using TotemClash.Network;

namespace TotemClash.Combat
{
    [RequireComponent(typeof(CombatSystem))]
    [RequireComponent(typeof(PlayerTotemInteraction))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float rotationSpeed = 10f;
        
        [Header("Totem Pickup Settings")]
        public float totemPickupTime = 1.5f;
        public GameObject totemPickupUIPrefab;
        
        [Header("References")]
        public Animator animator;
        public CharacterController characterController;
        public AimingSystem aimingSystem;
        public HealthSystem healthSystem;
        public CombatSystem combatSystem;
        
        [Header("Input Settings")]
        public KeyCode pickupKey = KeyCode.E;
        public KeyCode dropKey = KeyCode.G;
        
        private PlayerTotemInteraction totemInteraction;
        private Vector2 inputVector = Vector2.zero;
        private Vector3 moveDirection = Vector3.zero;
        private bool controlsEnabled = true;
        private float spawnTime = 0f;
        private TotemPickupUI totemPickupUI;
        private GameObject totemPickupUIInstance;
        private bool isCarryingTotem = false;
        
        private void Awake()
        {
            totemInteraction = GetComponent<PlayerTotemInteraction>();
            
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            
            if (combatSystem == null)
                combatSystem = GetComponent<CombatSystem>();
            
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
            
            if (healthSystem == null)
                healthSystem = GetComponent<HealthSystem>();
            
            if (aimingSystem == null)
                aimingSystem = GetComponent<AimingSystem>();
        }
        
        private void Start()
        {
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
        }
        
        private void OnDestroy()
        {
            if (healthSystem != null)
            {
                healthSystem.OnDeath.RemoveListener(OnPlayerDeath);
                healthSystem.OnRespawn.RemoveListener(OnPlayerRespawn);
            }
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (totemPickupUIInstance != null)
                Destroy(totemPickupUIInstance);
        }
        
        public void SetupCamera()
        {
            CameraController cameraController = FindFirstObjectByType<CameraController>();
            
            if (cameraController != null)
            {
                cameraController.SetTarget(transform);
            }
            else
            {
                Debug.LogWarning("[PlayerController] CameraController not found!");
            }
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
            
            // УБРАНО: if (Time.time - spawnTime < 2f) return; - больше нет фриза при спавне
            
            GetInput();
            
            if (controlsEnabled)
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
        }
        
        public void OnPlayerDeath()
        {
            EnableControls(false);
            
            if (combatSystem != null)
                combatSystem.enabled = false;
                
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
            
            if (combatSystem != null)
                combatSystem.enabled = true;
                
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
        }
        
        public void EnableControls(bool enable)
        {
            controlsEnabled = enable;
            
            if (characterController != null)
            {
                characterController.enabled = enable;
            }
            
            if (combatSystem != null)
            {
                combatSystem.enabled = enable;
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
    }
}