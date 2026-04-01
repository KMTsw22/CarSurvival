using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CheatWindow : EditorWindow
{
    // 몬스터 소환
    private int selectedMonsterIndex = 0;
    private float scaleOverride = 1f;
    private float bounceSpeed = 12f;
    private float bounceHeight = 0.25f;
    private float bounceSquash = 0.15f;
    private GameObject spawnedMonster;

    // 몬스터 이름 (테이블 순서)
    private readonly string[] monsterNames = {
        "0: 타이어 좀비",
        "1: 오일 슬라임",
        "2: 메탈 크러셔",
        "3: 스파크 유령",
        "4: 보스 트럭"
    };

    private Vector2 scrollPos;

    [MenuItem("Car Survivor/Cheat Window %#D")]  // Ctrl+Shift+D
    public static void ShowWindow()
    {
        var window = GetWindow<CheatWindow>("Cheat Window");
        window.minSize = new Vector2(320, 400);
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ─── 몬스터 소환 ───
        EditorGUILayout.LabelField("Monster Spawn", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        selectedMonsterIndex = EditorGUILayout.Popup("몬스터 선택", selectedMonsterIndex, monsterNames);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("소환", GUILayout.Height(30)))
            SpawnMonster();
        if (GUILayout.Button("제거", GUILayout.Height(30)))
            ClearMonster();
        if (GUILayout.Button("전부 제거", GUILayout.Height(30)))
            ClearAllEnemies();
        EditorGUILayout.EndHorizontal();

        // ─── 스케일 조정 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Scale 조정", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        float newScale = EditorGUILayout.Slider("Scale", scaleOverride, 0.1f, 5f);
        if (!Mathf.Approximately(newScale, scaleOverride))
        {
            scaleOverride = newScale;
            ApplyScaleToSpawned();
        }

        // 프리셋 버튼
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0.3")) { scaleOverride = 0.3f; ApplyScaleToSpawned(); }
        if (GUILayout.Button("0.5")) { scaleOverride = 0.5f; ApplyScaleToSpawned(); }
        if (GUILayout.Button("0.8")) { scaleOverride = 0.8f; ApplyScaleToSpawned(); }
        if (GUILayout.Button("1.0")) { scaleOverride = 1.0f; ApplyScaleToSpawned(); }
        if (GUILayout.Button("1.5")) { scaleOverride = 1.5f; ApplyScaleToSpawned(); }
        if (GUILayout.Button("2.0")) { scaleOverride = 2.0f; ApplyScaleToSpawned(); }
        EditorGUILayout.EndHorizontal();

        // ─── 바운스 조정 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Bounce 조정", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        float newBounceSpeed = EditorGUILayout.FloatField("Bounce Speed", bounceSpeed);
        newBounceSpeed = EditorGUILayout.Slider(" ", newBounceSpeed, 0.1f, 30f);
        float newBounceHeight = EditorGUILayout.FloatField("Bounce Height", bounceHeight);
        newBounceHeight = EditorGUILayout.Slider(" ", newBounceHeight, 0f, 2f);
        float newBounceSquash = EditorGUILayout.FloatField("Squash Amount", bounceSquash);
        newBounceSquash = EditorGUILayout.Slider(" ", newBounceSquash, 0f, 1f);

        bool bounceChanged = !Mathf.Approximately(newBounceSpeed, bounceSpeed)
            || !Mathf.Approximately(newBounceHeight, bounceHeight)
            || !Mathf.Approximately(newBounceSquash, bounceSquash);

        bounceSpeed = newBounceSpeed;
        bounceHeight = newBounceHeight;
        bounceSquash = newBounceSquash;

        if (bounceChanged)
            ApplyBounceToSpawned();

        // 현재 상태 표시
        if (spawnedMonster != null)
        {
            EditorGUILayout.HelpBox(
                $"현재: {spawnedMonster.name}\nScale: {scaleOverride:F2} | Bounce: spd={bounceSpeed:F1} hgt={bounceHeight:F2} sqsh={bounceSquash:F2}",
                MessageType.Info);
        }

        // ─── 시간 이동 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("시간 이동 (Wave 테스트)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var statsForTime = Application.isPlaying ? PlayerStats.Instance : null;
        if (statsForTime != null)
        {
            int min = Mathf.FloorToInt(statsForTime.survivalTime / 60f);
            int sec = Mathf.FloorToInt(statsForTime.survivalTime % 60f);
            EditorGUILayout.LabelField($"현재 시간: {min:00}:{sec:00} ({statsForTime.survivalTime:F1}초)");

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-30초", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, -30f);
            if (GUILayout.Button("-10초", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, -10f);
            if (GUILayout.Button("+10초", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, 10f);
            if (GUILayout.Button("+30초", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, 30f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+1분", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, 60f);
            if (GUILayout.Button("+3분", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, 180f);
            if (GUILayout.Button("+5분", GUILayout.Height(25)))
                CheatTimeSkip(statsForTime, 300f);
            if (GUILayout.Button("0으로 리셋", GUILayout.Height(25)))
                CheatTimeSet(statsForTime, 0f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("기존 적 전부 제거 + 웨이브 새로고침", GUILayout.Height(25)))
            {
                ClearAllEnemies();
                ForceTimeSync(statsForTime);
            }

            // waveRows 상태 표시
            var spawnerInfo = FindSpawner();
            if (spawnerInfo != null && spawnerInfo.waveRows != null)
                EditorGUILayout.HelpBox($"Wave 데이터: {spawnerInfo.waveRows.Length}행 로드됨", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Wave 데이터 없음 (레거시 모드)", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용 가능합니다.", MessageType.Warning);
        }

        // ─── 자동 스폰 토글 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Spawner 제어", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var spawner = FindSpawner();
        if (spawner != null)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = spawner.debugMode ? Color.red : Color.green;
            string label = spawner.debugMode ? "디버그 모드 ON (자동 스폰 차단중)" : "디버그 모드 OFF (자동 스폰 중)";
            if (GUILayout.Button(label, GUILayout.Height(30)))
                spawner.debugMode = !spawner.debugMode;
            GUI.backgroundColor = oldColor;
        }
        else
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용 가능합니다.", MessageType.Warning);
        }

        // ─── 게임 치트 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Game Cheats", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("HP 풀회복", GUILayout.Height(25)))
            CheatFullHP();
        if (GUILayout.Button("무적 토글", GUILayout.Height(25)))
            CheatToggleInvincible();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("레벨업", GUILayout.Height(25)))
            CheatLevelUp();
        if (GUILayout.Button("골드 +1000", GUILayout.Height(25)))
            CheatAddGold();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        var autoAttack = Application.isPlaying ? Object.FindFirstObjectByType<AutoAttack>() : null;
        if (autoAttack != null)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = autoAttack.enabled ? Color.green : Color.red;
            string label = autoAttack.enabled ? "총알 ON (클릭하면 OFF)" : "총알 OFF (클릭하면 ON)";
            if (GUILayout.Button(label, GUILayout.Height(25)))
                autoAttack.enabled = !autoAttack.enabled;
            GUI.backgroundColor = oldColor;
        }

        // ─── 테이블 변환 ───
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Table Export", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("엑셀 → .bytes 변환 (export_msgpack.py)", GUILayout.Height(30)))
            ExportTables();

        EditorGUILayout.EndScrollView();
    }

    // ─── Table Export ───

    private void ExportTables()
    {
        string scriptDir = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "../../CarSurvival_Plan/Table"));
        string pyScript = System.IO.Path.Combine(scriptDir, "export_msgpack.py");

        if (!System.IO.File.Exists(pyScript))
        {
            Debug.LogError($"[Cheat] export_msgpack.py를 찾을 수 없습니다: {pyScript}");
            return;
        }

        var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "py";
        process.StartInfo.Arguments = $"\"{pyScript}\"";
        process.StartInfo.WorkingDirectory = scriptDir;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        process.Start();

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(output))
            Debug.Log($"[Table Export]\n{output}");
        if (!string.IsNullOrEmpty(error))
            Debug.LogError($"[Table Export Error]\n{error}");

        AssetDatabase.Refresh();
        Debug.Log("[Table Export] 완료! Assets 새로고침됨.");
    }

    // ─── Monster Spawn ───

    private void SpawnMonster()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Cheat] Play 모드에서만 사용 가능합니다.");
            return;
        }

        ClearMonster();

        var spawner = FindSpawner();
        if (spawner == null) return;

        GameObject prefab;
        MonsterData data;

        if (selectedMonsterIndex < spawner.monsterDataList.Count)
        {
            prefab = spawner.enemyPrefabs[selectedMonsterIndex];
            data = spawner.monsterDataList[selectedMonsterIndex];
        }
        else if (spawner.bossDataList.Count > 0)
        {
            prefab = spawner.bossPrefabs[0];
            data = spawner.bossDataList[0];
        }
        else return;

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 spawnPos = player != null
            ? player.transform.position + player.transform.up * 5f
            : Vector3.zero;

        spawnedMonster = Instantiate(prefab);
        spawnedMonster.SetActive(true);
        spawnedMonster.transform.position = spawnPos;
        spawnedMonster.name = $"[CHEAT] {data.monsterName}";

        // 스프라이트 적용
        var sr = spawnedMonster.GetComponent<SpriteRenderer>();
        if (sr != null && data.sprite != null)
        {
            sr.sprite = data.sprite;
            if (data.tintColor != Color.white)
                sr.color = data.tintColor;
        }

        // HP 적용
        var eh = spawnedMonster.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.maxHealth = data.health;
            eh.currentHealth = data.health;
            eh.expDrop = data.expDrop;
            eh.goldDrop = data.goldDrop;
        }

        // 테이블 속도/데미지 적용
        var ai = spawnedMonster.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed = data.moveSpeed;
            ai.contactDamage = data.contactDamage;
        }

        scaleOverride = data.scale;
        spawnedMonster.transform.localScale = new Vector3(scaleOverride, scaleOverride, 1f);

        // 바운스 추가
        var bounce = spawnedMonster.AddComponent<MonsterBounce>();
        bounceSpeed = data.bounceSpeed;
        bounceHeight = data.bounceHeight;
        bounceSquash = data.bounceSquash;
        bounce.bounceSpeed = bounceSpeed;
        bounce.bounceHeight = bounceHeight;
        bounce.squashAmount = bounceSquash;
        bounce.RefreshBaseScale();

        Debug.Log($"[Cheat] Spawned: {data.monsterName} (scale: {scaleOverride}, bounce: spd={bounceSpeed} hgt={bounceHeight} sqsh={bounceSquash})");
    }

    private void ClearMonster()
    {
        if (spawnedMonster != null)
            DestroyImmediate(spawnedMonster);
    }

    private void ClearAllEnemies()
    {
        if (!Application.isPlaying) return;
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
            DestroyImmediate(e);
        spawnedMonster = null;
        Debug.Log($"[Cheat] 적 {enemies.Length}마리 제거");
    }

    private void ApplyScaleToSpawned()
    {
        if (spawnedMonster != null)
        {
            spawnedMonster.transform.localScale = new Vector3(scaleOverride, scaleOverride, 1f);
            var bounce = spawnedMonster.GetComponent<MonsterBounce>();
            if (bounce != null) bounce.RefreshBaseScale();
        }
    }

    private void ApplyBounceToSpawned()
    {
        if (spawnedMonster == null) return;
        var bounce = spawnedMonster.GetComponent<MonsterBounce>();
        if (bounce != null)
        {
            bounce.bounceSpeed = bounceSpeed;
            bounce.bounceHeight = bounceHeight;
            bounce.squashAmount = bounceSquash;
        }
    }

    // ─── Game Cheats ───

    private void CheatFullHP()
    {
        if (!Application.isPlaying) return;
        var player = PlayerStats.Instance;
        if (player != null)
        {
            player.currentHealth = player.maxHealth;
            Debug.Log("[Cheat] HP 풀회복");
        }
    }

    private void CheatToggleInvincible()
    {
        if (!Application.isPlaying) return;
        var player = PlayerStats.Instance;
        if (player != null)
        {
            player.isInvincible = !player.isInvincible;
            if (player.isInvincible)
            {
                player.currentHealth = player.maxHealth;
            }
            Debug.Log($"[Cheat] 무적 {(player.isInvincible ? "ON" : "OFF")}");
        }
    }

    private void CheatLevelUp()
    {
        if (!Application.isPlaying) return;
        var player = PlayerStats.Instance;
        if (player != null)
        {
            player.AddExperience(player.expToNextLevel);
            Debug.Log("[Cheat] 레벨업!");
        }
    }

    private void CheatAddGold()
    {
        if (!Application.isPlaying) return;
        var player = PlayerStats.Instance;
        if (player != null)
        {
            player.gold += 1000;
            Debug.Log("[Cheat] 골드 +1000");
        }
    }

    // ─── Time Skip ───

    private void CheatTimeSkip(PlayerStats stats, float delta)
    {
        stats.survivalTime = Mathf.Max(0f, stats.survivalTime + delta);
        if (delta < 0) ClearAllEnemies(); // 뒤로 가면 기존 적 정리
        ForceTimeSync(stats);
        int min = Mathf.FloorToInt(stats.survivalTime / 60f);
        int sec = Mathf.FloorToInt(stats.survivalTime % 60f);
        Debug.Log($"[Cheat] 시간 이동 → {min:00}:{sec:00} ({(delta > 0 ? "+" : "")}{delta}초)");
    }

    private void CheatTimeSet(PlayerStats stats, float time)
    {
        ClearAllEnemies();
        stats.survivalTime = time;
        ForceTimeSync(stats);
        Debug.Log($"[Cheat] 시간 리셋 → 00:00");
    }

    private void ForceTimeSync(PlayerStats stats)
    {
        // EnemySpawner 웨이브 재평가 + playerStats 참조 보장
        var spawner = FindSpawner();
        if (spawner != null)
        {
            spawner.currentWaveNo = -1;
            // playerStats 참조가 없으면 강제 주입
            var field = typeof(EnemySpawner).GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && field.GetValue(spawner) == null)
            {
                field.SetValue(spawner, stats);
                Debug.Log("[Cheat] EnemySpawner.playerStats 강제 주입");
            }
        }

        // HUDManager playerStats 참조 보장
        var hud = Object.FindFirstObjectByType<HUDManager>();
        if (hud != null)
        {
            var field = typeof(HUDManager).GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null && field.GetValue(hud) == null)
            {
                field.SetValue(hud, stats);
                Debug.Log("[Cheat] HUDManager.playerStats 강제 주입");
            }
        }
    }

    // ─── Util ───

    private EnemySpawner FindSpawner()
    {
        return Object.FindFirstObjectByType<EnemySpawner>();
    }
}
