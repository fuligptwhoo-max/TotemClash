using UnityEngine;

public class AttackRangeDetector : MonoBehaviour
{
    public NetworkPlayerController playerController;
    
    private void OnTriggerEnter(Collider other)
    {
        if (playerController != null && other.CompareTag("Player"))
        {
            // Игрок вошел в зону атаки
            Debug.Log($"Player {other.name} entered attack range");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (playerController != null && other.CompareTag("Player"))
        {
            // Игрок вышел из зоны атаки
            Debug.Log($"Player {other.name} left attack range");
        }
    }
}
