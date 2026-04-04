using MessagePack;

// ============================================================
// TB_Currency
// ============================================================
[MessagePackObject]
public class CurrencyRow
{
    [Key(0)] public string currency_id;
    [Key(1)] public string currency_name;
    [Key(2)] public string currency_desc;
    [Key(3)] public string icon_key;
}

// ============================================================
// TB_Car
// ============================================================
[MessagePackObject]
public class CarRow
{
    [Key(0)] public string car_id;
    [Key(1)] public string car_name;
    [Key(2)] public string car_type;
    [Key(3)] public float base_hp;
    [Key(4)] public float base_speed;
    [Key(5)] public float base_atk_speed;
    [Key(6)] public float base_damage;
    [Key(7)] public float collision_damage;
    [Key(8)] public string passive_type;
    [Key(9)] public float passive_value;
    [Key(10)] public string passive_desc;
    [Key(11)] public int unlock_cost;
    [Key(12)] public string unlock_currency_id;
    [Key(13)] public bool unlocked_by_default;
    [Key(14)] public string sprite_key;
    [Key(15)] public float flame_offset;      // 화염방사기 오프셋 거리 (최소 3)
}

// ============================================================
// TB_Weapon (주무기 + 보조무기)
// ============================================================
[MessagePackObject]
public class WeaponRow
{
    [Key(0)] public string weapon_id;
    [Key(1)] public string weapon_name;
    [Key(2)] public string weapon_category;
    [Key(3)] public float damage;
    [Key(4)] public string effect_desc;
    [Key(5)] public string aim_type;
    [Key(6)] public string weapon_type;
    [Key(7)] public float cooldown;
    [Key(8)] public float duration;
    [Key(9)] public int max_level;
    [Key(10)] public int drop_weight;
    [Key(11)] public string icon_key;
    [Key(12)] public float etc_value1;
    [Key(13)] public float etc_value2;
    [Key(14)] public float etc_value3;
    [Key(15)] public float etc_value4;
    [Key(16)] public float etc_value5;
    [Key(17)] public float damage_per_level;
}

// ============================================================
// TB_SpellBook (마법서 — 패시브 버프)
// ============================================================
[MessagePackObject]
public class SpellBookRow
{
    [Key(0)] public string book_id;
    [Key(1)] public string book_name;
    [Key(2)] public string effect_type;
    [Key(3)] public float base_value;
    [Key(4)] public string effect_desc;
    [Key(5)] public int max_level;
    [Key(6)] public bool stackable;
    [Key(7)] public int drop_weight;
    [Key(8)] public string icon_key;
}

// ============================================================
// TB_Part (빈 테이블 — 향후 사용)
// ============================================================
[MessagePackObject]
public class PartRow
{
    [Key(0)] public string part_id;
    [Key(1)] public string part_name;
    [Key(2)] public string part_desc;
    [Key(3)] public string effect_type;
    [Key(4)] public float effect_value;
    [Key(5)] public int max_level;
    [Key(6)] public string icon_key;
}

// ============================================================
// TB_Monster
// ============================================================
[MessagePackObject]
public class MonsterRow
{
    [Key(0)] public string mon_id;
    [Key(1)] public string mon_name;
    [Key(2)] public bool is_boss;
    [Key(3)] public float base_hp;
    [Key(4)] public float base_speed;
    [Key(5)] public float contact_damage;
    [Key(6)] public float scale;
    [Key(7)] public int chapter;
    [Key(8)] public int spawn_weight;
    [Key(9)] public string special_ability;
    [Key(10)] public string sprite_key;
    [Key(11)] public float bounce_speed;
    [Key(12)] public float bounce_height;
    [Key(13)] public float bounce_squash;
}

// ============================================================
// TB_MonsterDrop
// ============================================================
[MessagePackObject]
public class MonsterDropRow
{
    [Key(0)] public string drop_id;
    [Key(1)] public string mon_id;
    [Key(2)] public int exp_amount;
    [Key(3)] public int gold_amount;
    [Key(4)] public int screw_amount;
}

// ============================================================
// TB_Wave
// ============================================================
[MessagePackObject]
public class WaveRow
{
    [Key(0)] public string wave_group_id;
    [Key(1)] public int wave_no;
    [Key(2)] public string mon_id;
    [Key(3)] public int spawn_count;
    [Key(4)] public float spawn_interval;
    [Key(5)] public int max_enemies;
    [Key(6)] public float difficulty_scale;
    [Key(7)] public string note;
}

// ============================================================
// TB_Level
// ============================================================
[MessagePackObject]
public class LevelRow
{
    [Key(0)] public int level;
    [Key(1)] public int required_exp;
    [Key(2)] public int required_exp_gap;
    [Key(3)] public float difficulty_multiplier;
    [Key(4)] public string note;
}

// ============================================================
// TB_LangLevelUpSelect_name (이름 언어 테이블)
// ============================================================
[MessagePackObject]
public class LangNameRow
{
    [Key(0)] public string item_id;
    [Key(1)] public string ko;
    [Key(2)] public string en;
}

// ============================================================
// TB_LangLevelUpSelect_des (설명 언어 테이블)
// ============================================================
[MessagePackObject]
public class LangDesRow
{
    [Key(0)] public string item_id;
    [Key(1)] public string ko;
    [Key(2)] public string en;
}

// ============================================================
// TB_WarningWave (포탈 생성 후 Warning Wave 스폰 설정)
// ============================================================
[MessagePackObject]
public class WarningWaveRow
{
    [Key(0)] public string ww_group_id;      // Warning Wave 그룹 ID (예: WW_CH1_1)
    [Key(1)] public int wave_no;             // 웨이브 번호 (시간순)
    [Key(2)] public string mon_id;           // 몬스터 ID
    [Key(3)] public int spawn_count;         // 한번에 소환 수
    [Key(4)] public float spawn_interval;    // 소환 간격 (초)
    [Key(5)] public int max_enemies;         // 최대 동시 존재 수
    [Key(6)] public float difficulty_scale;  // 난이도 배율
    [Key(7)] public float start_time;        // 이 웨이브 시작 시간 (초)
    [Key(8)] public string note;
}

// ============================================================
// TB_Stage (스테이지 별 보스 소환 열쇠 정보)
// ============================================================
[MessagePackObject]
public class StageRow
{
    [Key(0)] public string stage_id;        // 예: STG_CH1_1
    [Key(1)] public string map_id;          // 소속 맵 (예: MAP_CH1)
    [Key(2)] public int stage_no;           // 스테이지 번호
    [Key(3)] public string boss_mon_id;     // 소환할 보스 몬스터 ID
    [Key(4)] public string key_item_id;     // 필요한 열쇠 아이템 ID
    [Key(5)] public int key_item_count;     // 필요한 열쇠 개수
    [Key(6)] public string key_icon;        // 열쇠 아이콘 스프라이트 키
    [Key(7)] public string key_name;        // 열쇠 아이템 표시 이름
    [Key(8)] public float arena_radius;     // 보스 아레나 반경
    [Key(9)] public string ww_group_id;     // Warning Wave 그룹 ID (예: WW_CH1_1)
}

// ============================================================
// TB_Map
// ============================================================
[MessagePackObject]
public class MapRow
{
    [Key(0)] public string map_id;
    [Key(1)] public string map_name;
    [Key(2)] public string map_desc;
    [Key(3)] public int chapter;
    [Key(4)] public string wave_group_id;
    [Key(5)] public string bg_sprite_key;
    [Key(6)] public int grid_size;
    [Key(7)] public string special_effect;
    [Key(8)] public bool unlocked_by_default;
}
