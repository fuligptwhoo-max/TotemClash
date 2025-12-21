using UnityEngine;

public class LightningProjectile : MonoBehaviour
{
    public float damage = 20f;
    public int chainCount = 3;
    public float chainRange = 4f;
    public GameObject owner;
    
    private void OnCollisionEnter(Collision collision)
    {
        // Логика удара молнии
        if (collision.gameObject != owner && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{owner.name} нанес {damage} урона молнией {collision.gameObject.name}");
        }
        
        Destroy(gameObject);
    }
}