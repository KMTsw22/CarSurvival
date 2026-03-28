using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 20f;
    public float currentHealth;
    public int expDrop = 1;
    public int goldDrop = 5;
    public GameObject expPickupPrefab;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        // Flash red
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            Invoke(nameof(ResetColor), 0.1f);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        // Drop experience pickups
        if (expPickupPrefab != null)
        {
            for (int i = 0; i < expDrop; i++)
            {
                Vector3 offset = Random.insideUnitCircle * 0.5f;
                var pickup = Instantiate(expPickupPrefab, transform.position + offset, Quaternion.identity);
                pickup.SetActive(true);
            }
        }

        // Notify stats
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        if (player != null)
        {
            player.enemiesKilled++;
            player.gold += goldDrop;
        }

        Destroy(gameObject);
    }
}
