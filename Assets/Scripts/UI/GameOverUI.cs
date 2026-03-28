using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject runCompletePanel;

    [Header("Game Over Elements")]
    public TextMeshProUGUI gameOverKillsText;
    public TextMeshProUGUI gameOverTimeText;
    public TextMeshProUGUI gameOverGoldText;
    public Button retryButton;

    [Header("Run Complete Elements")]
    public TextMeshProUGUI completeKillsText;
    public TextMeshProUGUI completeTimeText;
    public TextMeshProUGUI completeGoldText;
    public Button completeRetryButton;

    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = FindAnyObjectByType<PlayerStats>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (runCompletePanel != null) runCompletePanel.SetActive(false);

        GameManager.Instance.OnGameStateChanged += OnStateChanged;

        if (retryButton != null)
            retryButton.onClick.AddListener(() => GameManager.Instance.RestartRun());
        if (completeRetryButton != null)
            completeRetryButton.onClick.AddListener(() => GameManager.Instance.RestartRun());
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            ShowGameOver();
        }
        else if (state == GameManager.GameState.RunComplete)
        {
            ShowRunComplete();
        }
    }

    private void ShowGameOver()
    {
        if (gameOverPanel == null) return;
        gameOverPanel.SetActive(true);

        if (gameOverKillsText != null)
            gameOverKillsText.text = $"Kills: {playerStats.enemiesKilled}";
        if (gameOverTimeText != null)
        {
            int m = Mathf.FloorToInt(playerStats.survivalTime / 60f);
            int s = Mathf.FloorToInt(playerStats.survivalTime % 60f);
            gameOverTimeText.text = $"Time: {m:00}:{s:00}";
        }
        if (gameOverGoldText != null)
            gameOverGoldText.text = $"Gold: {playerStats.gold}";
    }

    private void ShowRunComplete()
    {
        if (runCompletePanel == null) return;
        runCompletePanel.SetActive(true);

        if (completeKillsText != null)
            completeKillsText.text = $"Kills: {playerStats.enemiesKilled}";
        if (completeTimeText != null)
        {
            int m = Mathf.FloorToInt(playerStats.survivalTime / 60f);
            int s = Mathf.FloorToInt(playerStats.survivalTime % 60f);
            completeTimeText.text = $"Time: {m:00}:{s:00}";
        }
        if (completeGoldText != null)
            completeGoldText.text = $"Gold: {playerStats.gold}";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnStateChanged;
    }
}
