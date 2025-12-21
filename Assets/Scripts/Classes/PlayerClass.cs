using UnityEngine;

public abstract class PlayerClass : MonoBehaviour
{
    protected PlayerController playerController;
    protected PlayerCombat playerCombat;
    protected Animator animator;
    
    public virtual void Initialize(PlayerController controller, PlayerCombat combat, Animator anim)
    {
        playerController = controller;
        playerCombat = combat;
        animator = anim;
    }
    
    public virtual bool PrimaryAttack(Vector3 targetPosition)
    {
        return false;
    }
    
    public virtual bool Ability1(Vector3 targetPosition)
    {
        return false;
    }
    
    public virtual bool Ability2(Vector3 targetPosition)
    {
        return false;
    }
    
    public virtual bool UltimateAbility(Vector3 targetPosition)
    {
        return false;
    }
    
    public virtual void UpdateClass()
    {
        // Переопределяется в наследниках
    }
    
    public virtual float GetCooldownProgress(int abilityIndex)
    {
        return 0f;
    }
    
    public virtual bool IsAbilityReady(int abilityIndex)
    {
        return false;
    }
}