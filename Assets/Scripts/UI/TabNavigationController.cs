using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TabNavigationController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;

    // 탭 → UXML 리소스 경로 매핑 (동적 로드용)
    private readonly Dictionary<string, string> tabToUxmlPath = new()
    {
        { "PartsTab", "Sprites/UI/OutGame/PartsTab/PartsContent" },
    };

    // 탭 → content-area 안의 컨테이너 이름 매핑
    private readonly Dictionary<string, string> tabToContainer = new()
    {
        { "BattleTab", "lobby-content" },
        { "PartsTab", "parts-content-container" },
    };

    // 이미 로드된 탭 추적
    private readonly HashSet<string> loadedTabs = new();

    private readonly string[] allTabNames = { "PartsTab", "BattleTab", "PaintTab", "UpgradeTab" };

    private string activeTab = "BattleTab";

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        foreach (var tabName in allTabNames)
        {
            var tab = root.Q<VisualElement>(tabName);
            if (tab == null) continue;

            var captured = tabName;
            tab.RegisterCallback<ClickEvent>(evt => SwitchTab(captured));
        }

        SwitchTab(activeTab);
    }

    public void SwitchTab(string tabName)
    {
        activeTab = tabName;

        // 동적 로드가 필요한 탭이면 UXML 로드
        if (tabToUxmlPath.ContainsKey(tabName) && !loadedTabs.Contains(tabName))
        {
            LoadTabContent(tabName);
        }

        // 모든 탭 비활성화
        foreach (var name in allTabNames)
        {
            var tab = root.Q<VisualElement>(name);
            if (tab == null) continue;

            tab.RemoveFromClassList("nav-active");
            var label = tab.Q<Label>(className: "nav-label");
            if (label != null)
            {
                label.RemoveFromClassList("nav-label-active");
            }
        }

        // 선택된 탭 활성화
        var activeTabEl = root.Q<VisualElement>(tabName);
        if (activeTabEl != null)
        {
            activeTabEl.AddToClassList("nav-active");
            var label = activeTabEl.Q<Label>(className: "nav-label");
            if (label != null)
            {
                label.AddToClassList("nav-label-active");
            }
        }

        // 모든 콘텐츠 숨기기
        foreach (var pair in tabToContainer)
        {
            var content = root.Q<VisualElement>(pair.Value);
            if (content == null) continue;

            content.RemoveFromClassList("tab-content--active");
            content.AddToClassList("tab-content");
        }

        // 해당 탭 콘텐츠 보이기
        if (tabToContainer.TryGetValue(tabName, out var containerName))
        {
            var content = root.Q<VisualElement>(containerName);
            if (content != null)
            {
                content.RemoveFromClassList("tab-content");
                content.AddToClassList("tab-content--active");
            }
        }
    }

    private void LoadTabContent(string tabName)
    {
        if (!tabToUxmlPath.TryGetValue(tabName, out var uxmlPath)) return;
        if (!tabToContainer.TryGetValue(tabName, out var containerName)) return;

        var container = root.Q<VisualElement>(containerName);
        if (container == null) return;

        var asset = Resources.Load<VisualTreeAsset>(uxmlPath);
        if (asset == null)
        {
            Debug.LogError($"[TabNav] UXML not found: {uxmlPath}");
            return;
        }

        container.Clear();
        asset.CloneTree(container);

        loadedTabs.Add(tabName);

        // 파츠탭 로드 후 PartsTabUI 초기화
        if (tabName == "PartsTab")
        {
            var partsTabUI = GetComponent<PartsTabUI>();
            partsTabUI?.Initialize();
        }
    }
}
