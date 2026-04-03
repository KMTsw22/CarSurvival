using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using MessagePack;

public class TableViewerWindow : EditorWindow
{
    private string[] tableNames = {
        "TB_Monster", "TB_MonsterDrop", "TB_Wave",
        "TB_Car", "TB_Weapon", "TB_SpellBook",
        "TB_Part", "TB_Level", "TB_Map", "TB_Currency"
    };

    private int selectedTable = 0;
    private Vector2 scrollPos;
    private List<string[]> currentRows = new List<string[]>();
    private string[] currentHeaders;
    private float[] columnWidths;

    [MenuItem("Car Survivor/Table Viewer %#T")]  // Ctrl+Shift+T
    public static void ShowWindow()
    {
        var window = GetWindow<TableViewerWindow>("Table Viewer");
        window.minSize = new Vector2(800, 400);
    }

    private void OnEnable()
    {
        LoadTable(selectedTable);
    }

    private void OnGUI()
    {
        // ─── 상단: 테이블 선택 ───
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        for (int i = 0; i < tableNames.Length; i++)
        {
            var style = selectedTable == i ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold } : EditorStyles.toolbarButton;
            if (GUILayout.Button(tableNames[i].Replace("TB_", ""), style, GUILayout.MinWidth(60)))
            {
                if (selectedTable != i)
                {
                    selectedTable = i;
                    LoadTable(i);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // ─── 새로고침 + 변환 버튼 ───
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("새로고침", GUILayout.Width(80)))
            LoadTable(selectedTable);
        if (GUILayout.Button("엑셀 → .bytes 변환", GUILayout.Width(150)))
        {
            ExportTables();
            LoadTable(selectedTable);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (currentHeaders == null || currentRows.Count == 0)
        {
            EditorGUILayout.HelpBox($"{tableNames[selectedTable]}: 데이터 없음 또는 로드 실패", MessageType.Warning);
            return;
        }

        // ─── 테이블 그리기 ───
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // 헤더
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        for (int c = 0; c < currentHeaders.Length; c++)
        {
            float w = GetColumnWidth(c);
            EditorGUILayout.LabelField(currentHeaders[c], EditorStyles.boldLabel, GUILayout.Width(w));
        }
        EditorGUILayout.EndHorizontal();

        // 구분선
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, Color.gray);

        // 데이터 행
        for (int r = 0; r < currentRows.Count; r++)
        {
            var bgColor = r % 2 == 0 ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.25f, 0.25f, 0.25f);
            var rowRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rowRect, bgColor);

            for (int c = 0; c < currentRows[r].Length; c++)
            {
                float w = GetColumnWidth(c);
                EditorGUILayout.LabelField(currentRows[r][c], GUILayout.Width(w));
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // ─── 하단 정보 ───
        EditorGUILayout.LabelField($"{tableNames[selectedTable]} | {currentRows.Count} rows | {currentHeaders.Length} columns", EditorStyles.miniLabel);
    }

    private float GetColumnWidth(int colIndex)
    {
        if (columnWidths != null && colIndex < columnWidths.Length)
            return columnWidths[colIndex];
        return 100f;
    }

    private void CalculateColumnWidths()
    {
        if (currentHeaders == null) return;
        columnWidths = new float[currentHeaders.Length];

        for (int c = 0; c < currentHeaders.Length; c++)
        {
            float maxLen = currentHeaders[c].Length;
            foreach (var row in currentRows)
            {
                if (c < row.Length && row[c] != null && row[c].Length > maxLen)
                    maxLen = row[c].Length;
            }
            columnWidths[c] = Mathf.Clamp(maxLen * 8f + 16f, 50f, 250f);
        }
    }

    // ─── 테이블 로드 ───

    private void LoadTable(int index)
    {
        currentRows.Clear();
        currentHeaders = null;

        string tableName = tableNames[index];
        string path = Path.Combine(Application.dataPath, "Resources/Tables", tableName + ".bytes");

        if (!File.Exists(path))
        {
            Debug.LogWarning($"[TableViewer] 파일 없음: {path}");
            return;
        }

        byte[] data = File.ReadAllBytes(path);

        switch (tableName)
        {
            case "TB_Monster":
                LoadRows(data, new[] { "mon_id","mon_name","is_boss","base_hp","base_speed","contact_damage","scale","chapter","spawn_weight","special_ability","sprite_key","bounce_speed","bounce_height","bounce_squash" },
                    (MonsterRow r) => new[] { r.mon_id, r.mon_name, r.is_boss.ToString(), F(r.base_hp), F(r.base_speed), F(r.contact_damage), F(r.scale), r.chapter.ToString(), r.spawn_weight.ToString(), r.special_ability, r.sprite_key, F(r.bounce_speed), F(r.bounce_height), F(r.bounce_squash) });
                break;
            case "TB_MonsterDrop":
                LoadRows(data, new[] { "drop_id","mon_id","exp_amount","gold_amount","screw_amount" },
                    (MonsterDropRow r) => new[] { r.drop_id, r.mon_id, r.exp_amount.ToString(), r.gold_amount.ToString(), r.screw_amount.ToString() });
                break;
            case "TB_Wave":
                LoadRows(data, new[] { "wave_group_id","wave_no","mon_id","spawn_count","spawn_interval","max_enemies","difficulty_scale","note" },
                    (WaveRow r) => new[] { r.wave_group_id, r.wave_no.ToString(), r.mon_id, r.spawn_count.ToString(), F(r.spawn_interval), r.max_enemies.ToString(), F(r.difficulty_scale), r.note });
                break;
            case "TB_Car":
                LoadRows(data, new[] { "car_id","car_name","car_type","base_hp","base_speed","base_atk_speed","base_damage","collision_damage","passive_type","passive_value","passive_desc","unlock_cost","unlock_currency_id","unlocked_default","sprite_key" },
                    (CarRow r) => new[] { r.car_id, r.car_name, r.car_type, F(r.base_hp), F(r.base_speed), F(r.base_atk_speed), F(r.base_damage), F(r.collision_damage), r.passive_type, F(r.passive_value), r.passive_desc, r.unlock_cost.ToString(), r.unlock_currency_id, r.unlocked_by_default.ToString(), r.sprite_key });
                break;
            case "TB_Weapon":
                LoadRows(data, new[] { "weapon_id","weapon_name","category","damage","effect_desc","aim_type","weapon_type","cooldown","duration","max_level","drop_weight","icon_key","etc1","etc2","etc3","etc4","etc5","dmg/lv" },
                    (WeaponRow r) => new[] { r.weapon_id, r.weapon_name, r.weapon_category, F(r.damage), r.effect_desc, r.aim_type, r.weapon_type, F(r.cooldown), F(r.duration), r.max_level.ToString(), r.drop_weight.ToString(), r.icon_key, F(r.etc_value1), F(r.etc_value2), F(r.etc_value3), F(r.etc_value4), F(r.etc_value5), F(r.damage_per_level) });
                break;
            case "TB_SpellBook":
                LoadRows(data, new[] { "book_id","book_name","effect_type","base_value","effect_desc","max_level","stackable","drop_weight","icon_key" },
                    (SpellBookRow r) => new[] { r.book_id, r.book_name, r.effect_type, F(r.base_value), r.effect_desc, r.max_level.ToString(), r.stackable.ToString(), r.drop_weight.ToString(), r.icon_key });
                break;
            case "TB_Part":
                LoadRows(data, new[] { "part_id","part_name","part_desc","effect_type","effect_value","max_level","icon_key" },
                    (PartRow r) => new[] { r.part_id, r.part_name, r.part_desc, r.effect_type, F(r.effect_value), r.max_level.ToString(), r.icon_key });
                break;
            case "TB_Level":
                LoadRows(data, new[] { "level","required_exp","required_exp_gap","difficulty_multiplier","note" },
                    (LevelRow r) => new[] { r.level.ToString(), r.required_exp.ToString(), r.required_exp_gap.ToString(), F(r.difficulty_multiplier), r.note });
                break;
            case "TB_Map":
                LoadRows(data, new[] { "map_id","map_name","map_desc","chapter","wave_group_id","bg_sprite_key","grid_size","special_effect","unlocked_default" },
                    (MapRow r) => new[] { r.map_id, r.map_name, r.map_desc, r.chapter.ToString(), r.wave_group_id, r.bg_sprite_key, r.grid_size.ToString(), r.special_effect, r.unlocked_by_default.ToString() });
                break;
            case "TB_Currency":
                LoadRows(data, new[] { "currency_id","currency_name","currency_desc","icon_key" },
                    (CurrencyRow r) => new[] { r.currency_id, r.currency_name, r.currency_desc, r.icon_key });
                break;
        }

        CalculateColumnWidths();
    }

    private void LoadRows<T>(byte[] data, string[] headers, System.Func<T, string[]> toRow)
    {
        currentHeaders = headers;
        try
        {
            var rows = MessagePackSerializer.Deserialize<T[]>(data);
            if (rows == null) return;
            foreach (var r in rows)
                currentRows.Add(toRow(r));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TableViewer] Deserialize 실패: {e.Message}");
        }
    }

    private static string F(float v) => v.ToString("F2");

    // ─── Export ───

    private void ExportTables()
    {
        string scriptDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../CarSurvival_Plan/Table"));
        string pyScript = Path.Combine(scriptDir, "export_msgpack.py");

        if (!File.Exists(pyScript))
        {
            Debug.LogError($"[TableViewer] export_msgpack.py 없음: {pyScript}");
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
        Debug.Log("[TableViewer] 변환 완료!");
    }
}
