using UnityEngine;

public class AIBotController : MonoBehaviour
{
    [Header("AI Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float attackRange = 10f;
    public float chaseRange = 20f;
    
    [Header("References")]
    public Animator animator;
    public CharacterController characterController;
    public HealthSystem healthSystem;
    public PlayerCombat playerCombat;
    
    private Transform targetPlayer;
    private Vector3 moveDirection = Vector3.zero;
    
    private void Start()
    {
        // Находим ближайшего игрока
        FindNearestPlayer();
    }
    
    private void Update()
    {
        if (healthSystem != null && healthSystem.currentHealth.Value <= 0) return;
        
        if (targetPlayer == null)
        {
            FindNearestPlayer();
            return;
        }
        
        float distanceToTarget = Vector3.Distance(transform.position, targetPlayer.position);
        
        if (distanceToTarget <= attackRange)
        {
            // Атакуем
            moveDirection = Vector3.zero;
            Attack();
        }
        else if (distanceToTarget <= chaseRange)
        {
            // Преследуем
            Vector3 direction = (targetPlayer.position - transform.position).normalized;
            direction.y = 0;
            moveDirection = direction;
            
            if (direction.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Патрулируем или ищем нового игрока
            moveDirection = Vector3.zero;
            FindNearestPlayer();
        }
        
        // Двигаемся
        if (characterController != null && characterController.enabled && moveDirection.magnitude > 0.1f)
        {
            Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
            characterController.Move(movement);
        }
        
        // Обновляем анимации
        UpdateAnimations();
    }
    
    private void FindNearestPlayer()
    {
        NetworkPlayerController[] players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        float closestDistance = Mathf.Infinity;
        NetworkPlayerController closestPlayer = null;
        
        foreach (var player in players)
        {
            if (player == null || player.gameObject == gameObject) continue;
            
            // Проверяем что игрок "живой" (не мертв)
            HealthSystem playerHealth = player.GetComponent<HealthSystem>();
            if (playerHealth != null && playerHealth.currentHealth.Value <= 0) continue;
            
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player;
            }
        }
        
        if (closestPlayer != null)
        {
            targetPlayer = closestPlayer.transform;
        }
    }
    
    private void Attack()
    {
        if (playerCombat != null && targetPlayer != null)
        {
            // Простая атака в направлении цели
            playerCombat.PrimaryAttack(targetPlayer.position);
        }
    }
    
    private void UpdateAnimations()
    {
        if (animator == null) return;
        
        float moveSpeedAnimation = Mathf.Clamp01(moveDirection.magnitude);
        animator.SetFloat("Speed", moveSpeedAnimation);
    }
}
