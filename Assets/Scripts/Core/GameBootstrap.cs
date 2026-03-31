using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Bootstraps the entire game scene at runtime.
/// Attach this to an empty GameObject in the scene to auto-generate everything.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    private GameObject playerCar;
    private PartsDatabase partsDB;
    private List<MonsterData> monsterDataList = new List<MonsterData>();

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
        part.partName = row.weapon_name;
        part.description = row.effect_desc;
        part.category = row.weapon_category == "Main" ? ItemCategory.MainWeapon : ItemCategory.SubWeapon;
        part.damageBonus = row.base_damage;
        part.weaponType = ParseWeaponType(row.weapon_type);
        part.aimType = ParseEnum<AimType>(row.aim_type);
        part.cooldown = row.cooldown;
        part.duration = row.duration;
        part.maxLevel = row.max_level;
        part.dropWeight = row.drop_weight;
        return part;
    }

    private PartsData CreateFromSpellBook(SpellBookRow row)
    {
        var part = ScriptableObject.CreateInstance<PartsData>();
        part.itemId = row.book_id;
        part.partName = row.book_name;
        part.description = row.effect_desc;
        part.category = ItemCategory.SpellBook;
        part.maxLevel = row.max_level;
        part.dropWeight = row.drop_weight;

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
            monster.monsterName = row.mon_name;
            monster.health = row.base_hp;
            monster.moveSpeed = row.base_speed;
            monster.contactDamage = row.contact_damage;
            monster.scale = row.scale;

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

            monsterDataList.Add(monster);
        }
    }

    // ─── Player ───
    private void CreatePlayer()
    {
        playerCar = new GameObject("PlayerCar");
        playerCar.tag = "Player";
        playerCar.layer = LayerMask.NameToLayer("Default");

        // Sprite - 실제 차량 이미지 사용
        var sr = playerCar.AddComponent<SpriteRenderer>();
        var carSprite = Resources.Load<Sprite>("Sprites/Cars/car_1");
        sr.sprite = carSprite != null ? carSprite : CreateCarSprite(Color.cyan);
        sr.sortingOrder = 10;
        // 500x500 이미지를 게임 크기에 맞게 스케일 조정
        if (carSprite != null)
            playerCar.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

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

    // ─── Enemy Spawner ───
    private void CreateEnemySpawner()
    {
        var spawnerObj = new GameObject("EnemySpawner");
        var spawner = spawnerObj.AddComponent<EnemySpawner>();

        // 각 몬스터 타입별 프리팹 생성
        foreach (var data in monsterDataList)
        {
            spawner.enemyPrefabs.Add(CreateEnemyPrefab());
            spawner.monsterDataList.Add(data);
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

    // ─── UI ───
    private void CreateUI()
    {
        // Canvas
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // HUD
        var hudObj = new GameObject("HUD");
        hudObj.transform.SetParent(canvasObj.transform, false);
        var hud = hudObj.AddComponent<HUDManager>();
        CreateHUDElements(canvasObj.transform, hud);

        // Level Up UI
        CreateLevelUpUI(canvasObj.transform);

        // Game Over UI
        CreateGameOverUI(canvasObj.transform);

        // Steam: 조이스틱 제거 — WASD + 게임패드만 사용
    }

    private void CreateHUDElements(Transform parent, HUDManager hud)
    {
        // Health Bar
        hud.healthBar = CreateSlider(parent, "HealthBar",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(20, -20), new Vector2(320, -20),
            Color.red, new Color(0.3f, 0f, 0f));
        hud.healthText = CreateText(parent, "HealthText",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(170, -20), new Vector2(200, 30),
            "100/100", 14, TextAlignmentOptions.Center);

        // Exp Bar (bottom)
        hud.expBar = CreateSlider(parent, "ExpBar",
            new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 30), new Vector2(0, 30),
            new Color(0.2f, 0.8f, 1f), new Color(0.1f, 0.2f, 0.3f),
            stretchWidth: true);

        // Level Text
        hud.levelText = CreateText(parent, "LevelText",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 55), new Vector2(100, 30),
            "Lv.1", 18, TextAlignmentOptions.Center);

        // Timer (top center)
        hud.timerText = CreateText(parent, "TimerText",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -25), new Vector2(150, 40),
            "00:00", 24, TextAlignmentOptions.Center);

        // Kill count (top right)
        hud.killCountText = CreateText(parent, "KillCount",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-100, -20), new Vector2(180, 30),
            "KILLS: 0", 16, TextAlignmentOptions.Right);

        // Gold (top right below kills)
        hud.goldText = CreateText(parent, "GoldText",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-100, -50), new Vector2(180, 30),
            "GOLD: 0", 16, TextAlignmentOptions.Right);
    }

    private void CreateLevelUpUI(Transform parent)
    {
        var luObj = new GameObject("LevelUpUI");
        luObj.transform.SetParent(parent, false);
        var lu = luObj.AddComponent<LevelUpUI>();
        lu.partsDatabase = partsDB;

        // Panel (fullscreen darkened overlay)
        var panel = new GameObject("LevelUpPanel");
        panel.transform.SetParent(luObj.transform, false);
        var panelRT = panel.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.sizeDelta = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);
        lu.levelUpPanel = panel;

        // Title
        CreateText(panel.transform, "LevelUpTitle",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -100), new Vector2(400, 60),
            "LEVEL UP!", 36, TextAlignmentOptions.Center);

        // Subtitle
        CreateText(panel.transform, "LevelUpSubtitle",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -160), new Vector2(400, 40),
            "Choose a part to install", 20, TextAlignmentOptions.Center);

        // Card container
        var container = new GameObject("CardContainer");
        container.transform.SetParent(panel.transform, false);
        var containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.5f);
        containerRT.anchorMax = new Vector2(0.5f, 0.5f);
        containerRT.sizeDelta = new Vector2(900, 350);
        containerRT.anchoredPosition = new Vector2(0, -30);

        var hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        lu.cardContainer = container.transform;
        lu.cardPrefab = CreateCardPrefab();
    }

    private void CreateGameOverUI(Transform parent)
    {
        var goObj = new GameObject("GameOverUI");
        goObj.transform.SetParent(parent, false);
        var goUI = goObj.AddComponent<GameOverUI>();

        // Game Over Panel
        goUI.gameOverPanel = CreateResultPanel(goObj.transform, "GameOverPanel",
            "GAME OVER", Color.red,
            out var goKills, out var goTime, out var goGold, out var goRetry);
        goUI.gameOverKillsText = goKills;
        goUI.gameOverTimeText = goTime;
        goUI.gameOverGoldText = goGold;
        goUI.retryButton = goRetry;

        // Run Complete Panel
        goUI.runCompletePanel = CreateResultPanel(goObj.transform, "RunCompletePanel",
            "RUN COMPLETE!", new Color(1f, 0.8f, 0.2f),
            out var rcKills, out var rcTime, out var rcGold, out var rcRetry);
        goUI.completeKillsText = rcKills;
        goUI.completeTimeText = rcTime;
        goUI.completeGoldText = rcGold;
        goUI.completeRetryButton = rcRetry;
    }

    private GameObject CreateResultPanel(Transform parent, string name, string title, Color titleColor,
        out TextMeshProUGUI killsText, out TextMeshProUGUI timeText,
        out TextMeshProUGUI goldText, out Button retryBtn)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);

        var titleTmp = CreateText(panel.transform, "Title",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 150), new Vector2(500, 70),
            title, 48, TextAlignmentOptions.Center);
        titleTmp.color = titleColor;

        killsText = CreateText(panel.transform, "Kills",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 50), new Vector2(400, 40),
            "Kills: 0", 24, TextAlignmentOptions.Center);

        timeText = CreateText(panel.transform, "Time",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, 0), new Vector2(400, 40),
            "Time: 00:00", 24, TextAlignmentOptions.Center);

        goldText = CreateText(panel.transform, "Gold",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0, -50), new Vector2(400, 40),
            "Gold: 0", 24, TextAlignmentOptions.Center);

        // Retry Button
        var btnObj = new GameObject("RetryButton");
        btnObj.transform.SetParent(panel.transform, false);
        var btnRT = btnObj.AddComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.sizeDelta = new Vector2(250, 60);
        btnRT.anchoredPosition = new Vector2(0, -130);
        var btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.6f, 1f);
        retryBtn = btnObj.AddComponent<Button>();

        var btnText = new GameObject("Text");
        btnText.transform.SetParent(btnObj.transform, false);
        var btnTextRT = btnText.AddComponent<RectTransform>();
        btnTextRT.anchorMin = Vector2.zero;
        btnTextRT.anchorMax = Vector2.one;
        btnTextRT.sizeDelta = Vector2.zero;
        var btnTMP = btnText.AddComponent<TextMeshProUGUI>();
        btnTMP.text = "RETRY";
        btnTMP.fontSize = 24;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;

        return panel;
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
        DontDestroyOnLoad(bullet);
        return bullet;
    }

    private GameObject CreateEnemyPrefab()
    {
        var enemy = new GameObject("EnemyPrefab");
        enemy.SetActive(false);
        enemy.tag = "Enemy";

        var sr = enemy.AddComponent<SpriteRenderer>();
        var enemyCarSprite = Resources.Load<Sprite>("Sprites/Cars/car_2");
        sr.sprite = enemyCarSprite != null ? enemyCarSprite : CreateCarSprite(Color.red);
        sr.sortingOrder = 8;
        // 스케일은 ApplyMonsterData에서 data.scale로 설정됨 — 프리팹은 1로 둠
        enemy.transform.localScale = Vector3.one;

        var col = enemy.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 1.2f);

        var rb = enemy.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.drag = 2f;
        rb.freezeRotation = true;

        var ai = enemy.AddComponent<EnemyAI>();
        ai.moveSpeed = 1.5f;

        var eh = enemy.AddComponent<EnemyHealth>();
        eh.maxHealth = 20f;
        eh.expDrop = 2;
        eh.goldDrop = 5;
        eh.expPickupPrefab = CreateExpPickupPrefab();

        DontDestroyOnLoad(enemy);
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

        DontDestroyOnLoad(pickup);
        return pickup;
    }

    private GameObject CreateCardPrefab()
    {
        var card = new GameObject("CardPrefab");
        card.SetActive(false);

        var rt = card.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(260, 350);

        var img = card.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.3f, 0.95f);

        var btn = card.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.4f, 0.4f, 0.6f);
        btn.colors = colors;

        // Layout
        var vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(15, 15, 15, 15);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Category
        CreateCardText(card.transform, "CategoryText", "[Engine]", 14,
            new Color(0.7f, 0.7f, 0.7f), 25);

        // Icon placeholder
        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(card.transform, false);
        var iconRT = iconObj.AddComponent<RectTransform>();
        var iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredHeight = 80;
        iconLE.preferredWidth = 80;
        var iconImg = iconObj.AddComponent<Image>();
        iconImg.color = new Color(1, 1, 1, 0.3f);

        // Title
        CreateCardText(card.transform, "TitleText", "Part Name", 22, Color.white, 35);

        // Description
        CreateCardText(card.transform, "DescriptionText", "Part description goes here", 16,
            new Color(0.8f, 0.8f, 0.8f), 60);

        DontDestroyOnLoad(card);
        return card;
    }

    private void CreateCardText(Transform parent, string name, string text, int fontSize,
        Color color, float height)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        var le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
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

    // ─── UI Helpers ───
    private Slider CreateSlider(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 posMin, Vector2 posMax,
        Color fillColor, Color bgColor, bool stretchWidth = false)
    {
        var sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        var sliderRT = sliderObj.AddComponent<RectTransform>();

        if (stretchWidth)
        {
            sliderRT.anchorMin = anchorMin;
            sliderRT.anchorMax = anchorMax;
            sliderRT.offsetMin = new Vector2(0, posMin.y - 10);
            sliderRT.offsetMax = new Vector2(0, posMax.y + 10);
        }
        else
        {
            sliderRT.anchorMin = anchorMin;
            sliderRT.anchorMax = anchorMax;
            sliderRT.sizeDelta = new Vector2(posMax.x - posMin.x, 20);
            sliderRT.anchoredPosition = new Vector2(
                (posMin.x + posMax.x) / 2f,
                (posMin.y + posMax.y) / 2f);
        }

        // Background
        var bg = new GameObject("Background");
        bg.transform.SetParent(sliderObj.transform, false);
        var bgRT = bg.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = Vector2.zero;
        fillAreaRT.anchorMax = Vector2.one;
        fillAreaRT.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRT = fill.AddComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.sizeDelta = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;

        var slider = sliderObj.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        return slider;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, Vector2 size,
        string text, int fontSize, TextAlignmentOptions alignment)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        return tmp;
    }
}
