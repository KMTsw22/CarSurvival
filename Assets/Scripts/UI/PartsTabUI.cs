using UnityEngine;
using UnityEngine.UIElements;

public class PartsTabUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Label partsCountLabel;
    private Label atkValueLabel;
    private Label hpValueLabel;
    private Label carLevelLabel;

    private VisualElement partsGrid;
    private ScrollView partsGridScroll;

    private bool initialized = false;

    private const int Columns = 5;
    private const float Gap = 6f;

    /// <summary>
    /// TabNavigationController가 PartsContent 로드 후 호출
    /// </summary>
    public void Initialize()
    {
        if (initialized) return;

        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        partsCountLabel = root.Q<Label>("parts-count-label");
        atkValueLabel = root.Q<Label>("parts-atk-value");
        hpValueLabel = root.Q<Label>("parts-hp-value");
        carLevelLabel = root.Q<Label>("parts-car-level");
        partsGrid = root.Q<VisualElement>("parts-grid");
        partsGridScroll = root.Q<ScrollView>("parts-grid-scroll");

        // 서브탭 클릭 이벤트
        var subTabGrade = root.Q<VisualElement>("sub-tab-grade");
        var subTabMyParts = root.Q<VisualElement>("sub-tab-myparts");
        var subTabUpgrade = root.Q<VisualElement>("sub-tab-upgrade");

        subTabGrade?.RegisterCallback<ClickEvent>(evt => SwitchSubTab("grade"));
        subTabMyParts?.RegisterCallback<ClickEvent>(evt => SwitchSubTab("myparts"));
        subTabUpgrade?.RegisterCallback<ClickEvent>(evt => SwitchSubTab("upgrade"));

        UpdateStats(0, 100, 0, 0);

        // 레이아웃 완료 후 슬롯 크기를 비율에 맞춰 조정
        partsGridScroll?.RegisterCallback<GeometryChangedEvent>(OnGridGeometryChanged);

        initialized = true;
    }

    /// <summary>
    /// slotCount개의 슬롯을 5개씩 row로 나눠서 생성
    /// </summary>
    public void BuildGrid(int slotCount)
    {
        if (partsGrid == null) return;
        partsGrid.Clear();

        int rowCount = Mathf.CeilToInt((float)slotCount / Columns);
        int placed = 0;

        for (int r = 0; r < rowCount; r++)
        {
            var row = new VisualElement();
            row.AddToClassList("parts-grid-row");

            int slotsInRow = Mathf.Min(Columns, slotCount - placed);
            for (int c = 0; c < slotsInRow; c++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("part-slot");
                row.Add(slot);
                placed++;
            }

            partsGrid.Add(row);
        }
    }

    private void OnGridGeometryChanged(GeometryChangedEvent evt)
    {
        if (partsGrid == null || partsGridScroll == null) return;

        float scrollHeight = partsGridScroll.resolvedStyle.height;
        if (scrollHeight <= 0) return;

        float rowHeight = (scrollHeight - Gap * 5) / 4f;
        if (rowHeight <= 0) return;

        foreach (var row in partsGrid.Children())
        {
            row.style.height = rowHeight;
            row.style.marginBottom = Gap;
        }
    }

    public void UpdateStats(int currentParts, int maxParts, int atkBonus, int hpBonus)
    {
        if (partsCountLabel != null) partsCountLabel.text = $"Parts: {currentParts}/{maxParts}";
        if (atkValueLabel != null) atkValueLabel.text = $"+{atkBonus}";
        if (hpValueLabel != null) hpValueLabel.text = $"+{hpBonus}";
    }

    public void SetCarLevel(int level)
    {
        if (carLevelLabel != null) carLevelLabel.text = $"LV. {level}";
    }

    private void SwitchSubTab(string subTab)
    {
        var root = uiDocument.rootVisualElement;
        var tabs = new[] { "sub-tab-grade", "sub-tab-myparts", "sub-tab-upgrade" };

        foreach (var tabName in tabs)
        {
            var tab = root.Q<VisualElement>(tabName);
            if (tab == null) continue;

            var label = tab.Q<Label>(className: "parts-sub-tab-label");

            if (tabName == $"sub-tab-{subTab}")
            {
                tab.RemoveFromClassList("parts-sub-tab--gold");
                tab.AddToClassList("parts-sub-tab--active");
                label?.AddToClassList("parts-sub-tab-label--active");
            }
            else
            {
                tab.RemoveFromClassList("parts-sub-tab--active");
                tab.AddToClassList("parts-sub-tab--gold");
                label?.RemoveFromClassList("parts-sub-tab-label--active");
            }
        }
    }
}
