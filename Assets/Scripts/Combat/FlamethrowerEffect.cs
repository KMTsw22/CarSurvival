using UnityEngine;

/// <summary>
/// 화염방사기: 차량 앞에 상시 부착, 닿은 적에게 화상(BurnEffect) 부여.
/// AutoAttack에서 한 번만 생성하고 차량의 자식으로 붙임.
/// </summary>
public class FlamethrowerEffect : MonoBehaviour
{
    public float burnDamage = 8f;
    public float burnDuration = 3f;
    public float radius = 2f;

    // 에디터 조정용
    public float offsetDist = 3f;
    public float scaleX = 0.18f;
    public float scaleY = 0.5f;
    public float alpha = 0.6f;

    private SpriteRenderer sr;
    private Transform player;
    private CarController carController;

    public void Setup(Transform playerTransform, float dmg, float rad, float offset = 3f, float duration = 3f)
    {
        player = playerTransform;
        carController = player.GetComponent<CarController>();
        burnDamage = dmg;
        burnDuration = duration;
        radius = rad;
        offsetDist = Mathf.Max(offset, 3f);

        // 콜라이더
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        // Rigidbody (트리거 감지용)
        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // 비주얼
        sr = gameObject.AddComponent<SpriteRenderer>();
        var sprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_Flamethrower");
        if (sprite != null)
            sr.sprite = sprite;
        else
            sr.sprite = CreateFlameSprite();

        sr.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        sr.sortingOrder = 11;
        transform.localScale = new Vector3(radius * 0.5f, radius * 0.3f, 1f);

        gameObject.tag = "PlayerProjectile";
    }

    public void UpdateStats(float dmg, float rad, float duration = 3f)
    {
        burnDamage = dmg;
        burnDuration = duration;
        radius = rad;
        transform.localScale = new Vector3(radius * 0.5f, radius * 0.3f, 1f);
        var col = GetComponent<CircleCollider2D>();
        if (col != null) col.radius = radius;
    }

    private void Update()
    {
        if (player == null) return;

        // 차량 진행 방향 고정
        Vector2 dir = carController != null ? carController.CurrentDirection : Vector2.up;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
        transform.position = player.position + (Vector3)dir * offsetDist;

        // 방향에 맞춰 회전
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 불꽃 깜빡임
        float flicker = 0.5f + Mathf.PingPong(Time.time * 3f, 0.3f);
        sr.color = new Color(1f, 0.5f + Random.Range(0f, 0.2f), 0.2f, alpha * flicker / 0.6f);

        // 스케일 미세 흔들림
        float sx = radius * scaleX * (1f + Mathf.Sin(Time.time * 8f) * 0.05f);
        float sy = radius * scaleY * (1f + Mathf.Sin(Time.time * 8f) * 0.05f);
        transform.localScale = new Vector3(sx, sy, 1f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;

        // 화상 부여 (이미 있으면 갱신, 중첩 안 됨)
        var burn = other.GetComponent<BurnEffect>();
        if (burn == null)
            burn = other.gameObject.AddComponent<BurnEffect>();

        burn.Apply(burnDamage, burnDuration);
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
