using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat References")]
    public PlayerClass currentClass;
    public Animator animator;
    public PlayerController playerController;
    
    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
            
        if (currentClass == null)
            currentClass = GetComponent<PlayerClass>();
            
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
    
    private void Start()
    {
        if (currentClass != null && playerController != null && animator != null)
        {
            currentClass.Initialize(playerController, this, animator);
        }
    }
    
    public bool PrimaryAttack(Vector3 targetPosition)
    {
        if (currentClass == null)
        {
            Debug.LogWarning("PlayerCombat: currentClass is null!");
            return false;
        }
        
        return currentClass.PrimaryAttack(targetPosition);
    }
    
    public bool UseAbility(int abilityIndex, Vector3 targetPosition)
    {
        if (currentClass == null) return false;
        
        switch (abilityIndex)
        {
            case 0:
                return currentClass.Ability1(targetPosition);
            case 1:
                return currentClass.Ability2(targetPosition);
            default:
                return false;
        }
    }
    
    public bool UseUltimate(Vector3 targetPosition)
    {
        if (currentClass != null)
        {
            return currentClass.UltimateAbility(targetPosition);
        }
        return false;
    }
    
    private void Update()
    {
        // Логика обновления не требуется
    }
    
    public float GetCooldownProgress(int abilityIndex)
    {
        return currentClass != null ? currentClass.GetCooldownProgress(abilityIndex) : 0f;
    }
    
    public bool IsAbilityReady(int abilityIndex)
    {
        return currentClass != null ? currentClass.IsAbilityReady(abilityIndex) : false;
    }
}