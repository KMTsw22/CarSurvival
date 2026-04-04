using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 스테이지 진행 관리: 열쇠 수집 → 카운트다운 → 보스 소환 → 아레나 → 다음 스테이지
/// 10분 경과 시 열쇠 충분하면 강제 소환, 부족하면 게임오버
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Current State")]
    public string currentMapId = "MAP_CH1";
    public int currentStageNo = 1;
    public int collectedKeys = 0;

    [Header("Settings")]
    public float countdownDuration = 3f;        // 소환 카운트다운 (초)
    public float forceSummonTime = 600f;         // 강제 소환 시간 (10분)

    // 상태
    public enum BossPhase { Collecting, Countdown, WarningWave, Fighting, Defeated }
    public BossPhase CurrentPhase { get; private set; } = BossPhase.Collecting;

    // 카운트다운
    private float countdownTimer;
    public float CountdownRemaining => countdownTimer;

    // 보스 아레나
    private GameObject arenaObj;
    private GameObject currentBoss;

    // 포탈
    private GameObject portalObj;

    // 현재 스테이지 테이블 데이터
    private StageRow currentStage;
    private Sprite keyIconSprite;

    // EnemySpawner 참조
    private EnemySpawner enemySpawner;

    // 이벤트
    public event Action<int, int> OnKeyCountChanged;       // collected, required
    public event Action<int> OnStageChanged;                // new stageNo
    public event Action OnBossSummoned;
    public event Action<float> OnCountdownTick;             // remaining seconds
    public event Action OnCountdownStart;
    public event Action OnForceGameOver;                    // 열쇠 부족 게임오버
    public event Action OnBossDefeatedEvent;
    public event Action OnForceSummonWarning;               // 강제 소환 임박 경고
    public event Action OnWarningWaveStart;                  // Warning Wave 시작

    /// <summary>현재 스테이지에서 필요한 열쇠 개수</summary>
    public int RequiredKeys => currentStage != null ? currentStage.key_item_count : 0;

    /// <summary>보스 소환 가능 여부</summary>
    public bool CanSummonBoss => currentStage != null && collectedKeys >= currentStage.key_item_count
                                 && CurrentPhase == BossPhase.Collecting;

    /// <summary>현재 스테이지의 열쇠 아이콘</summary>
    public Sprite KeyIcon => keyIconSprite;

    /// <summary>현재 스테이지의 열쇠 이름</summary>
    public string KeyName => currentStage != null ? currentStage.key_name : "";

    /// <summary>보스전 진행 중 여부 (스포너에서 참조)</summary>
    public bool IsBossFight => CurrentPhase == BossPhase.Countdown || CurrentPhase == BossPhase.Fighting;

    /// <summary>Warning Wave 진행 중 여부 (스포너에서 참조)</summary>
    public bool IsWarningWave => CurrentPhase == BossPhase.WarningWave;

    private bool forceSummonTriggered = false;
    private bool warningTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        enemySpawner = FindObjectOfType<EnemySpawner>();
        LoadStage(currentMapId, currentStageNo);
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        float survivalTime = PlayerStats.Instance != null ? PlayerStats.Instance.survivalTime : 0f;

        // 카운트다운 처리
        if (CurrentPhase == BossPhase.Countdown)
        {
            countdownTimer -= Time.deltaTime;
            OnCountdownTick?.Invoke(countdownTimer);

            if (countdownTimer <= 0f)
            {
                ExecuteBossSummon();
            }
            return;
        }

        // 보스전 중: 보스가 죽었는지 확인
        if (CurrentPhase == BossPhase.Fighting)
        {
            if (currentBoss == null)
            {
                OnBossDefeatedHandler();
            }
            return;
        }

        // 수집 단계: 10분 타이머 체크
        if (CurrentPhase == BossPhase.Collecting && !forceSummonTriggered)
        {
            // 30초 전 경고
            if (!warningTriggered && survivalTime >= forceSummonTime - 30f)
            {
                warningTriggered = true;
                OnForceSummonWarning?.Invoke();
                Debug.Log("[StageManager] 30초 후 강제 보스 소환!");
            }

            // 10분 도달
            if (survivalTime >= forceSummonTime)
            {
                forceSummonTriggered = true;
                if (collectedKeys >= RequiredKeys)
                {
                    Debug.Log("[StageManager] 10분 경과 - 열쇠 충분, 강제 보스 소환");
                    StartBossSummon();
                }
                else
                {
                    Debug.Log("[StageManager] 10분 경과 - 열쇠 부족, 게임 오버");
                    OnForceGameOver?.Invoke();
                    GameManager.Instance.OnPlayerDeath();
                }
            }
        }
    }

    /// <summary>특정 맵/스테이지 로드</summary>
    public void LoadStage(string mapId, int stageNo)
    {
        currentMapId = mapId;
        currentStageNo = stageNo;
        collectedKeys = 0;
        CurrentPhase = BossPhase.Collecting;
        forceSummonTriggered = false;
        warningTriggered = false;

        currentStage = TableManager.Instance.GetStage(mapId, stageNo);
        if (currentStage == null)
        {
            Debug.LogWarning($"[StageManager] Stage not found: {mapId} stage {stageNo}");
            return;
        }

        // 열쇠 아이콘 로드
        if (!string.IsNullOrEmpty(currentStage.key_icon))
        {
            keyIconSprite = Resources.Load<Sprite>("Sprites/Icons/Keys/" + currentStage.key_icon)
                         ?? Resources.Load<Sprite>("Sprites/Icons/" + currentStage.key_icon);
        }

        OnKeyCountChanged?.Invoke(collectedKeys, currentStage.key_item_count);
        OnStageChanged?.Invoke(stageNo);

        Debug.Log($"[StageManager] Stage {stageNo} loaded - Need {currentStage.key_item_count}x {currentStage.key_name}");
    }

    /// <summary>열쇠 아이템 획득</summary>
    public void AddKey(int amount = 1)
    {
        if (currentStage == null || CurrentPhase != BossPhase.Collecting) return;

        collectedKeys = Mathf.Min(collectedKeys + amount, currentStage.key_item_count);
        OnKeyCountChanged?.Invoke(collectedKeys, currentStage.key_item_count);
    }

    /// <summary>보스 소환 시작 - 카운트다운 진입 (UI 버튼에서 호출)</summary>
    public void TrySummonBoss()
    {
        if (!CanSummonBoss) return;
        StartBossSummon();
    }

    private void StartBossSummon()
    {
        // 열쇠 소모
        collectedKeys = 0;
        OnKeyCountChanged?.Invoke(collectedKeys, RequiredKeys);

        // 카운트다운 시작
        CurrentPhase = BossPhase.Countdown;
        countdownTimer = countdownDuration;
        OnCountdownStart?.Invoke();

        Debug.Log($"[StageManager] Countdown started: {countdownDuration}s");
    }

    /// <summary>카운트다운 완료 → 포탈 생성 + Warning Wave 시작</summary>
    private void ExecuteBossSummon()
    {
        CurrentPhase = BossPhase.WarningWave;

        // 타이머 리셋
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.survivalTime = 0f;

        // EnemySpawner에 Warning Wave 테이블 데이터 전달
        if (enemySpawner != null && currentStage != null
            && !string.IsNullOrEmpty(currentStage.ww_group_id))
        {
            enemySpawner.InitWarningWave(currentStage.ww_group_id);
        }

        // 포탈 생성
        SpawnPortal();

        // Warning Wave 시작 이벤트
        OnWarningWaveStart?.Invoke();

        Debug.Log("[StageManager] Warning Wave started! Portal spawned.");
    }

    /// <summary>포탈 생성</summary>
    private void SpawnPortal()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // 플레이어 앞 8유닛 위치에 포탈 생성
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 spawnPos = player.transform.position + new Vector3(
            Mathf.Cos(angle) * 8f, Mathf.Sin(angle) * 8f, 0f);

        portalObj = new GameObject("BossPortal");
        portalObj.transform.position = spawnPos;
        portalObj.tag = "BossPortal";

        // 스프라이트 설정
        var sr = portalObj.AddComponent<SpriteRenderer>();
        string portalImagePath = $"Sprites/Icons/BossEnter/map1_stage{currentStageNo}_boss_enter";
        sr.sprite = Resources.Load<Sprite>(portalImagePath);
        sr.sortingOrder = 5;

        // 포탈 스크립트 추가
        portalObj.AddComponent<BossPortal>();

        portalObj.transform.localScale = new Vector3(3f, 3f, 1f);

        Debug.Log($"[StageManager] Portal spawned at {spawnPos}");
    }

    /// <summary>플레이어가 포탈에 진입했을 때 호출</summary>
    public void OnPortalEntered()
    {
        if (CurrentPhase != BossPhase.WarningWave) return;

        CurrentPhase = BossPhase.Fighting;

        // 포탈 제거
        if (portalObj != null)
        {
            Destroy(portalObj);
            portalObj = null;
        }

        // 모든 일반 몬스터 제거
        DestroyAllEnemies();

        // 보스 소환
        SpawnBossById(currentStage.boss_mon_id);

        // 아레나 생성
        if (currentBoss != null)
            CreateArena();

        OnBossSummoned?.Invoke();
        Debug.Log($"[StageManager] Portal entered! Boss summoned: {currentStage.boss_mon_id}");
    }

    /// <summary>모든 일반 몬스터 제거</summary>
    private void DestroyAllEnemies()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var enemy in enemies)
        {
            Destroy(enemy);
        }
        Debug.Log($"[StageManager] {enemies.Length} enemies destroyed");
    }

    /// <summary>보스 아레나(벽) 생성</summary>
    private void CreateArena()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || currentBoss == null) return;

        // 플레이어와 보스의 중간 지점을 아레나 중심으로
        Vector3 center = (player.transform.position + currentBoss.transform.position) / 2f;
        float distToBoss = Vector3.Distance(player.transform.position, currentBoss.transform.position);
        float tableRadius = currentStage != null && currentStage.arena_radius > 0
            ? currentStage.arena_radius : 8f;
        float radius = Mathf.Max(tableRadius, distToBoss * 0.7f);

        arenaObj = new GameObject("BossArena");
        arenaObj.transform.position = center;

        // 벽 세그먼트 수
        int segments = 32;
        float wallThickness = 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            float nextAngle = (360f / segments) * (i + 1) * Mathf.Deg2Rad;

            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            var wall = new GameObject($"Wall_{i}");
            wall.transform.SetParent(arenaObj.transform);
            wall.transform.position = pos;

            // 벽 콜라이더
            var col = wall.AddComponent<BoxCollider2D>();
            float segLength = 2f * Mathf.PI * radius / segments;
            col.size = new Vector2(segLength, wallThickness);

            // 벽의 각도 맞추기
            float angleDeg = (360f / segments) * i + 90f;
            wall.transform.rotation = Quaternion.Euler(0, 0, angleDeg);

            // 비주얼
            var sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = CreateWallSprite();
            sr.color = new Color(1f, 0.3f, 0.2f, 0.7f);
            sr.sortingOrder = 5;
            wall.transform.localScale = new Vector3(segLength, wallThickness, 1f);
        }

        // 아레나 가장자리 시각 효과 (원형 라인)
        var ring = new GameObject("ArenaRing");
        ring.transform.SetParent(arenaObj.transform);
        ring.transform.position = center;

        var lr = ring.AddComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.startWidth = 0.15f;
        lr.endWidth = 0.15f;
        lr.loop = false;
        lr.useWorldSpace = true;
        lr.sortingOrder = 4;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.4f, 0.2f, 0.9f);
        lr.endColor = new Color(1f, 0.4f, 0.2f, 0.9f);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            lr.SetPosition(i, pos);
        }

        Debug.Log($"[StageManager] Arena created at {center}, radius={radius:F1}");
    }

    private Sprite CreateWallSprite()
    {
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    /// <summary>아레나 제거</summary>
    private void DestroyArena()
    {
        if (arenaObj != null)
        {
            Destroy(arenaObj);
            arenaObj = null;
        }
    }

    private void SpawnBossById(string bossMonId)
    {
        if (enemySpawner == null) return;

        for (int i = 0; i < enemySpawner.bossDataList.Count; i++)
        {
            if (enemySpawner.bossDataList[i].monId == bossMonId)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player == null) return;

                // 보스를 플레이어 앞 적당한 거리에 소환
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float spawnRadius = currentStage != null && currentStage.arena_radius > 0
                    ? currentStage.arena_radius : 8f;
                float spawnDist = spawnRadius * 0.5f;
                Vector3 spawnPos = player.transform.position + new Vector3(
                    Mathf.Cos(angle) * spawnDist,
                    Mathf.Sin(angle) * spawnDist, 0f);

                currentBoss = Instantiate(enemySpawner.bossPrefabs[i]);
                currentBoss.SetActive(true);
                currentBoss.transform.position = spawnPos;
                currentBoss.name = "BOSS";

                var data = enemySpawner.bossDataList[i];
                var sr = currentBoss.GetComponent<SpriteRenderer>();
                if (sr != null && data.sprite != null)
                    sr.sprite = data.sprite;

                currentBoss.transform.localScale = new Vector3(data.scale, data.scale, 1f);

                var bounce = currentBoss.AddComponent<MonsterBounce>();
                bounce.bounceSpeed = data.bounceSpeed;
                bounce.bounceHeight = data.bounceHeight;
                bounce.squashAmount = data.bounceSquash;
                bounce.RefreshBaseScale();

                var eh = currentBoss.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.maxHealth = data.health;
                    eh.currentHealth = eh.maxHealth;
                    eh.expDrop = data.expDrop;
                    eh.goldDrop = data.goldDrop;
                }

                var ai = currentBoss.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    ai.moveSpeed = data.moveSpeed;
                    ai.contactDamage = data.contactDamage;
                }

                return;
            }
        }

        Debug.LogWarning($"[StageManager] Boss not found: {bossMonId}");
    }

    /// <summary>보스 처치 처리</summary>
    private void OnBossDefeatedHandler()
    {
        CurrentPhase = BossPhase.Defeated;
        DestroyArena();
        OnBossDefeatedEvent?.Invoke();

        Debug.Log("[StageManager] Boss defeated!");

        // 다음 스테이지로
        int nextStage = currentStageNo + 1;
        var nextStageData = TableManager.Instance.GetStage(currentMapId, nextStage);

        if (nextStageData != null)
        {
            LoadStage(currentMapId, nextStage);
            Debug.Log($"[StageManager] Advanced to stage {nextStage}");
        }
        else
        {
            Debug.Log($"[StageManager] All stages cleared for {currentMapId}!");
            GameManager.Instance.OnRunComplete();
        }
    }
}
