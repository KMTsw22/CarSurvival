using UnityEngine;

[CreateAssetMenu(menuName = "CarSurvivor/PartsData")]
public class PartsData : ScriptableObject
{
    public string partName;
    public string description;
    public Sprite icon;
    public PartsCategory category;
    public PartsGrade grade;

    [Header("Effects")]
    public float speedBonus = 0f;
    public float attackSpeedBonus = 0f;
    public float damageBonus = 0f;
    public float healthBonus = 0f;
    public float defenseBonus = 0f;
    public float healthRegenPerSecond = 0f;

    [Header("Special")]
    public bool hasActiveAbility = false;
    public float abilityCooldown = 0f;
    public float abilityDuration = 0f;
    public WeaponType weaponType = WeaponType.None;

    [Header("Level & Evolution")]
    public int maxLevel = 3;
    public PartsData evolutionPartner;
    public PartsData evolutionResult;
}

public enum PartsCategory
{
    Engine,
    Weapon,
    Defense,
    Special
}

public enum PartsGrade
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum WeaponType
{
    None,
    MachineGun,
    MissileLauncher,
    EMPPulse,
    OilSlick,
    MineDrop
}
