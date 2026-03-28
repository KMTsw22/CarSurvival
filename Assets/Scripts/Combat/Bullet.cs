using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private Transform homingTarget;
    private bool isHoming;

    public void Initialize(Vector2 dir, float spd, float dmg, float lifetime)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        Destroy(gameObject, lifetime);

        // Set rotation to face direction
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void SetHoming(Transform target)
    {
        homingTarget = target;
        isHoming = true;
    }

    private void Update()
    {
        if (isHoming && homingTarget != null)
        {
            Vector2 toTarget = ((Vector2)homingTarget.position - (Vector2)transform.position).normalized;
            direction = Vector2.Lerp(direction, toTarget, Time.deltaTime * 5f).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        transform.Translate(Vector3.up * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
