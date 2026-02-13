using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class PlayerTotemInteraction : NetworkBehaviour
{
    [Header("Totem Interaction")]
    public float interactionRange = 2f;
    
    [Header("Carry Settings")]
    public Transform carryBone;
    
    // Ссылки
    private NetworkPlayerController playerController;
    
    // FishNet 4.x SyncVar - свойство
    public readonly SyncVar<bool> IsCarrying = new SyncVar<bool>(false);
    
    // Кэш тотема который несём
    private TotemController carriedTotem = null;

    public override void OnStartClient()
    {
        base.OnStartClient();
        playerController = GetComponent<NetworkPlayerController>();
        
        // Подписываемся на изменения
        IsCarrying.OnChange += OnCarryingStateChanged;
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        IsCarrying.OnChange -= OnCarryingStateChanged;
    }
    
    private void Update()
    {
        // Только для локального игрока
        if (!base.IsOwner) return;
        
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (IsCarrying.Value)
            {
                CmdDropTotem();
            }
            else
            {
                CmdTryPickUp();
            }
        }
    }
    
    /// <summary>
    /// Обработчик изменения SyncVar
    /// </summary>
    private void OnCarryingStateChanged(bool prev, bool next, bool asServer)
    {
        // Обновляем анимацию
        if (playerController != null)
        {
            playerController.UpdateCarryingAnimation(next);
        }
        
        if (base.IsOwner)
        {
            Debug.Log($"[CLIENT] Carrying state changed: {prev} -> {next}");
        }
    }

    [ServerRpc]
    public void CmdTryPickUp()
    {
        if (IsCarrying.Value) 
        {
            Debug.Log($"[SERVER] Player {base.ObjectId} already carrying totem");
            return;
        }
        
        TotemController closestTotem = TotemController.GetClosestTotem(transform.position, interactionRange);
        if (closestTotem != null && !closestTotem.IsBeingCarried())
        {
            // Поднимаем тотем на сервере
            closestTotem.ServerPickUp(base.ObjectId);
            
            // Устанавливаем состояние у игрока
            IsCarrying.Value = true;
            carriedTotem = closestTotem;
            
            // Уведомляем клиента
            TargetOnPickedUp(base.Owner);
            
            // Обновляем состояние в контроллере
            if (playerController != null)
            {
                playerController.SetCarryingState(true);
            }
            
            Debug.Log($"[SERVER] Player {base.ObjectId} picked up totem");
        }
        else
        {
            Debug.Log($"[SERVER] No available totems in range");
        }
    }

    [TargetRpc]
    private void TargetOnPickedUp(NetworkConnection target)
    {
        Debug.Log("[CLIENT] You picked up the totem!");
    }

    [ServerRpc]
    public void CmdDropTotem()
    {
        if (!IsCarrying.Value) 
        {
            Debug.Log($"[SERVER] Player {base.ObjectId} is not carrying totem");
            return;
        }
        
        // Ищем тотем, который несем
        if (carriedTotem != null)
        {
            carriedTotem.ServerDrop(base.ObjectId, false);
        }
        else
        {
            // Fallback - ищем по ObjectId
            TotemController[] allTotems = FindObjectsByType<TotemController>(FindObjectsSortMode.None);
            foreach (var totem in allTotems)
            {
                if (totem.GetCarrierId() == base.ObjectId)
                {
                    totem.ServerDrop(base.ObjectId, false);
                    break;
                }
            }
        }
        
        // Сбрасываем состояние
        IsCarrying.Value = false;
        carriedTotem = null;
        
        // Обновляем состояние в контроллере
        if (playerController != null)
        {
            playerController.SetCarryingState(false);
        }
        
        // Уведомляем клиента
        TargetOnDropped(base.Owner);
        
        Debug.Log($"[SERVER] Player {base.ObjectId} dropped totem");
    }

    [TargetRpc]
    private void TargetOnDropped(NetworkConnection target)
    {
        Debug.Log("[CLIENT] You dropped the totem!");
    }

    // Вызывается при смерти игрока
    [Server]
    public void OnPlayerDeath()
    {
        if (IsCarrying.Value)
        {
            // Сбрасываем тотем
            if (carriedTotem != null)
            {
                carriedTotem.ServerDrop(base.ObjectId, true);
            }
            else
            {
                TotemController[] allTotems = FindObjectsByType<TotemController>(FindObjectsSortMode.None);
                foreach (var totem in allTotems)
                {
                    if (totem.GetCarrierId() == base.ObjectId)
                    {
                        totem.ServerDrop(base.ObjectId, true);
                        break;
                    }
                }
            }
            
            IsCarrying.Value = false;
            carriedTotem = null;
            
            if (playerController != null)
            {
                playerController.SetCarryingState(false);
            }
        }
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        
        // Сбрасываем тотем при отключении
        if (IsCarrying.Value)
        {
            OnPlayerDeath();
        }
    }
}
