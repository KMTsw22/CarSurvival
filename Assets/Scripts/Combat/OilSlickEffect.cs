using UnityEngine;

public class OilSlickEffect : MonoBehaviour
{
    public float damage = 3f;
    public float slowPercent = 0.5f;
    public float slowDuration = 0.5f;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth eh = other.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage * Time.deltaTime);

            EnemyAI ai = other.GetComponent<EnemyAI>();
            if (ai != null) ai.ApplySlow(slowPercent, slowDuration);
        }
    }
}
