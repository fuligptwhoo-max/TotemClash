using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class PlayerScore : NetworkBehaviour
{
    [Header("Player Info")]
    public readonly SyncVar<int> playerId = new SyncVar<int>(0);
    
    [Header("Score")]
    public readonly SyncVar<int> score = new SyncVar<int>(0);
    
    private GameManager gameManager;
    
    public override void OnStartClient()
    {
        base.OnStartClient();
        gameManager = FindFirstObjectByType<GameManager>();
        
        // Подписываемся на изменения
        score.OnChange += OnScoreChanged;
    }
    
    public override void OnStopClient()
    {
        base.OnStopClient();
        score.OnChange -= OnScoreChanged;
    }
    
    [ServerRpc]
    public void CmdAddScore(int points)
    {
        AddScoreInternal(points);
    }
    
    [Server]
    private void AddScoreInternal(int points)
    {
        score.Value += points;
        
        if (gameManager != null)
        {
            gameManager.AddScore(points);
        }
    }
    
    // Вызывается на клиенте для добавления очков
    public void AddScore(int points)
    {
        if (base.IsOwner)
        {
            CmdAddScore(points);
        }
    }
    
    /// <summary>
    /// Обработчик изменения SyncVar
    /// </summary>
    private void OnScoreChanged(int prev, int next, bool asServer)
    {
        Debug.Log($"Player {gameObject.name} score: {prev} -> {next}");
    }
}
