using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Bootstraps the entire game scene at runtime.
/// Attach this to an empty GameObject in the scene to auto-generate everything.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private GameObject playerCar;
    private PartsDatabase partsDB;
    private List<MonsterData> monsterDataList = new List<MonsterData>();
    private List<MonsterData> bossDataList = new List<MonsterData>();

    private void Awake()
    {
        // 테이블 데이터 로드 (가장 먼저)
        TableManager.Instance.LoadAll();

        CreatePartsDatabase();
        CreateMonsterDatabase();
        CreatePlayer();
        CreateCamera();
        CreateGameManager();
        CreateEnemySpawner();
        CreateStageManager();
        CreateUI();
        CreateBackground();
    }

    // ─── Parts Database (TB_Weapon + TB_SpellBook 기반) ───
    private void CreatePartsDatabase()
    {
        partsDB = ScriptableObject.CreateInstance<PartsDatabase>();
        var tm = TableManager.Instance;

        // 무기 (주무기 + 보조무기)
        if (tm.Weapons != null)
        {
            foreach (var row in tm.Weapons)
                partsDB.allParts.Add(CreateFromWeapon(row));
        }

        // 마법서 (패시브 버프)
        if (tm.SpellBooks != null)
        {
            foreach (var row in tm.SpellBooks)
                partsDB.allParts.Add(CreateFromSpellBook(row));
        }
    }

    private PartsData CreateFromWeapon(WeaponRow row)
    {
        var part = ScriptableObject.CreateInstance<PartsData>();
        part.itemId = row.weapon_id;
        part.partName = TableManager.Instance.GetLangName(row.weapon_id);
        part.description = TableManager.Instance.GetLangDesc(row.weapon_id);
        part.category = row.weapon_category == "Main" ? ItemCategory.MainWeapon : ItemCategory.SubWeapon;
        part.damage = row.damage;
        part.weaponType = ParseWeaponType(row.weapon_type);
        part.aimType = ParseEnum<AimType>(row.aim_type);
        part.cooldown = row.cooldown;
        part.duration = row.duration;
        part.maxLevel = row.max_level;
        part.dropWeight = row.drop_weight;
        part.etcValue1 = row.etc_value1;
        part.etcValue2 = row.etc_value2;
        part.etcValue3 = row.etc_value3;
        part.etcValue4 = row.etc_value4;
        part.etcValue5 = row.etc_value5;
        part.icon = LoadIcon(row.icon_key);
        return part;
    }

    private PartsData CreateFromSpellBook(SpellBookRow row)
    {
        var part = ScriptableObject.CreateInstance<PartsData>();
        part.itemId = row.book_id;
        part.partName = TableManager.Instance.GetLangName(row.book_id);
        part.description = TableManager.Instance.GetLangDesc(row.book_id);
        part.category = ItemCategory.SpellBook;
        part.maxLevel = row.max_level;
        part.dropWeight = row.drop_weight;
        part.icon = LoadIcon(row.icon_key);

        switch (row.effect_type)
        {
            case "SpeedUp":
                part.speedBonus = row.base_value;
                break;
            case "DamageUp":
                part.damageBonus = row.base_value;
                break;
            case "HpUp":
                part.healthBonus = row.base_value;
                break;
        }

        return part;
    }

    private Sprite LoadIcon(string iconKey)
    {
        if (string.IsNullOrEmpty(iconKey)) return null;
        // Icons/ 하위 폴더 전부 탐색
        return Resources.Load<Sprite>("Sprites/Icons/Weapons/" + iconKey)
            ?? Resources.Load<Sprite>("Sprites/Icons/SpellBooks/" + iconKey)
            ?? Resources.Load<Sprite>("Sprites/Icons/Currency/" + iconKey)
            ?? Resources.Load<Sprite>("Sprites/Icons/" + iconKey);
    }

    private T ParseEnum<T>(string value) where T : struct
    {
        if (System.Enum.TryParse<T>(value, true, out var result))
            return result;
        return default;
    }

    private WeaponType ParseWeaponType(string value)
    {
        return value switch
        {
            "MachineGun" => WeaponType.MachineGun,
            "OilSlick" => WeaponType.OilSlick,
            "SawBlade" => WeaponType.SawBlade,
            _ => WeaponType.None,
        };
    }

    // ─── Monster Database (테이블 기반) ───
    private void CreateMonsterDatabase()
    {
        var tm = TableManager.Instance;

        foreach (var row in tm.Monsters)
        {
            var monster = ScriptableObject.CreateInstance<MonsterData>();
            monster.monId = row.mon_id;
            monster.monsterName = row.mon_name;
            monster.isBoss = row.is_boss;
            monster.health = row.base_hp;
            monster.moveSpeed = row.base_speed;
            monster.contactDamage = row.contact_damage;
            monster.scale = row.scale;
            monster.spawnWeight = row.spawn_weight;
            monster.specialAbility = row.special_ability;
            monster.bounceSpeed = row.bounce_speed;
            monster.bounceHeight = row.bounce_height;
            monster.bounceSquash = row.bounce_squash;

            // 드랍 정보는 TB_MonsterDrop에서 가져옴
            var drop = tm.GetMonsterDrop(row.mon_id);
            if (drop != null)
            {
                monster.expDrop = drop.exp_amount;
                monster.goldDrop = drop.gold_amount;
            }

            // 스프라이트 로드 시도 (Sprites/Monsters/ 하위 폴더 우선, 없으면 원본 경로)
            var sprite = Resources.Load<Sprite>("Sprites/Monsters/" + row.sprite_key)
                         ?? Resources.Load<Sprite>(row.sprite_key);
            if (sprite != null)
                monster.sprite = sprite;

            // 보스와 일반 몬스터 분리
            if (row.is_boss)
                bossDataList.Add(monster);
            else
                monsterDataList.Add(monster);
        }
    }
   
    // ─── Player ───
    private void CreatePlayer()
    {
        playerCar = new GameObject("PlayerCar");
        playerCar.tag = "Player";
        playerCar.layer = LayerMask.NameToLayer("Default");

        // Sprite - TB_Car의 sprite_key로 로드
        var sr = playerCar.AddComponent<SpriteRenderer>();
        var carData = TableManager.Instance.GetCar("CAR_001");
        string spriteKey = carData != null ? carData.sprite_key : "spr_car_super";
        var carSprite = Resources.Load<Sprite>("Sprites/Cars/" + spriteKey);
        sr.sprite = carSprite != null ? carSprite : CreateCarSprite(Color.cyan);
        sr.sortingOrder = 10;
        if (carSprite != null)
            playerCar.transform.localScale = new Vector3(0.8F,0.8F,0.8F);

        // Collider
        var col = playerCar.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);

        // Rigidbody
        var rb = playerCar.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.freezeRotation = true;

        // Scripts
        playerCar.AddComponent<CarController>();
        playerCar.AddComponent<PlayerStats>();
        var autoAttack = playerCar.AddComponent<AutoAttack>();
        autoAttack.bulletPrefab = CreateBulletPrefab();

        // Fire point
        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(playerCar.transform);
        firePoint.transform.localPosition = new Vector3(0, 0.7f, 0);
        autoAttack.firePoint = firePoint.transform;

        // 차량 파티클 트레일
        playerCar.AddComponent<DustTrail>();
        playerCar.AddComponent<CarExhaustTrail>();
    }

    // ─── Camera ───
    private void CreateCamera()
    {
        Camera.main.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        Camera.main.orthographicSize = 12f;
        var follow = Camera.main.gameObject.AddComponent<CameraFollow>();
        follow.target = playerCar.transform;
    }

    // ─── Game Manager ───
    private void CreateGameManager()
    {
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();
        gm.partsDatabase = partsDB;
    }

    // ─── Stage Manager ───
    private void CreateStageManager()
    {
        var stageObj = new GameObject("StageManager");
        stageObj.AddComponent<StageManager>();
    }

    // ─── Enemy Spawner ───
    private void CreateEnemySpawner()
    {
        var spawnerObj = new GameObject("EnemySpawner");
        var spawner = spawnerObj.AddComponent<EnemySpawner>();

        // 일반 몬스터만 스포너에 등록 (보스 제외)
        foreach (var data in monsterDataList)
        {
            spawner.enemyPrefabs.Add(CreateEnemyPrefab());
            spawner.monsterDataList.Add(data);
        }

        // 보스 데이터 등록
        foreach (var bossData in bossDataList)
        {
            spawner.bossPrefabs.Add(CreateEnemyPrefab());
            spawner.bossDataList.Add(bossData);
        }

        // Wave 데이터 로드
        var tm = TableManager.Instance;
        if (tm.Waves != null)
        {
            spawner.waveRows = tm.GetWavesByGroup("WG_CH1");
        }

        // fallback: 몬스터 데이터가 없으면 기본 적 프리팹 사용
        if (spawner.enemyPrefabs.Count == 0)
        {
            spawner.enemyPrefab = CreateEnemyPrefab();
        }
    }

    // ─── Background ───
    private void CreateBackground()
    {
        var mapSprite = Resources.Load<Sprite>("Sprites/Maps/map_1");

        if (mapSprite != null)
        {
            // map_1 이미지를 타일링하여 배경으로 사용
            int gridSize = 4;
            float tileSize = mapSprite.bounds.size.x;

            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int y = -gridSize; y <= gridSize; y++)
                {
                    var tile = new GameObject($"BG_{x}_{y}");
                    tile.transform.position = new Vector3(x * tileSize, y * tileSize, 0);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = mapSprite;
                    sr.sortingOrder = -10;
                }
            }
        }
        else
        {
            // Fallback: 단색 배경
            int gridSize = 10;
            float tileSize = 6f;

            for (int x = -gridSize; x <= gridSize; x++)
            {
                for (int y = -gridSize; y <= gridSize; y++)
                {
                    var tile = new GameObject($"Tile_{x}_{y}");
                    tile.transform.position = new Vector3(x * tileSize, y * tileSize, 0);

                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite = CreateSquareSprite();
                    sr.color = (x + y) % 2 == 0
                        ? new Color(0.15f, 0.15f, 0.2f)
                        : new Color(0.12f, 0.12f, 0.17f);
                    sr.sortingOrder = -10;
                    tile.transform.localScale = new Vector3(tileSize, tileSize, 1);
                }
            }
        }
    }

    // ─── UI (UI Toolkit) ───
    private void CreateUI()
    {
        var panelSettings = LoadOrCreatePanelSettings();

        CreateHUD(panelSettings);
        CreateLevelUpUI(panelSettings);
        CreateGameOverUI(panelSettings);
    }

    private UnityEngine.UIElements.PanelSettings LoadOrCreatePanelSettings()
    {
        // 에셋에서 로드 시도
        var ps = Resources.Load<UnityEngine.UIElements.PanelSettings>("Settings/InGamePanelSettings");
        if (ps != null) return ps;

        // fallback: 런타임 생성
        ps = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
        ps.scaleMode = UnityEngine.UIElements.PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = UnityEngine.UIElements.PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        ps.sortingOrder = 100;
        return ps;
    }

    private void CreateHUD(UnityEngine.UIElements.PanelSettings panelSettings)
    {
        var hudObj = new GameObject("HUD");
        var uiDoc = hudObj.AddComponent<UnityEngine.UIElements.UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.sortingOrder = 100;
        var uxml = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("Sprites/UI/InGame/HUD/HUD");
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        hudObj.AddComponent<HUDManager>();
    }

    private void CreateLevelUpUI(UnityEngine.UIElements.PanelSettings panelSettings)
    {
        var luObj = new GameObject("LevelUpUI");
        var uiDoc = luObj.AddComponent<UnityEngine.UIElements.UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.sortingOrder = 200;
        var uxml = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("Sprites/UI/InGame/LevelUpSelect/LevelUpUXML");
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        var lu = luObj.AddComponent<LevelUpUI>();
        lu.partsDatabase = partsDB;
    }

    private void CreateGameOverUI(UnityEngine.UIElements.PanelSettings panelSettings)
    {
        var goObj = new GameObject("GameOverUI");
        var uiDoc = goObj.AddComponent<UnityEngine.UIElements.UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.sortingOrder = 150;
        var uxml = Resources.Load<UnityEngine.UIElements.VisualTreeAsset>("Sprites/UI/InGame/GameOver/GameOver");
        if (uxml != null) uiDoc.visualTreeAsset = uxml;
        goObj.AddComponent<GameOverUI>();
    }

    // ─── Prefab Factories ───
    private GameObject CreateBulletPrefab()
    {
        var bullet = new GameObject("BulletPrefab");
        bullet.SetActive(false);

        var sr = bullet.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = Color.yellow;
        sr.sortingOrder = 5;
        bullet.transform.localScale = new Vector3(0.15f, 0.3f, 1f);

        var col = bullet.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        bullet.AddComponent<Bullet>();
        bullet.tag = "PlayerProjectile";

        // Keep it as a "prefab" by hiding it
        return bullet;
    }

    private GameObject CreateEnemyPrefab()
    {
        var enemy = new GameObject("EnemyPrefab");
        enemy.SetActive(false);
        enemy.tag = "Enemy";

        var sr = enemy.AddComponent<SpriteRenderer>();
        var enemyCarSprite = Resources.Load<Sprite>("Sprites/Cars/spr_car_suv");
        sr.sprite = enemyCarSprite != null ? enemyCarSprite : CreateCarSprite(Color.red);
        sr.sortingOrder = 8;
        // 스케일은 ApplyMonsterData에서 data.scale로 설정됨 — 프리팹은 1로 둠
        enemy.transform.localScale = Vector3.one;

        var col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 0f;
        rb.freezeRotation = true;

        var ai = enemy.AddComponent<EnemyAI>();
        ai.moveSpeed = 1.5f;

        var eh = enemy.AddComponent<EnemyHealth>();
        eh.maxHealth = 20f;
        eh.expDrop = 2;
        eh.goldDrop = 5;
        eh.expPickupPrefab = CreateExpPickupPrefab();

        // 몬스터 이펙트: 걷는 느낌 바운스 + 그림자 
        enemy.AddComponent<BounceEffect>();
        enemy.AddComponent<ShadowEffect>();

        // 템플릿은 DDL 안 씀 — spawner가 참조를 갖고 있으므로 GC 안 됨
        return enemy;
    }

    private GameObject CreateExpPickupPrefab()
    {
        var pickup = new GameObject("ExpPickupPrefab");
        pickup.SetActive(false);

        var sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite();
        sr.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        sr.sortingOrder = 3;
        pickup.transform.localScale = Vector3.one * 0.25f;

        var col = pickup.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        pickup.AddComponent<ExperiencePickup>();

        return pickup;
    }

    // ─── Sprite Helpers ───
    private Sprite CreateSquareSprite()
    {
        var tex = new Texture2D(4, 4);
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    private Sprite CreateCarSprite(Color baseColor)
    {
        int w = 16, h = 24;
        var tex = new Texture2D(w, h);

        // Clear
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                tex.SetPixel(x, y, Color.clear);

        // Car body shape
        for (int x = 3; x < 13; x++)
        {
            for (int y = 2; y < 22; y++)
            {
                // Body
                if (x >= 4 && x < 12 && y >= 3 && y < 21)
                    tex.SetPixel(x, y, baseColor);
                // Outline
                else
                    tex.SetPixel(x, y, baseColor * 0.6f);
            }
        }

        // Windshield
        for (int x = 5; x < 11; x++)
            for (int y = 15; y < 19; y++)
                tex.SetPixel(x, y, new Color(0.4f, 0.6f, 0.8f));

        // Headlights
        tex.SetPixel(5, 20, Color.yellow);
        tex.SetPixel(10, 20, Color.yellow);

        // Taillights
        tex.SetPixel(5, 3, Color.red);
        tex.SetPixel(10, 3, Color.red);

        // Wheels
        for (int y = 4; y < 8; y++) { tex.SetPixel(3, y, Color.gray); tex.SetPixel(12, y, Color.gray); }
        for (int y = 16; y < 20; y++) { tex.SetPixel(3, y, Color.gray); tex.SetPixel(12, y, Color.gray); }

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
    }

}
