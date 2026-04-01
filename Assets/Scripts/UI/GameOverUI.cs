using UnityEngine;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private VisualElement gameOverPanel;
    private VisualElement runCompletePanel;

    private Label gameOverKillsText;
    private Label gameOverTimeText;
    private Label gameOverGoldText;

    private Label completeKillsText;
    private Label completeTimeText;
    private Label completeGoldText;

    private PlayerStats playerStats;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;

        gameOverPanel = root.Q("gameover-panel");
        runCompletePanel = root.Q("complete-panel");

        gameOverKillsText = root.Q<Label>("gameover-kills");
        gameOverTimeText = root.Q<Label>("gameover-time");
        gameOverGoldText = root.Q<Label>("gameover-gold");

        completeKillsText = root.Q<Label>("complete-kills");
        completeTimeText = root.Q<Label>("complete-time");
        completeGoldText = root.Q<Label>("complete-gold");

        // Retry 버튼
        var retryBtn = root.Q("gameover-retry-btn");
        retryBtn?.RegisterCallback<ClickEvent>(evt => GameManager.Instance.RestartRun());

        var completeRetryBtn = root.Q("complete-retry-btn");
        completeRetryBtn?.RegisterCallback<ClickEvent>(evt => GameManager.Instance.RestartRun());

        playerStats = PlayerStats.Instance;

        HideAll();
        GameManager.Instance.OnGameStateChanged += OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        HideAll();
        if (state == GameManager.GameState.GameOver)
            ShowGameOver();
        else if (state == GameManager.GameState.RunComplete)
            ShowRunComplete();
    }

    private void ShowGameOver()
    {
        if (gameOverPanel == null || playerStats == null) return;
        gameOverPanel.style.display = DisplayStyle.Flex;

        gameOverKillsText.text = $"Kills: {playerStats.enemiesKilled}";
        int m = Mathf.FloorToInt(playerStats.survivalTime / 60f);
        int s = Mathf.FloorToInt(playerStats.survivalTime % 60f);
        gameOverTimeText.text = $"Time: {m:00}:{s:00}";
        gameOverGoldText.text = $"Gold: {playerStats.gold}";
    }

    private void ShowRunComplete()
    {
        if (runCompletePanel == null || playerStats == null) return;
        runCompletePanel.style.display = DisplayStyle.Flex;

        completeKillsText.text = $"Kills: {playerStats.enemiesKilled}";
        int m = Mathf.FloorToInt(playerStats.survivalTime / 60f);
        int s = Mathf.FloorToInt(playerStats.survivalTime % 60f);
        completeTimeText.text = $"Time: {m:00}:{s:00}";
        completeGoldText.text = $"Gold: {playerStats.gold}";
    }

    private void HideAll()
    {
        if (gameOverPanel != null)
            gameOverPanel.style.display = DisplayStyle.None;
        if (runCompletePanel != null)
            runCompletePanel.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
    }
}
