using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.UI;

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
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<bool> IsCarryingTotemSync = new SyncVar<bool>();
    
    private void Awake()
    {
        Debug.Log($"[NetworkPlayerController] Awake on {gameObject.name}");
        
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
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        spawnTime = Time.time;
        
        Debug.Log($"[NetworkPlayerController] OnStartClient - IsOwner: {IsOwner}, ObjectId: {ObjectId}");
        
        IsCarryingTotemSync.OnChange += OnCarryingChanged;
        
        if (base.IsOwner)
        {
            InitializeTotemPickupUI();
            
            if (aimingSystem != null)
                aimingSystem.enabled = true;
            
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
            
            // Настраиваем камеру
            SetupCamera();
            
            Debug.Log($"[NetworkPlayerController] Local player initialized: {gameObject.name}");
        }
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        
        Debug.Log($"[NetworkPlayerController] OnStopClient - {gameObject.name}");
        
        IsCarryingTotemSync.OnChange -= OnCarryingChanged;
        
        if (base.IsOwner)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (totemPickupUIInstance != null)
            {
                Destroy(totemPickupUIInstance);
            }
        }
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[NetworkPlayerController] OnStartServer - {gameObject.name}");
        
        if (MyNetworkManager.Instance != null)
        {
            MyNetworkManager.Instance.OnPlayerSpawned(base.NetworkObject);
        }
    }
    
    private void Start()
    {
        Debug.Log($"[NetworkPlayerController] Start - {gameObject.name}");
        
        // Управление включено сразу
        EnableControls(true);
    }
    
    private void SetupCamera()
    {
        Debug.Log("[NetworkPlayerController] SetupCamera called");
        
        // Ищем CameraController на сцене
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            Debug.Log("[NetworkPlayerController] Camera set to follow player");
        }
        else
        {
            Debug.LogWarning("[NetworkPlayerController] CameraController not found on scene! Creating fallback camera...");
            
            // Создаём камеру если нет
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
        
        Debug.Log("[NetworkPlayerController] Fallback camera created");
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
        if (!base.IsOwner || !controlsEnabled) return;
        
        // Не даём управлять если игра на паузе
        if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused()) return;
        
        // Задержка перед началом управления (2 секунды после спавна)
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
        // Не обрабатываем боевой ввод если игра на паузе
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
    
    private void OnCarryingChanged(bool prev, bool next, bool asServer)
    {
        UpdateCarryingAnimation(next);
        
        if (base.IsOwner && totemPickupUI != null)
        {
            if (next)
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
    
    public void UpdateCarryingAnimation(bool carrying)
    {
        if (animator != null)
        {
            animator.SetBool("IsCarrying", carrying);
        }
    }
    
    [Server]
    public void SetCarryingState(bool carrying)
    {
        IsCarryingTotemSync.Value = carrying;
    }
    
    public void OnPlayerDeath()
    {
        if (!base.IsOwner) return;
        
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
        
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetBool("IsDead", true);
        }
        
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
    
    public void OnPlayerRespawn()
    {
        if (!base.IsOwner) return;
        
        EnableControls(true);
        
        if (playerCombat != null)
            playerCombat.enabled = true;
            
        if (aimingSystem != null)
            aimingSystem.enabled = true;
        
        if (characterController != null)
            characterController.enabled = true;
        
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.SetTrigger("Respawn");
        }
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        // Переподключаем камеру
        SetupCamera();
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
        return totemInteraction != null && totemInteraction.IsCarrying.Value;
    }
    
    private void OnDestroy()
    {
        Debug.Log($"[NetworkPlayerController] OnDestroy - {gameObject.name}");
    }
}
