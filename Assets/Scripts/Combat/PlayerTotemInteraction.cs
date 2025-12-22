using UnityEngine;
using Mirror;

public class PlayerTotemInteraction : NetworkBehaviour
{
    [Header("Totem Interaction")]
    public float interactionRange = 2f;
    
    [Header("Carry Settings")]
    public Transform carryBone;
    
    // Ссылки
    private NetworkPlayerController playerController;
    
    public bool IsCarrying { get; private set; } = false;
    
    private void Start()
    {
        playerController = GetComponent<NetworkPlayerController>();
    }
    
    private void Update()
    {
        if (!isLocalPlayer) return;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsCarrying)
            {
                CmdDropTotem();
            }
            else
            {
                CmdTryPickUp();
            }
        }
    }
    
    [Command]
    public void CmdTryPickUp()
    {
        if (IsCarrying) 
        {
            Debug.Log($"[SERVER] Игрок {netId} уже несет тотем");
            return;
        }
        
        TotemController closestTotem = TotemController.GetClosestTotem(transform.position, interactionRange);
        if (closestTotem != null && !closestTotem.IsBeingCarried())
        {
            // ПОДНИМАЕМ ТОТЕМ НА СЕРВЕРЕ
            closestTotem.ServerPickUp(netId);
            
            // Устанавливаем состояние у игрока
            IsCarrying = true;
            
            // Уведомляем клиента
            TargetOnPickedUp(connectionToClient);
            
            Debug.Log($"[SERVER] Игрок {netId} теперь несет тотем");
        }
        else
        {
            Debug.Log($"[SERVER] Нет доступных тотемов в радиусе");
        }
    }
    
    [TargetRpc]
    private void TargetOnPickedUp(NetworkConnection target)
    {
        if (isLocalPlayer)
        {
            IsCarrying = true;
            Debug.Log("[CLIENT] Вы подняли тотем!");
            
            if (playerController != null)
            {
                playerController.UpdateCarryingAnimation(true);
            }
        }
    }
    
    [Command]
    public void CmdDropTotem()
    {
        if (!IsCarrying) 
        {
            Debug.Log($"[SERVER] Игрок {netId} не несет тотем");
            return;
        }
        
        // Ищем тотем, который несем
        TotemController[] allTotems = FindObjectsByType<TotemController>(FindObjectsSortMode.None);
        foreach (var totem in allTotems)
        {
            if (totem.GetCarrierId() == netId)
            {
                totem.ServerDrop(netId, false);
                break;
            }
        }
        
        // Сбрасываем состояние
        IsCarrying = false;
        
        // Уведомляем клиента
        TargetOnDropped(connectionToClient);
        
        Debug.Log($"[SERVER] Игрок {netId} сбросил тотем");
    }
    
    [TargetRpc]
    private void TargetOnDropped(NetworkConnection target)
    {
        if (isLocalPlayer)
        {
            IsCarrying = false;
            Debug.Log("[CLIENT] Вы сбросили тотем!");
            
            if (playerController != null)
            {
                playerController.UpdateCarryingAnimation(false);
            }
        }
    }
    
    // Вызывается при смерти игрока
    [Server]
    public void OnPlayerDeath()
    {
        if (IsCarrying)
        {
            // Ищем тотем, который несем
            TotemController[] allTotems = FindObjectsByType<TotemController>(FindObjectsSortMode.None);
            foreach (var totem in allTotems)
            {
                if (totem.GetCarrierId() == netId)
                {
                    totem.ServerDrop(netId, true);
                    break;
                }
            }
            
            IsCarrying = false;
        }
    }
}