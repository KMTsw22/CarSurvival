using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerStats] 중복 인스턴스 감지! 기존={Instance.GetInstanceID()}, 새로운={GetInstanceID()}. 새 것 파괴.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    [Header("Base Stats (TB_Car 기반)")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 5f;
    public float attackSpeed = 1f;
    public float damage = 10f;
    public float defense = 0f;

    // TB_Car에서 로드한 기본 스탯 (RecalculateStats에서 사용)
    [HideInInspector] public float baseMaxHealth;
    [HideInInspector] public float baseMoveSpeed;
    [HideInInspector] public float baseAtkSpeed;
    [HideInInspector] public float baseDamage;
    [HideInInspector] public string currentCarId = "CAR_001";

    [Header("Experience")]
    public int currentExp = 0;
    public int level = 1;
    public int expToNextLevel = 30; // TB_Level 레벨1 기준
    [HideInInspector] public int accumulatedExp = 0; // 누적 경험치

    [Header("Currency")]
    public int gold = 0;
    public int scrap = 0;

    [Header("Run Stats")]
    public int enemiesKilled = 0;
    public float survivalTime = 0f;

    [Header("Cheat")]
    public bool isInvincible = false;

    public List<OwnedPart> equippedParts = new List<OwnedPart>();

    public event Action<float, float> OnHealthChanged;
    public event Action<int, int, int> OnExpChanged; // current, toNext, level
    public event Action OnLevelUp;
    public event Action OnPlayerDeath;
    public event Action OnPartChanged;

    private void Start()
    {
        var tm = TableManager.Instance;

        // TB_Car에서 기본 스탯 로드
        var carData = tm.GetCar(currentCarId);
        if (carData != null)
        {
            baseMaxHealth = carData.base_hp;
            baseMoveSpeed = carData.base_speed * 1f;
            baseAtkSpeed = carData.base_atk_speed;
            baseDamage = carData.base_damage;

            maxHealth = baseMaxHealth;
            moveSpeed = baseMoveSpeed;
            attackSpeed = baseAtkSpeed;
            damage = baseDamage;
        }
        else
        {
            // 테이블 없으면 기본값 사용
            baseMaxHealth = maxHealth;
            baseMoveSpeed = moveSpeed;
            baseAtkSpeed = attackSpeed;
            baseDamage = damage;
        }

        // TB_Level 테이블에서 레벨1 경험치 로드
        var levelData = tm.GetLevel(1);
        if (levelData != null)
            expToNextLevel = levelData.required_exp_gap;

        currentHealth = maxHealth;

        // 기본 무기(MachineGun)를 equippedParts에 등록
        if (GameManager.Instance != null && GameManager.Instance.partsDatabase != null)
        {
            var defaultWeapon = GameManager.Instance.partsDatabase.allParts
                .Find(p => p.weaponType == WeaponType.MachineGun);
            if (defaultWeapon != null && equippedParts.Find(p => p.data == defaultWeapon) == null)
            {
                equippedParts.Add(new OwnedPart { data = defaultWeapon, level = 1 });
                OnPartChanged?.Invoke();
            }
        }

        NotifyAll();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        survivalTime += Time.deltaTime;

        // Health regen from parts (1초마다 회복)
        float regen = GetTotalHealthRegen();
        if (regen > 0f && currentHealth < maxHealth)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= 1f)
            {
                regenTimer -= 1f;
                currentHealth = Mathf.Min(maxHealth, currentHealth + regen);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
        else
        {
            regenTimer = 0f;
        }
    }

    /// <summary>
    /// 테이블 ID로 차량 변경. Garage에서 호출.
    /// </summary>
    public void InitFromCarTable(string carId)
    {
        currentCarId = carId;
        var carData = TableManager.Instance.GetCar(carId);
        if (carData == null) return;

        baseMaxHealth = carData.base_hp;
        baseMoveSpeed = carData.base_speed * 0.5f;
        baseAtkSpeed = carData.base_atk_speed;
        baseDamage = carData.base_damage;

        maxHealth = baseMaxHealth;
        moveSpeed = baseMoveSpeed;
        attackSpeed = baseAtkSpeed;
        damage = baseDamage;
        currentHealth = maxHealth;

        RecalculateStats();
        NotifyAll();
    }

    /// <summary>
    /// 기존 ScriptableObject 방식 호환용 (레거시)
    /// </summary>
    public void InitFromCarData(CarData carData)
    {
        baseMaxHealth = carData.maxHealth;
        baseMoveSpeed = carData.moveSpeed;
        baseAtkSpeed = carData.attackCooldownMultiplier;
        baseDamage = damage;

        maxHealth = baseMaxHealth;
        moveSpeed = baseMoveSpeed;
        attackSpeed *= baseAtkSpeed;
        defense = carData.defenseMultiplier;
        currentHealth = maxHealth;
        NotifyAll();
    }

    public void TakeDamage(float amount)
    {
        if (isInvincible) return;

        float reduced = amount * (1f - defense);
        currentHealth -= reduced;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnPlayerDeath?.Invoke();
            GameManager.Instance.OnPlayerDeath();
        }
    }

    private int pendingLevelUps = 0;

    public void AddExperience(int amount)
    {
        currentExp += amount;
        OnExpChanged?.Invoke(currentExp, expToNextLevel, level);

        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel;
            level++;

            // TB_Level 테이블에서 다음 레벨 경험치 조회
            var nextLevel = TableManager.Instance.GetLevel(level);
            if (nextLevel != null)
                expToNextLevel = nextLevel.required_exp_gap;

            pendingLevelUps++;
        }

        if (pendingLevelUps > 0)
        {
            pendingLevelUps--;
            OnLevelUp?.Invoke();
            GameManager.Instance.SetState(GameManager.GameState.LevelUp);
        }

        OnExpChanged?.Invoke(currentExp, expToNextLevel, level);
    }

    public void ProcessPendingLevelUp()
    {
        if (pendingLevelUps > 0)
        {
            pendingLevelUps--;
            OnLevelUp?.Invoke();
            GameManager.Instance.SetState(GameManager.GameState.LevelUp);
        }
    }

    public const int MaxWeaponSlots = 4;
    public const int MaxSpellBookSlots = 4;

    public bool IsWeaponSlotFull()
    {
        int count = equippedParts.FindAll(p =>
            p.data.category == ItemCategory.MainWeapon || p.data.category == ItemCategory.SubWeapon).Count;
        return count >= MaxWeaponSlots;
    }

    public bool IsSpellBookSlotFull()
    {
        int count = equippedParts.FindAll(p => p.data.category == ItemCategory.SpellBook).Count;
        return count >= MaxSpellBookSlots;
    }

    public void ApplyPart(PartsData part)
    {
        var existing = equippedParts.Find(p => p.data == part);
        if (existing != null)
        {
            if (existing.level < part.maxLevel)
                existing.level++;
        }
        else
        {
            bool isWeapon = part.category == ItemCategory.MainWeapon || part.category == ItemCategory.SubWeapon;
            if (isWeapon && IsWeaponSlotFull()) return;
            if (part.category == ItemCategory.SpellBook && IsSpellBookSlotFull()) return;

            equippedParts.Add(new OwnedPart { data = part, level = 1 });
        }

        RecalculateStats();
        OnPartChanged?.Invoke();
    }

    // 마법서 보너스 (외부 시스템에서 참조)
    [HideInInspector] public float expBonusPercent = 0f;
    [HideInInspector] public float magnetBonusPercent = 0f;

    private float healthRegenPercent = 0f;
    private float regenTimer = 0f;

    private void RecalculateStats()
    {
        float bonusSpeed = 0f;
        float bonusAttackSpeed = 0f;
        float bonusDamage = 0f; 
        float bonusHealth = 0f;
        float bonusDefense = 0f;
        float bonusHealthRegen = 0f;
        float bonusExp = 0f;
        float bonusMagnet = 0f;

        foreach (var part in equippedParts)
        {
            // 무기는 자체 데미지를 가짐 — 스탯 보너스에 기여하지 않음
            if (part.data.category == ItemCategory.MainWeapon
                || part.data.category == ItemCategory.SubWeapon)
                continue;

            // 마법서만 스탯 보너스 적용
            float multiplier = part.level;
            bonusSpeed += part.data.speedBonus * multiplier;
            bonusAttackSpeed += part.data.attackSpeedBonus * multiplier;
            bonusDamage += part.data.damageBonus * multiplier;
            bonusHealth += part.data.healthBonus * multiplier;
            bonusDefense += part.data.defenseBonus * multiplier;
            bonusHealthRegen += part.data.healthRegenBonus > 0 ? part.level : 0;
            bonusExp += part.data.expBonus * multiplier;
            bonusMagnet += part.data.magnetBonus * multiplier;
        }

        moveSpeed = baseMoveSpeed * (1f + bonusSpeed / 100f);
        attackSpeed = baseAtkSpeed * (1f + bonusAttackSpeed / 100f);
        damage = baseDamage * (1f + bonusDamage / 100f);
        float prevMax = maxHealth;
        maxHealth = baseMaxHealth * (1f + bonusHealth / 100f);
        // 최대 체력 증가분만큼 현재 체력도 같이 올려줌
        if (maxHealth > prevMax)
            currentHealth += maxHealth - prevMax;
        defense = Mathf.Clamp01(bonusDefense / 100f);
        healthRegenPercent = bonusHealthRegen;
        expBonusPercent = bonusExp;
        magnetBonusPercent = bonusMagnet;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private float GetTotalHealthRegen()
    {
        // 1초당 healthRegenPercent 고정값 회복 (레벨 * base_value)
        return healthRegenPercent;
    }

    private void NotifyAll()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnExpChanged?.Invoke(currentExp, expToNextLevel, level);
    }
}

[System.Serializable]
public class OwnedPart
{
    public PartsData data;
    public int level;
}
