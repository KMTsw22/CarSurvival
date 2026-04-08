using UnityEngine;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class CarController : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerStats stats;
    private float throttleInput;   // W/S: +1 전진, -1 후진
    private float steerInput;      // A/D: -1 좌회전, +1 우회전
    private float facingAngle;     // 차가 바라보는 각도 (도)
    private Vector2 currentDirection;
    public Vector2 CurrentDirection => currentDirection;

    [Header("Movement")]
    public float turnSpeed = 300f;          // 회전 속도 (도/초)
    public float reverseSpeedRatio = 0.5f;  // 후진 속도 비율

    [Header("Visual")]
    public Transform spriteTransform;

    [Header("Booster Shield")]
    public float boosterDamage = 1500f;         // 돌진 시 적에게 주는 데미지
    public float boosterShieldRadius = 6.0f;     // 방어막 크기

    [Header("Booster")]
    public float boosterMaxGauge = 100f;
    public float boosterDrainRate = 100f / 3f;      // 3초에 100 소모
    public float boosterRechargeRate = 100f / 3f;    // 3초에 100 충전
    public float boosterRechargeDelay = 1f;          // 사용 중지 후 1초 뒤 충전 시작
    public float boosterMaxSpeedMult = 2f;             // 최대 200%
    public float boosterRampUpTime = 0.5f;           // 0→150% 도달 시간

    private float boosterGauge;
    private float boosterRechargeTimer;
    private float currentBoostMult = 1f;
    private bool isBoosting;

    public event Action<float, float> OnBoosterChanged; // current, max

    private GameObject shieldVisual;

    public float BoosterGauge => boosterGauge;
    public float BoosterMax => boosterMaxGauge;
    public bool IsBoosting => isBoosting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        boosterGauge = boosterMaxGauge;
        facingAngle = 90f; // 초기 방향: 위쪽
        CreateShieldVisual();
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // 키보드 입력: W/S = 전진/후진, A/D = 좌/우 회전
        throttleInput = Input.GetAxisRaw("Vertical");   // W=+1, S=-1
        steerInput = Input.GetAxisRaw("Horizontal");    // A=-1, D=+1

        UpdateBooster();
    }

    private void UpdateBooster()
    {
        bool wantBoost = Input.GetKey(KeyCode.Space) && boosterGauge > 0f && Mathf.Abs(throttleInput) > 0.01f;

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
        UpdateShieldVisual();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // A/D 회전: 이동 중일 때만 회전
        if (Mathf.Abs(throttleInput) > 0.01f)
        {
            // 후진 시 조향 방향 반전
            float steerSign = throttleInput >= 0 ? 1f : -1f;
            facingAngle -= steerInput * steerSign * turnSpeed * Time.fixedDeltaTime;
        }

        // 현재 방향 벡터 갱신
        float rad = facingAngle * Mathf.Deg2Rad;
        currentDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        // 스프라이트 회전 (항상 적용)
        transform.rotation = Quaternion.Euler(0, 0, facingAngle - 90f);

        if (Mathf.Abs(throttleInput) > 0.01f)
        {
            // W/S 전진/후진
            float speedMult = throttleInput > 0 ? 1f : reverseSpeedRatio;
            rb.linearVelocity = currentDirection * throttleInput * stats.moveSpeed * speedMult * currentBoostMult;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        DetectEnemyContact();
    }

    public Vector2 GetMoveDirection()
    {
        return currentDirection.sqrMagnitude > 0.01f ? currentDirection : transform.up;
    }

    [Header("Contact Detection")]
    public float contactRadius = 1.0f;
    private readonly List<Collider2D> contactBuffer = new List<Collider2D>();

    // 부스터 충돌 이펙트 쿨다운 (과도한 흔들림 방지)
    private float boostImpactCooldown;

    private void DetectEnemyContact()
    {
        if (boostImpactCooldown > 0f)
            boostImpactCooldown -= Time.fixedDeltaTime;

        int count = Physics2D.OverlapCircle(transform.position, contactRadius, ContactFilter2D.noFilter, contactBuffer);
        for (int i = 0; i < count; i++)
        {
            var col = contactBuffer[i];
            if (col == null || !col.CompareTag("Enemy")) continue;

            if (isBoosting)
            {
                // 부스터 중: 적에게 데미지 + 넉백
                var eh = col.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    Vector2 knockDir = ((Vector2)col.transform.position - (Vector2)transform.position).normalized;
                    eh.TakeDamage(boosterDamage * Time.fixedDeltaTime, knockDir, 60f);

                    // 박치기 콤보 등록 (같은 적은 한 번만 카운트)
                    if (ComboSystem.Instance != null)
                        ComboSystem.Instance.RegisterRamHit(col.GetInstanceID());
                }

                // 부스터 중: 무적 (데미지 없음)

                // 이펙트 (쿨다운으로 프레임마다 발동 방지)
                if (boostImpactCooldown <= 0f)
                {
                    if (CameraFollow.Instance != null)
                        CameraFollow.Instance.Shake(0.8f, 0.25f);
                    if (ScreenEffects.Instance != null)
                        ScreenEffects.Instance.HitStop(0.06f);
                    boostImpactCooldown = 0.2f;
                }
            }
            else
            {
                // 비부스터: 플레이어가 데미지 받음 (100%)
                var ai = col.GetComponent<EnemyAI>();
                float dmg = ai != null ? ai.contactDamage : 10f;
                stats.TakeDamage(dmg * Time.fixedDeltaTime);
            }
        }
    }

    private void CreateShieldVisual()
    {
        shieldVisual = new GameObject("BoosterShield");
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.zero;

        var sr = shieldVisual.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(1f, 0.9f, 0f, 0.35f); // 노란색 반투명
        sr.sortingOrder = 10;
        shieldVisual.transform.localScale = Vector3.one * boosterShieldRadius * 2f;
        shieldVisual.SetActive(false);
    }

    private void UpdateShieldVisual()
    {
        if (shieldVisual == null) return;
        shieldVisual.SetActive(isBoosting);
    }

    private static Sprite CreateCircleSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist < radius)
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
