using UnityEngine;

public class PlayerTotemInteraction : MonoBehaviour
{
    [Header("Totem Interaction")]
    public float interactionRange = 2f;
    
    [Header("Carry Settings")]
    public Transform carryBone;
    
    private TotemController currentTotem = null;
    private bool isCarryingTotem = false;
    
    public bool IsCarrying
    {
        get { return isCarryingTotem; }
    }
    
    public void TryPickUp()
    {
        if (isCarryingTotem)
        {
            DropTotem();
            return;
        }
        
        TotemController closestTotem = FindClosestTotem();
        
        if (closestTotem != null)
        {
            bool success = closestTotem.TryPickUp(transform);
            if (success)
            {
                currentTotem = closestTotem;
                isCarryingTotem = true;
                Debug.Log($"{gameObject.name} теперь несет тотем!");
                
            }
        }
        else
        {
            Debug.Log("Нет тотемов в радиусе подбора!");
        }
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
            if (distance <= interactionRange && distance < closestDistance)
            {
                closestDistance = distance;
                closestTotem = totem;
            }
        }
        
        return closestTotem;
    }
    
    public void DropTotem()
    {
        if (currentTotem != null)
        {
            currentTotem.DropTotem(false);
            currentTotem = null;
            isCarryingTotem = false;
        }
    }
    
    public void OnPlayerDeath()
    {
        if (isCarryingTotem && currentTotem != null)
        {
            currentTotem.DropTotem(true, Vector3.up * 2f + transform.forward * 3f);
            currentTotem = null;
            isCarryingTotem = false;
        }
    }
    
    private void OnDestroy()
    {
        if (isCarryingTotem)
        {
            OnPlayerDeath();
        }
    }
}