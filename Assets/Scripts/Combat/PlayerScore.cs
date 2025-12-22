using UnityEngine;
using Mirror;

public class PlayerScore : NetworkBehaviour
{
    [Header("Player Info")]
    [SyncVar]
    public int playerId = 0; // 0 для первого игрока, 1 для второго и т.д.
    
    [Header("Score")]
    [SyncVar]
    public int score = 0;
    
    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }
    
    [Command]
    public void CmdAddScore(int points)
    {
        score += points;
        RpcUpdateScore(score);
        
        if (gameManager != null && isServer)
        {
            gameManager.AddScore(points);
        }
    }
    
    [ClientRpc]
    void RpcUpdateScore(int newScore)
    {
        score = newScore;
    }
    
    // Вызывается на клиенте для добавления очков
    public void AddScore(int points)
    {
        if (isLocalPlayer)
        {
            CmdAddScore(points);
        }
    }
}