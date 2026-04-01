using UnityEngine;
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
    public float minSpawnDistance = 12f;
    public float maxSpawnDistance = 15f;

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
    private bool bossSpawned = false;
    private const float BOSS_SPAWN_SEC = 600f;

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
        }
        BuildWaveSchedule();
    }

    /// <summary>
    /// 웨이브 시간표 생성:
    /// Wave 1~6 (20초 간격), Wave 7~10 (30초 간격), Wave 11+ (40초 간격)
    /// </summary>
    private void BuildWaveSchedule()
    {
        waveStartTimes.Clear();
        if (waveRows == null || waveRows.Length == 0) return;

        var waveNos = waveRows.Select(w => w.wave_no).Distinct().OrderBy(n => n).ToList();
        float time = 0f;
        for (int i = 0; i < waveNos.Count; i++)
        {
            waveStartTimes[waveNos[i]] = time;
            // 가변 간격
            if (waveNos[i] < 7)
                time += 20f;
            else if (waveNos[i] < 11)
                time += 30f;
            else
                time += 40f;
        }

        Debug.Log($"[Wave] 시간표 생성: {waveNos.Count}개 웨이브, 총 {time}초");
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (player == null) return;

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

        // 현재 시간에 맞는 웨이브 찾기
        float rawTime = PlayerStats.Instance != null ? PlayerStats.Instance.survivalTime : 0f;
        int waveNo = GetWaveNoForTime(rawTime);

        if (waveNo != currentWaveNo)
        {
            currentWaveNo = waveNo;
            UpdateActiveWaves(waveNo);
        }

        // 각 활성 웨이브별로 스폰 처리
        int totalEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        foreach (var wave in activeWaves)
        {
            wave.spawnTimer -= Time.deltaTime;
            if (wave.spawnTimer <= 0f)
            {
                // 한 번에 spawnCount 마리 스폰
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

        // 원래 속도로 이동 (바운스 테스트용)

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

        foreach (var row in currentWaves)
        {
            activeWaves.Add(new ActiveWave
            {
                monId = row.mon_id,
                spawnCount = row.spawn_count,
                spawnInterval = row.spawn_interval,
                maxEnemies = row.max_enemies,
                difficultyScale = row.difficulty_scale,
                spawnTimer = 0f
            });
        }

        float startF = waveStartTimes.ContainsKey(waveNo) ? waveStartTimes[waveNo] : 0f;
        int startSec = (int)startF;
        int min = startSec / 60;
        int sec = startSec % 60;
        Debug.Log($"[Wave #{waveNo}] {min:00}:{sec:00}~ → {activeWaves.Count}개: {string.Join(", ", activeWaves.Select(w => $"{w.monId}(x{w.spawnCount}/{w.spawnInterval}s, diff={w.difficultyScale})"))}");
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
                spawnTimer = 0f
            });
        }
    }

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

        Vector3 spawnPos = GetRandomSpawnPosition();
        GameObject enemy = Instantiate(prefab);
        enemy.SetActive(true);
        enemy.transform.position = spawnPos;

        ApplyMonsterData(enemy, data, wave.difficultyScale);
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

    private Vector3 GetRandomSpawnPosition()
    {
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        return player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f);
    }

    private void ApplyMonsterData(GameObject enemy, MonsterData data, float difficultyScale)
    {
        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null && data.sprite != null)
        {
            sr.sprite = data.sprite;
            if (data.tintColor != Color.white)
                sr.color = data.tintColor;
        }

        enemy.transform.localScale = new Vector3(data.scale, data.scale, 1f);

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
        }

        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed = data.moveSpeed;
            ai.contactDamage = data.contactDamage * difficultyScale;
        }
    }
}
