using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class CarController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerStats stats;
    private Vector2 moveInput;
    private Vector2 currentDirection;
    public Vector2 CurrentDirection => currentDirection;

    [Header("Movement")]
    public float turnSpeed = 720f;

    [Header("Visual")]
    public Transform spriteTransform;

    [Header("Booster")]
    public float boosterMaxGauge = 100f;
    public float boosterDrainRate = 100f / 3f;      // 3초에 100 소모
    public float boosterRechargeRate = 100f / 10f;   // 10초에 100 충전
    public float boosterRechargeDelay = 1f;          // 사용 중지 후 1초 뒤 충전 시작
    public float boosterMaxSpeedMult = 2f;             // 최대 200%
    public float boosterRampUpTime = 0.5f;           // 0→150% 도달 시간

    private float boosterGauge;
    private float boosterRechargeTimer;
    private float currentBoostMult = 1f;
    private bool isBoosting;

    public event Action<float, float> OnBoosterChanged; // current, max

    public float BoosterGauge => boosterGauge;
    public float BoosterMax => boosterMaxGauge;
    public bool IsBoosting => isBoosting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        boosterGauge = boosterMaxGauge;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // 키보드 입력
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
            moveInput = new Vector2(h, v).normalized;
        else
            moveInput = Vector2.zero;

        UpdateBooster();
    }

    private void UpdateBooster()
    {
        bool wantBoost = Input.GetKey(KeyCode.Space) && boosterGauge > 0f && moveInput.sqrMagnitude > 0.01f;

        if (wantBoost)
        {
            isBoosting = true;
            boosterGauge -= boosterDrainRate * Time.deltaTime;
            boosterGauge = Mathf.Max(0f, boosterGauge);
            boosterRechargeTimer = boosterRechargeDelay;

            // 부드럽게 가속
            currentBoostMult = Mathf.MoveTowards(currentBoostMult, boosterMaxSpeedMult,
                (boosterMaxSpeedMult - 1f) / boosterRampUpTime * Time.deltaTime);
        }
        else
        {
            isBoosting = false;
            // 부드럽게 감속
            currentBoostMult = Mathf.MoveTowards(currentBoostMult, 1f,
                (boosterMaxSpeedMult - 1f) / boosterRampUpTime * Time.deltaTime);

            // 충전 딜레이
            boosterRechargeTimer -= Time.deltaTime;
            if (boosterRechargeTimer <= 0f && boosterGauge < boosterMaxGauge)
            {
                boosterGauge += boosterRechargeRate * Time.deltaTime;
                boosterGauge = Mathf.Min(boosterMaxGauge, boosterGauge);
            }
        }

        OnBoosterChanged?.Invoke(boosterGauge, boosterMaxGauge);
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        if (moveInput.sqrMagnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            float currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;

            if (currentDirection.sqrMagnitude < 0.01f)
                currentAngle = targetAngle;

            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, turnSpeed * Time.fixedDeltaTime);
            float rad = newAngle * Mathf.Deg2Rad;
            currentDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            rb.linearVelocity = currentDirection * stats.moveSpeed * currentBoostMult;

            float spriteAngle = newAngle - 90f;
            transform.rotation = Quaternion.Euler(0, 0, spriteAngle);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
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
            stats.TakeDamage(5f * Time.fixedDeltaTime);
        }
    }
}
