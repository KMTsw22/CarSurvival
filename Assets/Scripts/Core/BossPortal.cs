using UnityEngine;

public class BossPortal : MonoBehaviour
{
    private float pulseTimer;
    private Vector3 baseScale;
    private SpriteRenderer sr;

    private void Start()
    {
        baseScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();

        // 트리거 콜라이더 추가
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 1.5f;
    }

    private void Update()
    {
        // 펄스 애니메이션
        pulseTimer += Time.deltaTime;
        float scale = 1f + Mathf.Sin(pulseTimer * 2f) * 0.08f;
        transform.localScale = baseScale * scale;

        // 회전
        transform.Rotate(0, 0, 30f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (StageManager.Instance != null)
            StageManager.Instance.OnPortalEntered();
    }
}
