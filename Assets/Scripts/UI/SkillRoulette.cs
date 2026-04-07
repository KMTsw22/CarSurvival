using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// 카드 배경 위에서 스킬 아이콘이 세로 한 줄로 룰렛처럼 돌아가는 뽑기 연출.
/// UI: Resources/Sprites/UI/InGame/FixBox/FixBoxRoulette.uxml
/// </summary>
public class SkillRoulette : MonoBehaviour
{
    private UIDocument uiDoc;
    private VisualElement iconStrip;
    private VisualElement cardEl;
    private VisualElement bannerEl;
    private Label resultLabel;
    private Label continueLabel;

    private List<Sprite> skillIcons = new List<Sprite>();
    private OwnedPart chosenPart;
    private PlayerStats playerStats;

    private float spinTimer;
    private float spinDuration = 2.5f;
    private float stripOffset;
    private float spinSpeed = 2000f;
    private float slotHeight = 220f;
    private int totalSlots;
    private int chosenIndex;
    private bool spinning;
    private bool waitingForClick;
    private bool ready;
    private float blinkTimer;

    public static void Show(List<OwnedPart> allParts, OwnedPart chosen, PlayerStats stats)
    {
        var obj = new GameObject("SkillRoulette");
        var roulette = obj.AddComponent<SkillRoulette>();
        roulette.Init(allParts, chosen, stats);
    }

    private void Init(List<OwnedPart> allParts, OwnedPart chosen, PlayerStats stats)
    {
        chosenPart = chosen;
        playerStats = stats;

        foreach (var part in allParts)
        {
            if (part.data.icon != null)
                skillIcons.Add(part.data.icon);
        }

        if (skillIcons.Count == 0)
        {
            chosen.level++;
            stats?.RecalculateStats();
            stats?.NotifyPartChanged();
            Destroy(gameObject);
            return;
        }

        chosenIndex = 0;
        for (int i = 0; i < allParts.Count; i++)
        {
            if (allParts[i] == chosen && allParts[i].data.icon != null)
            {
                int idx = skillIcons.IndexOf(allParts[i].data.icon);
                if (idx >= 0) chosenIndex = idx;
                break;
            }
        }

        totalSlots = skillIcons.Count * 10;
        Time.timeScale = 0f;

        if (!BuildUI())
        {
            // UI 로드 실패 → 바로 레벨업하고 복구
            Debug.LogError("[SkillRoulette] UI 로드 실패 — 바로 레벨업");
            chosen.level++;
            stats?.RecalculateStats();
            Time.timeScale = 1f;
            Destroy(gameObject);
            return;
        }

        spinning = true;
        ready = true;
    }

    private bool BuildUI()
    {
        uiDoc = gameObject.AddComponent<UIDocument>();

        // 기존 UIDocument에서 panelSettings 가져오기 (FindAnyObjectByType은 uiDoc 자체를 반환할 수 있음)
        var allDocs = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude);
        foreach (var doc in allDocs)
        {
            if (doc != uiDoc && doc.panelSettings != null)
            {
                uiDoc.panelSettings = doc.panelSettings;
                break;
            }
        }
        uiDoc.sortingOrder = 100; // HUD 위에 표시

        // UXML 로드
        var uxml = Resources.Load<VisualTreeAsset>("Sprites/UI/InGame/FixBox/FixBoxRoulette");
        if (uxml == null)
        {
            Debug.LogError("[SkillRoulette] FixBoxRoulette.uxml 로드 실패");
            return false;
        }

        uxml.CloneTree(uiDoc.rootVisualElement);

        var root = uiDoc.rootVisualElement;
        iconStrip = root.Q("roulette-strip");
        resultLabel = root.Q<Label>("result-label");
        continueLabel = root.Q<Label>("continue-label");
        cardEl = root.Q("roulette-card");
        bannerEl = root.Q("title-banner");

        if (iconStrip == null)
        {
            Debug.LogError("[SkillRoulette] roulette-strip 엘리먼트를 찾을 수 없음");
            return false;
        }

        // 배경 이미지를 Texture2D로 강제 적용 (spriteMode 문제 회피)
        var cardTex = Resources.Load<Texture2D>("Sprites/UI/InGame/FixBox/card_bg");
        if (cardTex != null && cardEl != null)
            cardEl.style.backgroundImage = new StyleBackground(Background.FromTexture2D(cardTex));

        var bannerTex = Resources.Load<Texture2D>("Sprites/UI/InGame/FixBox/ToolBoxBanner");
        if (bannerTex != null && bannerEl != null)
            bannerEl.style.backgroundImage = new StyleBackground(Background.FromTexture2D(bannerTex));

        // 초기 숨김
        if (resultLabel != null) resultLabel.style.display = DisplayStyle.None;
        if (continueLabel != null) continueLabel.style.display = DisplayStyle.None;

        // 아이콘 슬롯 채우기
        for (int i = 0; i < totalSlots; i++)
        {
            int idx = i % skillIcons.Count;
            var icon = new VisualElement();
            icon.AddToClassList("roulette-icon");
            icon.style.backgroundImage = new StyleBackground(skillIcons[idx]);
            iconStrip.Add(icon);
        }

        return true;
    }

    private void Update()
    {
        if (!ready) return;

        if (waitingForClick)
        {
            // "터치하여 계속" 깜빡이기
            if (continueLabel != null)
            {
                blinkTimer += Time.unscaledDeltaTime;
                float alpha = (Mathf.Sin(blinkTimer * 3f) + 1f) * 0.5f;
                continueLabel.style.opacity = alpha;
            }

            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
            {
                chosenPart.level++;
                if (playerStats != null)
                {
                    playerStats.RecalculateStats();
                    playerStats.NotifyPartChanged();
                }

                Debug.Log($"[SkillRoulette] {chosenPart.data.partName} → Lv.{chosenPart.level}");
                Time.timeScale = 1f;
                Destroy(gameObject);
            }
            return;
        }

        if (!spinning) return;

        spinTimer += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(spinTimer / spinDuration);

        float easedSpeed = spinSpeed * (1f - progress * progress);
        stripOffset += easedSpeed * Time.unscaledDeltaTime;

        float loopHeight = skillIcons.Count * slotHeight;
        if (stripOffset > loopHeight * 5f)
            stripOffset -= loopHeight;

        iconStrip.style.top = -stripOffset;

        if (spinTimer >= spinDuration)
        {
            spinning = false;

            float targetOffset = chosenIndex * slotHeight;
            float loops = Mathf.Floor(stripOffset / loopHeight);
            iconStrip.style.top = -(loops * loopHeight + targetOffset);

            if (resultLabel != null)
            {
                int prevLevel = chosenPart.level;
                int nextLevel = prevLevel + 1;
                resultLabel.text = $"★ {chosenPart.data.partName} ★\nLv.{prevLevel} → Lv.{nextLevel}";
                resultLabel.style.display = DisplayStyle.Flex;
            }
            if (continueLabel != null)
                continueLabel.style.display = DisplayStyle.Flex;

            waitingForClick = true;
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
            Time.timeScale = 1f;
    }
}
