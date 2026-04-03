using UnityEngine;

/// <summary>
/// 레이저 캐논: 마우스 방향으로 관통 레이저 발사.
/// etc1=기본레이저수, etc2=레벨당레이저추가
/// </summary>
public class LaserCannonEffect : MonoBehaviour
{
    public float damage = 30f;
    public float laserLength = 20f;
    public float lifetime = 0.3f;

    private LineRenderer lineRenderer;
    private float timer;

    public void Fire(Vector3 origin, Vector2 direction, float dmg, float length)
    {
        damage = dmg;
        laserLength = length;
        timer = lifetime;
        transform.position = origin;

        // 레이저 라인
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.25f;
        lineRenderer.endWidth = 0.08f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(1f, 0.2f, 0.2f, 1f);
        lineRenderer.endColor = new Color(1f, 0.5f, 0.3f, 0.5f);
        lineRenderer.sortingOrder = 15;

        Vector3 endPos = origin + (Vector3)(direction * laserLength);
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, endPos);

        // 레이저 경로상의 모든 적에게 데미지 (관통)
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, laserLength);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (!hit.collider.CompareTag("Enemy")) continue;
            var eh = hit.collider.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);
        }
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // 페이드 아웃
        float alpha = Mathf.Clamp01(timer / lifetime);
        if (lineRenderer != null)
        {
            lineRenderer.startColor = new Color(1f, 0.2f, 0.2f, alpha);
            lineRenderer.endColor = new Color(1f, 0.5f, 0.3f, alpha * 0.5f);
            lineRenderer.startWidth = 0.25f * alpha;
            lineRenderer.endWidth = 0.08f * alpha;
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
