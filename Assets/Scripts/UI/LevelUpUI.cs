using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class LevelUpUI : MonoBehaviour
{
    [Header("References")]
    public PartsDatabase partsDatabase;

    private UIDocument uiDocument;
    private VisualElement overlay;
    private VisualElement cardContainer;
    private VisualTreeAsset cardTemplate;

    private PlayerStats playerStats;
    private List<PartsData> currentChoices = new List<PartsData>();

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        playerStats = FindAnyObjectByType<PlayerStats>();

        // 카드 템플릿 로드
        cardTemplate = Resources.Load<VisualTreeAsset>("Sprites/UI/InGame/LevelUp/LevelUpCard");

        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            overlay = root.Q("levelup-overlay");
            cardContainer = root.Q("card-container");
        }

        Hide();

        if (playerStats != null)
            playerStats.OnLevelUp += ShowLevelUpCards;
    }

    private void ShowLevelUpCards()
    {
        if (partsDatabase == null || overlay == null) return;

        // 카드 컨테이너 초기화
        cardContainer.Clear();

        // 랜덤 3개 선택
        currentChoices = partsDatabase.GetRandomParts(3);

        for (int i = 0; i < currentChoices.Count; i++)
        {
            var card = CreateCard(currentChoices[i], i);
            cardContainer.Add(card);
        }

        Show();
    }

    private VisualElement CreateCard(PartsData data, int index)
    {
        VisualElement card;

        if (cardTemplate != null)
        {
            card = cardTemplate.Instantiate();
            card = card.Q("card-root") ?? card;
        }
        else
        {
            // fallback
            card = new VisualElement();
            card.AddToClassList("levelup-card");
            card.Add(new Label { name = "card-category" });
            card.Add(new VisualElement { name = "card-icon" });
            card.Add(new Label { name = "card-title" });
            card.Add(new Label { name = "card-desc" });
            card.Add(new Label { name = "card-level" });
        }

        // 카테고리
        var categoryLabel = card.Q<Label>("card-category");
        if (categoryLabel != null)
        {
            string categoryText = data.category switch
            {
                ItemCategory.MainWeapon => "Main Weapon",
                ItemCategory.SubWeapon => "Sub Weapon",
                ItemCategory.SpellBook => "Spell Book",
                _ => ""
            };
            categoryLabel.text = $"[{categoryText}]";

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
        if (iconEl != null && data.icon != null)
            iconEl.style.backgroundImage = new StyleBackground(data.icon);

        // 이름
        var titleLabel = card.Q<Label>("card-title");
        if (titleLabel != null)
            titleLabel.text = data.partName;

        // 설명
        var descLabel = card.Q<Label>("card-desc");
        if (descLabel != null)
            descLabel.text = data.description;

        // 레벨 (이미 장착 중이면 표시)
        var levelLabel = card.Q<Label>("card-level");
        if (levelLabel != null && playerStats != null)
        {
            var owned = playerStats.equippedParts.Find(p => p.data == data);
            if (owned != null)
                levelLabel.text = $"Lv.{owned.level} → Lv.{Mathf.Min(owned.level + 1, data.maxLevel)}";
            else
                levelLabel.text = "NEW";
        }

        // 클릭 이벤트
        int capturedIndex = index;
        card.RegisterCallback<ClickEvent>(evt => SelectPart(capturedIndex));

        return card;
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
