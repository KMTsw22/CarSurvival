using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LevelUpUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject levelUpPanel;
    public Transform cardContainer;
    public GameObject cardPrefab;

    [Header("References")]
    public PartsDatabase partsDatabase;

    private PlayerStats playerStats;
    private List<PartsData> currentChoices = new List<PartsData>();

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnLevelUp += ShowLevelUpCards;
        }

        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);
    }

    private void ShowLevelUpCards()
    {
        if (partsDatabase == null) return;

        levelUpPanel.SetActive(true);

        // Clear existing cards
        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        // Get 3 random parts
        currentChoices = partsDatabase.GetRandomParts(3);

        for (int i = 0; i < currentChoices.Count; i++)
        {
            GameObject card = Instantiate(cardPrefab, cardContainer);
            card.SetActive(true);
            SetupCard(card, currentChoices[i], i);
        }
    }

    private void SetupCard(GameObject card, PartsData part, int index)
    {
        // Card title
        var titleText = card.transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
        if (titleText != null)
            titleText.text = part.partName;

        // Card description
        var descText = card.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
        if (descText != null)
            descText.text = part.description;

        // Card icon
        var iconImage = card.transform.Find("Icon")?.GetComponent<Image>();
        if (iconImage != null && part.icon != null)
            iconImage.sprite = part.icon;

        // Grade color
        var bg = card.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = GetGradeColor(part.grade);
        }

        // Category label
        var categoryText = card.transform.Find("CategoryText")?.GetComponent<TextMeshProUGUI>();
        if (categoryText != null)
            categoryText.text = $"[{part.category}]";

        // Button click
        var button = card.GetComponent<Button>();
        if (button != null)
        {
            int capturedIndex = index;
            button.onClick.AddListener(() => SelectPart(capturedIndex));
        }
    }

    private Color GetGradeColor(PartsGrade grade)
    {
        return grade switch
        {
            PartsGrade.Common => new Color(0.8f, 0.8f, 0.8f, 0.9f),
            PartsGrade.Rare => new Color(0.3f, 0.5f, 1f, 0.9f),
            PartsGrade.Epic => new Color(0.6f, 0.2f, 0.8f, 0.9f),
            PartsGrade.Legendary => new Color(1f, 0.8f, 0.2f, 0.9f),
            _ => Color.white
        };
    }

    public void SelectPart(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;

        playerStats.ApplyPart(currentChoices[index]);
        levelUpPanel.SetActive(false);
        GameManager.Instance.SetState(GameManager.GameState.Playing);
        playerStats.ProcessPendingLevelUp();
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnLevelUp -= ShowLevelUpCards;
        }
    }
}
