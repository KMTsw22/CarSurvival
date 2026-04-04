using UnityEngine;

/// <summary>
/// 레이저 캐논: 마우스 방향으로 관통 레이저 발사.
/// EFT_Laser_Cannon 스프라이트를 사용하여 빔 표현.
/// etc1=기본레이저수, etc2=레벨당레이저추가
/// </summary>
public class LaserCannonEffect : MonoBehaviour
{
    public float damage = 30f;
    public float laserLength = 20f;
    public float lifetime = 0.3f;

    private SpriteRenderer sr;
    private float timer;
    private float startAlpha;

    public void Fire(Vector3 origin, Vector2 direction, float dmg, float length)
    {
        damage = dmg;
        laserLength = length;
        timer = lifetime;

        // 스프라이트 로드 (pivot을 왼쪽 중앙으로 → 원점에서 오른쪽으로 뻗음)
        sr = gameObject.AddComponent<SpriteRenderer>();
        var tex = Resources.Load<Texture2D>("Sprites/Icons/SkillEffect/EFT_Laser_Cannon");
        if (tex != null)
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0.5f), 100f);

        sr.sortingOrder = 15;
        sr.color = new Color(1f, 1f, 1f, 0.8f);
        startAlpha = 0.8f;

        // 위치 & 회전
        transform.position = origin;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 스프라이트를 레이저 길이에 맞게 스케일
        if (sr.sprite != null)
        {
            float spriteW = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
            float spriteH = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
            float scaleX = laserLength / spriteW;
            float beamWidth = 0.5f;
            float scaleY = beamWidth / spriteH;
            transform.localScale = new Vector3(scaleX, scaleY, 1f);
        }

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
        float alpha = Mathf.Clamp01(timer / lifetime) * startAlpha;
        if (sr != null)
            sr.color = new Color(1f, 1f, 1f, alpha);

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
