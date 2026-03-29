using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;

public class MainLobbyUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Label playerNameLabel;
    private Label lvLabel;
    private Label fuelLabel;
    private Label gemLabel;
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

        fuelLabel = root.Q("energy-box")?.Q<Label>(className: "currency-label");
        gemLabel = root.Q("gem-box")?.Q<Label>(className: "currency-label");
        coinLabel = root.Q("coin-box")?.Q<Label>(className: "currency-label");

        startBtn = root.Q("start-btn");
        startBtn?.RegisterCallback<ClickEvent>(evt =>
        {
            OnStartGameClicked?.Invoke();
        });

        SetPlayerInfo("DRIVER_001", 25, 22);
        SetCurrency(60, 60, 2240, 163000);
    }

    public void SetPlayerInfo(string name, int level, float expPercent)
    {
        if (playerNameLabel != null) playerNameLabel.text = name;
        if (lvLabel != null) lvLabel.text = $"LV{level}";
        if (expBar != null) expBar.value = expPercent;
    }

    public void SetCurrency(int fuel, int maxFuel, int gems, int gold)
    {
        if (fuelLabel != null) fuelLabel.text = $"{fuel}/{maxFuel}";
        if (gemLabel != null) gemLabel.text = gems.ToString();
        if (coinLabel != null)
        {
            if (gold >= 1000)
                coinLabel.text = $"{gold / 1000}K";
            else
                coinLabel.text = gold.ToString();
        }
    }
}
