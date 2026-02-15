using UnityEngine;
using UnityEngine.UI;
using TotemClash.UI;
using TotemClash.AI;

namespace TotemClash.Combat
{
    /// <summary>
    /// Автоматически инициализирует игру при старте сцены
    /// Создаёт необходимые объекты если их нет
    /// </summary>
    public class GameInitializer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject totemPrefab;
    
    [Header("Auto Create")]
    public bool createGameManager = true;
    public bool createLocalGameSpawner = true;
    public bool createBotSpawner = true;
    public bool createCanvas = true;
    public bool createEventSystem = true;
    
    [Header("Bot Settings")]
    public int botCount = 3;
    
    private void Awake()
    {
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        Debug.Log("[GameInitializer] Initializing game...");
        
        // Создаём EventSystem если нужно
        if (createEventSystem)
        {
            CreateEventSystem();
        }
        
        // Создаём Canvas если нужно
        if (createCanvas)
        {
            CreateCanvas();
        }
        
        // Создаём GameManager если нужно
        if (createGameManager)
        {
            CreateGameManager();
        }
        
        // Создаём LocalGameSpawner если нужно
        if (createLocalGameSpawner)
        {
            CreateLocalGameSpawner();
        }
        
        // Создаём BotSpawner если нужно
        if (createBotSpawner)
        {
            CreateBotSpawner();
        }
        
        // Ищем или создаём тотем
        SetupTotem();
        
        Debug.Log("[GameInitializer] Game initialization complete!");
    }
    
    private void CreateEventSystem()
    {
        var existing = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existing == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[GameInitializer] Created EventSystem");
        }
    }
    
    private void CreateCanvas()
    {
        var existing = FindFirstObjectByType<Canvas>();
        if (existing == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            Debug.Log("[GameInitializer] Created Canvas");
        }
    }
    
    private void CreateGameManager()
    {
        if (GameManager.Instance == null)
        {
            GameObject gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.AddComponent<GameManager>();
            
            // Находим или создаём UI элементы
            SetupGameManagerUI(gm);
            
            Debug.Log("[GameInitializer] Created GameManager");
        }
    }
    
    private void SetupGameManagerUI(GameManager gm)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        // Создаём TimerText если нет
        if (gm.timerText == null)
        {
            GameObject timerObj = new GameObject("TimerText", typeof(RectTransform));
            timerObj.transform.SetParent(canvas.transform, false);
            
            RectTransform rt = timerObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -50);
            rt.sizeDelta = new Vector2(200, 50);
            
            TMPro.TMP_Text text = timerObj.AddComponent<TMPro.TMP_Text>();
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.fontSize = 36;
            text.color = Color.white;
            
            gm.timerText = text;
            Debug.Log("[GameInitializer] Created TimerText");
        }
    }
    
    private void CreateLocalGameSpawner()
    {
        if (LocalGameSpawner.Instance == null)
        {
            GameObject spawnerObj = new GameObject("LocalGameSpawner");
            LocalGameSpawner spawner = spawnerObj.AddComponent<LocalGameSpawner>();
            
            spawner.playerPrefab = playerPrefab;
            
            // Находим CountdownDisplay или создаём
            CountdownDisplay cd = FindFirstObjectByType<CountdownDisplay>();
            if (cd == null)
            {
                GameObject cdObj = new GameObject("CountdownDisplay");
                cd = cdObj.AddComponent<CountdownDisplay>();
                
                // Создаём UI для отсчёта
                SetupCountdownUI(cd);
            }
            spawner.countdownDisplay = cd;
            
            Debug.Log("[GameInitializer] Created LocalGameSpawner");
        }
    }
    
    private void SetupCountdownUI(CountdownDisplay cd)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        
        // Создаём панель отсчёта
        GameObject panelObj = new GameObject("CountdownPanel", typeof(RectTransform));
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0, 0, 0, 0.5f);
        
        // Создаём текст отсчёта
        GameObject textObj = new GameObject("CountdownText", typeof(RectTransform));
        textObj.transform.SetParent(panelObj.transform, false);
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 0.5f);
        textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.anchoredPosition = Vector2.zero;
        textRt.sizeDelta = new Vector2(400, 200);
        
        TMPro.TMP_Text text = textObj.AddComponent<TMPro.TMP_Text>();
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.fontSize = 120;
        text.color = Color.white;
        text.text = "3";
        
        cd.countdownPanel = panelObj;
        cd.countdownText = text;
        
        panelObj.SetActive(false);
        
        Debug.Log("[GameInitializer] Created Countdown UI");
    }
    
    private void CreateBotSpawner()
    {
        if (BotSpawner.Instance == null)
        {
            GameObject spawnerObj = new GameObject("BotSpawner");
            BotSpawner spawner = spawnerObj.AddComponent<BotSpawner>();
            
            spawner.botPrefab = playerPrefab;
            spawner.botCount = botCount;
            
            Debug.Log("[GameInitializer] Created BotSpawner");
        }
    }
    
    private void SetupTotem()
    {
        TotemController totem = FindFirstObjectByType<TotemController>();
        
        if (totem == null && totemPrefab != null)
        {
            // Создаём тотем из префаба
            Instantiate(totemPrefab, Vector3.zero, Quaternion.identity);
            Debug.Log("[GameInitializer] Created Totem from prefab");
        }
        else if (totem == null)
        {
            // Создаём простой тотем
            GameObject totemObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            totemObj.name = "Totem";
            totemObj.transform.position = Vector3.zero;
            totemObj.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // Добавляем Rigidbody
            Rigidbody rb = totemObj.AddComponent<Rigidbody>();
            rb.mass = 1f;
            
            // Добавляем TotemController
            totemObj.AddComponent<TotemController>();
            
            Debug.Log("[GameInitializer] Created default Totem");
        }
    }
}
}
