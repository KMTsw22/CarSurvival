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
    public LevelRow[] Levels { get; private set; }
    public MapRow[] Maps { get; private set; }

    // Lookup dictionaries
    private Dictionary<string, CurrencyRow> _currencyDict;
    private Dictionary<string, CarRow> _carDict;
    private Dictionary<string, WeaponRow> _weaponDict;
    private Dictionary<string, SpellBookRow> _spellBookDict;
    private Dictionary<string, MonsterRow> _monsterDict;
    private Dictionary<string, MonsterDropRow> _dropByMonDict;
    private Dictionary<int, LevelRow> _levelDict;
    private Dictionary<string, MapRow> _mapDict;

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
        Levels = Load<LevelRow[]>("Tables/TB_Level");
        Maps = Load<MapRow[]>("Tables/TB_Map");

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
    }

    // ─── 조회 API ───

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

    /// <summary>특정 그룹 + 시간대(분)의 웨이브 행 목록 반환</summary>
    public WaveRow[] GetWavesByGroupAndMinute(string groupId, int minute)
    {
        return Waves?.Where(w => w.wave_group_id == groupId && w.time_min == minute).ToArray();
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
}
