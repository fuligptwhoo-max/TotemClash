using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }
    
    [Header("Cursor Settings")]
    public CursorLockMode defaultLockMode = CursorLockMode.Confined;
    public bool defaultVisibility = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        SetCursorState(defaultLockMode, defaultVisibility);
    }
    
    private void Update()
    {
        // Принудительно исправляем состояние курсора каждый кадр
        if (Application.isFocused)
        {
            if (Cursor.lockState != defaultLockMode)
            {
                Cursor.lockState = defaultLockMode;
            }
            
            if (Cursor.visible != defaultVisibility)
            {
                Cursor.visible = defaultVisibility;
            }
        }
    }
    
    public void SetCursorState(CursorLockMode lockMode, bool visible)
    {
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            SetCursorState(defaultLockMode, defaultVisibility);
        }
        else
        {
            // При потере фокуса разблокируем курсор
            SetCursorState(CursorLockMode.None, true);
        }
    }
}