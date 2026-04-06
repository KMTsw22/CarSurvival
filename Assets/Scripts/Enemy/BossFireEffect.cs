using UnityEngine;

/// <summary>
/// 보스가 이동할 때 뒤에 불꽃 자취를 남김.
/// 이동 방향에 따라 불꽃이 회전하고 자연스럽게 흩어짐.
/// </summary>
public class BossFireEffect : MonoBehaviour
{
    private Sprite[] frames;
    private Vector3 prevPos;
    private float spawnTimer;
    private float spawnInterval = 0.06f;
    private float minMoveDist = 0.03f;
    private Vector3 smoothDir; // 부드러운 이동 방향
    private float monsterScale = 1f;

    public void Initialize()
    {
        int frameCount = 5;
        frames = new Sprite[frameCount];

        for (int i = 0; i < frameCount; i++)
        {
            frames[i] = Resources.Load<Sprite>($"Sprites/Monsters/MonsterParticle/spr_boss_fire_{i}");
            if (frames[i] == null)
            {
                var tex = Resources.Load<Texture2D>($"Sprites/Monsters/MonsterParticle/spr_boss_fire_{i}");
                if (tex != null)
                    frames[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            }
        }

        if (frames[0] == null)
        {
            Debug.LogWarning("[BossFireEffect] spr_boss_fire_0 not found");
            frames = null;
            return;
        }

        prevPos = transform.position;
        smoothDir = Vector3.down;
        monsterScale = transform.localScale.x;
        Debug.Log($"[BossFireEffect] Loaded {frameCount} frames, scale={monsterScale}");
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - prevPos;
        float moved = delta.magnitude;

        if (moved > minMoveDist)
        {
            // 방향을 부드럽게 보간
            smoothDir = Vector3.Lerp(smoothDir, delta.normalized, Time.deltaTime * 10f);

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;

                Vector3 backDir = -smoothDir.normalized;
                // 이동 방향의 수직 벡터 (좌우 흩뿌리기용)
                Vector3 sideDir = new Vector3(-backDir.y, backDir.x, 0f);

                // 뒤쪽 + 좌우로 흩어지게 (몬스터 크기 비례)
                float sideOffset = Random.Range(-0.6f, 0.6f) * monsterScale;
                float backOffset = Random.Range(0.5f, 1.2f) * monsterScale;
                Vector3 spawnPos = currentPos + backDir * backOffset + sideDir * sideOffset;

                // 이동 방향 기준 회전 각도
                float angle = Mathf.Atan2(backDir.y, backDir.x) * Mathf.Rad2Deg;

                SpawnFireParticle(spawnPos, angle + Random.Range(-20f, 20f));
            }
        }

        prevPos = currentPos;
    }

    private void SpawnFireParticle(Vector3 pos, float angle)
    {
        var particle = new GameObject("FireTrail");
        particle.transform.position = pos;
        particle.transform.rotation = Quaternion.Euler(0, 0, angle);
        particle.transform.localScale = Vector3.one * Random.Range(0.6f, 1.1f) * monsterScale;

        var sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = frames[Random.Range(0, frames.Length)]; // 랜덤 프레임으로 시작
        sr.sortingOrder = 6;

        var anim = particle.AddComponent<FireParticleAnim>();
        anim.Initialize(frames, 0.07f, Random.Range(0.8f, 1.2f), smoothDir.normalized);
    }
}

/// <summary>
/// 개별 불꽃 파티클 — 프레임 애니메이션 + 이동 방향 반대로 흘러가며 페이드아웃.
/// </summary>
public class FireParticleAnim : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer sr;
    private int currentFrame;
    private float frameTimer;
    private float frameInterval;
    private float lifetime;
    private float elapsed;
    private Vector3 driftDir; // 흘러가는 방향
    private float driftSpeed;

    public void Initialize(Sprite[] sprites, float interval, float life, Vector3 moveDir)
    {
        frames = sprites;
        frameInterval = interval;
        lifetime = life;
        sr = GetComponent<SpriteRenderer>();

        // 이동 반대 방향으로 천천히 흘러감
        driftDir = -moveDir;
        driftSpeed = Random.Range(1.5f, 3f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // 프레임 애니메이션
        frameTimer += Time.deltaTime;
        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval;
            currentFrame = (currentFrame + 1) % frames.Length;
            sr.sprite = frames[currentFrame];
        }

        // 뒤쪽으로 흘러가면서 점점 느려짐
        float t = elapsed / lifetime;
        float currentSpeed = Mathf.Lerp(driftSpeed, 0f, t * t);
        transform.position += driftDir * currentSpeed * Time.deltaTime;

        // 페이드아웃
        var color = sr.color;
        color.a = Mathf.Lerp(0.8f, 0f, t * t); // 후반에 급격히 사라짐
        sr.color = color;

        // 약간 커지면서 사라짐 (연기 퍼지는 느낌)
        float scaleMulti = Mathf.Lerp(1f, 0.5f, t);
        transform.localScale = Vector3.one * scaleMulti * transform.localScale.x;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }
}
