using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 20f;
    public float currentHealth;
    public int expDrop = 1;
    public int goldDrop = 5;
    public GameObject expPickupPrefab;

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

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        // Drop experience pickups
        if (expPickupPrefab != null)
        {
            for (int i = 0; i < expDrop; i++)
            {
                Vector3 offset = Random.insideUnitCircle * 0.5f;
                var pickup = Instantiate(expPickupPrefab, transform.position + offset, Quaternion.identity);
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

        // 열쇠 아이템 드롭 (확률)
        if (StageManager.Instance != null && !StageManager.Instance.CanSummonBoss)
        {
            if (Random.value < 0.15f)
                StageManager.Instance.AddKey(1);
        }

        Destroy(gameObject);
    }
}
