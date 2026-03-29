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
    [Key(3)] public int daily_cap;
    [Key(4)] public string icon_key;
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
// TB_PartGrade
// ============================================================
[MessagePackObject]
public class PartGradeRow
{
    [Key(0)] public string grade_id;
    [Key(1)] public string grade_name;
    [Key(2)] public string grade_enum;
    [Key(3)] public float value_multiplier;
    [Key(4)] public string color_hex;
    [Key(5)] public int sort_order;
}

// ============================================================
// TB_Part
// ============================================================
[MessagePackObject]
public class PartRow
{
    [Key(0)] public string part_id;
    [Key(1)] public string part_name;
    [Key(2)] public string category;
    [Key(3)] public float base_value;
    [Key(4)] public string effect_type;
    [Key(5)] public string effect_desc;
    [Key(6)] public bool has_active;
    [Key(7)] public string weapon_type;
    [Key(8)] public float cooldown;
    [Key(9)] public float duration;
    [Key(10)] public int max_level;
    [Key(11)] public bool stackable;
    [Key(12)] public int drop_weight;
    [Key(13)] public bool is_evolution_result;
    [Key(14)] public string icon_key;
}

// ============================================================
// TB_Evolution
// ============================================================
[MessagePackObject]
public class EvolutionRow
{
    [Key(0)] public string evo_id;
    [Key(1)] public string evo_name;
    [Key(2)] public string material_a_id;
    [Key(3)] public int material_a_level;
    [Key(4)] public string material_b_id;
    [Key(5)] public int material_b_level;
    [Key(6)] public string result_part_id;
    [Key(7)] public string visual_desc;
    [Key(8)] public string priority;
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
    [Key(7)] public int spawn_start_min;
    [Key(8)] public int spawn_weight;
    [Key(9)] public string special_ability;
    [Key(10)] public string tint_color;
    [Key(11)] public string sprite_key;
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
    [Key(4)] public string special_drop_id;
    [Key(5)] public float special_drop_rate;
}

// ============================================================
// TB_Wave
// ============================================================
[MessagePackObject]
public class WaveRow
{
    [Key(0)] public string wave_id;
    [Key(1)] public int time_min;
    [Key(2)] public string phase_name;
    [Key(3)] public string mon_id;
    [Key(4)] public int spawn_per_30s;
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
    [Key(4)] public int grade_common_pct;
    [Key(5)] public int grade_rare_pct;
    [Key(6)] public int grade_epic_pct;
    [Key(7)] public int grade_legendary_pct;
    [Key(8)] public string note;
}

// ============================================================
// TB_Reward
// ============================================================
[MessagePackObject]
public class RewardRow
{
    [Key(0)] public string reward_id;
    [Key(1)] public string reward_trigger;
    [Key(2)] public string currency_id;
    [Key(3)] public int amount;
    [Key(4)] public float bonus_multiplier;
    [Key(5)] public string note;
}

// ============================================================
// TB_Shop
// ============================================================
[MessagePackObject]
public class ShopRow
{
    [Key(0)] public string shop_id;
    [Key(1)] public string shop_name;
    [Key(2)] public string shop_type;
    [Key(3)] public string price_type;
    [Key(4)] public int price_amount;
    [Key(5)] public string price_currency_id;
    [Key(6)] public string reward_type;
    [Key(7)] public string reward_target_id;
    [Key(8)] public int reward_amount;
    [Key(9)] public bool is_one_time;
    [Key(10)] public string release_phase;
    [Key(11)] public string note;
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
    [Key(3)] public string bg_sprite_key;
    [Key(4)] public float tile_size;
    [Key(5)] public int grid_size;
    [Key(6)] public string special_effect;
    [Key(7)] public int unlock_cost;
    [Key(8)] public string unlock_currency_id;
    [Key(9)] public bool unlocked_by_default;
    [Key(10)] public string release_phase;
}
