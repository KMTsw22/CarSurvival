using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 신호등 몬스터 색상 변환 + 색별 능력:
/// - 빨강: 감속 빔 — 범위 내 플레이어 이속 30% 감소 + 플레이어 파란 잔상
/// - 노랑: 광역 전기 충격 — 범위 내 주기적 최대체력 5% 데미지 + 전기 이펙트
/// - 초록: 약화 빔 — 범위 내 플레이어 공격력 30% 감소 + 플레이어 어두워짐
/// 10초마다 자기 색 제외 랜덤 색으로 변신.
/// </summary>
public class TrafficLightColor : MonoBehaviour
{
    public enum LightColor { Red, Yellow, Green }

    private SpriteRenderer sr;
    public LightColor currentColor = LightColor.Red;
    private float colorTimer;
    private float abilityTimer;
    private const float CHANGE_INTERVAL = 10f;

    // 빨강: 감속 빔
    private const float RED_RANGE = 10.5f;
    private const float RED_SLOW_PERCENT = 0.3f;

    // 노랑: 전기 충격
    private const float YELLOW_RANGE = 7.5f;
    private const float YELLOW_HP_PERCENT = 0.05f;
    private const float YELLOW_TICK_RATE = 0.8f;

    // 초록: 플레이어 공격력 감소
    private const float GREEN_RANGE = 9f;
    private const float GREEN_ATK_REDUCTION = 0.3f;

    private Sprite sprRed;
    private Sprite sprYellow;
    private Sprite sprGreen;

    private Transform player;
    private LineRenderer beamLine;
    private float beamFlickerTimer;

    // 추적 가속
    private const float CHASE_DIST = 20f;
    private const float CHASE_SPEED_MULT = 10f;
    private EnemyAI enemyAI;
    private float baseSpeed;

    // 디버프 상태 추적
    private bool isSlowing;
    private bool isWeakening;

    // 플레이어 이펙트
    private SpriteRenderer playerSr;
    private Color playerOrigColor;
    private float slowTrailTimer;
    private float greenPulseTimer;

