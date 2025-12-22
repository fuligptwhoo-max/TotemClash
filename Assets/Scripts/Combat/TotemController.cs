using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class TotemController : NetworkBehaviour
{
    [Header("Totem Settings")]
    public float pickUpRange = 2f;
    public float dropForce = 5f;
    
    [Header("Scoring")]
    public float scoreMultiplierIncrease = 0.1f;
    public float maxMultiplier = 3f;
    
    // Синхронизированные состояния
    [SyncVar(hook = nameof(OnCarrierChanged))]
    private uint currentCarrierId = 0;
    
    [SyncVar]
    private bool isBeingCarriedSync = false;
    
    // Локальные состояния
    private float carryTime = 0f;
    private float currentMultiplier = 1f;
    
    // Компоненты
    private Rigidbody rb;
    private Collider totemCollider;
    private GameObject currentCarrierObject;
    
    // Статический список всех тотемов
    private static List<TotemController> allTotems = new List<TotemController>();
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        totemCollider = GetComponent<Collider>();
        
        // Добавляем в список
        if (!allTotems.Contains(this))
            allTotems.Add(this);
    }
    
    private void Update()
    {
        if (isServer && isBeingCarriedSync)
        {
            carryTime += Time.deltaTime;
            currentMultiplier = Mathf.Min(1f + (carryTime * scoreMultiplierIncrease), maxMultiplier);
        }
        
        // Плавное следование за носителем
        if (isBeingCarriedSync && currentCarrierObject != null)
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
    public void ServerPickUp(uint carrierId)
    {
        if (currentCarrierId != 0 || isBeingCarriedSync) 
        {
            Debug.Log($"[SERVER] Тотем уже у игрока {currentCarrierId}");
            return;
        }
        
        currentCarrierId = carrierId;
        isBeingCarriedSync = true;
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
        
        Debug.Log($"[SERVER] Тотем поднят игроком {carrierId}");
    }
    
    // ТОЛЬКО на сервере вызывается, когда игрок хочет бросить тотем
    [Server]
    public void ServerDrop(uint carrierId, bool applyForce = false)
    {
        if (currentCarrierId != carrierId) 
        {
            Debug.Log($"[SERVER] Игрок {carrierId} не несет тотем");
            return;
        }
        
        currentCarrierId = 0;
        isBeingCarriedSync = false;
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
        
        Debug.Log($"[SERVER] Тотем сброшен игроком {carrierId}");
    }
    
    // Хук при изменении носителя
    private void OnCarrierChanged(uint oldCarrierId, uint newCarrierId)
    {
        Debug.Log($"[HOOK] Carrier changed from {oldCarrierId} to {newCarrierId}");
        
        if (newCarrierId == 0)
        {
            currentCarrierObject = null;
        }
        else if (NetworkClient.spawned.TryGetValue(newCarrierId, out NetworkIdentity identity))
        {
            currentCarrierObject = identity.gameObject;
            
            // На клиентах отключаем физику при подборе
            if (!isServer && rb != null)
            {
                rb.isKinematic = true;
            }
        }
    }
    
    // Геттеры
    public bool IsBeingCarried()
    {
        return currentCarrierId != 0;
    }
    
    public uint GetCarrierId()
    {
        return currentCarrierId;
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
    
    private void OnDestroy()
    {
        allTotems.Remove(this);
    }
}