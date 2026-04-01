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

    // 회전 톱날 관리
    private List<GameObject> activeSawBlades = new List<GameObject>();
    private int currentSawBladeLevel = 0;

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

            // 회전 톱날은 쿨타임 방식이 아니라 상시 회전 — 레벨 변경 시만 갱신
            if (type == WeaponType.SawBlade)
            {
                if (part.level != currentSawBladeLevel)
                    RebuildSawBlades(part.level, part.data);
                continue;
            }

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
            // 기관총 무기 데미지 = 기본 스탯 데미지 + 레벨당 증가 (etcValue4)
            float weaponDamage = stats.damage;
            var gunPart = stats.equippedParts.Find(p => p.data.weaponType == WeaponType.MachineGun);
            if (gunPart != null)
            {
                float perLevel = gunPart.data.etcValue4 > 0 ? gunPart.data.etcValue4 : gunPart.data.damage;
                weaponDamage += perLevel * gunPart.level;
            }

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
        }
    }

    // ─── 오일 슬릭: 현재 위치에 독 웅덩이 생성 ───
    // etc1=감속비율(%), etc2=감속지속(초), etc3=기본반경
    private void DropOil(int level, PartsData data)
    {
        GameObject oil = new GameObject("OilSlick");
        oil.transform.position = transform.position;

        float baseRadius = data.etcValue3 > 0 ? data.etcValue3 : 0.8f;
        float radiusPerLevel = data.etcValue4 > 0 ? data.etcValue4 : 0.2f;
        float radius = baseRadius + level * radiusPerLevel;

        var col = oil.AddComponent<CircleCollider2D>();
        col.radius = radius;
        col.isTrigger = true;

        var effect = oil.AddComponent<OilSlickEffect>();
        effect.damage = data.damage * level;
        effect.slowPercent = data.etcValue1 > 0 ? data.etcValue1 / 100f : 0.5f;
        effect.slowDuration = data.etcValue2 > 0 ? data.etcValue2 : 0.5f;
        oil.tag = "PlayerProjectile";

        var sr = oil.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_OilSlug");
        sr.color = Color.white;
        sr.sortingOrder = -1;
        oil.transform.localScale = new Vector3(radius * 2f, radius * 3f, 1f);

        float duration = data.duration > 0 ? data.duration : 5f;
        Destroy(oil, duration);
    }

    // ─── 회전 톱날: 레벨 = 톱날 개수, 상시 회전 ───
    // etc1=회전속도(도/초), etc2=궤도반경, etc3=타격간격(초)
    private void RebuildSawBlades(int level, PartsData data)
    {
        foreach (var blade in activeSawBlades)
        {
            if (blade != null) Destroy(blade);
        }
        activeSawBlades.Clear();
        currentSawBladeLevel = level;

        int count = level;
        float orbitRadius = data.etcValue2 > 0 ? data.etcValue2 : 2f;
        float dmg = data.damage * (0.5f + level * 0.15f);

        for (int i = 0; i < count; i++)
        {
            var blade = new GameObject($"SawBlade_{i}");
            blade.tag = "PlayerProjectile";

            // 비주얼: 톱날 스프라이트
            var sr = blade.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_SpinBlade");
            sr.color = Color.white;
            sr.sortingOrder = 9;
            blade.transform.localScale = Vector3.one * 0.4f;

            // Rigidbody2D (트리거 충돌 감지용)
            var rb = blade.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;

            // 콜라이더
            var col = blade.AddComponent<CircleCollider2D>();
            col.radius = 0.4f;
            col.isTrigger = true;

            // 회전 컴포넌트
            var orbit = blade.AddComponent<SawBladeOrbit>();
            orbit.center = transform;
            orbit.orbitRadius = orbitRadius;
            orbit.rotateSpeed = data.etcValue1 > 0 ? data.etcValue1 : 180f;
            orbit.damage = dmg;
            orbit.damageInterval = data.etcValue3 > 0 ? data.etcValue3 : 0.3f;
            orbit.bladeIndex = i;
            orbit.totalBlades = count;

            activeSawBlades.Add(blade);
        }
    }

}
