using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Глобальные настройки игры - синхронизируются между всеми игроками
/// Только хост может менять настройки
/// Сохраняется между сценами (DontDestroyOnLoad)
/// </summary>
public class GameSettings : NetworkBehaviour
{
    public static GameSettings Instance { get; private set; }
    
    [Header("Gameplay Settings")]
    public readonly SyncVar<float> GameTime = new SyncVar<float>(300f);
    public readonly SyncVar<float> PlayerSpeed = new SyncVar<float>(8f);
    public readonly SyncVar<float> ProjectileSpeed = new SyncVar<float>(20f);
    public readonly SyncVar<int> DamagePerHit = new SyncVar<int>(25);
    
    [Header("Default Values")]
    [SerializeField] private float defaultGameTime = 300f;
    [SerializeField] private float defaultPlayerSpeed = 8f;
    [SerializeField] private float defaultProjectileSpeed = 20f;
    [SerializeField] private int defaultDamage = 25;
    
    // Статические переменные для сохранения между перезапусками игры (не сцен!)
    private static float savedGameTime = -1f;
    private static float savedPlayerSpeed = -1f;
    private static float savedProjectileSpeed = -1f;
    private static int savedDamage = -1;
    private static bool settingsApplied = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Не делаем DontDestroyOnLoad здесь - это вызовет проблемы с NetworkObject
            // Вместо этого используем статические переменные для сохранения настроек
            Debug.Log("[GameSettings] Instance set");
        }
        else
        {
            Debug.LogWarning("[GameSettings] Another instance exists, destroying this one");
            Destroy(gameObject);
        }
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        Debug.Log($"[GameSettings] OnStartNetwork - IsServerInitialized: {base.IsServerInitialized}, IsClientInitialized: {base.IsClientInitialized}");
        
        // Только сервер инициализирует значения
        if (base.IsServerInitialized)
        {
            // Если есть сохранённые настройки - используем их
            if (settingsApplied && savedGameTime > 0)
            {
                GameTime.Value = savedGameTime;
                PlayerSpeed.Value = savedPlayerSpeed;
                ProjectileSpeed.Value = savedProjectileSpeed;
                DamagePerHit.Value = savedDamage;
                Debug.Log($"[GameSettings] Loaded saved settings: Time={savedGameTime}, Speed={savedPlayerSpeed}");
            }
            else
            {
                // Только если нет сохранённых настроек - используем дефолт
                GameTime.Value = defaultGameTime;
                PlayerSpeed.Value = defaultPlayerSpeed;
                ProjectileSpeed.Value = defaultProjectileSpeed;
                DamagePerHit.Value = defaultDamage;
                Debug.Log("[GameSettings] Using default settings (no saved settings found)");
            }
        }
        
        // Подписываемся на изменения
        GameTime.OnChange += OnGameTimeChanged;
        PlayerSpeed.OnChange += OnPlayerSpeedChanged;
        ProjectileSpeed.OnChange += OnProjectileSpeedChanged;
        DamagePerHit.OnChange += OnDamageChanged;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        
        GameTime.OnChange -= OnGameTimeChanged;
        PlayerSpeed.OnChange -= OnPlayerSpeedChanged;
        ProjectileSpeed.OnChange -= OnProjectileSpeedChanged;
        DamagePerHit.OnChange -= OnDamageChanged;
    }
    
    private void OnGameTimeChanged(float prev, float next, bool asServer)
    {
        Debug.Log($"[GameSettings] GameTime changed: {prev} -> {next} (asServer: {asServer})");
    }
    
    private void OnPlayerSpeedChanged(float prev, float next, bool asServer)
    {
        Debug.Log($"[GameSettings] PlayerSpeed changed: {prev} -> {next} (asServer: {asServer})");
    }
    
    private void OnProjectileSpeedChanged(float prev, float next, bool asServer)
    {
        Debug.Log($"[GameSettings] ProjectileSpeed changed: {prev} -> {next} (asServer: {asServer})");
    }
    
    private void OnDamageChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"[GameSettings] Damage changed: {prev} -> {next} (asServer: {asServer})");
    }
    
    /// <summary>
    /// Сохраняет текущие настройки для следующих игр
    /// </summary>
    private void SaveSettings()
    {
        savedGameTime = GameTime.Value;
        savedPlayerSpeed = PlayerSpeed.Value;
        savedProjectileSpeed = ProjectileSpeed.Value;
        savedDamage = DamagePerHit.Value;
        settingsApplied = true;
        
        Debug.Log($"[GameSettings] Settings saved: Time={savedGameTime}, Speed={savedPlayerSpeed}");
    }
    
    /// <summary>
    /// Сброс настроек к значениям по умолчанию (только сервер)
    /// </summary>
    public void ResetToDefaults()
    {
        if (!base.IsServerInitialized)
        {
            Debug.LogWarning("[GameSettings] ResetToDefaults called but not server!");
            return;
        }
        
        GameTime.Value = defaultGameTime;
        PlayerSpeed.Value = defaultPlayerSpeed;
        ProjectileSpeed.Value = defaultProjectileSpeed;
        DamagePerHit.Value = defaultDamage;
        
        // Сбрасываем сохранённые настройки
        settingsApplied = false;
        savedGameTime = -1f;
        savedPlayerSpeed = -1f;
        savedProjectileSpeed = -1f;
        savedDamage = -1;
        
        Debug.Log("[GameSettings] Reset to defaults");
    }
    
    #region Setters (только для сервера/хоста)
    
    public void SetGameTime(float value)
    {
        Debug.Log($"[GameSettings] SetGameTime called with {value}, IsServerInitialized={base.IsServerInitialized}");
        
        if (!base.IsServerInitialized)
        {
            Debug.LogWarning($"[GameSettings] SetGameTime({value}) called but IsServerInitialized={base.IsServerInitialized}");
            return;
        }
        
        float newValue = Mathf.Clamp(value, 60f, 600f);
        GameTime.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] GameTime set to: {newValue}");
    }
    
    public void SetPlayerSpeed(float value)
    {
        if (!base.IsServerInitialized)
        {
            Debug.LogWarning($"[GameSettings] SetPlayerSpeed({value}) called but IsServerInitialized={base.IsServerInitialized}");
            return;
        }
        
        float newValue = Mathf.Clamp(value, 4f, 15f);
        PlayerSpeed.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] PlayerSpeed set to: {newValue}");
    }
    
    public void SetProjectileSpeed(float value)
    {
        if (!base.IsServerInitialized)
        {
            Debug.LogWarning($"[GameSettings] SetProjectileSpeed({value}) called but IsServerInitialized={base.IsServerInitialized}");
            return;
        }
        
        float newValue = Mathf.Clamp(value, 10f, 40f);
        ProjectileSpeed.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] ProjectileSpeed set to: {newValue}");
    }
    
    public void SetDamage(int value)
    {
        if (!base.IsServerInitialized)
        {
            Debug.LogWarning($"[GameSettings] SetDamage({value}) called but IsServerInitialized={base.IsServerInitialized}");
            return;
        }
        
        int newValue = Mathf.Clamp(value, 10, 100);
        DamagePerHit.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] Damage set to: {newValue}");
    }
    
    #endregion
    
    #region Getters
    
    public float GetGameTime() => GameTime.Value;
    public float GetPlayerSpeed() => PlayerSpeed.Value;
    public float GetProjectileSpeed() => ProjectileSpeed.Value;
    public int GetDamage() => DamagePerHit.Value;
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            Debug.Log("[GameSettings] Instance cleared - settings saved in static variables");
        }
    }
}
