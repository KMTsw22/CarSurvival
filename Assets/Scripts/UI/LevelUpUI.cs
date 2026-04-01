using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class LevelUpUI : MonoBehaviour
{
    [Header("References")]
    public PartsDatabase partsDatabase;

    [Header("Reroll")]
    public int maxRerolls = 1;

    private UIDocument uiDocument;
    private VisualElement overlay;
    private VisualElement cardContainer;
    private List<VisualElement> cards = new List<VisualElement>();
    private Button rerollBtn;
    private Label rerollCountLabel;

    private PlayerStats playerStats;
    private List<PartsData> currentChoices = new List<PartsData>();
    private int remainingRerolls;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        playerStats = PlayerStats.Instance;

        BindUI();
        Hide();

        if (playerStats != null)
        {
            playerStats.OnLevelUp += ShowLevelUpCards;
        }
    }

    private void Update()
    {
        if (playerStats == null)
        {
            playerStats = PlayerStats.Instance;
            if (playerStats != null)
                playerStats.OnLevelUp += ShowLevelUpCards;
        }
    }

    private void BindUI()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;
        overlay = root.Q("levelup-overlay");
        cardContainer = root.Q("card-container");
        rerollBtn = root.Q<Button>("reroll-btn");
        rerollCountLabel = root.Q<Label>("reroll-count");

        // UXML에 있는 카드 3장 찾기
        cards.Clear();
        if (cardContainer != null)
        {
            foreach (var child in cardContainer.Children())
            {
                if (child.ClassListContains("levelup-card"))
                    cards.Add(child);
            }
        }

        // 각 카드에 클릭 이벤트 등록
        for (int i = 0; i < cards.Count; i++)
        {
            int idx = i;
            cards[i].RegisterCallback<ClickEvent>(evt => SelectPart(idx));
        }

        if (rerollBtn != null)
            rerollBtn.RegisterCallback<ClickEvent>(evt => OnReroll());
    }

    private void ShowLevelUpCards()
    {
        if (overlay == null || cardContainer == null)
            BindUI();

        if (partsDatabase == null || overlay == null || cards.Count == 0)
        {
            Debug.LogWarning("[LevelUpUI] UI 요소 없음 — 게임 재개");
            GameManager.Instance.SetState(GameManager.GameState.Playing);
            return;
        }

        remainingRerolls = maxRerolls;
        FillCards();
        UpdateRerollUI();
        Show();
    }

    private void FillCards()
    {
        currentChoices = partsDatabase.GetRandomParts(cards.Count);

        for (int i = 0; i < cards.Count; i++)
        {
            if (i < currentChoices.Count)
            {
                FillCard(cards[i], currentChoices[i]);
                cards[i].style.display = DisplayStyle.Flex;
            }
            else
            {
                cards[i].style.display = DisplayStyle.None;
            }
        }
    }

    private void FillCard(VisualElement card, PartsData data)
    {
        // 이름 (card-category 자리에 아이템 이름 표시)
        var categoryLabel = card.Q<Label>("card-category");
        if (categoryLabel != null)
        {
            categoryLabel.text = data.partName;

            categoryLabel.RemoveFromClassList("card-category-main");
            categoryLabel.RemoveFromClassList("card-category-sub");
            categoryLabel.RemoveFromClassList("card-category-spellbook");

            string colorClass = data.category switch
            {
                ItemCategory.MainWeapon => "card-category-main",
                ItemCategory.SubWeapon => "card-category-sub",
                ItemCategory.SpellBook => "card-category-spellbook",
                _ => ""
            };
            if (!string.IsNullOrEmpty(colorClass))
                categoryLabel.AddToClassList(colorClass);
        }

        // 아이콘
        var iconEl = card.Q("card-icon");
        if (iconEl != null)
            iconEl.style.backgroundImage = data.icon != null
                ? new StyleBackground(data.icon)
                : new StyleBackground(StyleKeyword.None);

        // 이름
        var titleLabel = card.Q<Label>("card-title");
        if (titleLabel != null)
            titleLabel.text = data.partName;

        // 설명: 테이블 값에서 자동 생성
        var descLabel = card.Q<Label>("card-desc");
        if (descLabel != null)
            descLabel.text = BuildStatsText(data);

        // 레벨
        var levelLabel = card.Q<Label>("card-level");
        if (levelLabel != null && playerStats != null)
        {
            var owned = playerStats.equippedParts.Find(p => p.data == data);
            if (owned != null)
                levelLabel.text = $"Lv.{owned.level} \u2192 Lv.{Mathf.Min(owned.level + 1, data.maxLevel)}";
            else
                levelLabel.text = "NEW";
        }
    }

    private string BuildStatsText(PartsData data)
    {
        var lines = new List<string>();

        switch (data.weaponType)
        {
            case WeaponType.MachineGun:
                float dmgPerLv = data.etcValue4 > 0 ? data.etcValue4 : data.damage;
                lines.Add($"DMG +{dmgPerLv:F0}/Lv");
                lines.Add($"CD {data.cooldown:F1}s");
                break;

            case WeaponType.OilSlick:
                float rangePerLv = data.etcValue4 > 0 ? data.etcValue4 : 0.2f;
                lines.Add($"범위 +{rangePerLv:F1}/Lv");
                if (data.etcValue1 > 0) lines.Add($"감속 {data.etcValue1:F0}%");
                if (data.duration > 0) lines.Add($"DUR {data.duration:F0}s");
                break;

            case WeaponType.SawBlade:
                lines.Add("톱날 +1/Lv");
                lines.Add($"DMG {data.damage:F0}");
                break;
        }

        // 스펠북
        if (data.category == ItemCategory.SpellBook)
        {
            if (data.damageBonus > 0) lines.Add($"ATK +{data.damageBonus:F0}%/Lv");
            if (data.speedBonus > 0) lines.Add($"SPD +{data.speedBonus:F0}%/Lv");
            if (data.healthBonus > 0) lines.Add($"HP +{data.healthBonus:F0}%/Lv");
            if (data.attackSpeedBonus > 0) lines.Add($"ASPD +{data.attackSpeedBonus:F0}%/Lv");
            if (data.defenseBonus > 0) lines.Add($"DEF +{data.defenseBonus:F0}%/Lv");
        }

        return lines.Count > 0 ? string.Join("\n", lines) : data.description;
    }

    private void OnReroll()
    {
        if (remainingRerolls <= 0) return;
        remainingRerolls--;
        FillCards();
        UpdateRerollUI();
    }

    private void UpdateRerollUI()
    {
        if (rerollCountLabel != null)
            rerollCountLabel.text = $"Remaining Rerolls: {remainingRerolls}";

        if (rerollBtn != null)
            rerollBtn.SetEnabled(remainingRerolls > 0);
    }

    private void SelectPart(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        playerStats.ApplyPart(currentChoices[index]);
        Hide();
        GameManager.Instance.SetState(GameManager.GameState.Playing);
        playerStats.ProcessPendingLevelUp();
    }

    private void Show()
    {
        if (overlay != null)
            overlay.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        if (overlay != null)
            overlay.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnLevelUp -= ShowLevelUpCards;
    }
}
