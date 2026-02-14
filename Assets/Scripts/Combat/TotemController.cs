using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class TotemController : NetworkBehaviour
{
    [Header("Totem Settings")]
    public float pickUpRange = 2f;
    public float dropForce = 5f;
    
    [Header("Scoring")]
    public float scoreMultiplierIncrease = 0.1f;
    public float maxMultiplier = 3f;
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<int> currentCarrierId = new SyncVar<int>(0);
    public readonly SyncVar<bool> isBeingCarriedSync = new SyncVar<bool>(false);
    
    // Локальные состояния
    private float carryTime = 0f;
    private float currentMultiplier = 1f;
    
    // Компоненты
    private Rigidbody rb;
    private Collider totemCollider;
    private GameObject currentCarrierObject;
    
    // Статический список всех тотемов
    private static List<TotemController> allTotems = new List<TotemController>();
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        rb = GetComponent<Rigidbody>();
        totemCollider = GetComponent<Collider>();
        
        // Подписываемся на изменения
        currentCarrierId.OnChange += OnCarrierChanged;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        currentCarrierId.OnChange -= OnCarrierChanged;
        allTotems.Remove(this);
    }
    
    private void OnEnable()
    {
        // Добавляем в список
        if (!allTotems.Contains(this))
            allTotems.Add(this);
    }
    
    private void OnDisable()
    {
        allTotems.Remove(this);
    }
    
    private void Update()
    {
        if (base.IsServerInitialized && isBeingCarriedSync.Value)
        {
            carryTime += Time.deltaTime;
            currentMultiplier = Mathf.Min(1f + (carryTime * scoreMultiplierIncrease), maxMultiplier);
        }
        
        // Плавное следование за носителем
        if (isBeingCarriedSync.Value && currentCarrierObject != null)
        {
            UpdateCarriedPosition();
        }
    }
    
    private void UpdateCarriedPosition()
    {
        if (currentCarrierObject == null) return;
        
        Vector3 targetPos;
        Quaternion targetRot;
        
        PlayerTotemInteraction playerInteraction = currentCarrierObject.GetComponent<PlayerTotemInteraction>();
        
        if (playerInteraction != null && playerInteraction.carryBone != null)
        {
            targetPos = playerInteraction.carryBone.position;
            targetRot = playerInteraction.carryBone.rotation;
        }
        else
        {
            targetPos = currentCarrierObject.transform.position + 
                       currentCarrierObject.transform.up * 1.5f - 
                       currentCarrierObject.transform.forward * 0.5f;
            targetRot = currentCarrierObject.transform.rotation;
        }
        
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }
    
    // ТОЛЬКО на сервере вызывается, когда игрок хочет поднять тотем
    [Server]
    public void ServerPickUp(int carrierId)
    {
        if (currentCarrierId.Value != 0 || isBeingCarriedSync.Value) 
        {
            Debug.Log($"[SERVER] Totem already carried by player {currentCarrierId.Value}");
            return;
        }
        
        currentCarrierId.Value = carrierId;
        isBeingCarriedSync.Value = true;
        carryTime = 0f;
        currentMultiplier = 1f;
        
        // Отключаем физику
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        if (totemCollider != null)
        {
            totemCollider.enabled = false;
        }
        
        Debug.Log($"[SERVER] Totem picked up by player {carrierId}");
    }
    
    // ТОЛЬКО на сервере вызывается, когда игрок хочет бросить тотем
    [Server]
    public void ServerDrop(int carrierId, bool applyForce = false)
    {
        if (currentCarrierId.Value != carrierId) 
        {
            Debug.Log($"[SERVER] Player {carrierId} is not carrying this totem");
            return;
        }
        
        currentCarrierId.Value = 0;
        isBeingCarriedSync.Value = false;
        carryTime = 0f;
        currentMultiplier = 1f;
        
        // Включаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            
            if (applyForce)
            {
                Vector3 forceDirection = Vector3.up * 2f + Random.insideUnitSphere * 3f;
                rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
            }
        }
        
        if (totemCollider != null)
        {
            totemCollider.enabled = true;
        }
        
        Debug.Log($"[SERVER] Totem dropped by player {carrierId}");
    }
    
    // Хук при изменении носителя
    private void OnCarrierChanged(int prevCarrierId, int newCarrierId, bool asServer)
    {
        Debug.Log($"[HOOK] Carrier changed from {prevCarrierId} to {newCarrierId}");
        
        if (newCarrierId == 0)
        {
            currentCarrierObject = null;
            
            // На клиентах включаем физику при сбросе
            if (!base.IsServerInitialized && rb != null)
            {
                rb.isKinematic = false;
            }
        }
        else
        {
            // Находим объект игрока по ObjectId
            NetworkObject playerObject = FindPlayerById(newCarrierId);
            if (playerObject != null)
            {
                currentCarrierObject = playerObject.gameObject;
                
                // На клиентах отключаем физику при подборе
                if (!base.IsServerInitialized && rb != null)
                {
                    rb.isKinematic = true;
                }
            }
        }
    }
    
    /// <summary>
    /// Находит игрока по ObjectId
    /// </summary>
    private NetworkObject FindPlayerById(int objectId)
    {
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.ObjectId == objectId)
                return netObj;
        }
        return null;
    }
    
    // Геттеры
    public bool IsBeingCarried()
    {
        return currentCarrierId.Value != 0;
    }
    
    public int GetCarrierId()
    {
        return currentCarrierId.Value;
    }
    
    public float GetCarryMultiplier()
    {
        return currentMultiplier;
    }
    
    // Статические методы для поиска тотемов
    public static TotemController GetClosestTotem(Vector3 position, float maxRange)
    {
        TotemController closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (var totem in allTotems)
        {
            if (totem == null) continue;
            if (totem.IsBeingCarried()) continue;
            
            float distance = Vector3.Distance(position, totem.transform.position);
            if (distance <= maxRange && distance < closestDistance)
            {
                closestDistance = distance;
                closest = totem;
            }
        }
        
        return closest;
    }
    
    /// <summary>
    /// Сбрасывает тотем в начальное состояние (вызывается при рестарте игры)
    /// </summary>
    [Server]
    public void ResetTotem()
    {
        // Сбрасываем носителя
        currentCarrierId.Value = 0;
        isBeingCarriedSync.Value = false;
        carryTime = 0f;
        currentMultiplier = 1f;
        currentCarrierObject = null;
        
        // Включаем физику
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        if (totemCollider != null)
        {
            totemCollider.enabled = true;
        }
        
        Debug.Log("[TotemController] Totem reset to default state");
    }
    
}
