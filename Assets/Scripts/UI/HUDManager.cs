using UnityEngine;
using UnityEngine.UIElements;

public class HUDManager : MonoBehaviour
{
    private UIDocument uiDocument;

    private VisualElement healthBarClip;
    private Label healthText;
    private VisualElement boosterBarClip;
    private VisualElement expBarClip;
    private Label levelText;
    private Label killCountText;
    private Label timerText;
    private Label goldText;

    private VisualElement keyIcon;
    private Label keyText;
    private Button summonBtn;
    private Label countdownText;
    private Label warningText;

    // Item Slots (4 weapons + 4 spellbooks)
    private const int SlotCount = 4;
    private VisualElement[] weaponSlots = new VisualElement[SlotCount];
    private VisualElement[] weaponIcons = new VisualElement[SlotCount];
    private Label[] weaponLevels = new Label[SlotCount];
    private VisualElement[] spellbookSlots = new VisualElement[SlotCount];
    private VisualElement[] spellbookIcons = new VisualElement[SlotCount];
    private Label[] spellbookLevels = new Label[SlotCount];

    private PlayerStats playerStats;
    private StageManager stageManager;
    private CarController carController;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        healthBarClip = root.Q("health-bar-clip");
        healthText = root.Q<Label>("health-text");
        boosterBarClip = root.Q("booster-bar-clip");
        expBarClip = root.Q("exp-bar-clip");
        levelText = root.Q<Label>("level-text");
        killCountText = root.Q<Label>("kills-text");
        timerText = root.Q<Label>("timer-text");
        goldText = root.Q<Label>("gold-text");
        keyIcon = root.Q("key-icon");
        keyText = root.Q<Label>("key-text");
        summonBtn = root.Q<Button>("summon-btn");
        countdownText = root.Q<Label>("countdown-text");
        warningText = root.Q<Label>("warning-text");

        // Item Slots (4 weapons + 4 spellbooks)
        for (int i = 0; i < SlotCount; i++)
        {
            weaponSlots[i] = root.Q($"weapon-slot-{i}");
            if (weaponSlots[i] != null)
            {
                weaponIcons[i] = weaponSlots[i].Q(className: "item-icon");
                weaponLevels[i] = weaponSlots[i].Q<Label>(className: "item-level");
                weaponSlots[i].style.display = DisplayStyle.None;
            }

            spellbookSlots[i] = root.Q($"spellbook-slot-{i}");
            if (spellbookSlots[i] != null)
            {
                spellbookIcons[i] = spellbookSlots[i].Q(className: "item-icon");
                spellbookLevels[i] = spellbookSlots[i].Q<Label>(className: "item-level");
                spellbookSlots[i].style.display = DisplayStyle.None;
            }
        }

        if (summonBtn != null)
            summonBtn.clicked += OnSummonBossClicked;

        playerStats = PlayerStats.Instance;
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealth;
            playerStats.OnExpChanged += UpdateExp;
            playerStats.OnPartChanged += UpdateItemSlots;
            UpdateItemSlots();

