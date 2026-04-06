using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체인 라이트닝: 가장 가까운 적에게 번개를 발사하고, 주변 적에게 연쇄 전이.
/// etc1=기본타격인원, etc2=레벨당추가인원
/// </summary>
public class ChainLightningEffect : MonoBehaviour
{
    public float damage = 25f;
    public int chainCount = 3;
    public float chainRange = 5f;
    public float lifetime = 0.4f;

    private float timer;

    public void Fire(Vector3 origin, float dmg, int chains, float range)
    {
        damage = dmg;
        chainCount = chains;
        chainRange = range;
        transform.position = origin;

        // 적 탐색 + 체인
        var enemies = FindChainTargets(origin);
        if (enemies.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        // 번개 이펙트 먼저 생성 후 데미지 적용 (스케일 펀치 전에 위치 캡처)
        foreach (var enemy in enemies)
        {
            SpawnLightningStrike(enemy);
            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);
        }

        timer = lifetime;
    }

    private List<Transform> FindChainTargets(Vector3 origin)
    {
        var result = new List<Transform>();
        var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (allEnemies.Length == 0) return result;

        Vector3 currentPos = origin;
        var used = new HashSet<EntityId>();

        for (int i = 0; i < chainCount; i++)
        {
            float bestDist = float.MaxValue;
            GameObject best = null;

            foreach (var e in allEnemies)
            {
                if (e == null || used.Contains(e.GetEntityId())) continue;
                float dist = Vector3.Distance(currentPos, e.transform.position);
                // 첫 타격은 넓은 범위, 이후는 chainRange
                float maxDist = i == 0 ? chainRange * 2f : chainRange;
                if (dist < bestDist && dist <= maxDist)
                {
                    bestDist = dist;
                    best = e;
                }
            }

            if (best == null) break;
            used.Add(best.GetEntityId());
            result.Add(best.transform);
            currentPos = best.transform.position;
        }

        return result;
    }

    private void SpawnLightningStrike(Transform enemy)
    {
        Vector3 targetPos = enemy.position;
        float boltHeight = 3f;
        float monsterScale = Mathf.Max(enemy.localScale.x, enemy.localScale.y);
        float scaleX = monsterScale * 0.6f;

        var obj = new GameObject("LightningBolt");
        var sr = obj.AddComponent<SpriteRenderer>();

        // pivot을 하단 중앙(0.5, 0)으로 스프라이트 재생성
        var srcSprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_Chain_Lightning");
        if (srcSprite != null)
        {
            var tex = srcSprite.texture;
            var rect = srcSprite.textureRect;
            sr.sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), srcSprite.pixelsPerUnit);
        }

        sr.sortingOrder = 16;
        sr.color = new Color(1f, 0.9f, 0.3f, 0.6f);

        // 몬스터 위치에 배치 → pivot이 하단이므로 위로만 뻗음
        obj.transform.position = targetPos;
        obj.transform.localScale = new Vector3(scaleX, boltHeight, 1f);

        var anim = obj.AddComponent<LightningStrikeAnim>();
        anim.targetPos = targetPos;
        anim.fadeTime = 0.3f;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
