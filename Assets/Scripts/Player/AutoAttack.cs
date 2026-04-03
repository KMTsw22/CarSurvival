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

    // 레이저 캐논 쿨타임 (주무기이므로 별도 관리)
    private float laserCooldownTimer;

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // 주무기 처리
        HandleMainWeapons();

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

    private void HandleMainWeapons()
    {
        // 기관총
        var gunPart = stats.equippedParts.Find(p => p.data.weaponType == WeaponType.MachineGun);
        if (gunPart != null)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                FireMachineGun(gunPart);
                cooldownTimer = baseCooldown / stats.attackSpeed;
            }
        }

        // 레이저 캐논
        var laserPart = stats.equippedParts.Find(p => p.data.weaponType == WeaponType.LaserCannon);
        if (laserPart != null)
        {
            laserCooldownTimer -= Time.deltaTime;
            if (laserCooldownTimer <= 0f)
            {
                FireLaserCannon(laserPart.level, laserPart.data);
                float cd = laserPart.data.cooldown > 0 ? laserPart.data.cooldown : 2f;
                laserCooldownTimer = cd;
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

    private float CalcDamage(PartsData data, int level)
    {
        return data.damage + data.damagePerLevel * (level - 1);
    }

    private void FireMachineGun(OwnedPart gunPart)
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
            float weaponDamage = stats.damage + CalcDamage(gunPart.data, gunPart.level);
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
            case WeaponType.ChainLightning:
                FireChainLightning(level, data);
                break;
            case WeaponType.EMPPulse:
                FireEMPPulse(level, data);
                break;
            case WeaponType.Flamethrower:
                FireFlamethrower(level, data);
                break;
            case WeaponType.MissilePod:
                FireMissilePod(level, data);
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
        effect.damage = CalcDamage(data, level);
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
        orbitRadius += 2f; //보정값
        float dmg = CalcDamage(data, level);

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
            rb.bodyType = RigidbodyType2D.Kinematic;

            // 콜라이더
            var bladeCol = blade.AddComponent<CircleCollider2D>();
            bladeCol.radius = 0.4f;
            bladeCol.isTrigger = true;

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

    // ─── 체인 라이트닝: 연쇄 번개 공격 ───
    // etc1=기본타격인원, etc2=레벨당추가인원
    private void FireChainLightning(int level, PartsData data)
    {
        var obj = new GameObject("ChainLightning");
        var effect = obj.AddComponent<ChainLightningEffect>();

        int baseChain = data.etcValue1 > 0 ? (int)data.etcValue1 : 3;
        int chainPerLevel = data.etcValue2 > 0 ? (int)data.etcValue2 : 1;
        int totalChain = baseChain + chainPerLevel * (level - 1);
        float dmg = CalcDamage(data, level);

        effect.Fire(transform.position, dmg, totalChain, 5f);
    }

    // ─── EMP 펄스: 범위 스턴 + 데미지 ───
    // etc1=기본지속시간(초), etc2=기본반경, etc3=레벨당반경증가
    private void FireEMPPulse(int level, PartsData data)
    {
        var obj = new GameObject("EMPPulse");
        var effect = obj.AddComponent<EMPPulseEffect>();

        float stunDur = data.etcValue1 > 0 ? data.etcValue1 : 2f;
        float baseRadius = data.etcValue2 > 0 ? data.etcValue2 : 3f;
        float radiusPerLevel = data.etcValue3 > 0 ? data.etcValue3 : 0.5f;
        float radius = baseRadius + radiusPerLevel * (level - 1);
        float dmg = CalcDamage(data, level);

        effect.Fire(transform.position, dmg, stunDur, radius);
    }

    // ─── 화염방사기: 전방 화염 영역 ───
    // etc1=기본지속시간(초), etc2=기본반경, etc3=레벨당반경증가
    private void FireFlamethrower(int level, PartsData data)
    {
        var obj = new GameObject("Flamethrower");
        var effect = obj.AddComponent<FlamethrowerEffect>();

        float dur = data.etcValue1 > 0 ? data.etcValue1 : 3f;
        float baseRadius = data.etcValue2 > 0 ? data.etcValue2 : 2f;
        float radiusPerLevel = data.etcValue3 > 0 ? data.etcValue3 : 0.3f;
        float radius = baseRadius + radiusPerLevel * (level - 1);
        float dmg = CalcDamage(data, level);

        // 차량이 바라보는 방향 (마우스 방향)
        Vector2 dir = GetMouseDirection();
        effect.Fire(transform.position, dir, dmg, radius, dur);
    }

    // ─── 레이저 캐논: 관통 레이저 (주무기) ───
    // etc1=기본레이저수, etc2=레벨당레이저추가
    private void FireLaserCannon(int level, PartsData data)
    {
        int baseLasers = data.etcValue1 > 0 ? (int)data.etcValue1 : 1;
        int lasersPerLevel = data.etcValue2 > 0 ? (int)data.etcValue2 : 1;
        int totalLasers = baseLasers + lasersPerLevel * (level - 1);
        float dmg = CalcDamage(data, level);

        Vector2 baseDir = GetMouseDirection();

        for (int i = 0; i < totalLasers; i++)
        {
            // 여러 줄기일 경우 부채꼴로 퍼짐
            float spreadAngle = (i - (totalLasers - 1) / 2f) * 10f;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float finalAngle = (baseAngle + spreadAngle) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

            var obj = new GameObject($"Laser_{i}");
            var effect = obj.AddComponent<LaserCannonEffect>();
            effect.Fire(transform.position, dir, dmg, 20f);
        }
    }

    // ─── 미사일 포드: 유도 미사일 ───
    // etc1=기본미사일수, etc2=레벨당미사일추가
    private void FireMissilePod(int level, PartsData data)
    {
        int baseMissiles = data.etcValue1 > 0 ? (int)data.etcValue1 : 2;
        int missilesPerLevel = data.etcValue2 > 0 ? (int)data.etcValue2 : 1;
        int totalMissiles = baseMissiles + missilesPerLevel * (level - 1);
        float dmg = CalcDamage(data, level);

        MissilePodEffect.FireMissiles(transform, totalMissiles, dmg, bulletSpeed * 0.8f, bulletPrefab);
    }
}
