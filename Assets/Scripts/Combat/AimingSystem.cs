using UnityEngine;
using UnityEngine.UI;

namespace TotemClash.Combat
{
    public class AimingSystem : MonoBehaviour
    {
        [Header("Control")]
        public bool isPlayerControlled = true; // TRUE для игрока, FALSE для ботов
        
        [Header("Camera")]
        public Camera playerCamera;
        
        [Header("Aim Settings")]
        public LayerMask aimLayers;
        public LayerMask groundLayers;
        public float maxAimDistance = 100f;
        public bool showDebugRays = true;
        
        [Header("Prediction")]
        public bool enablePrediction = true;
        public float predictionTime = 2f;
        
        [Header("Crosshair")]
        public GameObject crosshairPrefab;
        public Color normalColor = Color.white;
        public Color enemyColor = Color.red;
        
        private Vector3 currentAimPoint;
        private Transform lockedTarget;
        private Vector3 predictedPosition;
        private RectTransform crosshairRect;
        private Image crosshairImage;
        private bool crosshairVisible = true;
        
        public Vector3 AimPoint => currentAimPoint;
        public Transform LockedTarget => lockedTarget;
        public Vector3 PredictedPosition => predictedPosition;
        public Vector3 GetAimPosition() => currentAimPoint;
        public bool IsAimingAtPlayer() => lockedTarget != null;
        public Transform GetAimedTransform() => lockedTarget;
        
        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;
                
            if (aimLayers == 0)
                aimLayers = LayerMask.GetMask("Player", "Enemy", "Default");
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Ground", "Default");
            
            // Прицел создаем только для игрока
            if (isPlayerControlled)
            {
                CreateCrosshair();
            }
        }
        
        void Update()
        {
            if (isPlayerControlled)
            {
                UpdatePlayerAim();
                UpdateCrosshair();
            }
            else
            {
                UpdateBotAim();
            }
        }
        
        // Логика для игрока (мышь)
        void UpdatePlayerAim()
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            lockedTarget = null;
            predictedPosition = Vector3.zero;
            
            if (Physics.Raycast(ray, out hit, maxAimDistance, aimLayers))
            {
                if (hit.collider.CompareTag("Player") || hit.collider.CompareTag("Enemy"))
                {
                    lockedTarget = hit.collider.transform;
                    
                    if (enablePrediction)
                    {
                        predictedPosition = CalculatePredictedPosition(lockedTarget);
                        currentAimPoint = predictedPosition;
                    }
                    else
                    {
                        currentAimPoint = lockedTarget.position + Vector3.up * 1.2f;
                    }
                    
                    if (showDebugRays)
                        Debug.DrawLine(transform.position, currentAimPoint, Color.red, 0.1f);
                    return;
                }
            }
            
            if (Physics.Raycast(ray, out hit, maxAimDistance, groundLayers))
            {
                currentAimPoint = hit.point;
            }
            else
            {
                Plane groundPlane = new Plane(Vector3.up, transform.position);
                float distance;
                if (groundPlane.Raycast(ray, out distance))
                    currentAimPoint = ray.GetPoint(distance);
                else
                    currentAimPoint = transform.position + transform.forward * 10f;
            }
            
            if (showDebugRays)
                Debug.DrawLine(transform.position, currentAimPoint, Color.green, 0.1f);
        }
        
        // Логика для ботов (не использует мышь!)
        void UpdateBotAim()
        {
            // Боты не используют мышь - они целятся в lockedTarget или в текущую цель
            // Цель устанавливается через SetTarget() из AiBotController
            // Или автоматически если lockedTarget есть
            
            if (lockedTarget != null)
            {
                if (enablePrediction)
                {
                    predictedPosition = CalculatePredictedPosition(lockedTarget);
                    currentAimPoint = predictedPosition;
                }
                else
                {
                    currentAimPoint = lockedTarget.position + Vector3.up * 1.2f;
                }
            }
            // Если lockedTarget нет, оставляем текущий currentAimPoint (установленный через SetTarget)
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
            
            if (crosshairPrefab != null)
            {
                GameObject ch = Instantiate(crosshairPrefab, canvas.transform);
                crosshairRect = ch.GetComponent<RectTransform>();
                crosshairImage = ch.GetComponent<Image>();
                ch.name = "Crosshair_" + gameObject.name;
            }
            else
            {
                CreateDefaultCrosshair(canvas);
            }
            
            if (crosshairRect != null)
            {
                crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
                crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
                crosshairRect.anchoredPosition = Vector2.zero;
            }
        }
        
        void CreateDefaultCrosshair(Canvas canvas)
        {
            GameObject ch = new GameObject("Crosshair");
            ch.transform.SetParent(canvas.transform, false);
            
            crosshairRect = ch.AddComponent<RectTransform>();
            crosshairRect.sizeDelta = new Vector2(32, 32);
            
            crosshairImage = ch.AddComponent<Image>();
            crosshairImage.color = normalColor;
            
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            
            for (int x = 14; x < 18; x++)
                for (int y = 0; y < 32; y++)
                    pixels[y * 32 + x] = Color.white;
            
            for (int y = 14; y < 18; y++)
                for (int x = 0; x < 32; x++)
                    pixels[y * 32 + x] = Color.white;
            
            tex.SetPixels(pixels);
            tex.Apply();
            
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            crosshairImage.sprite = sprite;
        }
        
        void UpdateCrosshair()
        {
            if (crosshairRect == null) return;
            if (!crosshairVisible) return;
            if (!isPlayerControlled) return; // Только для игрока!
            
            crosshairRect.position = Input.mousePosition;
            
            if (crosshairImage != null)
            {
                crosshairImage.color = lockedTarget != null ? enemyColor : normalColor;
            }
        }
        
        Vector3 CalculatePredictedPosition(Transform target)
        {
            Rigidbody rb = target.GetComponent<Rigidbody>();
            CharacterController cc = target.GetComponent<CharacterController>();
            
            Vector3 velocity = Vector3.zero;
            if (rb != null) velocity = rb.linearVelocity;
            else if (cc != null) velocity = cc.velocity;
            
            Vector3 futurePos = target.position + velocity * predictionTime;
            futurePos.y = Mathf.Max(futurePos.y, target.position.y);
            futurePos += Vector3.up * 1.2f;
            
            return futurePos;
        }
        
        public bool HasTarget()
        {
            return lockedTarget != null;
        }
        
        public bool IsTargetInRange(float range)
        {
            if (lockedTarget == null) return false;
            return Vector3.Distance(transform.position, lockedTarget.position) <= range;
        }
        
        public void SetTarget(Vector3 position)
        {
            currentAimPoint = position;
        }
        
        // Для ботов - установка цели
        public void SetLockedTarget(Transform target)
        {
            lockedTarget = target;
        }
        
        public void ShowCrosshair(bool show)
        {
            if (!isPlayerControlled) return; // Ботам нельзя показывать прицел
            
            crosshairVisible = show;
            if (crosshairRect != null)
                crosshairRect.gameObject.SetActive(show);
        }
        
        void OnDestroy()
        {
            if (crosshairRect != null && isPlayerControlled)
                Destroy(crosshairRect.gameObject);
        }
    }
}