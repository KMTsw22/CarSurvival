using UnityEngine;

/// <summary>
/// 일정 시간 존재하며 플레이어에게 접촉 데미지를 주는 영역 (지면 공격 등).
/// 거리 기반 판정.
/// </summary>
public class MonsterDamageZone : MonoBehaviour
{
    public float damage = 30f;
    public float lifetime = 1.5f;
    public float hitRadius = 1.2f;

    private Transform player;

    private void Start()
    {
        Destroy(gameObject, lifetime);
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < hitRadius)
        {
            var stats = player.GetComponent<PlayerStats>();
            if (stats != null)
                stats.TakeDamage(damage * Time.deltaTime);
        }
    }
}
