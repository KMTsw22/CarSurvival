using UnityEngine;

/// <summary>
/// 신호등 포탑: 초록 → 노랑 → 빨강 순환하며 각 상태마다 능력이 변한다.
/// - 초록(Green): 가장 가까운 적에게 총알 발사 (공격)
/// - 노랑(Yellow): 범위 내 적 이동속도 감소 (감속)
/// - 빨강(Red): 범위 내 모든 적에게 폭발 데미지 (폭발)
/// </summary>
public class TrafficLightTurret : MonoBehaviour
{
    public enum Phase { Green, Yellow, Red }

    [Header("General")]
    public float duration = 30f;
    public float phaseInterval = 4f;
    public float detectRadius = 5f;

    [Header("Green - Attack")]
    public float bulletDamage = 15f;
    public float fireRate = 0.4f;
    public float bulletSpeed = 12f;

    [Header("Yellow - Slow")]
    public float slowPercent = 0.4f;
    public float slowDuration = 1.5f;

    [Header("Red - Explosion")]
    public float explosionDamage = 30f;
    public float explosionRadius = 4f;

    [Header("Sprites")]
    public Sprite greenSprite;
    public Sprite yellowSprite;
    public Sprite redSprite;

    private SpriteRenderer sr;
    private Phase currentPhase = Phase.Green;
    private float phaseTimer;
    private float fireTimer;
    private float elapsed;

    // 범위 표시
    private SpriteRenderer rangeIndicator;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;

        // 범위 원 표시
        var rangeObj = new GameObject("TurretRange");
        rangeObj.transform.SetParent(transform);
        rangeObj.transform.localPosition = Vector3.zero;
        rangeIndicator = rangeObj.AddComponent<SpriteRenderer>();
        rangeIndicator.sprite = CreateCircleSprite();
        rangeIndicator.sortingOrder = 0;
        float diameter = detectRadius * 2f;
        rangeObj.transform.localScale = new Vector3(diameter, diameter, 1f);

        ApplyPhase();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            Destroy(gameObject);
            return;
        }

        // 마지막 5초 깜빡임
        if (duration - elapsed <= 5f)
        {
            float blink = Mathf.PingPong(Time.time * 4f, 1f);
            if (rangeIndicator != null)
            {
                var c = rangeIndicator.color;
                rangeIndicator.color = new Color(c.r, c.g, c.b, 0.05f + 0.1f * blink);
            }
        }

        // 페이즈 전환
        phaseTimer += Time.deltaTime;
        if (phaseTimer >= phaseInterval)
        {
            phaseTimer = 0f;
            currentPhase = (Phase)(((int)currentPhase + 1) % 3);
            ApplyPhase();
        }

        // 현재 페이즈 능력 실행
        switch (currentPhase)
        {
            case Phase.Green:
                UpdateGreen();
                break;
            case Phase.Yellow:
                UpdateYellow();
                break;
            case Phase.Red:
                UpdateRed();
                break;
        }
    }

    private void ApplyPhase()
    {
        switch (currentPhase)
        {
            case Phase.Green:
                if (greenSprite != null) sr.sprite = greenSprite;
                if (rangeIndicator != null)
                    rangeIndicator.color = new Color(0.2f, 0.9f, 0.3f, 0.12f);
                break;
            case Phase.Yellow:
                if (yellowSprite != null) sr.sprite = yellowSprite;
                if (rangeIndicator != null)
                    rangeIndicator.color = new Color(0.9f, 0.9f, 0.2f, 0.12f);
                break;
            case Phase.Red:
                if (redSprite != null) sr.sprite = redSprite;
                if (rangeIndicator != null)
                    rangeIndicator.color = new Color(0.9f, 0.2f, 0.2f, 0.12f);
                // 빨간불 전환 시 즉시 폭발
                Explode();
                break;
        }
    }

    // ── 초록: 가장 가까운 적에게 총알 발사 ──
    private void UpdateGreen()
    {
        fireTimer -= Time.deltaTime;
        if (fireTimer > 0f) return;

        var target = FindNearestEnemy(detectRadius);
        if (target == null) return;

        fireTimer = fireRate;
        FireBullet(target);
    }

    private void FireBullet(Transform target)
    {
        Vector2 dir = (target.position - transform.position).normalized;

        var bulletObj = new GameObject("TurretBullet");
        bulletObj.transform.position = transform.position;
        bulletObj.tag = "PlayerProjectile";

        var sr2 = bulletObj.AddComponent<SpriteRenderer>();
        sr2.sprite = CreateSquareSprite();
        sr2.color = new Color(0.3f, 1f, 0.4f);
        sr2.sortingOrder = 3;
        bulletObj.transform.localScale = Vector3.one * 0.15f;

        var col = bulletObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        var bullet = bulletObj.AddComponent<Bullet>();
        bullet.Initialize(dir, bulletSpeed, bulletDamage, 3f);
    }

    // ── 노랑: 범위 내 적 감속 ──
    private void UpdateYellow()
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, detectRadius);
        foreach (var hit in enemies)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var ai = hit.GetComponent<EnemyAI>();
            if (ai != null)
                ai.ApplySlow(slowPercent, slowDuration);
        }
    }

    // ── 빨강: 범위 내 폭발 데미지 ──
    private void UpdateRed()
    {
        // 빨간불은 전환 시 한 번만 폭발 (ApplyPhase에서 호출)
    }

    private void Explode()
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in enemies)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var eh = hit.GetComponent<EnemyHealth>();
            if (eh != null)
                eh.TakeDamage(explosionDamage);
        }

        // 폭발 이펙트
        var fx = new GameObject("ExplosionFX");
        fx.transform.position = transform.position;
        var fxSr = fx.AddComponent<SpriteRenderer>();
        fxSr.sprite = CreateCircleSprite();
        fxSr.color = new Color(1f, 0.3f, 0.2f, 0.5f);
        fxSr.sortingOrder = 10;
        float d = explosionRadius * 2f;
        fx.transform.localScale = new Vector3(d, d, 1f);
        Destroy(fx, 0.3f);
    }

    // ── Helpers ──
    private Transform FindNearestEnemy(float range)
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, range);
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var hit in enemies)
        {
            if (!hit.CompareTag("Enemy")) continue;
            float dist = Vector2.Distance(transform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit.transform;
            }
        }
        return nearest;
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;
        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }
}
