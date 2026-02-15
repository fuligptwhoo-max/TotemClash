using UnityEngine;
using UnityEngine.UI;

namespace TotemClash.Combat
{
    public class AimingSystem : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public GameObject crosshairPrefab;
        
        [Header("Settings")]
        public LayerMask aimLayers;
        public float maxAimDistance = 100f;
        public float aimHeightOffset = 1.2f;
        
        [Header("Crosshair Colors")]
        public Color normalColor = Color.white;
        public Color enemyColor = Color.red;
        
        private Vector3 currentAimPosition;
        private GameObject aimedPlayer = null;
        private Transform aimedTransform = null;
        private GameObject crosshairObject;
        private Image crosshairImage;
        private RectTransform crosshairRect;
        private bool crosshairInitialized = false;
        
        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
                
            if (aimLayers == 0)
                aimLayers = ~LayerMask.GetMask("UI", "Ignore Raycast");
                
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
                    crosshairImage = crosshairObject.AddComponent<Image>();
                
                crosshairImage.color = normalColor;
                crosshairObject.SetActive(true);
                crosshairInitialized = true;
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
                
            UpdateAim();
            UpdateCrosshair();
        }
        
        void UpdateAim()
        {
            if (playerCamera == null) return;
            
            aimedPlayer = null;
            aimedTransform = null;
            
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Raycast на все слои
            if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayers))
            {
                // Проверяем теги Player или Enemy
                if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Enemy"))
                {
                    aimedPlayer = hit.collider.gameObject;
                    aimedTransform = hit.collider.transform; // Сохраняем Transform
                    
                    // Целимся в центр масс
                    currentAimPosition = aimedTransform.position + Vector3.up * aimHeightOffset;
                }
                else
                {
                    // Попали в землю/стену
                    currentAimPosition = hit.point;
                }
            }
            else
            {
                // Ничего не попали
                currentAimPosition = ray.GetPoint(maxAimDistance);
            }
        }
        
        void UpdateCrosshair()
        {
            if (crosshairImage == null) return;
            
            // Красный если есть цель
            crosshairImage.color = aimedPlayer != null ? enemyColor : normalColor;
            
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
        
        // ИСПРАВЛЕНО: Добавлен метод для MagicianClass
        public Transform GetAimedTransform()
        {
            return aimedTransform;
        }
        
        public bool IsAimingAtPlayer()
        {
            return aimedPlayer != null;
        }
        
        public void ShowCrosshair(bool show)
        {
            if (crosshairObject != null)
            {
                crosshairObject.SetActive(show);
            }
        }
    }
}