using UnityEngine;

[CreateAssetMenu(menuName = "CarSurvivor/PartsData")]
public class PartsData : ScriptableObject
{
    public string itemId;
    public string partName;
    public string description;
    public Sprite icon;
    public ItemCategory category;

    [Header("Effects")]
    public float speedBonus = 0f;
    public float attackSpeedBonus = 0f;
    public float damageBonus = 0f;
    public float healthBonus = 0f;
    public float defenseBonus = 0f;

    [Header("Weapon")]
    public WeaponType weaponType = WeaponType.None;
    public AimType aimType = AimType.None;
    public float damage = 0f;
    public float cooldown = 0f;
    public float duration = 0f;
    public float etcValue1 = 0f;
    public float etcValue2 = 0f;
    public float etcValue3 = 0f;
    public float etcValue4 = 0f;
    public float etcValue5 = 0f;

    [Header("Level")]
    public int maxLevel = 5;
    public int dropWeight = 0;
}

public enum ItemCategory
{
    MainWeapon,
    SubWeapon,
    SpellBook
}

public enum WeaponType
{
    None,
    MachineGun,
    OilSlick,
    SawBlade
}

public enum AimType
{
    None,
    Manual,
    Auto
}