    // 노랑: 전기 오라
    private GameObject yellowAura;
    private SpriteRenderer yellowAuraSr;
    private float yellowAuraPulse;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        sprRed = Resources.Load<Sprite>("Sprites/Monsters/spr_trafficlight_red");
        sprYellow = Resources.Load<Sprite>("Sprites/Monsters/spr_trafficlight_yellow");
        sprGreen = Resources.Load<Sprite>("Sprites/Monsters/spr_trafficlight_green");

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerSr = playerObj.GetComponent<SpriteRenderer>();
            if (playerSr != null)
                playerOrigColor = playerSr.color;
        }

        enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
            baseSpeed = enemyAI.moveSpeed;

        colorTimer = CHANGE_INTERVAL;
        abilityTimer = 1f;

        CreateBeamLine();
        ApplyColorVisual();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // 색 변환 타이머
        colorTimer -= Time.deltaTime;
        if (colorTimer <= 0f)
        {
            colorTimer = CHANGE_INTERVAL;
            ChangeColor();
        }

        // 거리 20 이상이면 10배속 추적
        if (enemyAI != null && player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            enemyAI.moveSpeed = dist >= CHASE_DIST ? baseSpeed * CHASE_SPEED_MULT : baseSpeed;
        }

        // 색별 능력
        abilityTimer -= Time.deltaTime;
        switch (currentColor)
        {
            case LightColor.Red:
                UpdateRedBeam();
                break;
            case LightColor.Yellow:
                UpdateYellowShock();
                break;
            case LightColor.Green:
                UpdateGreenWeaken();
                break;
        }
    }

    private void ChangeColor()
    {
        CleanupCurrentAbility();

        LightColor next;
        do
        {
            next = (LightColor)Random.Range(0, 3);
        } while (next == currentColor);

        currentColor = next;
        abilityTimer = 0.5f;
        Debug.Log($"[TrafficLight] Color changed to: {currentColor}");

        ApplyColorVisual();
    }

    private void ApplyColorVisual()
    {
        if (sr == null) return;

        switch (currentColor)
        {
            case LightColor.Red:    sr.sprite = sprRed;    break;
            case LightColor.Yellow: sr.sprite = sprYellow; break;
            case LightColor.Green:  sr.sprite = sprGreen;  break;
        }

        if (beamLine != null)
            beamLine.enabled = false;
    }

    private void CleanupCurrentAbility()
    {
        if (beamLine != null) beamLine.enabled = false;

        // 노랑 오라 숨기기
        if (yellowAura != null) yellowAura.SetActive(false);

        // 감속 해제 + 플레이어 색 복원
        if (isSlowing)
        {
            isSlowing = false;
            var stats = PlayerStats.Instance;
            if (stats != null) stats.RecalculateStats();
            RestorePlayerColor();
        }

        // 공격력 감소 해제 + 플레이어 색 복원
        if (isWeakening)
        {
            isWeakening = false;
            var stats = PlayerStats.Instance;
            if (stats != null) stats.RecalculateStats();
            RestorePlayerColor();
        }
    }

    // ══════════════════════════════════════
    // 빨강: 감속 빔 + 플레이어 파란 잔상
    // ══════════════════════════════════════
    private void UpdateRedBeam()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= RED_RANGE;
        var stats = PlayerStats.Instance;

        if (inRange)
        {
            if (beamLine != null)
            {
                beamLine.enabled = true;
                UpdateBeamVisual();
            }

            if (stats != null)
            {
                if (!isSlowing)
                    isSlowing = true;
                stats.RecalculateStats();
                stats.moveSpeed *= (1f - RED_SLOW_PERCENT);
            }

            // 플레이어 파란색 틴트 (느려진 느낌)
            if (playerSr != null)
            {
                float pulse = 0.7f + Mathf.PingPong(Time.time * 2f, 0.3f);
                playerSr.color = new Color(0.5f * pulse, 0.6f * pulse, 1f, 1f);
            }

            // 잔상 이펙트
            slowTrailTimer -= Time.deltaTime;
            if (slowTrailTimer <= 0f)
            {
                slowTrailTimer = 0.15f;
                SpawnSlowTrail();
            }
        }
        else
        {
            if (beamLine != null) beamLine.enabled = false;

            if (isSlowing)
            {
                isSlowing = false;
                if (stats != null) stats.RecalculateStats();
                RestorePlayerColor();
            }
        }
    }

    private void SpawnSlowTrail()
    {
        if (player == null || playerSr == null || playerSr.sprite == null) return;

        var trail = new GameObject("SlowTrail");
        trail.transform.position = player.position;
        trail.transform.rotation = player.rotation;
        trail.transform.localScale = player.localScale;

        var trailSr = trail.AddComponent<SpriteRenderer>();
        trailSr.sprite = playerSr.sprite;
        trailSr.color = new Color(0.3f, 0.5f, 1f, 0.4f);
        trailSr.sortingOrder = playerSr.sortingOrder - 1;

        var fade = trail.AddComponent<FadeAndDestroy>();
        fade.duration = 0.4f;
    }

    private void UpdateBeamVisual()
    {
        if (beamLine == null || player == null) return;

        Vector3 start = transform.position;
        Vector3 end = player.position;
        Vector3 dir = (end - start).normalized;
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f);

        beamFlickerTimer += Time.deltaTime;
        if (beamFlickerTimer > 0.05f)
        {
            beamFlickerTimer = 0f;

            beamLine.positionCount = 6;
            beamLine.SetPosition(0, start);
            for (int i = 1; i < 5; i++)
            {
                float t = i / 5f;
                Vector3 point = Vector3.Lerp(start, end, t);
                point += perp * Random.Range(-0.4f, 0.4f);
                beamLine.SetPosition(i, point);
            }
            beamLine.SetPosition(5, end);

            float flicker = Random.Range(0.6f, 1f);
            Color beamColor;
            switch (currentColor)
            {
                case LightColor.Red:    beamColor = new Color(1f, 0.3f, 0.3f, 1f); break;
                case LightColor.Green:  beamColor = new Color(0.3f, 1f, 0.4f, 1f); break;
                default:                beamColor = new Color(1f, 0.9f, 0.3f, 1f); break;
            }
            beamLine.startColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.8f * flicker);
            beamLine.endColor = new Color(beamColor.r, beamColor.g, beamColor.b, 0.5f * flicker);
        }
    }

    // ══════════════════════════════════════
    // 노랑: 전기 오라 (지속) + 범위 내 주기적 데미지
    // ══════════════════════════════════════
    private void UpdateYellowShock()
    {
        if (player == null) return;

        // 전기 오라 항상 표시
        EnsureYellowAura();
        if (yellowAura != null)
        {
            yellowAura.SetActive(true);
            yellowAura.transform.position = transform.position;

            float d = YELLOW_RANGE * 2f;
            yellowAura.transform.localScale = new Vector3(d, d, 1f);
        }

        // 범위 내 데미지
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= YELLOW_RANGE)
        {
            abilityTimer -= Time.deltaTime;
            if (abilityTimer <= 0f)
            {
                abilityTimer = YELLOW_TICK_RATE;
                var stats = PlayerStats.Instance;
                if (stats != null)
                    stats.TakeDamage(stats.maxHealth * YELLOW_HP_PERCENT);
            }
        }
    }

    private void EnsureYellowAura()
    {
        if (yellowAura != null) return;

        yellowAura = new GameObject("YellowAura");
        yellowAura.transform.SetParent(transform);
        yellowAura.transform.localPosition = Vector3.zero;

        yellowAuraSr = yellowAura.AddComponent<SpriteRenderer>();
        yellowAuraSr.sprite = CreateCircleSprite();
        yellowAuraSr.color = new Color(1f, 0.95f, 0.3f, 0.12f);
        yellowAuraSr.sortingOrder = 0;

        float d = YELLOW_RANGE * 2f;
        yellowAura.transform.localScale = new Vector3(d, d, 1f);
    }

    // ══════════════════════════════════════
    // 초록: 플레이어 공격력 30% 감소 + 어두워짐
    // ══════════════════════════════════════
    private void UpdateGreenWeaken()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inRange = dist <= GREEN_RANGE;
        var stats = PlayerStats.Instance;

        if (inRange)
        {
            if (beamLine != null)
            {
                beamLine.enabled = true;
                UpdateBeamVisual();
            }

            if (stats != null)
            {
                if (!isWeakening)
                    isWeakening = true;
                stats.RecalculateStats();
                stats.damage *= (1f - GREEN_ATK_REDUCTION);
            }

            // 플레이어 어두워짐 (약해진 느낌) + 초록 펄스
            if (playerSr != null)
            {
                greenPulseTimer += Time.deltaTime;
                float pulse = 0.5f + Mathf.PingPong(greenPulseTimer * 1.5f, 0.2f);
                playerSr.color = new Color(pulse, pulse * 0.8f, pulse, 1f);
            }
        }
        else
        {
            if (beamLine != null) beamLine.enabled = false;

            if (isWeakening)
            {
                isWeakening = false;
                if (stats != null) stats.RecalculateStats();
                RestorePlayerColor();
                greenPulseTimer = 0f;
            }
        }
    }

    // ── 공통 ──
    private void RestorePlayerColor()
    {
        if (playerSr != null)
            playerSr.color = playerOrigColor;
    }

    private void OnDestroy()
    {
        CleanupCurrentAbility();
    }

    private void CreateBeamLine()
    {
        beamLine = gameObject.AddComponent<LineRenderer>();
        beamLine.startWidth = 0.15f;
        beamLine.endWidth = 0.08f;
        beamLine.sortingOrder = 15;
        beamLine.material = new Material(Shader.Find("Sprites/Default"));
        beamLine.startColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        beamLine.endColor = new Color(1f, 0.5f, 0.5f, 0.5f);
        beamLine.enabled = false;
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
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, d <= radius ? Color.white : Color.clear);
            }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}

/// <summary>페이드 후 삭제 (감속 잔상용)</summary>
public class FadeAndDestroy : MonoBehaviour
{
    public float duration = 0.4f;
    private float elapsed;
    private SpriteRenderer sr;

    private void Start() { sr = GetComponent<SpriteRenderer>(); }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (sr != null)
        {
            float a = Mathf.Lerp(0.4f, 0f, elapsed / duration);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, a);
        }
        if (elapsed >= duration) Destroy(gameObject);
    }
}

