using UnityEngine;
using System.Collections;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visual Effects")]
    public GameObject deathEffect;
    public Material damageMaterial;
    public float flashDuration = 0.1f;
    
    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isDead = false;
    
    private void Start()
    {
        currentHealth = maxHealth;
        
        objectRenderer = GetComponentInChildren<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }
    }
    
    public void TakeDamage(float damage, GameObject damageSource)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        
        if (objectRenderer != null && damageMaterial != null)
        {
            StartCoroutine(FlashDamage());
        }
        
        Debug.Log($"{gameObject.name} получил {damage} урона от {damageSource.name}. Здоровье: {currentHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private IEnumerator FlashDamage()
    {
        if (objectRenderer != null && damageMaterial != null)
        {
            objectRenderer.material = damageMaterial;
            yield return new WaitForSeconds(flashDuration);
            objectRenderer.material = originalMaterial;
        }
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        
        Debug.Log($"{gameObject.name} погиб!");
        
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }
        
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.OnPlayerDeath();
            
            StartCoroutine(RespawnAfterDelay(3f));
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }
    
    // ИЗМЕНЕНО: Делаем метод public для доступа из NetworkManager
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            transform.position = spawnPoints[randomIndex].transform.position;
        }
        
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.EnableControls(true);
        }
        
        Debug.Log($"{gameObject.name} возродился!");
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    
    public bool IsAlive()
    {
        return !isDead;
    }
}