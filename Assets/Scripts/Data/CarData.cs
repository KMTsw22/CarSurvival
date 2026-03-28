using UnityEngine;

[CreateAssetMenu(menuName = "CarSurvivor/CarData")]
public class CarData : ScriptableObject
{
    public string carName;
    public CarType carType;
    public Sprite carSprite;

    [Header("Base Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 5f;
    public float attackCooldownMultiplier = 1f;
    public float defenseMultiplier = 1f;
    public float collisionDamageReflect = 0f;

    [Header("Unlock")]
    public int unlockCost = 0;
    public bool unlockedByDefault = false;
}

public enum CarType
{
    SportsCar,
    SUV,
    Truck
}
