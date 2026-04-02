using UnityEngine;
using UnityEngine.UIElements;

public class HUDManager : MonoBehaviour
{
    private UIDocument uiDocument;

    private VisualElement healthBarFill;
    private Label healthText;
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

    private PlayerStats playerStats;
    private StageManager stageManager;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        healthBarFill = root.Q("health-bar-fill");
        healthText = root.Q<Label>("health-text");
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

        if (summonBtn != null)
            summonBtn.clicked += OnSummonBossClicked;

        playerStats = PlayerStats.Instance;
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealth;
            playerStats.OnExpChanged += UpdateExp;
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

    private void UpdateHealth(float current, float max)
    {
        if (healthBarFill != null)
        {
            float ratio = Mathf.Clamp01(current / max);
            healthBarFill.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
        }
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void UpdateExp(int current, int toNext, int level)
    {
        if (expBarClip != null)
        {
            float ratio = Mathf.Clamp01((float)current / toNext);
            expBarClip.style.width = new StyleLength(new Length(ratio * 100f, LengthUnit.Percent));
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
        }
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
