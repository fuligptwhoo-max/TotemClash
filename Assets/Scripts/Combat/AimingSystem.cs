using UnityEngine;
using UnityEngine.UI;

public class AimingSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Image crosshairUI;
    
    [Header("Settings")]
    public LayerMask aimLayerMask = ~0;
    public float maxAimDistance = 50f;
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;
    
    private Vector3 currentAimPosition;
    
    private void Start()
    {
        if (crosshairUI == null)
        {
            Debug.LogWarning("Crosshair UI не назначен в AimingSystem!");
        }
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }
    
    private void Update()
    {
        UpdateAim();
        UpdateCrosshair();
    }
    
    private void UpdateAim()
    {
        if (playerCamera == null) 
        {
            playerCamera = Camera.main;
            if (playerCamera == null) return;
        }
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float rayDistance;
        
        if (groundPlane.Raycast(ray, out rayDistance))
        {
            currentAimPosition = ray.GetPoint(rayDistance);
            currentAimPosition.y = 0.1f;
        }
        else
        {
            currentAimPosition = ray.GetPoint(maxAimDistance);
            currentAimPosition.y = 0.1f;
        }
    }
    
    private void UpdateCrosshair()
    {
        if (crosshairUI == null || playerCamera == null) return;
        
        crosshairUI.rectTransform.position = Input.mousePosition;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayerMask))
        {
            if (hit.collider.CompareTag("Player") && hit.collider.gameObject != gameObject)
            {
                crosshairUI.color = enemyColor;
            }
            else
            {
                crosshairUI.color = normalColor;
            }
        }
        else
        {
            crosshairUI.color = normalColor;
        }
    }
    
    public Vector3 GetAimPosition()
    {
        return currentAimPosition;
    }
    
    public GameObject GetTargetAtAim()
    {
        if (playerCamera == null) return null;
        
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayerMask))
        {
            return hit.collider.gameObject;
        }
        
        return null;
    }
}