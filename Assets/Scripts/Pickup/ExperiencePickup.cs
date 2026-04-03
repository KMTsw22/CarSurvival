using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int expAmount = 1;
    public float baseMagnetRange = 3f;
    public float magnetSpeed = 10f;
    public float collectRange = 1f;

    private Transform player;
    private PlayerStats playerStats;
    private bool isMagneted;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        playerStats = PlayerStats.Instance;

        // Auto-destroy after 30 seconds
        Destroy(gameObject, 30f);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // 마그넷 보너스 적용
        float magnetRange = baseMagnetRange * (1f + (playerStats != null ? playerStats.magnetBonusPercent : 0f) / 100f);

        if (dist < magnetRange)
        {
            isMagneted = true;
        }

        if (isMagneted)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                magnetSpeed * Time.deltaTime);

            if (dist < collectRange)
            {
                if (playerStats != null)
                {
                    // EXP 보너스 적용
                    int finalExp = Mathf.RoundToInt(expAmount * (1f + playerStats.expBonusPercent / 100f));
                    playerStats.AddExperience(finalExp);
                }
                Destroy(gameObject);
            }
        }
    }
}
