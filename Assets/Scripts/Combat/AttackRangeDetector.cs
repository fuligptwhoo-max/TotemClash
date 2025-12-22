using UnityEngine;
using Mirror;

public class AttackRangeDetector : MonoBehaviour
{
    public NetworkPlayerController playerController;
    
    private void OnTriggerEnter(Collider other)
    {
        if (playerController != null && other.CompareTag("Player"))
        {
            // Игрок вошел в зону атаки
            Debug.Log($"Игрок {other.name} в зоне атаки");
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (playerController != null && other.CompareTag("Player"))
        {
            // Игрок вышел из зоны атаки
            Debug.Log($"Игрок {other.name} вышел из зоны атаки");
        }
    }
}