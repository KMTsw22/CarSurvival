using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float contactDamage = 10f;

    private Transform player;
    private Rigidbody2D rb;
    private float stunTimer;
    private float slowMultiplier = 1f;
    private float slowTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
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

        // Chase player — transform 직접 이동
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        transform.position += (Vector3)(direction * moveSpeed * slowMultiplier * Time.deltaTime);

        // 좌우 플립
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && direction.x != 0f)
            sr.flipX = direction.x < 0;

        // Despawn if too far from player
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > 40f)
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
}
