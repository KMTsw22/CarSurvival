using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
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

    [Header("Fuel")]
    public float maxFuel = 600f; // 10 minutes in seconds
    public float currentFuel;

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

    public List<OwnedPart> equippedParts = new List<OwnedPart>();

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnFuelChanged;
    public event Action<int, int, int> OnExpChanged; // current, toNext, level
    public event Action OnLevelUp;
    public event Action OnPlayerDeath;

    private void Start()
    {
        var tm = TableManager.Instance;

        // TB_Car에서 기본 스탯 로드
        var carData = tm.GetCar(currentCarId);
        if (carData != null)
        {
            baseMaxHealth = carData.base_hp;
            baseMoveSpeed = carData.base_speed;
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
        currentFuel = maxFuel;
        NotifyAll();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Fuel consumption
        currentFuel -= Time.deltaTime;
        survivalTime += Time.deltaTime;
        OnFuelChanged?.Invoke(currentFuel, maxFuel);

        if (currentFuel <= 0f)
        {
            currentFuel = 0f;
            GameManager.Instance.OnRunComplete();
        }

        // Health regen from parts
        float regen = GetTotalHealthRegen();
        if (regen > 0f && currentHealth < maxHealth)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + regen * maxHealth * Time.deltaTime);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
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
        baseMoveSpeed = carData.base_speed;
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

    public void ApplyPart(PartsData part)
    {
        // Check if we already have this part
        var existing = equippedParts.Find(p => p.data == part);
        if (existing != null)
        {
            existing.level++;
            // Check for evolution
            if (existing.level >= part.maxLevel && part.evolutionResult != null)
            {
                var partner = equippedParts.Find(p => p.data == part.evolutionPartner);
                if (partner != null)
                {
                    equippedParts.Remove(partner);
                    existing.data = part.evolutionResult;
                    existing.level = 1;
                }
            }
        }
        else
        {
            equippedParts.Add(new OwnedPart { data = part, level = 1 });
        }

        RecalculateStats();
    }

    private void RecalculateStats()
    {
        float bonusSpeed = 0f;
        float bonusAttackSpeed = 0f;
        float bonusDamage = 0f;
        float bonusHealth = 0f;
        float bonusDefense = 0f;

        foreach (var part in equippedParts)
        {
            float multiplier = part.level;
            bonusSpeed += part.data.speedBonus * multiplier;
            bonusAttackSpeed += part.data.attackSpeedBonus * multiplier;
            bonusDamage += part.data.damageBonus * multiplier;
            bonusHealth += part.data.healthBonus * multiplier;
            bonusDefense += part.data.defenseBonus * multiplier;
        }

        moveSpeed = baseMoveSpeed * (1f + bonusSpeed / 100f);
        attackSpeed = baseAtkSpeed * (1f + bonusAttackSpeed / 100f);
        damage = baseDamage * (1f + bonusDamage / 100f);
        maxHealth = baseMaxHealth * (1f + bonusHealth / 100f);
        defense = Mathf.Clamp01(bonusDefense / 100f);
    }

    private float GetTotalHealthRegen()
    {
        float regen = 0f;
        foreach (var part in equippedParts)
        {
            regen += part.data.healthRegenPerSecond * part.level;
        }
        return regen;
    }

    private void NotifyAll()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnFuelChanged?.Invoke(currentFuel, maxFuel);
        OnExpChanged?.Invoke(currentExp, expToNextLevel, level);
    }
}

[System.Serializable]
public class OwnedPart
{
    public PartsData data;
    public int level;
}
