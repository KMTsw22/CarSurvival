using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;

public class MainLobbyUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Label playerNameLabel;
    private Label lvLabel;
    private Label coinLabel;
    private ProgressBar expBar;
    private VisualElement startBtn;

    public event Action OnStartGameClicked;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        playerNameLabel = root.Q<Label>("player-name");
        lvLabel = root.Q<Label>("lv-label");
        expBar = root.Q<ProgressBar>();

        coinLabel = root.Q("coin-box")?.Q<Label>(className: "currency-label");

        // Steam 버전: fuel/gem UI 제거
        var energyBox = root.Q("energy-box");
        if (energyBox != null) energyBox.style.display = DisplayStyle.None;
        var gemBox = root.Q("gem-box");
        if (gemBox != null) gemBox.style.display = DisplayStyle.None;

        startBtn = root.Q("start__btn");
        startBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            OnStartGameClicked?.Invoke();
        });

        SetPlayerInfo("DRIVER_001", 25, 22);
        SetGold(163000);
    }

    public void SetPlayerInfo(string name, int level, float expPercent)
    {
        if (playerNameLabel != null) playerNameLabel.text = name;
        if (lvLabel != null) lvLabel.text = $"LV{level}";
        if (expBar != null) expBar.value = expPercent;
    }

    public void SetGold(int gold)
    {
        if (coinLabel != null)
        {
            if (gold >= 1000)
                coinLabel.text = $"{gold / 1000}K";
            else
                coinLabel.text = gold.ToString();
        }
    }
}
