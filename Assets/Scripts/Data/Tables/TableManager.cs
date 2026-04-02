using UnityEngine;
using MessagePack;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 게임 시작 시 모든 .bytes 테이블을 로드하고 조회 API를 제공합니다.
/// GameBootstrap 또는 GameManager에서 TableManager.Instance.LoadAll() 호출 필요.
/// </summary>
public class TableManager
{
    private static TableManager _instance;
    public static TableManager Instance => _instance ??= new TableManager();

    // Raw arrays
    public CurrencyRow[] Currencies { get; private set; }
    public CarRow[] Cars { get; private set; }
    public WeaponRow[] Weapons { get; private set; }
    public SpellBookRow[] SpellBooks { get; private set; }
    public PartRow[] Parts { get; private set; }
    public MonsterRow[] Monsters { get; private set; }
    public MonsterDropRow[] MonsterDrops { get; private set; }
    public WaveRow[] Waves { get; private set; }
    public WarningWaveRow[] WarningWaves { get; private set; }
    public LevelRow[] Levels { get; private set; }
    public MapRow[] Maps { get; private set; }
    public StageRow[] Stages { get; private set; }
    public LangNameRow[] LangNames { get; private set; }
    public LangDesRow[] LangDescs { get; private set; }

    // Lookup dictionaries
    private Dictionary<string, CurrencyRow> _currencyDict;
    private Dictionary<string, CarRow> _carDict;
    private Dictionary<string, WeaponRow> _weaponDict;
    private Dictionary<string, SpellBookRow> _spellBookDict;
    private Dictionary<string, MonsterRow> _monsterDict;
    private Dictionary<string, MonsterDropRow> _dropByMonDict;
    private Dictionary<int, LevelRow> _levelDict;
    private Dictionary<string, MapRow> _mapDict;
    private Dictionary<string, StageRow> _stageDict;
    private Dictionary<string, LangNameRow> _langNameDict;
    private Dictionary<string, LangDesRow> _langDesDict;

    public bool IsLoaded { get; private set; }

    public void LoadAll()
    {
        Currencies = Load<CurrencyRow[]>("Tables/TB_Currency");
        Cars = Load<CarRow[]>("Tables/TB_Car");
        Weapons = Load<WeaponRow[]>("Tables/TB_Weapon");
        SpellBooks = Load<SpellBookRow[]>("Tables/TB_SpellBook");
        Parts = Load<PartRow[]>("Tables/TB_Part");
        Monsters = Load<MonsterRow[]>("Tables/TB_Monster");
        MonsterDrops = Load<MonsterDropRow[]>("Tables/TB_MonsterDrop");
        Waves = Load<WaveRow[]>("Tables/TB_Wave");
        WarningWaves = Load<WarningWaveRow[]>("Tables/TB_WarningWave");
        Levels = Load<LevelRow[]>("Tables/TB_Level");
        Maps = Load<MapRow[]>("Tables/TB_Map");
        Stages = Load<StageRow[]>("Tables/TB_Stage");
        LangNames = Load<LangNameRow[]>("Tables/TB_LangLevelUpSelect_name");
        LangDescs = Load<LangDesRow[]>("Tables/TB_LangLevelUpSelect_des");

        BuildDictionaries();
        IsLoaded = true;
        Debug.Log("[TableManager] All tables loaded.");
    }

