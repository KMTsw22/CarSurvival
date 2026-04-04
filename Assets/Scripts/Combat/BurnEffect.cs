using UnityEngine;

/// <summary>
/// 화상 디버프: 3초간 0.5초마다 데미지. 중첩 불가 — 이미 걸려있으면 갱신만.
/// </summary>
public class BurnEffect : MonoBehaviour
{
    public float totalDamage;
    public float duration = 3f;
    public float tickInterval = 0.5f;

    private float timer;
    private float tickTimer;
    private float damagePerTick;
    private EnemyHealth enemyHealth;

    public void Apply(float dmg, float dur)
    {
        totalDamage = dmg;
        duration = dur;
        timer = duration;
        tickTimer = 0f;
        damagePerTick = totalDamage / (duration / tickInterval);

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Destroy(this);
            return;
        }

        tickTimer -= Time.deltaTime;
        if (tickTimer <= 0f)
        {
            tickTimer = tickInterval;
            if (enemyHealth != null)
                enemyHealth.TakeDamage(damagePerTick);
        }
    }
}
