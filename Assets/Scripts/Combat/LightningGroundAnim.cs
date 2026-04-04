using UnityEngine;

/// <summary>
/// 바닥 번개 충격파: 빠르게 퍼지면서 페이드아웃.
/// LightningStrikeAnim에서 착지 시 생성.
/// </summary>
public class LightningGroundAnim : MonoBehaviour
{
    public float duration = 0.4f;

    private SpriteRenderer sr;
    private float timer;
    private Vector3 startScale;
    private float startAlpha;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startAlpha = sr.color.a;
        startScale = transform.localScale;
        timer = duration;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        float t = 1f - Mathf.Clamp01(timer / duration);

        // 바깥으로 퍼지는 효과
        float expand = 1f + t * 0.8f;
        transform.localScale = startScale * expand;

        // 페이드아웃
        float alpha = Mathf.Lerp(startAlpha, 0f, t);
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