            carController = playerStats.GetComponent<CarController>();
            if (carController != null)
                carController.OnBoosterChanged += UpdateBooster;
        }

        BindStageManager();
    }

    private void BindStageManager()
    {
        stageManager = StageManager.Instance;
        if (stageManager == null) return;

        stageManager.OnKeyCountChanged += UpdateKeyCount;
        stageManager.OnCountdownStart += OnCountdownStart;
        stageManager.OnCountdownTick += OnCountdownTick;
        stageManager.OnBossSummoned += OnBossSummoned;
        stageManager.OnForceSummonWarning += OnForceSummonWarning;
        stageManager.OnBossDefeatedEvent += OnBossDefeated;
        stageManager.OnForceGameOver += OnForceGameOver;
        stageManager.OnWarningWaveStart += OnWarningWaveStart;

        UpdateKeyCount(stageManager.collectedKeys, stageManager.RequiredKeys);
        UpdateKeyIcon();
    }

    private void Update()
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Instance;
            if (playerStats == null) return;
            playerStats.OnHealthChanged += UpdateHealth;
            playerStats.OnExpChanged += UpdateExp;
            playerStats.OnPartChanged += UpdateItemSlots;
        }

        if (stageManager == null)
            BindStageManager();

        if (killCountText != null)
            killCountText.text = $"KILLS: {playerStats.enemiesKilled}";

        if (timerText != null)
        {
            // 보스전 중에는 타이머 숨기기
            if (stageManager != null && stageManager.IsBossFight)
            {
                timerText.style.display = DisplayStyle.None;
            }
            else
            {
                timerText.style.display = DisplayStyle.Flex;
                int minutes = Mathf.FloorToInt(playerStats.survivalTime / 60f);
                int seconds = Mathf.FloorToInt(playerStats.survivalTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";

                // Warning Wave 중에는 빨간색 타이머
                if (stageManager != null && stageManager.IsWarningWave)
                {
                    timerText.style.color = new Color(1f, 0.3f, 0.3f);
                }
                // 9분30초 이후 타이머 빨간색
                else if (stageManager != null && stageManager.CurrentPhase == StageManager.BossPhase.Collecting
                    && playerStats.survivalTime >= stageManager.forceSummonTime - 30f)
                {
                    timerText.style.color = new Color(1f, 0.3f, 0.3f);
                }
            }
        }

        if (goldText != null)
            goldText.text = $"GOLD: {playerStats.gold}";

        // 보스전 중에는 열쇠/소환 버튼 숨기기
        if (stageManager != null && stageManager.IsBossFight)
        {
            if (summonBtn != null)
                summonBtn.style.display = DisplayStyle.None;
        }
    }

    private void UpdateBooster(float current, float max)
    {
        if (boosterBarClip != null)
        {
            float ratio = Mathf.Clamp01(current / max);
            boosterBarClip.style.width = new Length(63f * ratio, LengthUnit.Percent);
        }
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthBarClip != null)
        {
            float ratio = Mathf.Clamp01(current / max);
            healthBarClip.style.width = new Length(63f * ratio, LengthUnit.Percent);
        }
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void UpdateExp(int current, int toNext, int level)
    {
        if (expBarClip != null)
        {
            float ratio = Mathf.Clamp01((float)current / toNext);
            expBarClip.style.width = new Length(63f * ratio, LengthUnit.Percent);
        }
        if (levelText != null)
            levelText.text = $"Lv.{level}";
    }

    private void UpdateKeyCount(int collected, int required)
    {
        if (keyText != null)
            keyText.text = $"{collected}/{required}";

        if (summonBtn != null)
        {
            bool canSummon = collected >= required && required > 0;
            summonBtn.style.display = canSummon ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }

    private void UpdateKeyIcon()
    {
        if (keyIcon == null || stageManager == null) return;

        var sprite = stageManager.KeyIcon;
        if (sprite != null)
            keyIcon.style.backgroundImage = new StyleBackground(sprite);
    }

    // ─── 카운트다운 ───

    private void OnCountdownStart()
    {
        if (countdownText != null)
            countdownText.style.display = DisplayStyle.Flex;

        // 경고 텍스트 숨기기
        if (warningText != null)
            warningText.style.display = DisplayStyle.None;
    }

    private void OnCountdownTick(float remaining)
    {
        if (countdownText == null) return;
        int sec = Mathf.CeilToInt(remaining);
        countdownText.text = sec > 0 ? sec.ToString() : "GO!";
    }

    private void OnBossSummoned()
    {
        // 카운트다운 텍스트 잠시 후 숨기기
        if (countdownText != null)
        {
            countdownText.text = "BOSS!";
            // 1초 후 숨기기
            Invoke(nameof(HideCountdown), 1.5f);
        }
    }

    private void HideCountdown()
    {
        if (countdownText != null)
            countdownText.style.display = DisplayStyle.None;
    }

    // ─── Warning Wave ───

    private void OnWarningWaveStart()
    {
        // 카운트다운 텍스트에 WARNING 표시
        if (countdownText != null)
        {
            countdownText.style.display = DisplayStyle.Flex;
            countdownText.text = "WARNING!";
            Invoke(nameof(HideCountdown), 2f);
        }

        // 경고 텍스트
        if (warningText != null)
        {
            warningText.style.display = DisplayStyle.Flex;
            warningText.text = "포탈로 이동하세요!";
        }

        // 소환 버튼 숨기기
        if (summonBtn != null)
            summonBtn.style.display = DisplayStyle.None;
    }

    // ─── 강제 소환 경고 ───

    private void OnForceSummonWarning()
    {
        if (warningText == null) return;
        warningText.style.display = DisplayStyle.Flex;

        if (stageManager != null && stageManager.CanSummonBoss)
            warningText.text = "30초 후 보스 강제 소환!";
        else
            warningText.text = "30초 내 열쇠를 모으세요!";
    }

    private void OnForceGameOver()
    {
        if (warningText == null) return;
        warningText.text = "열쇠 부족 - GAME OVER";
    }

    // ─── 보스 처치 ───

    private void OnBossDefeated()
    {
        if (timerText != null)
            timerText.style.color = Color.white;

        if (warningText != null)
            warningText.style.display = DisplayStyle.None;

        // 새 스테이지 아이콘 갱신
        UpdateKeyIcon();
    }

    // ─── Item Slots ───

    private void UpdateItemSlots()
    {
        if (playerStats == null) return;

        int weaponIdx = 0;
        int spellIdx = 0;

        foreach (var part in playerStats.equippedParts)
        {
            if (part.data.category == ItemCategory.MainWeapon || part.data.category == ItemCategory.SubWeapon)
            {
                if (weaponIdx < SlotCount && weaponSlots[weaponIdx] != null)
                {
                    weaponSlots[weaponIdx].style.display = DisplayStyle.Flex;
                    if (weaponIcons[weaponIdx] != null && part.data.icon != null)
                        weaponIcons[weaponIdx].style.backgroundImage = new StyleBackground(part.data.icon);
                    if (weaponLevels[weaponIdx] != null)
                        weaponLevels[weaponIdx].text = $"Lv.{part.level}";
                    weaponIdx++;
                }
            }
            else if (part.data.category == ItemCategory.SpellBook)
            {
                if (spellIdx < SlotCount && spellbookSlots[spellIdx] != null)
                {
                    spellbookSlots[spellIdx].style.display = DisplayStyle.Flex;
                    if (spellbookIcons[spellIdx] != null && part.data.icon != null)
                        spellbookIcons[spellIdx].style.backgroundImage = new StyleBackground(part.data.icon);
                    if (spellbookLevels[spellIdx] != null)
                        spellbookLevels[spellIdx].text = $"Lv.{part.level}";
                    spellIdx++;
                }
            }
        }

        // Hide unused slots
        for (int i = weaponIdx; i < SlotCount; i++)
            if (weaponSlots[i] != null)
                weaponSlots[i].style.display = DisplayStyle.None;
        for (int i = spellIdx; i < SlotCount; i++)
            if (spellbookSlots[i] != null)
                spellbookSlots[i].style.display = DisplayStyle.None;
    }

    private void OnSummonBossClicked()
    {
        if (stageManager != null)
            stageManager.TrySummonBoss();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealth;
            playerStats.OnExpChanged -= UpdateExp;
            playerStats.OnPartChanged -= UpdateItemSlots;
        }
        if (carController != null)
            carController.OnBoosterChanged -= UpdateBooster;
        if (stageManager != null)
        {
            stageManager.OnKeyCountChanged -= UpdateKeyCount;
            stageManager.OnCountdownStart -= OnCountdownStart;
            stageManager.OnCountdownTick -= OnCountdownTick;
            stageManager.OnBossSummoned -= OnBossSummoned;
            stageManager.OnForceSummonWarning -= OnForceSummonWarning;
            stageManager.OnBossDefeatedEvent -= OnBossDefeated;
            stageManager.OnForceGameOver -= OnForceGameOver;
            stageManager.OnWarningWaveStart -= OnWarningWaveStart;
        }
        if (summonBtn != null)
            summonBtn.clicked -= OnSummonBossClicked;
    }
}
