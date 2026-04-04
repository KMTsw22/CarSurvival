using UnityEngine;

/// <summary>
/// 하늘에서 몬스터까지 연결된 번개 볼트.
/// 즉시 나타나서 페이드아웃 후 소멸.
/// </summary>
public class LightningStrikeAnim : MonoBehaviour
{
    public Vector3 targetPos;
    public float fadeTime = 0.3f;

    private SpriteRenderer sr;
    private float fadeTimer;
    private float startAlpha;

    private void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        startAlpha = sr.color.a;
        fadeTimer = fadeTime;
    }

    private void Update()
    {
        fadeTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(fadeTimer / fadeTime) * startAlpha;
        sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);

        if (fadeTimer <= 0f)
            Destroy(gameObject);
    }
}
