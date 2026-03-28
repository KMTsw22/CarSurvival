using UnityEngine;

public class AutoAttack : MonoBehaviour
{
    [Header("Machine Gun (Default Weapon)")]
    public GameObject bulletPrefab;
    public float baseCooldown = 0.3f;
    public float bulletSpeed = 15f;
    public float bulletLifetime = 2f;

    [Header("Weapon Slots")]
    public Transform firePoint;

    private PlayerStats stats;
    private CarController carController;
    private float cooldownTimer;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        carController = GetComponent<CarController>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            FireMachineGun();

            // Check for additional weapons from parts
            foreach (var part in stats.equippedParts)
            {
                if (part.data.weaponType != WeaponType.None)
                {
                    FireWeapon(part.data.weaponType, part.level);
                }
            }

            cooldownTimer = baseCooldown / stats.attackSpeed;
        }
    }

    private void FireMachineGun()
    {
        if (bulletPrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + transform.up * 0.5f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, transform.rotation);
        bullet.SetActive(true);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            b.Initialize(transform.up, bulletSpeed, stats.damage, bulletLifetime);
        }
    }

    private void FireWeapon(WeaponType type, int level)
    {
        switch (type)
        {
            case WeaponType.MissileLauncher:
                FireMissile(level);
                break;
            case WeaponType.EMPPulse:
                FireEMP(level);
                break;
            case WeaponType.OilSlick:
                DropOil(level);
                break;
            case WeaponType.MineDrop:
                DropMine(level);
                break;
        }
    }

    private void FireMissile(int level)
    {
        // Find nearest enemy
        GameObject nearest = FindNearestEnemy();
        if (nearest == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.SetActive(true);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            Vector2 dir = (nearest.transform.position - transform.position).normalized;
            b.Initialize(dir, bulletSpeed * 0.8f, stats.damage * (1.5f + level * 0.3f), bulletLifetime);
            b.SetHoming(nearest.transform);
            bullet.transform.localScale = Vector3.one * 1.5f;
        }
    }

    private void FireEMP(int level)
    {
        float radius = 10f + level;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth eh = hit.GetComponent<EnemyHealth>();
                if (eh != null)
                {
                    eh.TakeDamage(stats.damage * 0.5f * level);
                    EnemyAI ai = hit.GetComponent<EnemyAI>();
                    if (ai != null) ai.Stun(1f);
                }
            }
        }
    }

    private void DropOil(int level)
    {
        GameObject oil = new GameObject("OilSlick");
        oil.transform.position = transform.position - transform.up * 1f;
        var col = oil.AddComponent<CircleCollider2D>();
        col.radius = 1f + level * 0.3f;
        col.isTrigger = true;
        oil.AddComponent<OilSlickEffect>().damage = stats.damage * 0.3f * level;
        oil.tag = "PlayerProjectile";

        var sr = oil.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        sr.sortingOrder = -1;

        Destroy(oil, 5f);
    }

    private void DropMine(int level)
    {
        GameObject mine = new GameObject("Mine");
        mine.transform.position = transform.position - transform.up * 1f;
        var col = mine.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;
        mine.AddComponent<MineEffect>().damage = stats.damage * 2f * level;
        mine.tag = "PlayerProjectile";

        var sr = mine.AddComponent<SpriteRenderer>();
        sr.color = Color.red;

        Destroy(mine, 10f);
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDist = float.MaxValue;

        foreach (var enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = enemy;
            }
        }

        return nearest;
    }
}
