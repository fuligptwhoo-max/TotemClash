using UnityEngine;

namespace TotemClash.Combat
{
    /// <summary>
    /// Простой контроллер камеры - следует за целью с указанным смещением
    /// </summary>
    public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Смещение камеры относительно цели")]
    public Vector3 offset = new Vector3(0f, 25f, -10f);
    
    [Tooltip("Скорость сглаживания движения")]
    public float smoothSpeed = 5f;
    
    [Tooltip("Угол наклона камеры")]
    public float pitchAngle = 53f;
    
    [Tooltip("Следовать за целью")]
    public bool followTarget = true;

    private Transform target;
    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (!followTarget) return;
        if (target == null) return;

        // Позиция куда должна быть камера
        Vector3 desiredPosition = target.position + offset;
        
        // Плавное движение
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 1f / smoothSpeed);
        
        // Фиксированный поворот
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
    }
    
    /// <summary>
    /// Устанавливает цель для следования
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        if (newTarget == null) return;
        
        target = newTarget;
        
        // Сразу перемещаем камеру на позицию без сглаживания
        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(pitchAngle, 0f, 0f);
        
        Debug.Log($"[CameraController] Target set: {target.name}");
    }
    
    public Transform GetTarget()
    {
        return target;
    }
}
}
