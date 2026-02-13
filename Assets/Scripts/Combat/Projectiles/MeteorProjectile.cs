using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class MeteorProjectile : NetworkBehaviour
{
    [Header("Settings")]
    public Vector3 targetPosition;
    public float damage = 50f;
    public float radius = 5f;
    public float fallSpeed = 15f;
    
    // FishNet 4.x SyncVar
    public readonly SyncVar<GameObject> owner = new SyncVar<GameObject>();
    
    private bool hasExploded = false;
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Если targetPosition не задана, берем из SyncVar или устанавливаем впереди
        if (targetPosition == Vector3.zero)
        {
            targetPosition = transform.position + transform.forward * 10f;
        }
    }
    
    private void Update()
    {
        if (!base.IsServerInitialized) return;
        if (hasExploded) return;
        
        // Движение к цели
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, fallSpeed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            Explode();
        }
    }
    
    [Server]
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Взрыв метеорита
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius);
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != owner.Value && collider.CompareTag("Player"))
            {
                HealthSystem health = collider.GetComponent<HealthSystem>();
                if (health != null)
                {
                    health.TakeDamage(damage, owner.Value);
                    Debug.Log($"{owner.Value?.name} dealt {damage} meteor damage to {collider.name}");
                }
            }
        }
        
        // Эффект взрыва можно заспавнить здесь
        // SpawnExplosionEffect();
        
        // Уничтожаем
        base.ServerManager.Despawn(gameObject);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
        
        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            Gizmos.DrawWireSphere(targetPosition, 0.5f);
        }
    }
}
