using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float contactDamage = 10f;

    private Transform player;
    private Rigidbody2D rb;
    private float stunTimer;
    private float slowMultiplier = 1f;
    private float slowTimer;

    // 추적 각도 오프셋: 스폰 시 랜덤 배정, 플레이어 옆을 향해 걸어옴
    private float chaseAngleOffset;
    // 가까워지면 오프셋 줄어듦 (근접하면 직선 추적)
    private const float offsetFullDist = 10f;  // 이 거리 이상이면 오프셋 100%
    private const float offsetZeroDist = 3f;   // 이 거리 이하면 오프셋 0% (직선)

    // 몬스터 겹침 방지
    private static readonly float separationRadius = 2.0f;
    private static readonly float separationForce = 12f;
    private static readonly List<Collider2D> overlapBuffer = new List<Collider2D>(16);

    // 넉백
    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    // 분노 (넉백 후 더 빠르게 돌아옴)
    private float rageMultiplier = 1f;
    private float rageTimer;
    private const float rageDuration = 3f;
    private const float rageSpeedMult = 1.8f;

    private SpriteRenderer cachedSR;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        cachedSR = GetComponent<SpriteRenderer>();
        // 각 몬스터마다 -40°~+40° 랜덤 오프셋 → 부채꼴로 퍼져서 포위
        chaseAngleOffset = Random.Range(-40f, 40f) * Mathf.Deg2Rad;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogError($"[EnemyAI] Player not found! {gameObject.name}");
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
            return;
        }

        // 넉백 처리 (넉백 중에는 이동 불가)
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;
            // 감속하면서 날아감
            float t = knockbackTimer > 0f ? knockbackTimer / 0.3f : 0f;
            transform.position += (Vector3)(knockbackVelocity * t * Time.deltaTime);
            return;
        }

        // Stun check
        if (stunTimer > 0f)
        {
            stunTimer -= Time.deltaTime;
            return;
        }

        // Slow check
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
        }
        else
        {
            slowMultiplier = 1f;
        }

        // 분노 타이머
        if (rageTimer > 0f)
        {
            rageTimer -= Time.deltaTime;
            rageMultiplier = rageSpeedMult;
            // 분노 중 빨간 틴트
            if (cachedSR != null)
                cachedSR.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), 0.5f);
        }
        else if (rageMultiplier > 1f)
        {
            rageMultiplier = 1f;
            if (cachedSR != null)
                cachedSR.color = Color.white;
        }

        // Chase player — 각도 오프셋으로 포위하듯 접근
        Vector2 myPos = transform.position;
        Vector2 toPlayer = (Vector2)player.position - myPos;
        float distToPlayer = toPlayer.magnitude;
        Vector2 direction = toPlayer.normalized;

        // 거리에 따라 오프셋 강도 조절: 멀면 옆으로 돌아오고, 가까우면 직선
        float offsetStrength = Mathf.InverseLerp(offsetZeroDist, offsetFullDist, distToPlayer);
        float currentOffset = chaseAngleOffset * offsetStrength;

        // 방향 벡터를 오프셋만큼 회전
        float cos = Mathf.Cos(currentOffset);
        float sin = Mathf.Sin(currentOffset);
        Vector2 offsetDir = new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        );

        Vector2 move = offsetDir * moveSpeed * slowMultiplier * rageMultiplier * Time.deltaTime;

        // 몬스터 겹침 방지: 가까운 적과 서로 밀어냄
        int count = Physics2D.OverlapCircle(myPos, separationRadius, new ContactFilter2D().NoFilter(), overlapBuffer);
        Vector2 separation = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            var other = overlapBuffer[i];
            if (other == null || other.gameObject == gameObject) continue;
            if (!other.CompareTag("Enemy")) continue;

            Vector2 diff = myPos - (Vector2)other.transform.position;
            float dist = diff.magnitude;
            if (dist > 0.01f && dist < separationRadius)
            {
                // 가까울수록 강하게 밀어냄
                separation += diff.normalized * (separationRadius - dist) / separationRadius;
            }
        }

        move += separation * separationForce * Time.deltaTime;
        transform.position += (Vector3)move;

        // 좌우 플립
        if (cachedSR != null && direction.x != 0f)
            cachedSR.flipX = direction.x < 0;

        // Despawn if too far from player
        float despawnDist = Vector2.Distance(transform.position, player.position);
        if (despawnDist > 60f)
        {
            Destroy(gameObject);
        }
    }

    public void Stun(float duration)
    {
        stunTimer = duration;
    }

    public void ApplySlow(float multiplier, float duration)
    {
        slowMultiplier = multiplier;
        slowTimer = duration;
    }

    /// <summary>넉백: direction 방향으로 force 세기만큼 밀려남. 넉백 후 분노 상태 진입</summary>
    public void Knockback(Vector2 direction, float force)
    {
        knockbackVelocity = direction.normalized * force;
        knockbackTimer = 0.5f;
        // 넉백 후 분노: 더 빨리 돌아옴
        rageTimer = rageDuration;
    }
}
