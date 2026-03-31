using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Health")]
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    [Header("Experience")]
    public Slider expBar;
    public TextMeshProUGUI levelText;

    [Header("Stats")]
    public TextMeshProUGUI killCountText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI goldText;

    private PlayerStats playerStats;

    private void Start()
    {
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
        if (healthBar != null)
            healthBar.value = current / max;
        if (healthText != null)
            healthText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }

    private void UpdateExp(int current, int toNext, int level)
    {
        if (expBar != null)
            expBar.value = (float)current / toNext;
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
