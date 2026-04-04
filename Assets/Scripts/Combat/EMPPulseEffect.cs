using UnityEngine;

/// <summary>
/// EMP 펄스: 플레이어 주변 상시 자기장. 범위 내 적에게 지속 데미지.
/// etc1=타격간격(초), etc2=기본반경, etc3=레벨당반경증가
/// </summary>
public class EMPPulseEffect : MonoBehaviour
{
    public float damage = 10f;
    public float radius = 3f;
    public float hitInterval = 0.5f;

    private Transform player;
    private SpriteRenderer sr;
    private float hitTimer;

    public void Setup(Transform playerTransform, float dmg, float rad, float interval)
    {
        player = playerTransform;
        damage = dmg;
        radius = rad;
        hitInterval = interval;

        // 비주얼
        sr = gameObject.AddComponent<SpriteRenderer>();
        // 텍스처 전체를 스프라이트로 사용
        Sprite sprite = null;
        var tex = Resources.Load<Texture2D>("Sprites/Icons/SkillEffect/EFT_EMP_Pulse");
        if (tex != null)
            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        sr.sprite = sprite != null ? sprite : CreateCircleSprite();

        sr.color = new Color(1f, 1f, 1f, 0.3f);
        sr.sortingOrder = -1;
        ApplyScale();

        gameObject.tag = "PlayerProjectile";
    }

    public void UpdateStats(float dmg, float rad, float interval)
    {
        damage = dmg;
        radius = rad;
        hitInterval = interval;
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (sr.sprite != null)
        {
            float diameter = radius * 2f;
            float spriteW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            float spriteH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
            transform.localScale = new Vector3(diameter / spriteW, diameter / spriteH, 1f);
        }
        else
        {
            transform.localScale = Vector3.one * radius * 2f;
        }
    }

    private void Update()
    {
        if (player == null) return;

        // 플레이어 따라다니기
        transform.position = player.position;

        // 자기장 시각 효과: 회전 + 깜빡임
        transform.Rotate(0, 0, 30f * Time.deltaTime);
        float pulse = 0.35f + Mathf.PingPong(Time.time * 0.5f, 0.15f);
        sr.color = new Color(1f, 1f, 1f, pulse);

        // 주기적 데미지
        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitTimer = hitInterval;
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var enemy in enemies)
        {
            if (!enemy.CompareTag("Enemy")) continue;
            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);
        }
    }

    private Sprite CreateCircleSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        float r = center - 1;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (dist <= r)
                {
                    float edge = Mathf.Clamp01(1f - (dist / r));
                    tex.SetPixel(x, y, new Color(1, 1, 1, edge * 0.8f));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
