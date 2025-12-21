using UnityEngine;
using Mirror;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 55f, -25f);
    public float smoothSpeed = 10f;
    
    [Header("View Settings")]
    public float pitchAngle = 53f;
    
    private Vector3 velocity = Vector3.zero;
    private Camera cam;
    private Quaternion fixedRotation;
    private NetworkPlayerController networkPlayer;
    private bool isInitialized = false;
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        fixedRotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        transform.rotation = fixedRotation;
        
        if (cam != null)
        {
            cam.fieldOfView = 90f;
        }
        
        Debug.Log("CameraController инициализирован");
    }
    
    private void Update()
    {
        // Если цель не установлена, пытаемся найти локального игрока
        if (target == null && !isInitialized)
        {
            FindLocalPlayer();
        }
        
        // Если нашли игрока, настраиваем камеру
        if (target != null && !isInitialized)
        {
            transform.position = target.position + offset;
            isInitialized = true;
            Debug.Log($"Камера привязана к: {target.name}");
        }
    }
    
    private void LateUpdate()
    {
        if (target == null) 
        {
            if (!FindLocalPlayer())
                return;
        }
        
        UpdateCameraPosition();
    }
    
    private bool FindLocalPlayer()
    {
        // Ищем всех сетевых игроков на сцене
        var players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        
        foreach (var player in players)
        {
            // Проверяем, является ли игрок локальным (свойство isLocalPlayer из NetworkBehaviour)
            if (player.isLocalPlayer)
            {
                target = player.transform;
                networkPlayer = player;
                
                Debug.Log($"Найден локальный игрок для камеры: {target.name}");
                return true;
            }
        }
        
        Debug.LogWarning("Локальный игрок не найден для камеры!");
        return false;
    }
    
    private void UpdateCameraPosition()
    {
        if (target == null) return;
        
        Vector3 desiredPosition = target.position + offset;
        
        // Плавное перемещение камеры
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        
        // Фиксированный угол обзора
        transform.rotation = fixedRotation;
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            transform.position = target.position + offset;
            isInitialized = true;
        }
    }
}