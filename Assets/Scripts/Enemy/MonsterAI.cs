using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 몬스터 스킬 AI — TB_MonsterSkill 테이블 기반으로 스킬을 쿨타임에 따라 실행.
/// EnemyAI(이동)와 함께 사용.
/// </summary>
public class MonsterAI : MonoBehaviour
{
    private Transform player;
    private List<SkillState> skills = new List<SkillState>();
    private SkillState activeSkill;
    private EnemyAI enemyAI;

    private class SkillState
    {
        public MonsterSkillRow data;
        public float cooldownTimer;
        public bool isReady => cooldownTimer <= 0f;
    }

    public void Initialize(MonsterSkillRow[] skillRows)
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        enemyAI = GetComponent<EnemyAI>();

        foreach (var row in skillRows)
        {
            skills.Add(new SkillState
            {
                data = row,
                cooldownTimer = 2f // 시작 시 2초 유예
            });
            Debug.Log($"[MonsterAI] Skill loaded: {row.skill_id} type={row.skill_type} priority={row.priority}");
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (player == null) return;

        // 스킬 실행 중이면 대기
        if (activeSkill != null) return;

        // 쿨타임 감소
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].cooldownTimer > 0f)
                skills[i].cooldownTimer -= Time.deltaTime;
        }

        // 준비된 스킬 중 랜덤 선택
        var readySkills = new List<SkillState>();
        foreach (var s in skills)
        {
            if (s.isReady)
                readySkills.Add(s);
        }

        if (readySkills.Count > 0)
            ExecuteSkill(readySkills[Random.Range(0, readySkills.Count)]);
    }

    private void ExecuteSkill(SkillState skill)
    {
        activeSkill = skill;
        Debug.Log($"[MonsterAI] Executing: {skill.data.skill_id} type={skill.data.skill_type}");

        switch (skill.data.skill_type)
        {
            case "Charge":
                StartCoroutine(DoCharge(skill));
                break;
            case "Projectile":
                StartCoroutine(DoProjectile(skill));
                break;
            case "GroundAttack":
                StartCoroutine(DoGroundAttack(skill));
                break;
            case "SlaveSpawn":
                StartCoroutine(DoSlaveSpawn(skill));
                break;
            default:
                FinishSkill(skill);
                break;
        }
    }

    private void FinishSkill(SkillState skill)
    {
        skill.cooldownTimer = skill.data.cooldown;
        activeSkill = null;

        // 스킬 사용 후 이동 재개
        if (enemyAI != null)
            enemyAI.enabled = true;
    }

    // ═══════════════════════════════════════
    // Charge — 빠르게 플레이어에게 돌진
    // ═══════════════════════════════════════
    private System.Collections.IEnumerator DoCharge(SkillState skill)
    {
        var data = skill.data;

        if (enemyAI != null) enemyAI.enabled = false;
        var sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;

        // 돌진 방향 미리 결정
        Vector2 chargeDir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // 경고 라인 표시
        var warningLine = CreateChargeWarning((Vector2)transform.position, chargeDir, data.speed * data.duration);

        // 경고 시간 동안 깜빡임
        float warningElapsed = 0f;
        while (warningElapsed < data.warning_time)
        {
            if (sr != null)
                sr.color = Mathf.PingPong(warningElapsed * 8f, 1f) > 0.5f ? Color.red : origColor;
            warningElapsed += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = origColor;
        if (warningLine != null) Destroy(warningLine);

        // 아레나 범위 가져오기
        var arenaObj = GameObject.Find("BossArena");
        Vector2 arenaCenter = arenaObj != null ? (Vector2)arenaObj.transform.position : (Vector2)transform.position;
        float arenaHalfW = 48f, arenaHalfH = 27f;
        if (arenaObj != null)
        {
            // 벽 위치에서 아레나 크기 추정
            var walls = arenaObj.GetComponentsInChildren<BoxCollider2D>();
            foreach (var w in walls)
            {
                if (w.gameObject.name == "Wall_Right")
                    arenaHalfW = w.transform.position.x - arenaCenter.x - w.size.x / 2f;
                if (w.gameObject.name == "Wall_Top")
                    arenaHalfH = w.transform.position.y - arenaCenter.y - w.size.y / 2f;
            }
        }

        // 돌진
        float elapsed = 0f;
        while (elapsed < data.duration)
        {
            Vector3 nextPos = transform.position + (Vector3)(chargeDir * data.speed * Time.deltaTime);

            // 아레나 밖으로 못 나가게 클램핑
            float margin = 1f;
            nextPos.x = Mathf.Clamp(nextPos.x, arenaCenter.x - arenaHalfW + margin, arenaCenter.x + arenaHalfW - margin);
            nextPos.y = Mathf.Clamp(nextPos.y, arenaCenter.y - arenaHalfH + margin, arenaCenter.y + arenaHalfH - margin);
            transform.position = nextPos;

            if (sr != null && chargeDir.x != 0f)
                sr.flipX = chargeDir.x < 0;

            elapsed += Time.deltaTime;
            yield return null;
        }

        FinishSkill(skill);
    }

    private GameObject CreateChargeWarning(Vector2 from, Vector2 dir, float length)
    {
        var warning = new GameObject("ChargeWarning");

        // 직선 라인으로 돌진 경로 표시
        var lr = warning.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, (Vector3)from);
        lr.SetPosition(1, (Vector3)(from + dir * length));
        lr.startWidth = 0.8f;
        lr.endWidth = 0.3f;
        lr.sortingOrder = 15;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.2f, 0f, 0.8f);
        lr.endColor = new Color(1f, 0.2f, 0f, 0.2f);

        StartCoroutine(AnimateChargeWarning(warning));

        return warning;
    }

    private System.Collections.IEnumerator AnimateChargeWarning(GameObject warning)
    {
        if (warning == null) yield break;

        var lr = warning.GetComponent<LineRenderer>();
        float elapsed = 0f;

        while (warning != null && elapsed < 10f)
        {
            if (lr != null)
            {
                float pulse = Mathf.PingPong(elapsed * 4f, 1f) * 0.3f + 0.7f;
                lr.startColor = new Color(1f, 0.2f, 0f, 0.8f * pulse);
                lr.endColor = new Color(1f, 0.2f, 0f, 0.2f * pulse);
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ═══════════════════════════════════════
    // Projectile — 타이어 던지기
    // ═══════════════════════════════════════
    private System.Collections.IEnumerator DoProjectile(SkillState skill)
    {
        var data = skill.data;
        if (enemyAI != null) enemyAI.enabled = false;

        // 잠깐 멈춤
        yield return new WaitForSeconds(0.3f);

        Vector2 baseDir = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // count 개수만큼 부채꼴로 발사
        float spreadAngle = 15f;
        float startAngle = -(data.count - 1) / 2f * spreadAngle;

        for (int i = 0; i < data.count; i++)
        {
            float angle = startAngle + i * spreadAngle;
            Vector2 dir = RotateVector(baseDir, angle);
            SpawnProjectile(dir, data);
        }

        yield return new WaitForSeconds(0.3f);
        FinishSkill(skill);
    }

    private void SpawnProjectile(Vector2 dir, MonsterSkillRow data)
    {
        var proj = new GameObject("MonsterProjectile");
        proj.transform.position = transform.position;
        proj.layer = LayerMask.NameToLayer("Default");

        var sr = proj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // 스프라이트 로드 시도 (Sprite → Texture2D 폴백)
        Sprite sprite = null;
        string[] paths = {
            $"Sprites/Monsters/MonsterParticle/{data.sprite_key}",
            $"Sprites/Effects/{data.sprite_key}",
            $"Sprites/Monsters/{data.sprite_key}"
        };
        if (!string.IsNullOrEmpty(data.sprite_key))
        {
            foreach (var p in paths)
            {
                sprite = Resources.Load<Sprite>(p);
                if (sprite != null) break;
                var tex = Resources.Load<Texture2D>(p);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100f);
                    break;
                }
            }
        }
        if (sprite != null)
            sr.sprite = sprite;
        else
            sr.color = Color.gray;

        proj.transform.localScale = Vector3.one * 0.3f;

        var col = proj.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;
        col.isTrigger = true;

        var rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        var bullet = proj.AddComponent<MonsterBullet>();
        bullet.Initialize(dir, data.speed, data.damage, 5f);
    }

    // ═══════════════════════════════════════
    // GroundAttack — 플레이어 발밑에서 솟아오름
    // ═══════════════════════════════════════
    private System.Collections.IEnumerator DoGroundAttack(SkillState skill)
    {
        var data = skill.data;
        if (enemyAI != null) enemyAI.enabled = false;

        for (int i = 0; i < data.count; i++)
        {
            if (player == null) break;

            // 플레이어 현재 위치 + 약간의 랜덤 오프셋
            Vector2 targetPos = (Vector2)player.position + Random.insideUnitCircle * data.range * 0.5f;
            StartCoroutine(SpawnGroundAttack(targetPos, data));

            yield return new WaitForSeconds(0.3f);
        }

        // 모든 돌출이 끝날 때까지 대기
        yield return new WaitForSeconds(data.warning_time + data.duration);
        FinishSkill(skill);
    }

    private System.Collections.IEnumerator SpawnGroundAttack(Vector2 pos, MonsterSkillRow data)
    {
        // 경고 표시 — 작은 원에서 커지면서 깜빡임
        var warning = new GameObject("GroundWarning");
        warning.transform.position = pos;
        var warnSr = warning.AddComponent<SpriteRenderer>();
        warnSr.sprite = CreateCircleSprite();
        warnSr.sortingOrder = 5;

        Vector3 targetScale = Vector3.one * data.range;
        float warnElapsed = 0f;
        while (warnElapsed < data.warning_time)
        {
            float t = warnElapsed / data.warning_time;
            // 작은 원에서 점점 커짐
            warning.transform.localScale = Vector3.Lerp(targetScale * 0.3f, targetScale, t);
            // 깜빡이면서 점점 밝아짐
            float pulse = Mathf.PingPong(warnElapsed * 6f, 1f);
            float alpha = Mathf.Lerp(0.15f, 0.5f, t) * (0.7f + pulse * 0.3f);
            warnSr.color = new Color(1f, 0.15f, 0f, alpha);
            warnElapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(warning);

        // 돌출 공격
        var spike = new GameObject("GroundFire");
        spike.transform.position = pos;

        var sr = spike.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;

        // 스프라이트 로드 (Sprite → Texture2D 폴백)
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(data.sprite_key))
        {
            string[] gPaths = {
                $"Sprites/Monsters/MonsterParticle/{data.sprite_key}",
                $"Sprites/Effects/{data.sprite_key}"
            };
            foreach (var p in gPaths)
            {
                sprite = Resources.Load<Sprite>(p);
                if (sprite != null) break;
                var tex = Resources.Load<Texture2D>(p);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100f);
                    break;
                }
            }
        }
        if (sprite != null)
        {
            sr.sprite = sprite;
            // 경고 원 크기(64px 기준)와 맞추기
            float targetWorldSize = data.range * 64f / 100f; // 경고 원의 월드 크기
            float spriteWorldW = sprite.bounds.size.x;
            float matchScale = targetWorldSize / spriteWorldW;
            spike.transform.localScale = Vector3.one * matchScale;
        }
        else
        {
            sr.sprite = CreateCircleSprite();
            sr.color = new Color(1f, 0.3f, 0f, 0.9f);
            spike.transform.localScale = Vector3.one * data.range;
        }

        var dmg = spike.AddComponent<MonsterDamageZone>();
        dmg.damage = data.damage;
        dmg.lifetime = data.duration;

        // 솟아오르는 스케일 애니메이션
        float elapsed = 0f;
        Vector3 fullScale = spike.transform.localScale;
        while (elapsed < 0.25f)
        {
            float t = elapsed / 0.25f;
            spike.transform.localScale = fullScale * t;
            elapsed += Time.deltaTime;
            yield return null;
        }
        spike.transform.localScale = fullScale;

        // 지속 후 페이드아웃
        float fadeStart = data.duration - 0.5f;
        yield return new WaitForSeconds(Mathf.Max(0f, fadeStart - 0.25f));

        float fadeElapsed = 0f;
        while (fadeElapsed < 0.5f)
        {
            float t = fadeElapsed / 0.5f;
            var color = sr.color;
            color.a = Mathf.Lerp(1f, 0f, t);
            sr.color = color;
            fadeElapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(spike);
    }

    // ═══════════════════════════════════════
    // SlaveSpawn — 부하 몬스터 소환
    // ═══════════════════════════════════════
    private System.Collections.IEnumerator DoSlaveSpawn(SkillState skill)
    {
        var data = skill.data;
        if (enemyAI != null) enemyAI.enabled = false;

        // 소환 전 경고 연출
        var sr = GetComponent<SpriteRenderer>();
        Color origColor = sr != null ? sr.color : Color.white;
        float warnElapsed = 0f;
        while (warnElapsed < data.warning_time)
        {
            if (sr != null)
                sr.color = Mathf.PingPong(warnElapsed * 6f, 1f) > 0.5f ? Color.yellow : origColor;
            warnElapsed += Time.deltaTime;
            yield return null;
        }
        if (sr != null) sr.color = origColor;

        // sprite_key로 소환할 몬스터 ID 결정
        string slaveMonId = FindMonIdBySpriteKey(data.sprite_key);
        if (string.IsNullOrEmpty(slaveMonId))
        {
            Debug.LogWarning($"[MonsterAI] SlaveSpawn: mon not found for sprite_key={data.sprite_key}");
            FinishSkill(skill);
            yield break;
        }

        // 소환 충격파 이펙트
        StartCoroutine(SpawnShockwave(transform.position));
        yield return new WaitForSeconds(0.3f);

        // EnemySpawner를 통해 소환
        var spawner = FindAnyObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            // etc_value1: 0=플레이어 주변 원형, 1=아레나 내 랜덤
            bool randomSpawn = data.etc_value1 > 0f;

            if (randomSpawn)
            {
                // 아레나 내 랜덤 위치에 소환
                SpawnRandomInArena(spawner, slaveMonId, data.count);
            }
            else
            {
                // 플레이어 주변 원형 소환 (거리 = range)
                spawner.SpawnSurround(slaveMonId, data.count, data.range, 1f);
            }
            Debug.Log($"[MonsterAI] SlaveSpawn: {data.count}x {slaveMonId} (random={randomSpawn})");
        }

        yield return new WaitForSeconds(0.5f);
        FinishSkill(skill);
    }

    private void SpawnRandomInArena(EnemySpawner spawner, string monId, int count)
    {
        var arenaObj = GameObject.Find("BossArena");
        if (arenaObj == null) return;

        Vector2 center = arenaObj.transform.position;
        float halfW = 30f, halfH = 17f;

        var walls = arenaObj.GetComponentsInChildren<BoxCollider2D>();
        foreach (var w in walls)
        {
            if (w.gameObject.name == "Wall_Right")
                halfW = Mathf.Abs(w.transform.position.x - center.x);
            if (w.gameObject.name == "Wall_Top")
                halfH = Mathf.Abs(w.transform.position.y - center.y);
        }

        float margin = 3f;
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = new Vector3(
                center.x + Random.Range(-halfW + margin, halfW - margin),
                center.y + Random.Range(-halfH + margin, halfH - margin),
                0f);
            spawner.SpawnAt(monId, pos);
        }
    }

    private System.Collections.IEnumerator SpawnShockwave(Vector3 center)
    {
        int ringCount = 2;
        for (int r = 0; r < ringCount; r++)
        {
            var ring = new GameObject("Shockwave");
            ring.transform.position = center;

            var lr = ring.AddComponent<LineRenderer>();
            int segments = 48;
            lr.positionCount = segments + 1;
            lr.startWidth = 0.4f;
            lr.endWidth = 0.4f;
            lr.loop = false;
            lr.useWorldSpace = true;
            lr.sortingOrder = 20;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.3f, 0f, 0.9f);
            lr.endColor = new Color(1f, 0.3f, 0f, 0.9f);

            // 원형 초기화 (반지름 0)
            for (int i = 0; i <= segments; i++)
            {
                float angle = (360f / segments) * i * Mathf.Deg2Rad;
                lr.SetPosition(i, center);
            }

            // 확장 + 페이드아웃
            float duration = 0.6f;
            float maxRadius = 8f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float radius = Mathf.Lerp(0f, maxRadius, t);
                float alpha = Mathf.Lerp(0.9f, 0f, t);
                float width = Mathf.Lerp(0.5f, 0.05f, t);

                lr.startWidth = width;
                lr.endWidth = width;
                lr.startColor = new Color(1f, 0.3f, 0f, alpha);
                lr.endColor = new Color(1f, 0.3f, 0f, alpha);

                for (int i = 0; i <= segments; i++)
                {
                    float angle = (360f / segments) * i * Mathf.Deg2Rad;
                    lr.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(ring);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private string FindMonIdBySpriteKey(string spriteKey)
    {
        var monsters = TableManager.Instance.Monsters;
        if (monsters == null) return null;
        foreach (var m in monsters)
        {
            if (m.sprite_key == spriteKey)
                return m.mon_id;
        }
        return null;
    }

    // ─── 유틸 ───
    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    private static Sprite _circleSprite;
    private static Sprite CreateCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist < center ? Color.white : Color.clear);
            }
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        return _circleSprite;
    }
}
