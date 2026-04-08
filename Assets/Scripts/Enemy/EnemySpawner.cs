using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs & Data")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public List<MonsterData> monsterDataList = new List<MonsterData>();
    public List<GameObject> bossPrefabs = new List<GameObject>();
    public List<MonsterData> bossDataList = new List<MonsterData>();

    [Header("Spawn Settings")]
    public float minSpawnDistance = 25f;
    public float maxSpawnDistance = 35f;

    [Header("=== DEBUG MODE ===")]
    public bool debugMode = false;
    [Range(0.1f, 5f)] public float debugScaleOverride = 1f;
    [Tooltip("소환할 몬스터 인덱스 (0~3: 일반, 4: 보스)")]
    [Range(0, 4)] public int debugSpawnIndex = 0;

    // Legacy single prefab support
    public GameObject enemyPrefab;

    // Wave 데이터 (GameBootstrap에서 주입)
    [HideInInspector] public WaveRow[] waveRows;

    private Transform player;
    private PlayerStats playerStats;
    [HideInInspector] public int currentWaveNo = -1;

    // 웨이브 시간표: wave_no → 시작 시간(초)
    private Dictionary<int, float> waveStartTimes = new Dictionary<int, float>();

    // 현재 활성 웨이브 상태
    private List<ActiveWave> activeWaves = new List<ActiveWave>();

    // 보스 스폰 상태
#pragma warning disable CS0414 // 할당되었지만 읽히지 않는 필드
    private bool bossSpawned = false;
