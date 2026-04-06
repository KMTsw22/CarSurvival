using UnityEngine;

/// <summary>
/// 몬스터가 발사하는 투사체 — 플레이어에게 데미지를 줌.
/// </summary>
public class MonsterBullet : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private float damage;
    private float rotateSpeed = 360f;
    private Transform player;
    private float hitRadius = 1.0f;
    private bool hasHit;

    public void Initialize(Vector2 dir, float spd, float dmg, float lifetime)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        Destroy(gameObject, lifetime);

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);

        // 거리 기반 히트 체크
        if (!hasHit && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < hitRadius)
            {
                var stats = player.GetComponent<PlayerStats>();
                if (stats != null)
                    stats.TakeDamage(damage);
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}
