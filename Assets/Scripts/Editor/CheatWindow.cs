using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CheatWindow : EditorWindow
{
    // 탭
    private int selectedTab = 0;
    private readonly string[] tabNames = { "일반", "아이템 테스트" };

    // 몬스터 소환
    private int selectedMonsterIndex = 0;
    private float scaleOverride = 1f;
    private float bounceSpeed = 12f;
    private float bounceHeight = 0.25f;
    private float bounceSquash = 0.15f;
    private GameObject spawnedMonster;

    // 몬스터 스탯 테이블 오버라이드 (런타임 MonsterRow 직접 수정)
    private Vector2 statScrollPos;
    private bool statFoldout = true;

    // 몬스터 이름 (런타임에 스포너에서 자동 생성)
    private string[] monsterNames = null;

    private Vector2 scrollPos;

    // 아이템 테스트 탭
    private Vector2 itemScrollPos;
    private bool weaponFoldout = true;
    private bool spellBookFoldout = true;

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
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(28));
        EditorGUILayout.Space(4);

        switch (selectedTab)
        {
            case 0: DrawGeneralTab(); break;
            case 1: DrawItemTestTab(); break;
        }
    }

    // ════════════════════════════════════════
    // 탭 0: 일반 (기존 기능)
    // ════════════════════════════════════════
    private void DrawGeneralTab()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ─── 몬스터 소환 ───
        EditorGUILayout.LabelField("Monster Spawn", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        RefreshMonsterNames();
        if (monsterNames != null && monsterNames.Length > 0)
        {
            if (selectedMonsterIndex >= monsterNames.Length)
                selectedMonsterIndex = 0;
            selectedMonsterIndex = EditorGUILayout.Popup("몬스터 선택", selectedMonsterIndex, monsterNames);
        }
        else
        {
            EditorGUILayout.HelpBox("Play 모드에서 몬스터 목록이 표시됩니다.", MessageType.Info);
        }

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

        // ─── 몬스터 스탯 조정 (테이블 기반) ───
        EditorGUILayout.Space(10);
        statFoldout = EditorGUILayout.Foldout(statFoldout, "몬스터 스탯 조정 (TB_Monster)", true, EditorStyles.foldoutHeader);

        if (statFoldout)
        {
            if (!Application.isPlaying || !TableManager.Instance.IsLoaded)
            {
                EditorGUILayout.HelpBox("Play 모드에서 테이블 로드 후 사용 가능합니다.", MessageType.Info);
            }
            else
            {
                var monsters = TableManager.Instance.Monsters;
                if (monsters != null && monsters.Length > 0)
                {
                    // 헤더
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("mon_id", EditorStyles.miniLabel, GUILayout.Width(100));
                    EditorGUILayout.LabelField("이름", EditorStyles.miniLabel, GUILayout.Width(80));
                    EditorGUILayout.LabelField("base_hp", EditorStyles.miniLabel, GUILayout.Width(70));
                    EditorGUILayout.LabelField("base_speed", EditorStyles.miniLabel, GUILayout.Width(70));
                    EditorGUILayout.LabelField("dmg", EditorStyles.miniLabel, GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();

                    statScrollPos = EditorGUILayout.BeginScrollView(statScrollPos, GUILayout.Height(Mathf.Min(monsters.Length * 22 + 10, 200)));
                    for (int i = 0; i < monsters.Length; i++)
                    {
                        var m = monsters[i];
                        EditorGUILayout.BeginHorizontal();

                        // mon_id (읽기 전용)
                        EditorGUILayout.LabelField(m.mon_id, GUILayout.Width(100));
                        // 이름 (읽기 전용)
                        EditorGUILayout.LabelField(m.mon_name, GUILayout.Width(80));
                        // base_hp (수정 가능)
                        m.base_hp = EditorGUILayout.FloatField(m.base_hp, GUILayout.Width(70));
                        // base_speed (수정 가능)
                        m.base_speed = EditorGUILayout.FloatField(m.base_speed, GUILayout.Width(70));
                        // contact_damage (수정 가능)
                        m.contact_damage = EditorGUILayout.FloatField(m.contact_damage, GUILayout.Width(50));

                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();

                    EditorGUILayout.Space(4);
                    if (GUILayout.Button("변경사항 → 필드 몬스터에 적용", GUILayout.Height(28)))
                        ApplyTableStatsToAlive();

                    int aliveCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
                    if (aliveCount > 0)
                        EditorGUILayout.HelpBox($"생존 몬스터: {aliveCount}마리 (적용 시 테이블 값으로 덮어씁니다)", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("TB_Monster 데이터가 비어있습니다.", MessageType.Warning);
                }
            }
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

    // ════════════════════════════════════════
    // 탭 1: 아이템 테스트 (무기 / 스펠북)
    // ════════════════════════════════════════
    private void DrawItemTestTab()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play 모드에서만 사용 가능합니다.", MessageType.Warning);
            return;
        }

        var player = PlayerStats.Instance;
        var gm = GameManager.Instance;
        if (player == null || gm == null || gm.partsDatabase == null)
        {
            EditorGUILayout.HelpBox("PlayerStats 또는 PartsDatabase를 찾을 수 없습니다.", MessageType.Warning);
            return;
        }

        var db = gm.partsDatabase;

        // ─── 현재 장착 현황 ───
        EditorGUILayout.LabelField("현재 장착 중", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        if (player.equippedParts.Count == 0)
        {
            EditorGUILayout.HelpBox("장착된 아이템이 없습니다.", MessageType.Info);
        }
        else
        {
            for (int i = player.equippedParts.Count - 1; i >= 0; i--)
            {
                var owned = player.equippedParts[i];
                EditorGUILayout.BeginHorizontal();

                string catTag = owned.data.category switch
                {
                    ItemCategory.MainWeapon => "[주무기]",
                    ItemCategory.SubWeapon => "[보조]",
                    ItemCategory.SpellBook => "[마법서]",
                    _ => "[???]"
                };
                EditorGUILayout.LabelField($"{catTag} {owned.data.partName}", GUILayout.Width(180));
                EditorGUILayout.LabelField($"Lv.{owned.level}/{owned.data.maxLevel}", GUILayout.Width(60));

                if (owned.level < owned.data.maxLevel)
                {
                    if (GUILayout.Button("레벨업", GUILayout.Width(55), GUILayout.Height(20)))
                    {
                        owned.level++;
                        ForceRecalculate(player);
                    }
                }

                if (GUILayout.Button("MAX", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    owned.level = owned.data.maxLevel;
                    ForceRecalculate(player);
                }

                if (GUILayout.Button("해제", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    player.equippedParts.RemoveAt(i);
                    ForceRecalculate(player);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("전부 해제", GUILayout.Height(24)))
        {
            player.equippedParts.Clear();
            ForceRecalculate(player);
        }

        EditorGUILayout.Space(10);

        // ─── 무기 / 스펠북 목록 ───
        itemScrollPos = EditorGUILayout.BeginScrollView(itemScrollPos);

        // 무기
        weaponFoldout = EditorGUILayout.Foldout(weaponFoldout, "무기 목록", true, EditorStyles.foldoutHeader);
        if (weaponFoldout)
        {
            foreach (var part in db.allParts)
            {
                if (part.category != ItemCategory.MainWeapon && part.category != ItemCategory.SubWeapon)
                    continue;
                DrawItemRow(player, part);
            }
        }

        EditorGUILayout.Space(6);

        // 스펠북
        spellBookFoldout = EditorGUILayout.Foldout(spellBookFoldout, "스펠북 목록", true, EditorStyles.foldoutHeader);
        if (spellBookFoldout)
        {
            foreach (var part in db.allParts)
            {
                if (part.category != ItemCategory.SpellBook) continue;
                DrawItemRow(player, part);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawItemRow(PlayerStats player, PartsData part)
    {
        var owned = player.equippedParts.Find(p => p.data == part);
        bool isEquipped = owned != null;

        EditorGUILayout.BeginHorizontal();

        // 상태 색상
        var oldBg = GUI.backgroundColor;
        if (isEquipped)
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f, 1f);

        string catLabel = part.category switch
        {
            ItemCategory.MainWeapon => "[주]",
            ItemCategory.SubWeapon => "[보]",
            ItemCategory.SpellBook => "[마]",
            _ => ""
        };

        string statusText = isEquipped ? $"Lv.{owned.level}/{part.maxLevel}" : "미장착";
        EditorGUILayout.LabelField($"{catLabel} {part.partName}", GUILayout.Width(150));
        EditorGUILayout.LabelField(statusText, GUILayout.Width(70));

        if (!isEquipped)
        {
            if (GUILayout.Button("장착", GUILayout.Width(45), GUILayout.Height(20)))
            {
                player.equippedParts.Add(new OwnedPart { data = part, level = 1 });
                ForceRecalculate(player);
            }
        }
        else
        {
            if (owned.level < part.maxLevel)
            {
                if (GUILayout.Button("+Lv", GUILayout.Width(35), GUILayout.Height(20)))
                {
                    owned.level++;
                    ForceRecalculate(player);
                }
            }
            if (GUILayout.Button("MAX", GUILayout.Width(35), GUILayout.Height(20)))
            {
                owned.level = part.maxLevel;
                ForceRecalculate(player);
            }
            if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
            {
                player.equippedParts.Remove(owned);
                ForceRecalculate(player);
            }
        }

        GUI.backgroundColor = oldBg;
        EditorGUILayout.EndHorizontal();
    }

    private void ForceRecalculate(PlayerStats player)
    {
        // RecalculateStats는 private이므로 리플렉션 사용
        var method = typeof(PlayerStats).GetMethod("RecalculateStats",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        method?.Invoke(player, null);

        // OnPartChanged는 event이므로 backing field는 NonPublic
        var evt = typeof(PlayerStats).GetField("OnPartChanged",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (evt != null)
        {
            var action = evt.GetValue(player) as System.Action;
            action?.Invoke();
        }
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

        int normalCount = spawner.monsterDataList.Count;
        if (selectedMonsterIndex < normalCount)
        {
            prefab = spawner.enemyPrefabs[selectedMonsterIndex];
            data = spawner.monsterDataList[selectedMonsterIndex];
        }
        else
        {
            int bossIdx = selectedMonsterIndex - normalCount;
            if (bossIdx < spawner.bossDataList.Count)
            {
                prefab = spawner.bossPrefabs[bossIdx];
                data = spawner.bossDataList[bossIdx];
            }
            else return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        Vector3 spawnPos = player != null
            ? player.transform.position + player.transform.up * 5f
            : Vector3.zero;

        spawnedMonster = Instantiate(prefab);
        spawnedMonster.SetActive(true);
        spawnedMonster.transform.position = spawnPos;
        spawnedMonster.name = $"[CHEAT] {data.monsterName}";

        // mon_id 기록
        var eid = spawnedMonster.GetComponent<EnemyIdentifier>();
        if (eid == null) eid = spawnedMonster.AddComponent<EnemyIdentifier>();
        eid.monId = data.monId;

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

    // ─── Monster Stat Override (테이블 기반) ───

    private void ApplyTableStatsToAlive()
    {
        if (!Application.isPlaying || !TableManager.Instance.IsLoaded) return;

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        int applied = 0;
        foreach (var enemy in enemies)
        {
            // mon_id 매칭: EnemySpawner가 monId를 저장했는지 확인
            var identifier = enemy.GetComponent<EnemyIdentifier>();
            if (identifier == null) continue;

            var row = TableManager.Instance.GetMonster(identifier.monId);
            if (row == null) continue;

            var ai = enemy.GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.moveSpeed = row.base_speed;
                ai.contactDamage = row.contact_damage;
            }

            var eh = enemy.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                float hpRatio = eh.maxHealth > 0 ? eh.currentHealth / eh.maxHealth : 1f;
                eh.maxHealth = row.base_hp;
                eh.currentHealth = row.base_hp * hpRatio;
            }

            applied++;
        }
        Debug.Log($"[Cheat] 테이블 스탯 적용: {applied}/{enemies.Length}마리 (EnemyIdentifier 필요)");
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

    // ─── Monster Names ───

    private void RefreshMonsterNames()
    {
        if (!Application.isPlaying) { monsterNames = null; return; }

        var spawner = FindSpawner();
        if (spawner == null) { monsterNames = null; return; }

        int normalCount = spawner.monsterDataList.Count;
        int bossCount = spawner.bossDataList.Count;
        int total = normalCount + bossCount;

        if (monsterNames != null && monsterNames.Length == total) return;

        monsterNames = new string[total];
        for (int i = 0; i < normalCount; i++)
            monsterNames[i] = $"{i}: {spawner.monsterDataList[i].monsterName}";
        for (int i = 0; i < bossCount; i++)
            monsterNames[normalCount + i] = $"{normalCount + i}: [BOSS] {spawner.bossDataList[i].monsterName}";
    }

    // ─── Util ───

    private EnemySpawner FindSpawner()
    {
        return Object.FindFirstObjectByType<EnemySpawner>();
    }
}
