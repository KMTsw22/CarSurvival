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

    private List<Transform> hitTargets = new List<Transform>();
    private LineRenderer lineRenderer;
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

        // 데미지 적용
        foreach (var enemy in enemies)
        {
            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null) eh.TakeDamage(damage);
        }

        // 번개 라인 렌더러
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.12f;
        lineRenderer.endWidth = 0.06f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = new Color(0.4f, 0.7f, 1f, 1f);
        lineRenderer.endColor = new Color(0.8f, 0.9f, 1f, 0.6f);
        lineRenderer.sortingOrder = 15;

        lineRenderer.positionCount = enemies.Count + 1;
        lineRenderer.SetPosition(0, origin);
        for (int i = 0; i < enemies.Count; i++)
            lineRenderer.SetPosition(i + 1, enemies[i].position);

        timer = lifetime;
    }

    private List<Transform> FindChainTargets(Vector3 origin)
    {
        var result = new List<Transform>();
        var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (allEnemies.Length == 0) return result;

        Vector3 currentPos = origin;
        var used = new HashSet<int>();

        for (int i = 0; i < chainCount; i++)
        {
            float bestDist = float.MaxValue;
            GameObject best = null;

            foreach (var e in allEnemies)
            {
                if (e == null || used.Contains(e.GetInstanceID())) continue;
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
            used.Add(best.GetInstanceID());
            result.Add(best.transform);
            currentPos = best.transform.position;
        }

        return result;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        // 번개 지글지글 효과
        if (lineRenderer != null && lineRenderer.positionCount > 1)
        {
            for (int i = 1; i < lineRenderer.positionCount - 1; i++)
            {
                Vector3 pos = lineRenderer.GetPosition(i);
                pos += (Vector3)Random.insideUnitCircle * 0.08f;
                lineRenderer.SetPosition(i, pos);
            }

            float alpha = Mathf.Clamp01(timer / lifetime);
            lineRenderer.startColor = new Color(0.4f, 0.7f, 1f, alpha);
            lineRenderer.endColor = new Color(0.8f, 0.9f, 1f, alpha * 0.6f);
        }

        if (timer <= 0f)
            Destroy(gameObject);
    }
}
