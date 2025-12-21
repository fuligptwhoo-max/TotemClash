using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformHybrid))]
public class NetworkPlayerController : NetworkBehaviour
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
    public Camera playerCamera;
    public AudioListener audioListener;
    
    [Header("Input Settings")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode ability1Key = KeyCode.Alpha1;
    public KeyCode ability2Key = KeyCode.Alpha2;
    public KeyCode ultimateKey = KeyCode.R;
    public KeyCode pickupKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;
    public KeyCode pauseKey = KeyCode.Escape;
    
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerName = "Player";
    
    [SyncVar(hook = nameof(OnIsCarryingChanged))]
    private bool syncIsCarrying = false;
    
    private PlayerTotemInteraction totemInteraction;
    private Vector2 inputVector = Vector2.zero;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded = true;
    
    private bool isPickingUpTotem = false;
    private float totemPickupTimer = 0f;
    private NetworkTotemController totemToPickup = null;
    
    private bool isMovementEnabled = true;
    private bool isAttackEnabled = true;
    private bool isPaused = false;
    private GameManager gameManager;
    
    private float lastAttackTime = 0f;
    private NetworkAnimator networkAnimator;
    
    private NetworkTotemController currentTotem = null;
    private bool isCarryingTotem = false;
    
    public bool IsCarrying
    {
        get { return isCarryingTotem; }
    }
    
    private void Awake()
    {
        totemInteraction = GetComponent<PlayerTotemInteraction>();
        networkAnimator = GetComponent<NetworkAnimator>();
        
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        
        if (playerCombat == null)
            playerCombat = GetComponent<PlayerCombat>();
        
        if (aimingSystem == null)
            aimingSystem = GetComponent<AimingSystem>();
        
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        
        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>();
    }
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        Debug.Log($"Локальный игрок запущен: {playerName}");
        
        if (playerCamera != null)
            playerCamera.enabled = true;
        
        if (audioListener != null)
            audioListener.enabled = true;
        
        SetupCursor();
        
        if (totemPickupSlider != null)
        {
            totemPickupSlider.gameObject.SetActive(false);
        }
        
        gameManager = FindAnyObjectByType<GameManager>();
        
        EnableControls(true);
        
        CmdSetPlayerName($"Player_{netId}");
        
        CameraController cameraController = FindAnyObjectByType<CameraController>();
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
        }
    }
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!isLocalPlayer)
        {
            if (playerCamera != null)
                playerCamera.enabled = false;
            
            if (audioListener != null)
                audioListener.enabled = false;
            
            if (aimingSystem != null && aimingSystem.crosshairUI != null)
            {
                aimingSystem.crosshairUI.gameObject.SetActive(false);
            }
        }
    }
    
    private void SetupCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        if (aimingSystem != null && aimingSystem.crosshairUI != null)
        {
            aimingSystem.crosshairUI.gameObject.SetActive(true);
        }
    }
    
    private void Update()
    {
        if (!isLocalPlayer) return;
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
                    Vector3 aimPosition = aimingSystem.GetAimPosition();
                    CmdPrimaryAttack(aimPosition);
                    lastAttackTime = Time.time;
                }
            }
        }
        
        if (Input.GetKeyDown(ability1Key) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                CmdAbility1(aimPosition);
            }
        }
        
        if (Input.GetKeyDown(ability2Key) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                CmdAbility2(aimPosition);
            }
        }
        
        if (Input.GetKeyDown(ultimateKey) && isAttackEnabled && aimingSystem != null)
        {
            if (playerCombat != null)
            {
                Vector3 aimPosition = aimingSystem.GetAimPosition();
                CmdUltimateAbility(aimPosition);
            }
        }
        
        if (Input.GetKeyDown(pickupKey) && !isCarryingTotem)
        {
            StartTotemPickup();
        }
        
        if (Input.GetKeyUp(pickupKey) && isPickingUpTotem)
        {
            CancelTotemPickup();
        }
        
        if (Input.GetKeyDown(dropKey))
        {
            CmdDropTotem();
        }
        
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }
    
    [Command]
    private void CmdPrimaryAttack(Vector3 targetPosition)
    {
        if (playerCombat != null)
        {
            bool attackPerformed = playerCombat.PrimaryAttack(targetPosition);
            if (attackPerformed)
            {
                RpcPlayAttackAnimation();
            }
        }
    }
    
    [Command]
    private void CmdAbility1(Vector3 targetPosition)
    {
        if (playerCombat != null)
        {
            playerCombat.UseAbility(0, targetPosition);
        }
    }
    
    [Command]
    private void CmdAbility2(Vector3 targetPosition)
    {
        if (playerCombat != null)
        {
            playerCombat.UseAbility(1, targetPosition);
        }
    }
    
    [Command]
    private void CmdUltimateAbility(Vector3 targetPosition)
    {
        if (playerCombat != null)
        {
            playerCombat.UseUltimate(targetPosition);
        }
    }
    
    [Command]
    private void CmdDropTotem()
    {
        if (currentTotem != null)
        {
            currentTotem.DropTotem(false);
            currentTotem = null;
            isCarryingTotem = false;
            syncIsCarrying = false;
        }
    }
    
    [Command]
    private void CmdSetPlayerName(string name)
    {
        playerName = name;
    }
    
    [ClientRpc]
    private void RpcPlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetBool("IsAttacking", true);
            
            Invoke(nameof(ResetAttackAnimation), 0.5f);
        }
    }
    
    private void ResetAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
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
            
            if (networkAnimator != null && networkAnimator.animator != null)
            {
                networkAnimator.animator.SetFloat("Speed", moveDirection.magnitude);
            }
        }
        else
        {
            if (networkAnimator != null && networkAnimator.animator != null)
            {
                networkAnimator.animator.SetFloat("Speed", 0f);
            }
        }
    }
    
    private void StartTotemPickup()
    {
        if (isCarryingTotem || isPickingUpTotem) return;
        
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
    
    private void FinishTotemPickup()
    {
        if (totemToPickup != null)
        {
            CmdPickupTotem(totemToPickup.gameObject);
        }
        
        CancelTotemPickup();
    }
    
    [Command]
    private void CmdPickupTotem(GameObject totemObject)
    {
        NetworkTotemController totem = totemObject.GetComponent<NetworkTotemController>();
        if (totem != null)
        {
            bool success = totem.TryPickUp(netIdentity);
            if (success)
            {
                currentTotem = totem;
                isCarryingTotem = true;
                syncIsCarrying = true;
                RpcOnPickupTotem();
            }
        }
    }
    
    [ClientRpc]
    private void RpcOnPickupTotem()
    {
        if (isLocalPlayer)
        {
            Debug.Log("Вы подобрали тотем!");
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
    
    private NetworkTotemController FindClosestTotem()
    {
        var totems = FindObjectsByType<NetworkTotemController>(FindObjectsSortMode.None);
        float closestDistance = float.MaxValue;
        NetworkTotemController closestTotem = null;
        
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
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        float moveSpeedAnimation = Mathf.Clamp01(moveDirection.magnitude);
        animator.SetFloat("Speed", moveSpeedAnimation);
        animator.SetBool("IsCarrying", syncIsCarrying);
    }
    
    private void OnIsCarryingChanged(bool oldValue, bool newValue)
    {
        syncIsCarrying = newValue;
        
        if (animator != null)
        {
            animator.SetBool("IsCarrying", newValue);
        }
        
        isCarryingTotem = newValue;
    }
    
    private void OnPlayerNameChanged(string oldName, string newName)
    {
        playerName = newName;
        gameObject.name = newName;
    }
    
    private void TogglePause()
    {
        if (!isLocalPlayer) return;
        
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
        if (!isLocalPlayer) return;
        
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
    
    public void OnPlayerDeath()
    {
        if (currentTotem != null)
        {
            CmdForceDropTotem();
        }
        
        EnableControls(false);
        
        if (playerCombat != null)
            playerCombat.enabled = false;
        if (aimingSystem != null)
            aimingSystem.enabled = false;
            
        if (isLocalPlayer)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    [Command]
    private void CmdForceDropTotem()
    {
        if (currentTotem != null)
        {
            currentTotem.DropTotem(true, Vector3.up * 2f + transform.forward * 3f);
            currentTotem = null;
            isCarryingTotem = false;
            syncIsCarrying = false;
        }
    }
}