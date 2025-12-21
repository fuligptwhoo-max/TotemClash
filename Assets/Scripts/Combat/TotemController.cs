using UnityEngine;

public class TotemController : MonoBehaviour
{
    [Header("Totem Settings")]
    public float pickUpRange = 2f;
    public float dropForce = 5f;
    
    [Header("Scoring")]
    public int baseScorePerSecond = 1;
    public float scoreMultiplierIncrease = 0.1f;
    public float maxMultiplier = 3f;
    
    // Состояние тотема
    private Transform currentCarrier = null;
    private bool isBeingCarried = false;
    private float carryTime = 0f;
    private float currentMultiplier = 1f;
    private Transform previousParent;
    private Vector3 previousScale;
    
    // Компоненты
    private Rigidbody rb;
    private Collider totemCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        totemCollider = GetComponent<Collider>();
    }
    
    private void Update()
    {
        if (isBeingCarried && currentCarrier != null)
        {
            // Обновляем время ношения
            carryTime += Time.deltaTime;
            currentMultiplier = Mathf.Min(1f + (carryTime * scoreMultiplierIncrease), maxMultiplier);
            
            // Позиция обновляется автоматически через parent-child связь
        }
    }
    
    private void LateUpdate()
    {
        // Гарантируем, что тотем точно на месте в конце кадра
        if (isBeingCarried && currentCarrier != null)
        {
            // Ищем кость для ношения на спине игрока
            PlayerTotemInteraction playerInteraction = currentCarrier.GetComponent<PlayerTotemInteraction>();
            if (playerInteraction != null && playerInteraction.carryBone != null)
            {
                // Используем точную позицию кости
                transform.position = playerInteraction.carryBone.position;
                transform.rotation = playerInteraction.carryBone.rotation;
            }
            else
            {
                // Запасной вариант: точная позиция сзади игрока
                Vector3 targetPosition = currentCarrier.position - currentCarrier.forward * 0.525f + Vector3.up * 1.5f;
                transform.position = targetPosition;
                transform.rotation = currentCarrier.rotation;
            }
        }
    }
    
    // Вызывается игроком для подбора
    public bool TryPickUp(Transform carrier)
    {
        if (isBeingCarried) return false;
        
        // Проверяем расстояние
        float distance = Vector3.Distance(transform.position, carrier.position);
        if (distance > pickUpRange) return false;
        
        // Сохраняем предыдущий parent и масштаб
        previousParent = transform.parent;
        previousScale = transform.localScale;
        
        // Подбираем тотем
        currentCarrier = carrier;
        isBeingCarried = true;
        carryTime = 0f;
        currentMultiplier = 1f;
        
        // Отключаем физику
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        
        if (totemCollider != null) totemCollider.enabled = false;
        
        // Сразу устанавливаем позицию на спине
        PlayerTotemInteraction playerInteraction = carrier.GetComponent<PlayerTotemInteraction>();
        if (playerInteraction != null && playerInteraction.carryBone != null)
        {
            // Делаем дочерним объектом кости
            transform.SetParent(playerInteraction.carryBone, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            // Альтернатива: делаем дочерним объектом игрока
            transform.SetParent(carrier, true);
        }
        
        Debug.Log($"{carrier.name} подобрал тотем!");
        return true;
    }
    
    // Вызывается когда игрок умирает или сбрасывает тотем
    public void DropTotem(bool applyForce = false, Vector3 forceDirection = default)
    {
        if (!isBeingCarried) return;
        
        // Возвращаем оригинальный parent и масштаб
        transform.SetParent(previousParent, true);
        transform.localScale = previousScale;
        
        // Включаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            
            // Применяем силу только если нужно (при смерти)
            if (applyForce && forceDirection != Vector3.zero)
            {
                rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
            }
        }
        
        if (totemCollider != null) totemCollider.enabled = true;
        
        // Сбрасываем состояние
        currentCarrier = null;
        isBeingCarried = false;
        carryTime = 0f;
        currentMultiplier = 1f;
        
        if (applyForce)
        {
            Debug.Log("Тотем выбит при смерти игрока!");
        }
        else
        {
            Debug.Log("Тотем сброшен!");
        }
    }
    
    public void ForceDropTotem()
    {
        DropTotem(true, Vector3.up * 2f);
    }
    
    public int CalculateScore()
    {
        return Mathf.RoundToInt(baseScorePerSecond * currentMultiplier);
    }
    
    public Transform GetCurrentCarrier()
    {
        return currentCarrier;
    }
    
    public bool IsBeingCarried()
    {
        return isBeingCarried;
    }
    
    public float GetCarryMultiplier()
    {
        return currentMultiplier;
    }
}