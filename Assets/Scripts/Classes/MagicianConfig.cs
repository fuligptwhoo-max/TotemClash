using UnityEngine;

[CreateAssetMenu(fileName = "MagicianConfig", menuName = "Game/Classes/Magician Config")]
public class MagicianConfig : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject fireballPrefab;
    public GameObject iceSpikePrefab;
    public GameObject meteorPrefab;
    public GameObject lightningPrefab;
    
    [Header("Settings")]
    public float fireballSpeed = 20f;
    public float fireballCooldown = 1f;
    public float iceSpikeCooldown = 3f;
    public float meteorCooldown = 10f;
    public float lightningCooldown = 5f;
}