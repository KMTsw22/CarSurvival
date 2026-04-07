using UnityEngine;
using System.Collections.Generic;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 20f;
    public float currentHealth;
    public int expDrop = 1;
    public int goldDrop = 5;
    public GameObject expPickupPrefab;
    public GameObject fuelPickupPrefab;
    public float fuelDropRate = 0f;
    public GameObject toolboxPickupPrefab;
    [HideInInspector] public float damageReduction;
    [HideInInspector] public bool isMiniBoss = false;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private float flashTimer;
    private const float flashDuration = 0.15f;
    private bool isFlashing;

    // 피격 스케일 효과
    private Vector3 originalScale;
    private float scaleTimer;
    private const float scaleDuration = 0.12f;
    private const float scaleAmount = 1.2f;
    private bool isScaling;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        originalScale = transform.localScale;
        // MonsterBounce가 Start에서 자식 SR을 만드므로 한 프레임 뒤에 캐시
        Invoke(nameof(CacheSpriteRenderers), 0f);
    }

    private void CacheSpriteRenderers()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        // 활성화된 렌더러만 필터링
        var active = new System.Collections.Generic.List<SpriteRenderer>();
        foreach (var sr in spriteRenderers)
        {
            if (sr.enabled)
                active.Add(sr);
        }
        spriteRenderers = active.ToArray();
        originalColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            originalColors[i] = spriteRenderers[i].color;
    }

    private void Update()
    {
        // 색상 플래시 복귀
        if (isFlashing)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f)
            {
                isFlashing = false;
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] != null)
                        spriteRenderers[i].color = originalColors[i];
                }
            }
        }

        // 스케일 펀치 복귀
        if (isScaling)
        {
            scaleTimer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(scaleTimer / scaleDuration);
            transform.localScale = Vector3.Lerp(originalScale * scaleAmount, originalScale, t);
            if (scaleTimer <= 0f)
            {
                isScaling = false;
                transform.localScale = originalScale;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, Vector2.zero, 0f);
    }

    public void TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce)
    {
        if (damageReduction > 0f)
            damage *= (1f - damageReduction);
        currentHealth -= damage;

        // 빨간색 플래시
        if (spriteRenderers != null && spriteRenderers.Length > 0)
        {
            isFlashing = true;
            flashTimer = flashDuration;
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].color = Color.red;
            }
        }

        // 살짝 커졌다 돌아오는 스케일 펀치
        isScaling = true;
        scaleTimer = scaleDuration;

        // 넉백 적용
        if (knockbackForce > 0f)
        {
            var ai = GetComponent<EnemyAI>();
            if (ai != null)
                ai.Knockback(knockbackDir, knockbackForce);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // 20% 확률로 exp를 드롭하지 않는 약한 몬스터 목록
    private static readonly HashSet<string> weakMonsters = new HashSet<string>
    {
        "MON_001", "MON_027", // 타이어 좀비
        "MON_002", "MON_028", // 오일 슬라임
        "MON_006", "MON_032", // Cone Head
        "MON_004", "MON_030", // 스파크 러너
    };

    private void Die()
    {
        // 사망 파티클 폭발
        SpawnDeathParticles();

        // 콤보 등록
        if (ComboSystem.Instance != null)
            ComboSystem.Instance.RegisterKill();

        // Drop experience pickups
        if (expPickupPrefab != null)
        {
            // 약한 몬스터: 20% 확률로 exp 드롭 스킵
            var identifier = GetComponent<EnemyIdentifier>();
            bool skipExp = identifier != null
                && weakMonsters.Contains(identifier.monId)
                && Random.value < 0.2f;

            if (!skipExp)
            {
                Vector3 offset = Random.insideUnitCircle * 0.3f;
                var pickup = Instantiate(expPickupPrefab, transform.position + offset, Quaternion.identity);
                var expComp = pickup.GetComponent<ExperiencePickup>();
                if (expComp != null)
                    expComp.expAmount = expDrop;
                pickup.SetActive(true);
            }
        }

        // Notify stats
        PlayerStats player = PlayerStats.Instance;
        if (player != null)
        {
            player.enemiesKilled++;
            player.gold += goldDrop;
        }

        // 연료 아이템 드롭 (확률)
        if (fuelPickupPrefab != null && fuelDropRate > 0f && Random.value * 100f < fuelDropRate)
        {
            Vector3 offset = Random.insideUnitCircle * 0.3f;
            var fuel = Instantiate(fuelPickupPrefab, transform.position + offset, Quaternion.identity);
            fuel.SetActive(true);
        }

        // 공구상자 드롭 (신호등 몬스터 전용 - 100%)
        if (toolboxPickupPrefab != null)
        {
            var toolbox = Instantiate(toolboxPickupPrefab, transform.position, Quaternion.identity);
            toolbox.SetActive(true);
        }

        // 열쇠 아이템 드롭 (미니보스 처치 시 확정)
        if (isMiniBoss && StageManager.Instance != null && !StageManager.Instance.CanSummonBoss)
        {
            StageManager.Instance.AddKey(1);
        }

        // 미니보스 처치 트로피 표시
        if (isMiniBoss && StageManager.Instance != null)
        {
            StageManager.Instance.NotifyMiniBossKilled();
        }

        Destroy(gameObject);
    }

    private void SpawnDeathParticles()
    {
        var go = new GameObject("DeathBurst");
        go.transform.position = transform.position;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 28f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, new Color(1f, 0.2f, 0f));
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 25) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

        // 기본 머티리얼 (스프라이트 없이 파티클 렌더링)
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));

        ps.Play();
    }
}
