using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();
    public List<MonsterData> monsterDataList = new List<MonsterData>();
    public float spawnInterval = 2f;
    public float minSpawnDistance = 12f;
    public float maxSpawnDistance = 15f;
    public int maxEnemies = 50;

    [Header("Wave Settings")]
    public float difficultyRampTime = 60f;
    public float spawnRateIncrease = 0.1f;
    public float enemyHealthIncrease = 5f;

    // Legacy single prefab support
    public GameObject enemyPrefab;

    private Transform player;
    private float spawnTimer;
    private float elapsedTime;
    private int currentDifficulty = 0;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Legacy: if only single prefab was set, add it to the list
        if (enemyPrefabs.Count == 0 && enemyPrefab != null)
        {
            enemyPrefabs.Add(enemyPrefab);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (player == null) return;

        elapsedTime += Time.deltaTime;

        // Increase difficulty over time
        int newDifficulty = Mathf.FloorToInt(elapsedTime / difficultyRampTime);
        if (newDifficulty > currentDifficulty)
        {
            currentDifficulty = newDifficulty;
            spawnInterval = Mathf.Max(0.3f, spawnInterval - spawnRateIncrease);
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
            spawnTimer = spawnInterval;
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Count == 0) return;

        // Random monster type selection
        int index = Random.Range(0, enemyPrefabs.Count);
        GameObject prefab = enemyPrefabs[index];

        // Spawn at random position around player, outside screen
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f);

        GameObject enemy = Instantiate(prefab);
        enemy.SetActive(true);
        enemy.transform.position = spawnPos;

        // Apply MonsterData if available
        if (index < monsterDataList.Count && monsterDataList[index] != null)
        {
            ApplyMonsterData(enemy, monsterDataList[index]);
        }

        // Scale difficulty
        EnemyHealth eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.maxHealth += currentDifficulty * enemyHealthIncrease;
            eh.currentHealth = eh.maxHealth;
        }

        EnemyAI ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed += currentDifficulty * 0.3f;
        }
    }

    private void ApplyMonsterData(GameObject enemy, MonsterData data)
    {
        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null && data.sprite != null)
        {
            sr.sprite = data.sprite;
            if (data.tintColor != Color.white)
                sr.color = data.tintColor;
        }

        enemy.transform.localScale = new Vector3(data.scale, data.scale, 1f);

        var eh = enemy.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.maxHealth = data.health;
            eh.currentHealth = data.health;
            eh.expDrop = data.expDrop;
            eh.goldDrop = data.goldDrop;
        }

        var ai = enemy.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.moveSpeed = data.moveSpeed;
            ai.contactDamage = data.contactDamage;
        }
    }
}