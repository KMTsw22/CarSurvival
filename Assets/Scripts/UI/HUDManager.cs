using UnityEngine;
using UnityEngine.UIElements;

public class HUDManager : MonoBehaviour
{
    private UIDocument uiDocument;

    private VisualElement healthBarFill;
    private Label healthText;
    private VisualElement expBarFill;
    private Label levelText;
    private Label killCountText;
    private Label timerText;
    private Label goldText;

    private PlayerStats playerStats;

    private void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null) return;

        var root = uiDocument.rootVisualElement;
        healthBarFill = root.Q("health-bar-fill");
        healthText = root.Q<Label>("health-text");
        expBarFill = root.Q("exp-bar-fill");
        levelText = root.Q<Label>("level-text");
        killCountText = root.Q<Label>("kills-text");
        timerText = root.Q<Label>("timer-text");
        goldText = root.Q<Label>("gold-text");

        playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealth;
            playerStats.OnExpChanged += UpdateExp;
        }
    }

    private void Update()
    {
        if (playerStats == null) return;

        if (killCountText != null)
            killCountText.text = $"KILLS: {playerStats.enemiesKilled}";

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(playerStats.survivalTime / 60f);
            int seconds = Mathf.FloorToInt(playerStats.survivalTime % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        if (goldText != null)
            goldText.text = $"GOLD: {playerStats.gold}";
    }

    private void UpdateHealth(float current, float max)
    {
        if (healthBarFill != null)
            healthBarFill.style.width = new StyleLength(new Length(current / max * 100f, LengthUnit.Percent));
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void UpdateExp(int current, int toNext, int level)
    {
        if (expBarFill != null)
            expBarFill.style.width = new StyleLength(new Length((float)current / toNext * 100f, LengthUnit.Percent));
        if (levelText != null)
            levelText.text = $"Lv.{level}";
    }

    private void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealth;
            playerStats.OnExpChanged -= UpdateExp;
        }
    }
}
