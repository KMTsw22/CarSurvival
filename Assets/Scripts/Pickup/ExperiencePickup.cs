using UnityEngine;

public class ExperiencePickup : MonoBehaviour
{
    public int expAmount = 1;
    public float magnetRange = 3f;
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
                    playerStats.AddExperience(expAmount);
                }
                Destroy(gameObject);
            }
        }
    }
}
