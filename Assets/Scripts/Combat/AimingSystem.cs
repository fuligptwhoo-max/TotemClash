using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;

public class AimingSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public GameObject crosshairPrefab;
    
    [Header("Settings")]
    public LayerMask playerLayerMask;
    public LayerMask groundLayerMask;
    public float maxAimDistance = 50f;
    public float aimHeightOffset = 1f;
    
    [Header("Crosshair Colors")]
    public Color normalColor = Color.white;
    public Color enemyColor = Color.red;
    
    private Vector3 currentAimPosition;
    private GameObject aimedPlayer = null;
    private GameObject crosshairObject;
    private Image crosshairImage;
    private RectTransform crosshairRect;
    private bool crosshairInitialized = false;
    
    // Оптимизация: кэш для Raycast
    private RaycastHit[] raycastHits = new RaycastHit[10];
    
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        InitializeCrosshair();
    }
    
    void InitializeCrosshair()
    {
        if (crosshairInitialized) return;
        
        if (crosshairObject == null)
        {
            crosshairObject = GameObject.Find("Crosshair");
            if (crosshairObject == null)
            {
                CreateCrosshair();
            }
        }
        
        if (crosshairObject != null)
        {
            crosshairRect = crosshairObject.GetComponent<RectTransform>();
            crosshairImage = crosshairObject.GetComponent<Image>();
            
            if (crosshairImage == null)
            {
                crosshairImage = crosshairObject.AddComponent<Image>();
            }
            
            crosshairImage.color = normalColor;
            crosshairObject.SetActive(true);
            crosshairInitialized = true;
            
            Debug.Log("Crosshair initialized");
        }
    }
    
    void CreateCrosshair()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("CrosshairCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        crosshairObject = new GameObject("Crosshair");
        crosshairObject.transform.SetParent(canvas.transform, false);
        
        crosshairRect = crosshairObject.AddComponent<RectTransform>();
        crosshairRect.sizeDelta = new Vector2(32, 32);
        
        crosshairImage = crosshairObject.AddComponent<Image>();
        crosshairImage.color = normalColor;
        
        CreateSimpleCrosshairSprite();
    }
    
    void CreateSimpleCrosshairSprite()
    {
        Texture2D texture = new Texture2D(64, 64);
        Color[] colors = new Color[64 * 64];
        
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                bool isCross = (x >= 30 && x <= 34) || (y >= 30 && y <= 34);
                colors[y * 64 + x] = isCross ? Color.white : Color.clear;
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
        crosshairImage.sprite = sprite;
    }
    
    void Update()
    {
        if (!crosshairInitialized)
            InitializeCrosshair();
            
        // Оптимизация: Обновляем прицел не каждый кадр
        if (Time.frameCount % 2 == 0)
        {
            UpdateAim();
        }
        
        UpdateCrosshair();
    }
    
    void UpdateAim()
    {
        if (playerCamera == null) return;
        
        aimedPlayer = null;
        
        // Луч от камеры через курсор
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        
        // Используем NonAlloc версию Raycast
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, maxAimDistance, playerLayerMask);
        
        for (int i = 0; i < hitCount; i++)
        {
            GameObject hitObject = raycastHits[i].collider.gameObject;
            
            // Проверяем что это игрок и не мы сами
            if (hitObject.CompareTag("Player") && hitObject != gameObject)
            {
                aimedPlayer = hitObject;
                // Целимся в центр игрока
                CharacterController controller = aimedPlayer.GetComponent<CharacterController>();
                if (controller != null)
                {
                    currentAimPosition = aimedPlayer.transform.position + Vector3.up * (controller.height * 0.7f);
                }
                else
                {
                    currentAimPosition = aimedPlayer.transform.position + Vector3.up * 1.5f;
                }
                return;
            }
        }
        
        // Если не попали в игрока - точка на земле
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        
        if (groundPlane.Raycast(ray, out distance))
        {
            currentAimPosition = ray.GetPoint(distance);
            currentAimPosition.y = 0.1f;
        }
        else
        {
            currentAimPosition = ray.GetPoint(maxAimDistance);
            currentAimPosition.y = 0.1f;
        }
    }
    
    void UpdateCrosshair()
    {
        if (crosshairImage == null) return;
        
        // Меняем цвет прицела
        crosshairImage.color = aimedPlayer != null ? enemyColor : normalColor;
        
        // Обновляем позицию прицела
        if (crosshairRect != null)
        {
            crosshairRect.position = Input.mousePosition;
        }
    }
    
    public Vector3 GetAimPosition()
    {
        return currentAimPosition;
    }
    
    public GameObject GetAimedPlayer()
    {
        return aimedPlayer;
    }
    
    public bool IsAimingAtPlayer()
    {
        return aimedPlayer != null;
    }
    
    void OnDestroy()
    {
        if (crosshairObject != null)
        {
            Destroy(crosshairObject);
        }
    }
}
