using UnityEngine;

[CreateAssetMenu(menuName = "CarSurvivor/MonsterData")]
public class MonsterData : ScriptableObject
{
    public string monId;
    public string monsterName;
    public bool isBoss;
    public Sprite sprite;
    public float health = 20f;
    public float moveSpeed = 1.5f;
    public float contactDamage = 10f;
    public int expDrop = 2;
    public int goldDrop = 5;
    public float scale = 0.375f;
    public int spawnWeight = 1;
    public string specialAbility = "None";
    public float bounceSpeed = 12f;
    public float bounceHeight = 0.25f;
    public float bounceSquash = 0.15f;
    public Color tintColor = Color.white;
}
