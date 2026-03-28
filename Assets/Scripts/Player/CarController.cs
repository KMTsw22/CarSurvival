using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class CarController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerStats stats;
    private Vector2 moveInput;

    [Header("Visual")]
    public Transform spriteTransform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        rb.gravityScale = 0f;
        rb.drag = 3f;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Keyboard input (for testing, joystick will override)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            moveInput = new Vector2(h, v).normalized;
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            rb.velocity = moveInput * stats.moveSpeed;

            // Rotate car to face movement direction
            float angle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                Quaternion.Euler(0, 0, angle),
                Time.fixedDeltaTime * 10f);
        }
        else
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * 5f);
        }
    }

    public void SetMoveInput(Vector2 input)
    {
        moveInput = input;
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
