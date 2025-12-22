using UnityEngine;
using Mirror;

public class PlayerCombat : NetworkBehaviour
{
    [Header("Combat References")]
    public Animator animator;
    public NetworkPlayerController playerController;
    
    [Header("Class")]
    public MagicianClass magicianClass;
    
    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<NetworkPlayerController>();
            
        if (magicianClass == null)
            magicianClass = GetComponent<MagicianClass>();
            
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
    
    private void Start()
    {
        if (magicianClass != null && playerController != null && animator != null)
        {
            magicianClass.Initialize(playerController, this, animator);
        }
    }
    
    public bool PrimaryAttack(Vector3 targetPosition)
    {
        if (!isLocalPlayer || magicianClass == null) return false;
        
        return magicianClass.PrimaryAttack(targetPosition);
    }
    
    public bool UseAbility(int abilityIndex, Vector3 targetPosition)
    {
        if (!isLocalPlayer || magicianClass == null) return false;
        
        switch (abilityIndex)
        {
            case 0: return magicianClass.Ability1(targetPosition);
            case 1: return magicianClass.Ability2(targetPosition);
            default: return false;
        }
    }
    
    public bool UseUltimate(Vector3 targetPosition)
    {
        if (!isLocalPlayer || magicianClass == null) return false;
        
        return magicianClass.UltimateAbility(targetPosition);
    }
    
    public float GetCooldownProgress(int abilityIndex)
    {
        return magicianClass != null ? magicianClass.GetCooldownProgress(abilityIndex) : 0f;
    }
    
    public bool IsAbilityReady(int abilityIndex)
    {
        return magicianClass != null ? magicianClass.IsAbilityReady(abilityIndex) : false;
    }
}