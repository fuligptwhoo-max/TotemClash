using UnityEngine;
using Mirror;

public class NetworkTotemController : NetworkBehaviour
{
    [Header("Totem Settings")]
    public float pickUpRange = 2f;
    public float dropForce = 5f;
    
    [Header("Scoring")]
    public int baseScorePerSecond = 1;
    public float scoreMultiplierIncrease = 0.1f;
    public float maxMultiplier = 3f;
    
    [Header("Carry Position")]
    public Vector3 carryOffset = new Vector3(0f, 1.5f, -0.5f);
    
    [SyncVar(hook = nameof(OnCarrierChanged))]
    private NetworkIdentity currentCarrier;
    
    [SyncVar]
    private bool isBeingCarried = false;
    
    [SyncVar]
    private float carryTime = 0f;
    
    [SyncVar]
    private float currentMultiplier = 1f;
    
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private Transform originalParent;
    
    private Rigidbody rb;
    private Collider totemCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        totemCollider = GetComponent<Collider>();
        
        // Сохраняем оригинальные трансформы
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;
        originalParent = transform.parent;
    }
    
    [Server]
    private void Update()
    {
        if (isBeingCarried && currentCarrier != null)
        {
            carryTime += Time.deltaTime;
            currentMultiplier = Mathf.Min(1f + (carryTime * scoreMultiplierIncrease), maxMultiplier);
            
            UpdatePositionOnServer();
        }
    }
    
    [Server]
    private void UpdatePositionOnServer()
    {
        if (currentCarrier != null)
        {
            // Рассчитываем позицию на спине игрока
            Vector3 carryPosition = currentCarrier.transform.position + 
                                   currentCarrier.transform.forward * carryOffset.z + 
                                   currentCarrier.transform.right * carryOffset.x + 
                                   Vector3.up * carryOffset.y;
            
            transform.position = carryPosition;
            
            // Поворачиваем тотем так же как игрок
            transform.rotation = currentCarrier.transform.rotation;
            
            // Сохраняем оригинальный масштаб
            transform.localScale = originalScale;
        }
    }
    
    [Server]
    public bool TryPickUp(NetworkIdentity carrier)
    {
        if (isBeingCarried) return false;
        
        float distance = Vector3.Distance(transform.position, carrier.transform.position);
        if (distance > pickUpRange) return false;
        
        currentCarrier = carrier;
        isBeingCarried = true;
        carryTime = 0f;
        currentMultiplier = 1f;
        
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
        
        if (totemCollider != null) totemCollider.enabled = false;
        
        Debug.Log($"{carrier.name} подобрал тотем!");
        return true;
    }
    
    [Server]
    public void DropTotem(bool applyForce = false, Vector3 forceDirection = default)
    {
        if (!isBeingCarried) return;
        
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            
            if (applyForce && forceDirection != Vector3.zero)
            {
                rb.AddForce(forceDirection * dropForce, ForceMode.Impulse);
            }
        }
        
        if (totemCollider != null) totemCollider.enabled = true;
        
        // Возвращаем оригинальный масштаб
        transform.localScale = originalScale;
        
        currentCarrier = null;
        isBeingCarried = false;
        carryTime = 0f;
        currentMultiplier = 1f;
    }
    
    private void OnCarrierChanged(NetworkIdentity oldCarrier, NetworkIdentity newCarrier)
    {
        currentCarrier = newCarrier;
        
        if (newCarrier != null)
        {
            Debug.Log($"Тотем теперь у {newCarrier.name}");
        }
        else
        {
            Debug.Log("Тотем сброшен");
        }
    }
    
    public Transform GetCurrentCarrier()
    {
        return currentCarrier != null ? currentCarrier.transform : null;
    }
    
    public bool IsBeingCarried()
    {
        return isBeingCarried;
    }
    
    public float GetCarryMultiplier()
    {
        return currentMultiplier;
    }
    
    public int CalculateScore()
    {
        return Mathf.RoundToInt(baseScorePerSecond * currentMultiplier);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickUpRange);
    }
}