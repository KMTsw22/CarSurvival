using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 화염방사기: 차량 전방에 화염 영역 생성, 지속 데미지.
/// etc1=기본지속시간(초), etc2=기본반경, etc3=레벨당반경증가
/// </summary>
public class FlamethrowerEffect : MonoBehaviour
{
    public float damage = 8f;
    public float radius = 2f;
    public float duration = 3f;

    private float timer;
    private SpriteRenderer sr;
    private Dictionary<int, float> hitTimers = new Dictionary<int, float>();
    private float damageInterval = 0.25f;
    private CircleCollider2D col;

    public void Fire(Vector3 position, Vector2 direction, float dmg, float rad, float dur)
    {
        damage = dmg;
        radius = rad;
        duration = dur;
        timer = duration;

        // 차량 앞쪽에 배치
        transform.position = position + (Vector3)direction * (radius * 0.8f);

        // 콜라이더
        col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        // Rigidbody (트리거 감지용)
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 비주얼
        sr = gameObject.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/Flamethrower-in game-removebg");
        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else
        {
            sr.sprite = CreateFlameSprite();
        }
        sr.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        sr.sortingOrder = 11;
        transform.localScale = Vector3.one * radius;

        gameObject.tag = "PlayerProjectile";
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // 불꽃 깜빡임
        float flicker = 0.5f + Mathf.PingPong(Time.time * 3f, 0.3f);
        float alpha = Mathf.Clamp01(timer / duration) * flicker;
        sr.color = new Color(1f, 0.5f + Random.Range(0f, 0.2f), 0.2f, alpha);

        // 스케일 미세 흔들림
        float s = radius * (1f + Mathf.Sin(Time.time * 8f) * 0.05f);
        transform.localScale = Vector3.one * s;

        if (timer <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        int id = other.GetInstanceID();
        if (hitTimers.ContainsKey(id) && Time.time < hitTimers[id]) return;
        hitTimers[id] = Time.time + damageInterval;

        var eh = other.GetComponent<EnemyHealth>();
        if (eh != null) eh.TakeDamage(damage * damageInterval);
    }

    private Sprite CreateFlameSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        float center = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = dist / center;
                if (norm <= 1f)
                {
                    float a = (1f - norm) * 0.9f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, a));
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
