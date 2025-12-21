using UnityEngine;

public class IceSpikeProjectile : MonoBehaviour
{
    public float damage = 25f;
    public float slowAmount = 0.5f;
    public float slowDuration = 3f;
    public GameObject owner;
    public float ignoreCollisionTime = 0.2f;
    
    private float spawnTime;
    private bool collisionsIgnored = false;
    
    private void Start()
    {
        spawnTime = Time.time;
    }
    
    private void Update()
    {
        if (collisionsIgnored && Time.time - spawnTime > ignoreCollisionTime)
        {
            EnableCollisions();
        }
    }
    
    public void IgnoreCollisionWithOwner()
    {
        if (owner != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    Physics.IgnoreCollision(projCollider, ownerCollider, true);
                }
            }
            
            collisionsIgnored = true;
        }
    }
    
    private void EnableCollisions()
    {
        if (owner != null && collisionsIgnored)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            Collider[] projectileColliders = GetComponentsInChildren<Collider>();
            
            foreach (var projCollider in projectileColliders)
            {
                foreach (var ownerCollider in ownerColliders)
                {
                    Physics.IgnoreCollision(projCollider, ownerCollider, false);
                }
            }
            
            collisionsIgnored = false;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject != owner && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{owner.name} нанес {damage} урона ледяным шипом {collision.gameObject.name}");
            
            HealthSystem health = collision.gameObject.GetComponent<HealthSystem>();
            if (health != null)
            {
                health.TakeDamage(damage, owner);
            }
        }
        
        Destroy(gameObject);
    }
}