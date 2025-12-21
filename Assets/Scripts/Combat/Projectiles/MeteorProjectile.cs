using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    public Vector3 targetPosition;
    public float damage = 50f;
    public float radius = 5f;
    public float fallSpeed = 15f;
    public GameObject owner;
    
    private void Update()
    {
        // Движение к цели
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, fallSpeed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            Explode();
        }
    }
    
    private void Explode()
    {
        // Взрыв метеорита
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != owner && collider.CompareTag("Player"))
            {
                Debug.Log($"{owner.name} нанес {damage} урона метеоритом {collider.name}");
            }
        }
        
        Destroy(gameObject);
    }
}