using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public float spawnInterval = 2f;
    public float minSpawnDistance = 12f;
    public float maxSpawnDistance = 15f;
    public int maxEnemies = 50;

    [Header("Wave Settings")]
    public float difficultyRampTime = 60f; // Seconds per difficulty increase
    public float spawnRateIncrease = 0.1f;
    public float enemyHealthIncrease = 5f;

    private Transform player;
    private float spawnTimer;
    private float elapsedTime;
    private int currentDifficulty = 0;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
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
        // Spawn at random position around player, outside screen
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 spawnPos = player.position + new Vector3(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance,
            0f);

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.SetActive(true);

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
}