#pragma warning restore CS0414
    private const float BOSS_SPAWN_SEC = 480f;

    // 플레이어 이동 방향 추적
    private Vector3 lastPlayerPos;
    private Vector3 playerMoveDir = Vector3.right;

    // 포위망 스폰 (Siege Wave)
    private bool siegeWaveActive;

    // 미니보스 스폰
    [Header("Mini Boss")]
    public float miniBossInterval = 40f;         // 미니보스 스폰 간격 (초)
    public float miniBossHealthScale = 5f;       // 체력 배율
    public float miniBossSizeScale = 1.6f;       // 크기 배율
    public Color miniBossTint = new Color(1f, 0.3f, 0.3f, 1f); // 붉은 색조
    private float miniBossTimer = 0f;

    // 디버그용: 현재 소환된 디버그 몬스터
    private GameObject debugMonster;

    private class ActiveWave
    {
        public string monId;
        public int spawnCount;
        public float spawnInterval;
        public int maxEnemies;
        public float difficultyScale;
        public float spawnTimer;
        // 스폰 위치 제어 (테이블 기반)
        public float spawnDistMin;
        public float spawnDistMax;
        public float forwardBias;
        public int clusterSize;
        public float clusterRadius;
        public float speedScale;
        public float minSpawnGap;
    }

    private float elapsedTime
    {
        get => PlayerStats.Instance != null ? PlayerStats.Instance.survivalTime : 0f;
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
            lastPlayerPos = player.position;
        }
        BuildWaveSchedule();
    }

    /// <summary>
    /// 웨이브 시간표 생성: 30초 고정 간격
    /// </summary>
    private void BuildWaveSchedule()
    {
        waveStartTimes.Clear();
        if (waveRows == null || waveRows.Length == 0) return;

        var waveNos = waveRows.Select(w => w.wave_no).Distinct().OrderBy(n => n).ToList();
        for (int i = 0; i < waveNos.Count; i++)
        {
            waveStartTimes[waveNos[i]] = i * 30f;
        }

        Debug.Log($"[Wave] 시간표 생성: {waveNos.Count}개 웨이브, 총 {waveNos.Count * 30}초");
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (player == null) return;

        // 플레이어 이동 방향 추적
        Vector3 delta = player.position - lastPlayerPos;
        if (delta.sqrMagnitude > 0.001f)
            playerMoveDir = delta.normalized;
        lastPlayerPos = player.position;

        // 디버그 모드: 자동 스폰 차단, 스케일 실시간 반영
        if (debugMode)
        {
            // F1: 선택된 몬스터 소환, F2: 디버그 몬스터 제거
            if (Input.GetKeyDown(KeyCode.F1))
                DebugSpawnMonster();
            if (Input.GetKeyDown(KeyCode.F2))
                DebugClearMonster();

            // 슬라이더 변경 시 실시간 스케일 반영
            if (debugMonster != null)
                debugMonster.transform.localScale = new Vector3(debugScaleOverride, debugScaleOverride, 1f);

            return; // 자동 스폰 차단
        }

        // 보스전 중에는 일반 몬스터 스폰 중지
        if (StageManager.Instance != null && StageManager.Instance.IsBossFight)
            return;

        // 미니보스 스폰 (Collecting 단계에서만, 키를 아직 다 모으지 않았을 때)
        if (StageManager.Instance != null
            && StageManager.Instance.CurrentPhase == StageManager.BossPhase.Collecting
            && !StageManager.Instance.CanSummonBoss)
        {
            miniBossTimer += Time.deltaTime;
            if (miniBossTimer >= miniBossInterval)
            {
                miniBossTimer = 0f;
                SpawnMiniBoss();
            }
        }

        // Warning Wave: 대량 스폰
        if (StageManager.Instance != null && StageManager.Instance.IsWarningWave)
        {
            UpdateWarningWaveSpawn();
            return;
        }

        // 현재 시간에 맞는 웨이브 찾기
        float rawTime = PlayerStats.Instance != null ? PlayerStats.Instance.survivalTime : 0f;
        int waveNo = GetWaveNoForTime(rawTime);

        if (waveNo != currentWaveNo)
        {
            currentWaveNo = waveNo;
            UpdateActiveWaves(waveNo);
        }

        // 각 활성 웨이브별로 스폰 처리
        var allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        int totalEnemies = allEnemies.Length;
        foreach (var wave in activeWaves)
        {
            wave.spawnTimer -= Time.deltaTime;
            if (wave.spawnTimer <= 0f)
            {
                // maxEnemies가 작으면(≤5) 해당 몬스터 종류만 카운트, 크면 전체 카운트
                int currentCount;
                if (wave.maxEnemies <= 5)
                {
                    currentCount = 0;
                    foreach (var e in allEnemies)
                    {
                        if (e == null) continue;
                        var eh = e.GetComponent<EnemyHealth>();
                        if (eh != null && e.name.Contains(wave.monId))
                            currentCount++;
                    }
                }
                else
                {
                    currentCount = totalEnemies;
                }

                // 한 번에 spawnCount 마리 스폰
                for (int i = 0; i < wave.spawnCount; i++)
                {
                    if (currentCount >= wave.maxEnemies) break;
                    SpawnMonster(wave);
                    currentCount++;
                    totalEnemies++;
                }
                wave.spawnTimer = wave.spawnInterval;
            }
        }
    }

    // ─── DEBUG ───
    private void DebugSpawnMonster()
    {
        DebugClearMonster();

        GameObject prefab;
        MonsterData data;

        if (debugSpawnIndex < monsterDataList.Count)
        {
            prefab = enemyPrefabs[debugSpawnIndex];
            data = monsterDataList[debugSpawnIndex];
        }
        else if (bossDataList.Count > 0)
        {
            prefab = bossPrefabs[0];
            data = bossDataList[0];
        }
        else return;

        // 플레이어 앞 5유닛 위치에 소환
        Vector3 spawnPos = player.position + player.up * 5f;
        debugMonster = Instantiate(prefab);
        debugMonster.SetActive(true);
        debugMonster.transform.position = spawnPos;
        debugMonster.name = $"[DEBUG] {data.monsterName}";

        ApplyMonsterData(debugMonster, data, 1f);
        debugScaleOverride = data.scale;

        Debug.Log($"[DEBUG] Spawned: {data.monsterName} (scale: {data.scale})");
    }

    private void DebugClearMonster()
    {
        if (debugMonster != null)
            Destroy(debugMonster);
    }

    /// <summary>현재 시간(초)에 해당하는 wave_no 반환</summary>
    private int GetWaveNoForTime(float time)
    {
        int result = -1;
        foreach (var kv in waveStartTimes)
        {
            if (kv.Value <= time)
                result = kv.Key;
        }
        return result;
    }

    private void UpdateActiveWaves(int waveNo)
    {
        activeWaves.Clear();

        if (waveRows == null || waveRows.Length == 0)
        {
            SetupLegacyWave();
            return;
        }

        // 같은 wave_no를 가진 모든 행을 한 웨이브로 실행
        var currentWaves = waveRows.Where(w => w.wave_no == waveNo);

        // 포위망 스폰은 SpawnSiegeWave()로 수동 호출 가능 (현재 자동 트리거 비활성)

        foreach (var row in currentWaves)
        {
            activeWaves.Add(new ActiveWave
            {
                monId = row.mon_id,
                spawnCount = row.spawn_count,
                spawnInterval = row.spawn_interval,
                maxEnemies = row.max_enemies,
                difficultyScale = row.difficulty_scale,
                spawnTimer = 0f,
                // 테이블 값이 0이면 기본값 사용
                spawnDistMin = row.spawn_dist_min > 0 ? row.spawn_dist_min : minSpawnDistance,
                spawnDistMax = row.spawn_dist_max > 0 ? row.spawn_dist_max : maxSpawnDistance,
                forwardBias = row.forward_bias > 0 ? row.forward_bias : 0.7f,
                clusterSize = row.cluster_size > 1 ? row.cluster_size : 1,
                clusterRadius = row.cluster_radius > 0 ? row.cluster_radius : 1.5f,
                speedScale = row.speed_scale > 0 ? row.speed_scale : 1f,
                minSpawnGap = row.min_spawn_gap > 0 ? row.min_spawn_gap : 1.5f,
            });
        }

        float startF = waveStartTimes.ContainsKey(waveNo) ? waveStartTimes[waveNo] : 0f;
        int startSec = (int)startF;
        int min = startSec / 60;
        int sec = startSec % 60;
        Debug.Log($"[Wave #{waveNo}] {min:00}:{sec:00}~ → {activeWaves.Count}개: {string.Join(", ", activeWaves.Select(w => $"{w.monId}(x{w.spawnCount}/{w.spawnInterval}s, diff={w.difficultyScale}, dist={w.spawnDistMin}~{w.spawnDistMax}, fwd={w.forwardBias}, cluster={w.clusterSize})"))}");
    }

    private void SetupLegacyWave()
    {
        if (monsterDataList.Count == 0) return;

        int totalWeight = 0;
        foreach (var data in monsterDataList)
            totalWeight += data.spawnWeight;

        int elapsed = Mathf.FloorToInt(elapsedTime);
        foreach (var data in monsterDataList)
        {
            float ratio = (float)data.spawnWeight / totalWeight;
            int count = Mathf.Max(1, Mathf.RoundToInt(ratio * 3));
            activeWaves.Add(new ActiveWave
            {
                monId = data.monId,
                spawnCount = count,
                spawnInterval = 5f,
                maxEnemies = 30 + elapsed / 60 * 5,
                difficultyScale = 1f + elapsed / 60 * 0.1f,
                spawnTimer = 0f,
                spawnDistMin = minSpawnDistance,
                spawnDistMax = maxSpawnDistance,
                forwardBias = 0.7f,
                clusterSize = 1,
                clusterRadius = 1.5f,
                speedScale = 1f,
                minSpawnGap = 1.5f,
            });
        }
    }

    // ─── SPAWN ───

    private void SpawnMonster(ActiveWave wave)
    {
        // mon_id로 몬스터 데이터/프리팹 찾기
        int index = FindMonsterIndex(wave.monId);
        if (index < 0)
        {
            // mon_id 매칭 실패 시 가중치 기반 랜덤 선택
            index = GetWeightedRandomIndex();
        }
        if (index < 0 || index >= enemyPrefabs.Count) return;

        GameObject prefab = enemyPrefabs[index];
        MonsterData data = monsterDataList[index];

        // 군집 스폰: cluster_size > 1이면 전방 부채꼴(±30°)로 분산 배치
        if (wave.clusterSize > 1)
        {
            float baseAngle = Mathf.Atan2(playerMoveDir.y, playerMoveDir.x);
            float spreadDeg = 60f; // 총 부채꼴 각도 (±30°)
            float sliceAngle = spreadDeg / (wave.clusterSize - 1);
            float startAngle = baseAngle - (spreadDeg * 0.5f) * Mathf.Deg2Rad;

            for (int c = 0; c < wave.clusterSize; c++)
            {
                // 부채꼴 내 균등 배치 + 약간의 랜덤
                float angle = startAngle + (sliceAngle * c) * Mathf.Deg2Rad;
                angle += Random.Range(-5f, 5f) * Mathf.Deg2Rad; // 살짝 흔들림
                float dist = Random.Range(wave.spawnDistMin, wave.spawnDistMax);
                Vector3 pos = player.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);
                pos = AdjustForSpawnGap(pos, wave.minSpawnGap);
                SpawnOneEnemy(prefab, data, pos, wave.difficultyScale, wave.speedScale);
            }
        }
        else
        {
            Vector3 pos = GetSpawnPosition(wave);
            pos = AdjustForSpawnGap(pos, wave.minSpawnGap);
            SpawnOneEnemy(prefab, data, pos, wave.difficultyScale, wave.speedScale);
        }
    }

    /// <summary>
    /// 기존 적과 겹치지 않도록 스폰 위치를 조정.
    /// minGap 반경 내에 적이 있으면 바깥으로 밀어냄.
    /// </summary>
    private Vector3 AdjustForSpawnGap(Vector3 pos, float minGap)
    {
        if (minGap <= 0f) return pos;

        float gapSq = minGap * minGap;
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");

        for (int attempt = 0; attempt < 3; attempt++)
        {
            bool tooClose = false;
            foreach (var e in enemies)
            {
                if ((e.transform.position - pos).sqrMagnitude < gapSq)
                {
                    tooClose = true;
                    break;
                }
            }
            if (!tooClose) return pos;

            // 겹치면 랜덤 방향으로 minGap만큼 밀기
            Vector2 offset = Random.insideUnitCircle.normalized * minGap;
            pos += new Vector3(offset.x, offset.y, 0f);
        }
        return pos;
    }

    private void SpawnOneEnemy(GameObject prefab, MonsterData data, Vector3 pos, float difficultyScale, float speedScale)
    {
        GameObject enemy = Instantiate(prefab);
        enemy.name = data.monId;
        enemy.SetActive(true);
        enemy.transform.position = pos;
        ApplyMonsterData(enemy, data, difficultyScale);

        // 속도 배율 적용
        if (speedScale > 0f && !Mathf.Approximately(speedScale, 1f))
        {
            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
                ai.moveSpeed = data.moveSpeed * speedScale * 2f;
        }
    }

    /// <summary>
    /// 플레이어 주변 적 밀도를 분석하여 적이 적은 방향에 스폰.
    /// forward_bias >= 0.9 이면 시야 밖 360° 완전 분산 모드 (Surge 전용).
    /// </summary>
    private Vector3 GetSpawnPosition(ActiveWave wave)
    {
        float angle;

        // Surge 모드: 시야 밖 360° 완전 분산 (forward_bias >= 0.9 이 신호)
        if (wave.forwardBias >= 0.9f)
        {
            angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        }
        else
        {
            // 일반 모드: 적 밀도 반대쪽 + ±70° 분산
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Vector2 avgDir = Vector2.zero;

            if (enemies.Length > 0)
            {
                foreach (var e in enemies)
                {
                    if (e == null) continue;
                    Vector2 toEnemy = (Vector2)e.transform.position - (Vector2)player.position;
                    if (toEnemy.sqrMagnitude > 0.01f)
                        avgDir += toEnemy.normalized;
                }
            }

            if (avgDir.sqrMagnitude > 0.01f)
            {
                float enemyAngle = Mathf.Atan2(avgDir.y, avgDir.x);
                float oppositeAngle = enemyAngle + Mathf.PI;
                angle = oppositeAngle + Random.Range(-70f, 70f) * Mathf.Deg2Rad;
            }
            else
            {
                angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            }
        }

        float distance = Random.Range(wave.spawnDistMin, wave.spawnDistMax);
        return player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f);
    }

    /// <summary>
    /// 포위 스폰: 플레이어 주변 360°에 균등하게 N마리 동시 배치.
    /// 이벤트성 호출용 (Warning Wave, 특정 웨이브 트리거 등).
    /// </summary>
    public void SpawnSurround(string monId, int count, float distance, float difficultyScale, float speedScale = 1f)
    {
        int index = FindMonsterIndex(monId);
        if (index < 0) index = GetWeightedRandomIndex();
        if (index < 0 || index >= enemyPrefabs.Count) return;

        GameObject prefab = enemyPrefabs[index];
        MonsterData data = monsterDataList[index];

        float sliceAngle = 360f / count;
        float randomOffset = Random.Range(0f, 360f); // 매번 다른 시작 각도

        for (int i = 0; i < count; i++)
        {
            float angle = (randomOffset + sliceAngle * i) * Mathf.Deg2Rad;
            // 약간의 거리/각도 랜덤 추가 (기계적이지 않게)
            float dist = distance + Random.Range(-0.5f, 0.5f);
            float jitter = Random.Range(-sliceAngle * 0.2f, sliceAngle * 0.2f) * Mathf.Deg2Rad;

            Vector3 pos = player.position + new Vector3(
                Mathf.Cos(angle + jitter) * dist,
                Mathf.Sin(angle + jitter) * dist,
                0f);

            SpawnOneEnemy(prefab, data, pos, difficultyScale, speedScale);
        }

        Debug.Log($"[Surround] {data.monsterName} x{count} at dist={distance}, diff={difficultyScale}");
    }

    /// <summary>특정 위치에 몬스터 1마리 소환</summary>
    public void SpawnAt(string monId, Vector3 pos, float difficultyScale = 1f, float speedScale = 1f)
    {
        int index = FindMonsterIndex(monId);
        if (index < 0 || index >= enemyPrefabs.Count)
        {
            string allIds = string.Join(", ", monsterDataList.ConvertAll(m => m.monId));
            Debug.LogWarning($"[SpawnAt] Failed: monId={monId}, index={index}, prefabs={enemyPrefabs.Count}, monsters={monsterDataList.Count}\nLoaded: {allIds}");
            return;
        }
        SpawnOneEnemy(enemyPrefabs[index], monsterDataList[index], pos, difficultyScale, speedScale);
    }

    /// <summary>
    /// 포위 스폰 (여러 종류 혼합): 전체 count를 균등 배치하되 몬스터 종류를 랜덤 선택.
    /// </summary>
    public void SpawnSurroundMixed(int count, float distance, float difficultyScale, float speedScale = 1f)
    {
        if (monsterDataList.Count == 0 || enemyPrefabs.Count == 0) return;

        float sliceAngle = 360f / count;
        float randomOffset = Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            int index = GetWeightedRandomIndex();
            if (index < 0) continue;

            GameObject prefab = enemyPrefabs[index];
            MonsterData data = monsterDataList[index];

            float angle = (randomOffset + sliceAngle * i) * Mathf.Deg2Rad;
            float dist = distance + Random.Range(-0.5f, 0.5f);
            float jitter = Random.Range(-sliceAngle * 0.2f, sliceAngle * 0.2f) * Mathf.Deg2Rad;

            Vector3 pos = player.position + new Vector3(
                Mathf.Cos(angle + jitter) * dist,
                Mathf.Sin(angle + jitter) * dist,
                0f);

            SpawnOneEnemy(prefab, data, pos, difficultyScale, speedScale);
        }

        Debug.Log($"[Surround Mixed] x{count} at dist={distance}, diff={difficultyScale}");
    }

    // ─── LEGACY ───

    /// <summary>기존 코드 호환용: 기본 설정으로 랜덤 스폰</summary>
    private Vector3 GetRandomSpawnPosition()
    {
        float baseAngle = Mathf.Atan2(playerMoveDir.y, playerMoveDir.x);
        float angle;
        if (Random.value < 0.7f)
            angle = baseAngle + Random.Range(-90f, 90f) * Mathf.Deg2Rad;
        else
            angle = baseAngle + Random.Range(90f, 270f) * Mathf.Deg2Rad;

        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        return player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f);
    }

    private int FindMonsterIndex(string monId)
    {
        for (int i = 0; i < monsterDataList.Count; i++)
        {
            if (monsterDataList[i].monId == monId)
                return i;
        }
        return -1;
    }

    private int GetWeightedRandomIndex()
    {
        if (monsterDataList.Count == 0) return -1;

        int totalWeight = 0;
        foreach (var data in monsterDataList)
            totalWeight += data.spawnWeight;

        if (totalWeight <= 0) return Random.Range(0, monsterDataList.Count);

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < monsterDataList.Count; i++)
        {
            cumulative += monsterDataList[i].spawnWeight;
            if (roll < cumulative)
                return i;
        }
        return monsterDataList.Count - 1;
    }

    private void SpawnBoss()
    {
        if (bossDataList.Count == 0 || bossPrefabs.Count == 0) return;

        bossSpawned = true;

        // 첫 번째 보스 스폰 (챕터1 기준)
        int bossIndex = 0;
        GameObject prefab = bossPrefabs[bossIndex];
        MonsterData bossData = bossDataList[bossIndex];

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject boss = Instantiate(prefab);
        boss.SetActive(true);
        boss.transform.position = spawnPos;

        ApplyMonsterData(boss, bossData, 1f);

        Debug.Log($"[EnemySpawner] BOSS spawned: {bossData.monsterName} at {elapsedTime / 60f:F1} min");
    }

    // ─── MINI BOSS ───

    public void SpawnMiniBoss()
    {
        if (monsterDataList.Count == 0 || enemyPrefabs.Count == 0) return;

        // 랜덤 몬스터를 미니보스로 선택
        int index = GetWeightedRandomIndex();
        if (index < 0) return;

        GameObject prefab = enemyPrefabs[index];
        MonsterData data = monsterDataList[index];

        // 플레이어 전방에 스폰
        float angle = Mathf.Atan2(playerMoveDir.y, playerMoveDir.x) + Random.Range(-45f, 45f) * Mathf.Deg2Rad;
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 pos = player.position + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0f);

        GameObject enemy = Instantiate(prefab);
        enemy.name = $"[MiniBoss] {data.monId}";
        enemy.SetActive(true);
        enemy.transform.position = pos;
        ApplyMonsterData(enemy, data, 1f);

        // 미니보스 강화: 체력, 크기, 색상
        var eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.maxHealth = data.health * miniBossHealthScale;
            eh.currentHealth = eh.maxHealth;
            eh.isMiniBoss = true;
        }

        enemy.transform.localScale = new Vector3(data.scale * miniBossSizeScale, data.scale * miniBossSizeScale, 1f);

        // MonsterBounce의 baseScale도 갱신
        var bounce = enemy.GetComponent<MonsterBounce>();
        if (bounce != null)
            bounce.RefreshBaseScale();

        // 붉은 색조 적용
        var renderers = enemy.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr.enabled)
                sr.color = miniBossTint;
        }

        Debug.Log($"[MiniBoss] Spawned: {data.monId} (HP={data.health * miniBossHealthScale}, scale={data.scale * miniBossSizeScale})");
    }

    // ─── SIEGE WAVE (서서히 조여드는 포위망) ───

    /// <summary>
    /// 포위망 웨이브: 소규모 그룹이 0.5~1초마다 여러 방향에서 지속 소환.
    /// 시간이 지날수록 간격이 짧아지고 그룹이 늘어나 포위가 조여든다.
    /// duration초 동안 지속된 후 자동 종료.
    /// </summary>
    public void SpawnSiegeWave(float difficultyScale)
    {
        if (siegeWaveActive) return;
        StartCoroutine(SiegeWaveCoroutine(difficultyScale));
    }

    private IEnumerator SiegeWaveCoroutine(float difficultyScale)
    {
        siegeWaveActive = true;

        // ── 포위망 설정 ──
        float duration = 25f;            // 포위망 지속 시간 (초)
        float spawnRadiusMin = 16f;      // 스폰 최소 거리 (화면 가장자리)
        float spawnRadiusMax = 22f;      // 스폰 최대 거리 (화면 바로 바깥)
        float siegeSpeedScale = 0.6f;    // 느린 이동 (돌진 없음)
        float eliteChance = 0.10f;       // 강한 몬스터 비율 10%

        // 시간에 따라 변하는 값
        float startInterval = 0.8f;      // 초반 소환 간격 (여유 있음)
        float endInterval = 0.25f;       // 후반 소환 간격 (빡빡함)
        int startGroupSize = 3;          // 초반 그룹 크기
        int endGroupSize = 6;            // 후반 그룹 크기
        int startDirections = 3;         // 초반 동시 방향 수
        int endDirections = 6;           // 후반 동시 방향 수

        int totalSpawned = 0;
        float elapsed = 0f;

        Debug.Log($"[Siege] 포위망 시작! {duration}초간, diff={difficultyScale}");

        // 약한 몬스터 인덱스 미리 구분 (기본 90% / 강한 10%)
        var weakIndices = new List<int>();
        var eliteIndices = new List<int>();
        for (int i = 0; i < monsterDataList.Count; i++)
        {
            if (monsterDataList[i].health >= 200)
                eliteIndices.Add(i);
            else
                weakIndices.Add(i);
        }
        // 약한 몬스터가 없으면 전부 약한 취급
        if (weakIndices.Count == 0)
            weakIndices.AddRange(eliteIndices);

        while (elapsed < duration)
        {
            // 진행도 0→1
            float t = elapsed / duration;

            // 현재 프레임의 설정값 (선형 보간)
            float interval = Mathf.Lerp(startInterval, endInterval, t);
            int groupSize = Mathf.RoundToInt(Mathf.Lerp(startGroupSize, endGroupSize, t));
            int directions = Mathf.RoundToInt(Mathf.Lerp(startDirections, endDirections, t));

            // 여러 방향에서 동시 소환
            for (int d = 0; d < directions; d++)
            {
                // 360° 균등 분배 + 랜덤 오프셋
                float baseAngle = (360f / directions * d + Random.Range(-20f, 20f)) * Mathf.Deg2Rad;
                float dist = Random.Range(spawnRadiusMin, spawnRadiusMax);
                Vector3 center = player.position + new Vector3(
                    Mathf.Cos(baseAngle) * dist,
                    Mathf.Sin(baseAngle) * dist,
                    0f);

                // 그 방향에서 그룹 소환 (center 주변에 모여서)
                for (int g = 0; g < groupSize; g++)
                {
                    Vector2 offset = Random.insideUnitCircle * 1.5f;
                    Vector3 pos = center + new Vector3(offset.x, offset.y, 0f);

                    // 90% 약한 몬스터, 10% 강한 몬스터
                    int index;
                    if (Random.value < eliteChance && eliteIndices.Count > 0)
                        index = eliteIndices[Random.Range(0, eliteIndices.Count)];
                    else
                        index = weakIndices[Random.Range(0, weakIndices.Count)];

                    SpawnOneEnemy(
                        enemyPrefabs[index],
                        monsterDataList[index],
                        pos,
                        difficultyScale,
                        siegeSpeedScale
                    );
                    totalSpawned++;
                }
            }

            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }

        siegeWaveActive = false;
        Debug.Log($"[Siege] 포위망 종료! 총 {totalSpawned}마리 소환, {elapsed:F1}초 경과");
    }

    // ─── WARNING WAVE ───
    private List<ActiveWave> warningWaves = new List<ActiveWave>();
    private WarningWaveRow[] warningWaveRows;
    private float warningWaveTime;
