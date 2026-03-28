using UnityEngine;

public class MineEffect : MonoBehaviour
{
    public float damage = 20f;
    public float explosionRadius = 2f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Explode
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    EnemyHealth eh = hit.GetComponent<EnemyHealth>();
                    if (eh != null) eh.TakeDamage(damage);
                }
            }
            Destroy(gameObject);
        }
    }
}
