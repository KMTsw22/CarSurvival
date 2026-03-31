using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class CarController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerStats stats;
    private Vector2 moveInput;
    private Vector2 currentDirection;

    [Header("Movement")]
    public float turnSpeed = 720f; // 초당 회전 각도

    [Header("Visual")]
    public Transform spriteTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        rb.gravityScale = 0f;
        rb.drag = 0f;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // 키보드 입력
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            moveInput = new Vector2(h, v).normalized;
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            // 현재 방향에서 목표 방향으로 부드럽게 회전
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            if (currentDirection.sqrMagnitude < 0.01f)
            {
                currentAngle = targetAngle;
            }

            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);
            float rad = newAngle * Mathf.Deg2Rad;
            currentDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            rb.velocity = currentDirection * stats.moveSpeed;

            // 스프라이트 회전
            float spriteAngle = newAngle - 90f;
            transform.rotation = Quaternion.Euler(0, 0, spriteAngle);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }

    }

    public Vector2 GetMoveDirection()
    {
        return moveInput.sqrMagnitude > 0.01f ? moveInput : transform.up;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Collision damage
            stats.TakeDamage(5f * Time.fixedDeltaTime);
        }
    }
}