#pragma warning disable CS0414
    private bool warningWaveInitialized;
#pragma warning restore CS0414

    /// <summary>Warning Wave 테이블 데이터 로드 (StageManager에서 호출)</summary>
    public void InitWarningWave(string wwGroupId)
    {
        warningWaves.Clear();
        warningWaveTime = 0f;
        warningWaveInitialized = true;

        warningWaveRows = TableManager.Instance.GetWarningWavesByGroup(wwGroupId);
        if (warningWaveRows == null || warningWaveRows.Length == 0)
        {
            // 테이블 데이터 없으면 기본 하드코딩 폴백
            warningWaveRows = null;
            Debug.LogWarning($"[EnemySpawner] Warning Wave 테이블 없음: {wwGroupId}, 기본 스폰 사용");
        }
        else
        {
            Debug.Log($"[EnemySpawner] Warning Wave 로드: {wwGroupId}, {warningWaveRows.Length}행");
        }
    }

    private void UpdateWarningWaveSpawn()
    {
        warningWaveTime += Time.deltaTime;

        if (warningWaveRows != null && warningWaveRows.Length > 0)
        {
            // 테이블 기반 스폰
            // 현재 시간에 맞는 웨이브 활성화
            foreach (var row in warningWaveRows)
            {
                if (row.start_time <= warningWaveTime)
                {
                    // 이미 활성화된 웨이브인지 확인
                    bool exists = false;
                    foreach (var w in warningWaves)
                    {
                        if (w.monId == row.mon_id && w.spawnInterval == row.spawn_interval)
                        { exists = true; break; }
                    }
                    if (!exists)
                    {
                        warningWaves.Add(new ActiveWave
                        {
                            monId = row.mon_id,
                            spawnCount = row.spawn_count,
                            spawnInterval = row.spawn_interval,
                            maxEnemies = row.max_enemies,
                            difficultyScale = row.difficulty_scale,
                            spawnTimer = 0f,
                            spawnDistMin = minSpawnDistance,
                            spawnDistMax = maxSpawnDistance,
                            forwardBias = 0.7f,
                            clusterSize = 1,
                            clusterRadius = 1.5f,
                            speedScale = 1f,
                            minSpawnGap = 1.5f,
                        });
                    }
                }
            }

            int totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            foreach (var wave in warningWaves)
            {
                wave.spawnTimer -= Time.deltaTime;
                if (wave.spawnTimer <= 0f)
                {
                    for (int i = 0; i < wave.spawnCount; i++)
                    {
                        if (totalEnemies >= wave.maxEnemies) break;
                        SpawnMonster(wave);
                        totalEnemies++;
                    }
                    wave.spawnTimer = wave.spawnInterval;
                }
            }
        }
        else
        {
            // 폴백: 기본 대량 스폰
            int totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (totalEnemies >= 100) return;

            foreach (var wave in warningWaves)
            {
                wave.spawnTimer -= Time.deltaTime;
                if (wave.spawnTimer <= 0f)
                {
                    for (int i = 0; i < wave.spawnCount; i++)
                    {
                        if (totalEnemies >= 100) break;
                        SpawnMonster(wave);
                        totalEnemies++;
                    }
                    wave.spawnTimer = wave.spawnInterval;
                }
            }

            // 폴백 웨이브가 비어있으면 기본 생성
            if (warningWaves.Count == 0 && monsterDataList.Count > 0)
            {
                foreach (var data in monsterDataList)
                {
                    warningWaves.Add(new ActiveWave
                    {
                        monId = data.monId,
                        spawnCount = 5,
                        spawnInterval = 0.5f,
                        maxEnemies = 100,
                        difficultyScale = 3f,
                        spawnTimer = 0f,
                        spawnDistMin = minSpawnDistance,
                        spawnDistMax = maxSpawnDistance,
                        forwardBias = 0.7f,
                        clusterSize = 1,
                        clusterRadius = 1.5f,
                        speedScale = 1f,
                    });
                }
            }
        }
    }

    private void ApplyMonsterData(GameObject enemy, MonsterData data, float difficultyScale)
    {
        // mon_id 기록 (CheatWindow 테이블 스탯 적용용)
        var identifier = enemy.GetComponent<EnemyIdentifier>();
        if (identifier == null)
            identifier = enemy.AddComponent<EnemyIdentifier>();
        identifier.monId = data.monId;

        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null && data.sprite != null)
        {
            sr.sprite = data.sprite;
            if (data.tintColor != Color.white)
                sr.color = data.tintColor;
        }

        enemy.transform.localScale = new Vector3(data.scale, data.scale, 1f);

        // 스프라이트 크기에 맞게 콜라이더 자동 조정 (70%로 여유)
        var col = enemy.GetComponent<BoxCollider2D>();
        if (col != null && sr != null && sr.sprite != null)
        {
            var bounds = sr.sprite.bounds;
            col.size = bounds.size * 0.7f;
        }

        // 바운스 효과 추가
        var bounce = enemy.AddComponent<MonsterBounce>();
        bounce.bounceSpeed = data.bounceSpeed;
        bounce.bounceHeight = data.bounceHeight;
        bounce.squashAmount = data.bounceSquash;
        bounce.RefreshBaseScale();

        var eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.maxHealth = data.health * difficultyScale;
            eh.currentHealth = eh.maxHealth;
            eh.expDrop = data.expDrop;
            eh.goldDrop = data.goldDrop;
            eh.fuelDropRate = data.fuelDropRate;
        }

        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed = data.moveSpeed * 0.85f * 2f;
            ai.contactDamage = data.contactDamage * difficultyScale;
        }

        // 신호등 몬스터: 공구상자 드롭 + 색상 변환
        if (data.specialAbility == "TrafficLight")
        {
            if (eh != null)
                eh.toolboxPickupPrefab = CreateToolboxPickupPrefab();
            enemy.AddComponent<TrafficLightColor>();
        }

        // 스킬이 있는 몬스터: MonsterAI 부착
        var skillRows = TableManager.Instance.GetSkillsByMonster(data.monId);
        if (skillRows != null && skillRows.Length > 0)
        {
            var monsterAI = enemy.AddComponent<MonsterAI>();
            monsterAI.Initialize(skillRows);
        }
    }

    private static GameObject _toolboxPrefabCache;
    private GameObject CreateToolboxPickupPrefab()
    {
        if (_toolboxPrefabCache != null) return _toolboxPrefabCache;

        var pickup = new GameObject("ToolboxPickupPrefab");
        pickup.SetActive(false);

        var sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Icons/Item/FixBox/FixBox");
        sr.sortingOrder = 3;
        pickup.transform.localScale = Vector3.one * 0.17f;

        var col = pickup.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        pickup.AddComponent<ToolboxPickup>();

        _toolboxPrefabCache = pickup;
        return pickup;
    }
}
