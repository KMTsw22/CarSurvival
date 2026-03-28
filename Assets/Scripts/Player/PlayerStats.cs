using UnityEngine;
using System;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 5f;
    public float attackSpeed = 1f;
    public float damage = 10f;
    public float defense = 0f;

    [Header("Fuel")]
    public float maxFuel = 600f; // 10 minutes in seconds
    public float currentFuel;

    [Header("Experience")]
    public int currentExp = 0;
    public int level = 1;
    public int expToNextLevel = 10;
    public float expGrowthRate = 1.2f;

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

    public void InitFromCarData(CarData carData)
    {
        maxHealth = carData.maxHealth;
        moveSpeed = carData.moveSpeed;
        attackSpeed *= carData.attackCooldownMultiplier;
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
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * expGrowthRate);
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

        moveSpeed = 5f * (1f + bonusSpeed / 100f);
        attackSpeed = 1f * (1f + bonusAttackSpeed / 100f);
        damage = 10f * (1f + bonusDamage / 100f);
        maxHealth = 100f * (1f + bonusHealth / 100f);
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
