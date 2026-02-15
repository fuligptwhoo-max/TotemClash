using UnityEngine;
using UnityEngine.Events;

namespace TotemClash.Combat
{
    /// <summary>
    /// Локальная версия взаимодействия с тотемом для одиночной игры
    /// Не использует сетевую синхронизацию - вся логика выполняется локально
    /// </summary>
    public class PlayerTotemInteraction : MonoBehaviour
{
    [Header("Totem Interaction")]
    public float interactionRange = 2f;
    
    [Header("Carry Settings")]
    public Transform carryBone;
    
    [Header("Events")]
    public UnityEvent<bool> OnCarryingStateChanged = new UnityEvent<bool>();
    public UnityEvent OnPickedUp = new UnityEvent();
    public UnityEvent OnDropped = new UnityEvent();
    
    // Состояние
    private bool isCarrying = false;
    private TotemController carriedTotem = null;
    
    // Ссылки
    private PlayerController playerController;
    
    // Публичное свойство для чтения состояния
    public bool IsCarrying => isCarrying;
    
    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }
    
    private void OnEnable()
    {
        // Подписываемся на события если нужно
    }
    
    private void OnDisable()
    {
        // Отписываемся от событий
        OnCarryingStateChanged.RemoveAllListeners();
        OnPickedUp.RemoveAllListeners();
        OnDropped.RemoveAllListeners();
    }
    
    /// <summary>
    /// Пытается поднять тотем в радиусе действия
    /// Вызывается из PlayerController при нажатии клавиши E
    /// </summary>
    public void TryPickUp()
    {
        if (isCarrying) 
        {
            Debug.Log("[PlayerTotemInteraction] Already carrying totem");
            return;
        }
        
        // Ищем ближайший тотем
        TotemController closestTotem = TotemController.FindClosestInRange(transform.position);
        
        if (closestTotem != null && !closestTotem.IsBeingCarried())
        {
            // Поднимаем тотем
            bool pickedUp = closestTotem.PickUp(gameObject);
            
            if (pickedUp)
            {
                isCarrying = true;
                carriedTotem = closestTotem;
                
                // Обновляем анимацию
                if (playerController != null)
                {
                    playerController.UpdateCarryingAnimation(true);
                }
                
                // Вызываем события
                OnCarryingStateChanged?.Invoke(true);
                OnPickedUp?.Invoke();
                
                Debug.Log("[PlayerTotemInteraction] Picked up totem");
            }
        }
        else
        {
            Debug.Log("[PlayerTotemInteraction] No available totems in range");
        }
    }
    
    /// <summary>
    /// Бросает тотем
    /// Вызывается из PlayerController при нажатии клавиши E (если несем) или G
    /// </summary>
    public void DropTotem()
    {
        if (!isCarrying) 
        {
            Debug.Log("[PlayerTotemInteraction] Not carrying totem");
            return;
        }
        
        // Бросаем тотем
        if (carriedTotem != null)
        {
            carriedTotem.Drop(false);
        }
        
        // Сбрасываем состояние
        isCarrying = false;
        carriedTotem = null;
        
        // Обновляем анимацию
        if (playerController != null)
        {
            playerController.UpdateCarryingAnimation(false);
        }
        
        // Вызываем события
        OnCarryingStateChanged?.Invoke(false);
        OnDropped?.Invoke();
        
        Debug.Log("[PlayerTotemInteraction] Dropped totem");
    }
    
    /// <summary>
    /// Вызывается при смерти игрока - сбрасывает тотем с силой
    /// </summary>
    public void OnPlayerDeath()
    {
        if (isCarrying)
        {
            // Бросаем тотем с силой (эффект "выпадения" при смерти)
            if (carriedTotem != null)
            {
                carriedTotem.Drop(true);
            }
            
            // Сбрасываем состояние
            isCarrying = false;
            carriedTotem = null;
            
            // Обновляем анимацию
            if (playerController != null)
            {
                playerController.UpdateCarryingAnimation(false);
            }
            
            // Вызываем события
            OnCarryingStateChanged?.Invoke(false);
            OnDropped?.Invoke();
            
            Debug.Log("[PlayerTotemInteraction] Dropped totem on death");
        }
    }
    
    /// <summary>
    /// Возвращает текущий тотем который несет игрок
    /// </summary>
    public TotemController GetCarriedTotem()
    {
        return carriedTotem;
    }
    
    /// <summary>
    /// Получает точку крепления для тотема
    /// </summary>
    public Transform GetCarryBone()
    {
        return carryBone;
    }
}
}
