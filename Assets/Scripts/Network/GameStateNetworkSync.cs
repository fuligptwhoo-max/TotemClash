using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// Сетевой компонент для синхронизации состояния игры
/// ДОЛЖЕН быть на сцене заранее (не создавать динамически!)
/// </summary>
public class GameStateNetworkSync : NetworkBehaviour
{
    public static GameStateNetworkSync Instance { get; private set; }
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<float> CurrentTime = new SyncVar<float>(300f);
    public readonly SyncVar<int> TotalScore = new SyncVar<int>(0);
    public readonly SyncVar<bool> IsGameActive = new SyncVar<bool>(false);
    
    // События для подписки
    public event System.Action OnTimeChanged;
    public event System.Action OnScoreChanged;
    public event System.Action OnGameEnded;
    
    [Header("Game Settings")]
    [SerializeField] private float gameTime = 300f;
    
    private float carrierScoreAccumulator = 0f;
    
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
        CurrentTime.OnChange += OnTimeChangedInternal;
        TotalScore.OnChange += OnScoreChangedInternal;
    }
    
    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        CurrentTime.OnChange -= OnTimeChangedInternal;
        TotalScore.OnChange -= OnScoreChangedInternal;
    }
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        CurrentTime.Value = gameTime;
        IsGameActive.Value = true; // Сразу активна
    }
    
    private void Update()
    {
        if (!base.IsServerInitialized) return;
        if (!IsGameActive.Value) return;
        
        CurrentTime.Value -= Time.deltaTime;
        UpdateScoreFromTotem();
        
        if (CurrentTime.Value <= 0f)
        {
            CurrentTime.Value = 0f;
            IsGameActive.Value = false;
            RpcEndGame();
        }
    }
    
    private void UpdateScoreFromTotem()
    {
        TotemController totem = FindFirstObjectByType<TotemController>();
        if (totem != null && totem.IsBeingCarried())
        {
            carrierScoreAccumulator += totem.GetCarryMultiplier() * Time.deltaTime;
            if (carrierScoreAccumulator >= 1f)
            {
                int pointsToAdd = Mathf.FloorToInt(carrierScoreAccumulator);
                TotalScore.Value += pointsToAdd;
                carrierScoreAccumulator -= pointsToAdd;
            }
        }
        else
        {
            carrierScoreAccumulator = 0f;
        }
    }
    
    [Server]
    public void AddScore(int points)
    {
        TotalScore.Value += points;
    }
    
    private void OnTimeChangedInternal(float prev, float next, bool asServer)
    {
        OnTimeChanged?.Invoke();
    }
    
    private void OnScoreChangedInternal(int prev, int next, bool asServer)
    {
        OnScoreChanged?.Invoke();
    }
    
    [ObserversRpc]
    private void RpcEndGame()
    {
        OnGameEnded?.Invoke();
    }
}
