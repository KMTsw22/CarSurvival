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
        rb.gravityScale = 0f;
        rb.drag = 2f;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        // Stun check
        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            rb.velocity = Vector2.zero;
            return;
        }

        // Slow check
        if (slowTimer > 0f)
        {
            slowTimer -= Time.fixedDeltaTime;
        }
        else
        {
            slowMultiplier = 1f;
        }

        // Chase player
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.velocity = direction * moveSpeed * slowMultiplier;

        // Rotate to face player
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0, 0, angle);
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

    // Despawn if too far from player
    private void Update()
    {
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > 30f)
            {
                Destroy(gameObject);
            }
        }
    }
}
