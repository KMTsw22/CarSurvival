using UnityEngine;
using System.Collections.Generic;

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
    private float cooldownTimer;
    private Dictionary<WeaponType, float> subWeaponTimers = new Dictionary<WeaponType, float>();

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // 주무기 (기관총) — 마우스 커서 방향으로 발사
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0f)
        {
            FireMachineGun();
            cooldownTimer = baseCooldown / stats.attackSpeed;
        }

        // 보조무기 — 각자 쿨타임으로 자동 발동
        foreach (var part in stats.equippedParts)
        {
            if (part.data.aimType != AimType.Auto) continue;
            if (part.data.weaponType == WeaponType.None) continue;

            var type = part.data.weaponType;
            if (!subWeaponTimers.ContainsKey(type))
                subWeaponTimers[type] = 0f;

            subWeaponTimers[type] -= Time.deltaTime;
            if (subWeaponTimers[type] <= 0f)
            {
                FireWeapon(type, part.level, part.data);
                float cd = part.data.cooldown > 0 ? part.data.cooldown : 1f;
                subWeaponTimers[type] = cd;
            }
        }
    }

    private Vector2 GetMouseDirection()
    {
        if (Camera.main == null) return transform.up;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector2 dir = (mouseWorld - transform.position).normalized;
        return dir.sqrMagnitude > 0.01f ? dir : (Vector2)transform.up;
    }

    private void FireMachineGun()
    {
        if (bulletPrefab == null) return;

        Vector2 aimDir = GetMouseDirection();
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg - 90f;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)aimDir * 0.5f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0, 0, angle));
        bullet.SetActive(true);
        Bullet b = bullet.GetComponent<Bullet>();
        if (b != null)
        {
            // 기관총 무기 데미지 = 기본 스탯 데미지 + 장착된 기관총 레벨 보너스
            float weaponDamage = stats.damage;
            var gunPart = stats.equippedParts.Find(p => p.data.weaponType == WeaponType.MachineGun);
            if (gunPart != null)
                weaponDamage += gunPart.data.damageBonus * gunPart.level;

            b.Initialize(aimDir, bulletSpeed, weaponDamage, bulletLifetime);
        }
    }

    private void FireWeapon(WeaponType type, int level, PartsData data)
    {
        switch (type)
        {
            case WeaponType.OilSlick:
                DropOil(level, data);
                break;
            case WeaponType.SawBlade:
                ActivateSawBlade(level, data);
                break;
        }
    }

    private void DropOil(int level, PartsData data)
    {
        GameObject oil = new GameObject("OilSlick");
        oil.transform.position = transform.position - transform.up * 1f;
        var col = oil.AddComponent<CircleCollider2D>();
        col.radius = 1f + level * 0.3f;
        col.isTrigger = true;
        oil.AddComponent<OilSlickEffect>().damage = data.damageBonus * level;
        oil.tag = "PlayerProjectile";

        var sr = oil.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);
        sr.sortingOrder = -1;

        Destroy(oil, data.duration > 0 ? data.duration : 5f);
    }

    private void ActivateSawBlade(int level, PartsData data)
    {
        float radius = 1.5f + level * 0.3f;
        float damage = data.damageBonus * (0.5f + level * 0.2f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                EnemyHealth eh = hit.GetComponent<EnemyHealth>();
                if (eh != null)
                    eh.TakeDamage(damage);
            }
        }
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
