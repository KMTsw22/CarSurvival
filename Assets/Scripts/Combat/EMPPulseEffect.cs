using UnityEngine;

/// <summary>
/// EMP 펄스: 범위 내 모든 적에게 데미지 + 스턴.
/// etc1=기본지속시간(초), etc2=기본반경, etc3=레벨당반경증가
/// </summary>
public class EMPPulseEffect : MonoBehaviour
{
    public float damage = 10f;
    public float stunDuration = 2f;
    public float radius = 3f;
    public float expandSpeed = 15f;

    private SpriteRenderer sr;
    private float currentScale;
    private float targetScale;
    private float lifetime;
    private float timer;
    private bool hasDamaged;

    public void Fire(Vector3 position, float dmg, float stun, float rad)
    {
        damage = dmg;
        stunDuration = stun;
        radius = rad;
        transform.position = position;
        targetScale = radius * 2f;
        lifetime = 0.6f;
        timer = lifetime;

        sr = gameObject.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EMP Pulse-in game-removebg");
        sr.sprite = sprite != null ? sprite : CreateCircleSprite();
        sr.color = new Color(0.3f, 0.6f, 1f, 0.5f);
        sr.sortingOrder = 12;

        currentScale = 0f;
        transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // 확장 애니메이션
        currentScale = Mathf.MoveTowards(currentScale, targetScale, expandSpeed * Time.deltaTime);
        transform.localScale = Vector3.one * currentScale;

        // 확장 완료 시 데미지
        if (!hasDamaged && currentScale >= targetScale * 0.9f)
        {
            hasDamaged = true;
            ApplyDamage();
        }

        // 페이드 아웃
        float alpha = Mathf.Clamp01(timer / lifetime) * 0.5f;
        sr.color = new Color(0.3f, 0.6f, 1f, alpha);

        if (timer <= 0f)
            Destroy(gameObject);
    }

    private void ApplyDamage()
    {
        var enemies = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var col in enemies)
        {
            if (!col.CompareTag("Enemy")) continue;
            var eh = col.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);
            var ai = col.GetComponent<EnemyAI>();
            if (ai != null) ai.Stun(stunDuration);
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
