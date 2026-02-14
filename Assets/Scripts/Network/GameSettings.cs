using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Глобальные настройки игры - синхронизируются между всеми игроками
/// Только хост может менять настройки
/// Настройки сохраняются при перезапуске игры (играть снова)
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
    
    // Статические переменные для сохранения между перезапусками
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
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
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
    [Server]
    public void ResetToDefaults()
    {
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
    
    [Server]
    public void SetGameTime(float value)
    {
        float newValue = Mathf.Clamp(value, 60f, 600f);
        GameTime.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] GameTime set to: {newValue}");
    }
    
    [Server]
    public void SetPlayerSpeed(float value)
    {
        float newValue = Mathf.Clamp(value, 4f, 15f);
        PlayerSpeed.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] PlayerSpeed set to: {newValue}");
    }
    
    [Server]
    public void SetProjectileSpeed(float value)
    {
        float newValue = Mathf.Clamp(value, 10f, 40f);
        ProjectileSpeed.Value = newValue;
        SaveSettings();
        Debug.Log($"[GameSettings] ProjectileSpeed set to: {newValue}");
    }
    
    [Server]
    public void SetDamage(int value)
    {
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
}
