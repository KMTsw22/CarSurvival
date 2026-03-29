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
    public PartGradeRow[] PartGrades { get; private set; }
    public PartRow[] Parts { get; private set; }
    public EvolutionRow[] Evolutions { get; private set; }
    public MonsterRow[] Monsters { get; private set; }
    public MonsterDropRow[] MonsterDrops { get; private set; }
    public WaveRow[] Waves { get; private set; }
    public LevelRow[] Levels { get; private set; }
    public RewardRow[] Rewards { get; private set; }
    public ShopRow[] Shops { get; private set; }
    public MapRow[] Maps { get; private set; }

    // Lookup dictionaries (ID → Row)
    private Dictionary<string, CurrencyRow> _currencyDict;
    private Dictionary<string, CarRow> _carDict;
    private Dictionary<string, PartGradeRow> _gradeDict;
    private Dictionary<string, PartRow> _partDict;
    private Dictionary<string, EvolutionRow> _evoDict;
    private Dictionary<string, MonsterRow> _monsterDict;
    private Dictionary<string, MonsterDropRow> _dropByMonDict;
    private Dictionary<int, LevelRow> _levelDict;
    private Dictionary<string, MapRow> _mapDict;

    public bool IsLoaded { get; private set; }

    public void LoadAll()
    {
        Currencies = Load<CurrencyRow[]>("Tables/TB_Currency");
        Cars = Load<CarRow[]>("Tables/TB_Car");
        PartGrades = Load<PartGradeRow[]>("Tables/TB_PartGrade");
        Parts = Load<PartRow[]>("Tables/TB_Part");
        Evolutions = Load<EvolutionRow[]>("Tables/TB_Evolution");
        Monsters = Load<MonsterRow[]>("Tables/TB_Monster");
        MonsterDrops = Load<MonsterDropRow[]>("Tables/TB_MonsterDrop");
        Waves = Load<WaveRow[]>("Tables/TB_Wave");
        Levels = Load<LevelRow[]>("Tables/TB_Level");
        Rewards = Load<RewardRow[]>("Tables/TB_Reward");
        Shops = Load<ShopRow[]>("Tables/TB_Shop");
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
            Debug.LogError($"[TableManager] Table not found: {path}");
            return default;
        }
        return MessagePackSerializer.Deserialize<T>(asset.bytes);
    }

    private void BuildDictionaries()
    {
        _currencyDict = Currencies?.ToDictionary(r => r.currency_id);
        _carDict = Cars?.ToDictionary(r => r.car_id);
        _gradeDict = PartGrades?.ToDictionary(r => r.grade_id);
        _partDict = Parts?.ToDictionary(r => r.part_id);
        _evoDict = Evolutions?.ToDictionary(r => r.evo_id);
        _monsterDict = Monsters?.ToDictionary(r => r.mon_id);
        _dropByMonDict = MonsterDrops?.ToDictionary(r => r.mon_id);
        _levelDict = Levels?.ToDictionary(r => r.level);
        _mapDict = Maps?.ToDictionary(r => r.map_id);
    }

    // ─── 조회 API ───

    public CurrencyRow GetCurrency(string id) => _currencyDict.GetValueOrDefault(id);
    public CarRow GetCar(string id) => _carDict.GetValueOrDefault(id);
    public PartGradeRow GetPartGrade(string id) => _gradeDict.GetValueOrDefault(id);
    public PartRow GetPart(string id) => _partDict.GetValueOrDefault(id);
    public EvolutionRow GetEvolution(string id) => _evoDict.GetValueOrDefault(id);
    public MapRow GetMap(string id) => _mapDict.GetValueOrDefault(id);
    public MonsterRow GetMonster(string id) => _monsterDict.GetValueOrDefault(id);
    public MonsterDropRow GetMonsterDrop(string monId) => _dropByMonDict.GetValueOrDefault(monId);
    public LevelRow GetLevel(int level) => _levelDict.GetValueOrDefault(level);

    /// <summary>특정 시간대(분)의 웨이브 행 목록 반환</summary>
    public WaveRow[] GetWavesByMinute(int minute)
    {
        return Waves?.Where(w => w.time_min == minute).ToArray();
    }

    /// <summary>특정 트리거의 보상 목록 반환</summary>
    public RewardRow[] GetRewardsByTrigger(string trigger)
    {
        return Rewards?.Where(r => r.reward_trigger == trigger).ToArray();
    }

    /// <summary>드랍 가능한 파츠 목록 (진화 파츠 제외)</summary>
    public PartRow[] GetDroppableParts()
    {
        return Parts?.Where(p => !p.is_evolution_result && p.drop_weight > 0).ToArray();
    }

    /// <summary>특정 파츠가 재료로 들어가는 진화 레시피 찾기</summary>
    public EvolutionRow FindEvolutionByMaterials(string partIdA, string partIdB)
    {
        return Evolutions?.FirstOrDefault(e =>
            (e.material_a_id == partIdA && e.material_b_id == partIdB) ||
            (e.material_a_id == partIdB && e.material_b_id == partIdA));
    }

    /// <summary>등급 enum 문자열로 배율 조회</summary>
    public float GetGradeMultiplier(string gradeEnum)
    {
        var grade = PartGrades?.FirstOrDefault(g => g.grade_enum == gradeEnum);
        return grade?.value_multiplier ?? 1f;
    }
}
