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
    [Key(3)] public float base_damage;
    [Key(4)] public string effect_desc;
    [Key(5)] public string aim_type;
    [Key(6)] public string weapon_type;
    [Key(7)] public float cooldown;
    [Key(8)] public float duration;
    [Key(9)] public int max_level;
    [Key(10)] public int drop_weight;
    [Key(11)] public string icon_key;
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
    [Key(0)] public string wave_id;
    [Key(1)] public string wave_group_id;
    [Key(2)] public int time_min;
    [Key(3)] public string phase_name;
    [Key(4)] public string mon_id;
    [Key(5)] public int spawn_per_30s;
    [Key(6)] public int max_enemies;
    [Key(7)] public float difficulty_scale;
    [Key(8)] public string note;
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
