using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NetworkAnimator))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerTotemInteraction))]
public class NetworkPlayerController : NetworkBehaviour
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
    public Camera playerCamera;
    
    [Header("Input Settings")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode ability1Key = KeyCode.Q;
    public KeyCode ability2Key = KeyCode.R;
    public KeyCode ultimateKey = KeyCode.F;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.G;
    
    private PlayerTotemInteraction totemInteraction;
    private NetworkAnimator networkAnimator;
    
    private Vector2 inputVector = Vector2.zero;
    private Vector3 moveDirection = Vector3.zero;
    
    private bool isMovementEnabled = true;
    private bool isAttackEnabled = true;
    private bool controlsEnabled = true;
    
    private float lastAttackTime = 0f;
    
    private TotemPickupUI totemPickupUI;
    private GameObject totemPickupUIInstance;
    
    private bool isBeingDestroyed = false;
    private bool isApplicationQuitting = false;
    
    private float originalMoveSpeed;
    private float originalRotationSpeed;
    
    private void Awake()
    {
        totemInteraction = GetComponent<PlayerTotemInteraction>();
        networkAnimator = GetComponent<NetworkAnimator>();
        
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
        
        originalMoveSpeed = moveSpeed;
        originalRotationSpeed = rotationSpeed;
        
        Application.quitting += () => isApplicationQuitting = true;
    }
    
    public override void OnStartLocalPlayer()
    {
        if (aimingSystem != null)
            aimingSystem.enabled = true;
            
        SetupCamera();
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        InitializeTotemPickupUI();
        
        Debug.Log($"Локальный игрок инициализирован: {gameObject.name}");
    }
    
    private void Start()
    {
        EnableControls(true);
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    
    private void SetupCamera()
    {
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }
    }
    
    private void InitializeTotemPickupUI()
    {
        if (totemPickupUIPrefab == null)
        {
            Debug.LogWarning("Префаб TotemPickupUI не назначен!");
            return;
        }
        
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
        
        Debug.Log("UI для подбора тотема инициализировано");
    }
    
    private void Update()
    {
        if (!isLocalPlayer || !controlsEnabled) return;
        
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
        if (!isMovementEnabled) return;
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        inputVector = new Vector2(horizontal, vertical);
    }
    
    private void HandleInput()
    {
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
        if (IsCarryingTotem()) return;
        
        if (totemInteraction != null)
        {
            totemInteraction.CmdTryPickUp();
        }
    }
    
    public void DropTotem()
    {
        if (totemInteraction != null)
        {
            totemInteraction.CmdDropTotem();
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        float moveSpeedAnimation = Mathf.Clamp01(moveDirection.magnitude);
        animator.SetFloat("Speed", moveSpeedAnimation);
        
        bool isCarrying = IsCarryingTotem();
        animator.SetBool("IsCarrying", isCarrying);
    }
    
    public void UpdateCarryingAnimation(bool isCarrying)
    {
        if (animator != null)
        {
            animator.SetBool("IsCarrying", isCarrying);
        }
    }
    
    public void OnPlayerDeath()
    {
        if (!isLocalPlayer) return;
        
        // Отключаем управление
        EnableControls(false);
        
        // Отключаем компоненты
        if (playerCombat != null)
            playerCombat.enabled = false;
            
        if (aimingSystem != null)
            aimingSystem.enabled = false;
        
        // Если несем тотем - сбрасываем его на сервере
        if (totemInteraction != null)
        {
            totemInteraction.OnPlayerDeath();
        }
        
        // Отключаем контроллер
        if (characterController != null)
            characterController.enabled = false;
        
        // Проигрываем анимацию смерти
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsDead", true);
        }
        
        // Включаем курсор
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        Debug.Log($"{gameObject.name} умер!");
    }
    
    public void OnPlayerRespawn()
    {
        if (!isLocalPlayer) return;
        
        // Включаем управление
        EnableControls(true);
        
        // Включаем компоненты
        if (playerCombat != null)
            playerCombat.enabled = true;
            
        if (aimingSystem != null)
            aimingSystem.enabled = true;
        
        // Включаем контроллер
        if (characterController != null)
            characterController.enabled = true;
        
        // Сбрасываем анимацию смерти
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.SetTrigger("Respawn");
        }
        
        // Скрываем курсор
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        Debug.Log($"{gameObject.name} возродился!");
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
        
        Debug.Log($"Controls {(enable ? "enabled" : "disabled")} for {gameObject.name}");
    }
    
    public bool IsCarryingTotem()
    {
        return totemInteraction != null && totemInteraction.IsCarrying;
    }
    
    private void OnDestroy()
    {
        if (isBeingDestroyed) return;
        isBeingDestroyed = true;
        
        if (isApplicationQuitting) return;
        
        if (isLocalPlayer && totemPickupUIInstance != null)
        {
            Destroy(totemPickupUIInstance);
        }
        
        if (isLocalPlayer && NetworkClient.active && !isApplicationQuitting)
        {
            if (IsCarryingTotem())
            {
                OnPlayerDeath();
            }
        }
        
        CancelInvoke();
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        
        if (isLocalPlayer)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Debug.Log($"Authority started for {gameObject.name}");
    }
    
    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        Debug.Log($"Authority stopped for {gameObject.name}");
    }
    
    public void DebugLogState()
    {
        Debug.Log($"NetworkPlayerController State:");
        Debug.Log($"- IsLocalPlayer: {isLocalPlayer}");
        Debug.Log($"- IsMovementEnabled: {isMovementEnabled}");
        Debug.Log($"- IsAttackEnabled: {isAttackEnabled}");
        Debug.Log($"- IsCarryingTotem: {IsCarryingTotem()}");
        Debug.Log($"- Health: {healthSystem?.currentHealth}/{healthSystem?.maxHealth}");
        Debug.Log($"- Position: {transform.position}");
    }
}