    private T Load<T>(string path)
    {
        var asset = Resources.Load<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogWarning($"[TableManager] Table not found (may be empty): {path}");
            return default;
        }
        return MessagePackSerializer.Deserialize<T>(asset.bytes);
    }

    private void BuildDictionaries()
    {
        _currencyDict = Currencies?.ToDictionary(r => r.currency_id);
        _carDict = Cars?.ToDictionary(r => r.car_id);
        _weaponDict = Weapons?.ToDictionary(r => r.weapon_id);
        _spellBookDict = SpellBooks?.ToDictionary(r => r.book_id);
        _monsterDict = Monsters?.ToDictionary(r => r.mon_id);
        _dropByMonDict = MonsterDrops?.ToDictionary(r => r.mon_id);
        _levelDict = Levels?.ToDictionary(r => r.level);
        _mapDict = Maps?.ToDictionary(r => r.map_id);
        _stageDict = Stages?.ToDictionary(r => r.stage_id);
        _langNameDict = LangNames?.ToDictionary(r => r.item_id);
        _langDesDict = LangDescs?.ToDictionary(r => r.item_id);
    }

    // ─── 조회 API ───

    /// <summary>현재 언어 설정 ("en" 또는 "ko")</summary>
    public string CurrentLanguage { get; set; } = "en";

    /// <summary>아이템 ID로 이름 조회</summary>
    public string GetLangName(string itemId)
    {
        if (_langNameDict != null && _langNameDict.TryGetValue(itemId, out var row))
        {
            string text = CurrentLanguage == "ko" ? row.ko : row.en;
            if (string.IsNullOrEmpty(text))
                text = !string.IsNullOrEmpty(row.en) ? row.en : row.ko;
            return !string.IsNullOrEmpty(text) ? text : itemId;
        }
        return itemId;
    }

    /// <summary>아이템 ID로 설명 조회</summary>
    public string GetLangDesc(string itemId)
    {
        if (_langDesDict != null && _langDesDict.TryGetValue(itemId, out var row))
        {
            string text = CurrentLanguage == "ko" ? row.ko : row.en;
            if (string.IsNullOrEmpty(text))
                text = !string.IsNullOrEmpty(row.en) ? row.en : row.ko;
            return !string.IsNullOrEmpty(text) ? text : itemId;
        }
        return itemId;
    }

    public CurrencyRow GetCurrency(string id) => _currencyDict.GetValueOrDefault(id);
    public CarRow GetCar(string id) => _carDict.GetValueOrDefault(id);
    public WeaponRow GetWeapon(string id) => _weaponDict.GetValueOrDefault(id);
    public SpellBookRow GetSpellBook(string id) => _spellBookDict.GetValueOrDefault(id);
    public MapRow GetMap(string id) => _mapDict.GetValueOrDefault(id);
    public MonsterRow GetMonster(string id) => _monsterDict.GetValueOrDefault(id);
    public MonsterDropRow GetMonsterDrop(string monId) => _dropByMonDict.GetValueOrDefault(monId);
    public LevelRow GetLevel(int level) => _levelDict.GetValueOrDefault(level);

    /// <summary>주무기 목록</summary>
    public WeaponRow[] GetMainWeapons()
    {
        return Weapons?.Where(w => w.weapon_category == "Main").ToArray();
    }

    /// <summary>보조무기 목록</summary>
    public WeaponRow[] GetSubWeapons()
    {
        return Weapons?.Where(w => w.weapon_category == "Sub").ToArray();
    }

    /// <summary>특정 그룹의 전체 웨이브 반환</summary>
    public WaveRow[] GetWavesByGroup(string groupId)
    {
        return Waves?.Where(w => w.wave_group_id == groupId).ToArray();
    }

    /// <summary>특정 그룹 + 웨이브 번호의 행 목록 반환</summary>
    public WaveRow[] GetWavesByGroupAndNo(string groupId, int waveNo)
    {
        return Waves?.Where(w => w.wave_group_id == groupId && w.wave_no == waveNo).ToArray();
    }

    /// <summary>특정 챕터의 몬스터 목록</summary>
    public MonsterRow[] GetMonstersByChapter(int chapter)
    {
        return Monsters?.Where(m => m.chapter == chapter).ToArray();
    }

    /// <summary>특정 챕터의 보스 목록</summary>
    public MonsterRow[] GetBossesByChapter(int chapter)
    {
        return Monsters?.Where(m => m.chapter == chapter && m.is_boss).ToArray();
    }

    /// <summary>특정 맵의 스테이지 목록 (stage_no 순)</summary>
    public StageRow[] GetStagesByMap(string mapId)
    {
        return Stages?.Where(s => s.map_id == mapId).OrderBy(s => s.stage_no).ToArray();
    }

    /// <summary>특정 맵 + 스테이지 번호의 스테이지 조회</summary>
    public StageRow GetStage(string mapId, int stageNo)
    {
        return Stages?.FirstOrDefault(s => s.map_id == mapId && s.stage_no == stageNo);
    }

    public StageRow GetStageById(string stageId) => _stageDict?.GetValueOrDefault(stageId);

    /// <summary>특정 그룹의 Warning Wave 전체 반환</summary>
    public WarningWaveRow[] GetWarningWavesByGroup(string groupId)
    {
        return WarningWaves?.Where(w => w.ww_group_id == groupId).OrderBy(w => w.wave_no).ToArray();
    }
}
