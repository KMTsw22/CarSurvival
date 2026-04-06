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

    // 배경 맵 교체용
    private Sprite originalMapSprite;
    private Sprite bossMapSprite;
    private GameObject bossMapObj;
    private Vector3 arenaCenter;
    private float savedOrthoSize;

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
    public event Action OnMiniBossKilled;                    // 미니보스 처치

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
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
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

    /// <summary>미니보스 처치 알림 (EnemyHealth에서 호출)</summary>
    public void NotifyMiniBossKilled()
    {
        OnMiniBossKilled?.Invoke();
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

        // 플레이어를 원점(맵 중앙)으로 텔레포트
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = Vector3.zero;

        // 보스 소환
        SpawnBossById(currentStage.boss_mon_id);

        // 아레나 생성 → 보스맵 교체 (arenaCenter 필요)
        if (currentBoss != null)
            CreateArena();
        SwapBackground(true);

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

    /// <summary>보스 아레나(보이지 않는 사각형 벽) 생성</summary>
    private void CreateArena()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || currentBoss == null) return;

        // 플레이어 위치(원점)를 아레나 중심으로
        Vector3 center = player.transform.position;

        if (bossMapSprite == null)
            bossMapSprite = Resources.Load<Sprite>("Sprites/BossMap/Map1_Boss");

        // 카메라 영역 기준으로 아레나 크기 결정
        var cam = Camera.main;
        float halfW, halfH;
        if (cam != null)
        {
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
        }
        else
        {
            halfW = 21f;
            halfH = 12f;
        }

        arenaCenter = center;
        arenaObj = new GameObject("BossArena");
        arenaObj.transform.position = center;

        float wallThickness = 3f;

        // 상하좌우 4개의 보이지 않는 벽 (벽 중심을 바깥쪽으로 밀어서 맵 안쪽 공간 유지)
        float halfT = wallThickness / 2f;
        CreateWall("Wall_Top", center + new Vector3(0, halfH + halfT, 0), halfW * 3.3f + wallThickness * 3.3f, wallThickness);
        CreateWall("Wall_Bottom", center + new Vector3(0, -halfH - halfT, 0), halfW * 3.3f + wallThickness * 3.3f, wallThickness);
        CreateWall("Wall_Left", center + new Vector3(-halfW - halfT, 0, 0), wallThickness, halfH * 3.3f + wallThickness * 3.3f);
        CreateWall("Wall_Right", center + new Vector3(halfW + halfT, 0, 0), wallThickness, halfH * 3.3f + wallThickness * 3.3f);

        // 카메라 클램핑 설정
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
            camFollow.SetBounds(center, halfW, halfH);

        Debug.Log($"[StageManager] Arena created at {center}, size={halfW * 3.3f:F1}x{halfH * 3.3f:F1}");
    }

    private void CreateWall(string name, Vector3 pos, float width, float height)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(arenaObj.transform);
        wall.transform.position = pos;
        var col = wall.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
        var rb = wall.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    /// <summary>아레나 제거</summary>
    private void DestroyArena()
    {
        if (arenaObj != null)
        {
            Destroy(arenaObj);
            arenaObj = null;
        }

        // 카메라 클램핑 해제
        var camFollow = Camera.main?.GetComponent<CameraFollow>();
        if (camFollow != null)
            camFollow.ClearBounds();
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
                float spawnDist = 5f;
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

                // 스킬 AI 초기화
                var skillRows = TableManager.Instance.GetSkillsByMonster(data.monId);
                if (skillRows != null && skillRows.Length > 0)
                {
                    var monsterAI = currentBoss.AddComponent<MonsterAI>();
                    monsterAI.Initialize(skillRows);
                }

                // 콜라이더를 차체 중심부에 맞춤
                var col = currentBoss.GetComponent<BoxCollider2D>();
                var bossSr = currentBoss.GetComponent<SpriteRenderer>();
                if (col != null && bossSr != null && bossSr.sprite != null)
                {
                    var bounds = bossSr.sprite.bounds;
                    col.size = new Vector2(bounds.size.x * 0.5f, bounds.size.y * 0.4f);
                    col.offset = new Vector2(0f, -bounds.size.y * 0.1f);
                }

                // 불꽃 이펙트
                var fireEffect = currentBoss.AddComponent<BossFireEffect>();
                fireEffect.Initialize();

                return;
            }
        }

        Debug.LogWarning($"[StageManager] Boss not found: {bossMonId}");
    }

    /// <summary>배경 맵 교체 (보스맵 ↔ 일반맵) + 카메라 고정</summary>
    private void SwapBackground(bool toBoss)
    {
        if (bossMapSprite == null)
            bossMapSprite = Resources.Load<Sprite>("Sprites/BossMap/Map1_Boss");

        // 기존 타일 숨기기/보이기
        var allRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude);
        foreach (var sr in allRenderers)
        {
            if (!sr.gameObject.name.StartsWith("BG_")) continue;
            sr.enabled = !toBoss;
        }

        var cam = Camera.main;
        var camFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;

        if (toBoss)
        {
            if (bossMapObj != null) Destroy(bossMapObj);

            // 카메라를 기본 크기로 리셋
            if (cam != null)
                cam.orthographicSize = 12f;

            bossMapObj = new GameObject("BossMap");
            var sr2 = bossMapObj.AddComponent<SpriteRenderer>();
            sr2.sprite = bossMapSprite;
            sr2.sortingOrder = -10;
            bossMapObj.transform.position = new Vector3(arenaCenter.x, arenaCenter.y, 0f);

            // 카메라 영역에 딱 맞게 스케일링 (가로/세로 독립)
            if (cam != null && bossMapSprite != null)
            {
                float camH = cam.orthographicSize * 2f;
                float camW = camH * cam.aspect;
                float scaleX = camW / bossMapSprite.bounds.size.x;
                float scaleY = camH / bossMapSprite.bounds.size.y;
                bossMapObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }
        else
        {
            // 보스맵 제거
            if (bossMapObj != null)
            {
                Destroy(bossMapObj);
                bossMapObj = null;
            }
        }
    }

    /// <summary>보스 처치 처리</summary>
    private void OnBossDefeatedHandler()
    {
        CurrentPhase = BossPhase.Defeated;
        DestroyArena();
        SwapBackground(false);
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
