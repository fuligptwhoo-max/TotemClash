using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float rotationSpeed = 10f;
    public float gravity = -9.81f;
    
    [Header("Combat Settings")]
    public float attackCooldown = 0.5f;
    
    [Header("Totem Pickup Settings")]
    public float totemPickupTime = 1.5f;
    
    [Header("References")]
    public Animator animator;
    public CharacterController characterController;
    public Slider totemPickupSlider;
    public AimingSystem aimingSystem;
    public PlayerCombat playerCombat;
    
    [Header("Input Settings")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode ability1Key = KeyCode.Alpha1;
    public KeyCode ability2Key = KeyCode.Alpha2;
    public KeyCode ultimateKey = KeyCode.R;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public KeyCode pauseKey = KeyCode.Escape;
    
    private PlayerTotemInteraction totemInteraction;
    private Vector2 inputVector = Vector2.zero;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded = true;
    
    private bool isPickingUpTotem = false;
    private float totemPickupTimer = 0f;
    private TotemController totemToPickup = null;
    
    private bool isMovementEnabled = true;
    private bool isAttackEnabled = true;
    private bool isPaused = false;
    private GameManager gameManager;
    
    private float lastAttackTime = 0f;
    
    private void Awake()
    {
        totemInteraction = GetComponent<PlayerTotemInteraction>();
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat>();
        
        if (playerCombat == null)
        {
            playerCombat = gameObject.AddComponent<PlayerCombat>();
        }
        
        if (aimingSystem == null)
            aimingSystem = GetComponent<AimingSystem>();
        
        if (playerCombat != null && playerCombat.animator == null)
        {
            playerCombat.animator = animator;
        }
        
        if (playerCombat != null && playerCombat.playerController == null)
        {
            playerCombat.playerController = this;
        }
    }
    
    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        
        SetupCursor();
        
        if (totemPickupSlider != null)
        {
            totemPickupSlider.gameObject.SetActive(false);
        }
        
        EnableControls(true);
    }
    
    private void SetupCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
        
        if (aimingSystem != null && aimingSystem.crosshairUI != null)
        {
            aimingSystem.crosshairUI.gameObject.SetActive(true);
        }
    }
    
    private void Update()
    {
        if (isPaused) return;
        
        GetInput();
        CheckGround();
        ApplyGravity();
        
        if (isMovementEnabled)
        {
            MovePlayer();
        }
        
        UpdateTotemPickup();
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
        if (Input.GetKeyDown(attackKey) && isAttackEnabled && aimingSystem != null)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                if (playerCombat != null)
                {
                    bool attackPerformed = playerCombat.PrimaryAttack(aimingSystem.GetAimPosition());
                    if (attackPerformed)
                    {
                        lastAttackTime = Time.time;
                    }
                }
            }
        }
        
        if (Input.GetKeyDown(ability1Key) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                bool abilityUsed = playerCombat.UseAbility(0, aimingSystem.GetAimPosition());
            }
        }
        
        if (Input.GetKeyDown(ability2Key) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                bool abilityUsed = playerCombat.UseAbility(1, aimingSystem.GetAimPosition());
            }
        }
        
        if (Input.GetKeyDown(ultimateKey) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                bool ultimateUsed = playerCombat.UseUltimate(aimingSystem.GetAimPosition());
            }
        }
        
        if (Input.GetKeyDown(pickupKey) && !IsCarryingTotem())
        {
            StartTotemPickup();
        }
        
        if (Input.GetKeyUp(pickupKey) && isPickingUpTotem)
        {
            CancelTotemPickup();
        }
        
        if (Input.GetKeyDown(dropKey))
        {
            DropTotem();
        }
        
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }
    
    private void CheckGround()
    {
        if (characterController != null && characterController.enabled)
        {
            isGrounded = characterController.isGrounded;
        }
        else
        {
            float rayLength = 0.2f;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayLength);
        }
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    
    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(velocity * Time.deltaTime);
        }
    }
    
    private void MovePlayer()
    {
        moveDirection = new Vector3(inputVector.x, 0f, inputVector.y);
        
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
            
            if (characterController != null)
            {
                characterController.Move(movement);
            }
        }
    }
    
    private void StartTotemPickup()
    {
        if (IsCarryingTotem() || isPickingUpTotem) return;
        
        totemToPickup = FindClosestTotem();
        
        if (totemToPickup != null)
        {
            isPickingUpTotem = true;
            totemPickupTimer = 0f;
            
            if (totemPickupSlider != null)
            {
                totemPickupSlider.gameObject.SetActive(true);
                totemPickupSlider.value = 0f;
            }
            
            moveSpeed *= 0.5f;
            rotationSpeed *= 0.5f;
        }
    }
    
    private void UpdateTotemPickup()
    {
        if (!isPickingUpTotem) return;
        
        if (totemToPickup == null || totemToPickup.IsBeingCarried())
        {
            CancelTotemPickup();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, totemToPickup.transform.position);
        if (distance > 2f)
        {
            CancelTotemPickup();
            return;
        }
        
        totemPickupTimer += Time.deltaTime;
        
        if (totemPickupSlider != null)
        {
            totemPickupSlider.value = totemPickupTimer / totemPickupTime;
        }
        
        if (totemPickupTimer >= totemPickupTime)
        {
            FinishTotemPickup();
        }
    }
    
    private void CancelTotemPickup()
    {
        isPickingUpTotem = false;
        totemPickupTimer = 0f;
        totemToPickup = null;
        
        moveSpeed = 8f;
        rotationSpeed = 10f;
        
        if (totemPickupSlider != null)
        {
            totemPickupSlider.gameObject.SetActive(false);
        }
    }
    
    private void FinishTotemPickup()
    {
        if (totemToPickup != null && totemInteraction != null)
        {
            totemInteraction.TryPickUp();
        }
        
        CancelTotemPickup();
    }
    
    private TotemController FindClosestTotem()
    {
        var totems = FindObjectsByType<TotemController>(FindObjectsSortMode.None);
        float closestDistance = float.MaxValue;
        TotemController closestTotem = null;
        
        foreach (var totem in totems)
        {
            if (totem.IsBeingCarried()) continue;
            
            float distance = Vector3.Distance(transform.position, totem.transform.position);
            if (distance <= 2f && distance < closestDistance)
            {
                closestDistance = distance;
                closestTotem = totem;
            }
        }
        
        return closestTotem;
    }
    
    public void DropTotem()
    {
        if (totemInteraction != null && totemInteraction.IsCarrying)
        {
            totemInteraction.DropTotem();
        }
    }
    
    public void OnPlayerDeath()
    {
        if (totemInteraction != null)
        {
            totemInteraction.OnPlayerDeath();
        }
        
        EnableControls(false);
        
        if (playerCombat != null)
            playerCombat.enabled = false;
        if (aimingSystem != null)
            aimingSystem.enabled = false;
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        float moveSpeedAnimation = Mathf.Clamp01(moveDirection.magnitude);
        animator.SetFloat("Speed", moveSpeedAnimation);
        
        bool isCarrying = IsCarryingTotem();
        animator.SetBool("IsCarrying", isCarrying);
    }
    
    private void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            if (aimingSystem != null && aimingSystem.crosshairUI != null)
            {
                aimingSystem.crosshairUI.gameObject.SetActive(false);
            }
            
            if (gameManager != null)
            {
                gameManager.TogglePause();
            }
        }
        else
        {
            Time.timeScale = 1f;
            SetupCursor();
            
            if (gameManager != null)
            {
                gameManager.TogglePause();
            }
        }
    }
    
    public void EnableControls(bool enable)
    {
        isMovementEnabled = enable;
        isAttackEnabled = enable;
        
        if (characterController != null)
        {
            characterController.enabled = enable;
        }
        
        if (!enable)
        {
            CancelTotemPickup();
        }
    }
    
    public bool IsCarryingTotem()
    {
        return totemInteraction != null && totemInteraction.IsCarrying;
    }
    
    public PlayerClass GetCurrentClass()
    {
        return playerCombat != null ? playerCombat.currentClass : null;
    }
    
    private void OnDestroy()
    {
        if (IsCarryingTotem())
        {
            OnPlayerDeath();
        }
    }
}