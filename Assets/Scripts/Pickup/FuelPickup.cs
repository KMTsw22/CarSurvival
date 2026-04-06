using UnityEngine;

public class FuelPickup : MonoBehaviour
{
    public float baseMagnetRange = 3f;
    public float magnetSpeed = 10f;
    public float collectRange = 1f;
    public float buildTime = 10f;

    // 스프라이트 참조 (GameBootstrap에서 설정)
    public Sprite clockBorderSprite;
    public Sprite clockInnerSprite;
    public Sprite gasStationSprite;

    private Transform player;
    private PlayerStats playerStats;
    private bool isMagneted;
    private Vector3 dropPosition;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        playerStats = PlayerStats.Instance;
        dropPosition = transform.position;
        Destroy(gameObject, 30f);
    }

    private void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        float magnetRange = baseMagnetRange * (1f + (playerStats != null ? playerStats.magnetBonusPercent : 0f) / 100f);

        if (dist < magnetRange)
            isMagneted = true;

        if (isMagneted)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                magnetSpeed * Time.deltaTime);

            if (dist < collectRange)
            {
                // 드롭된 위치에 타이머 생성
                SpawnBuildTimer(dropPosition);
                Destroy(gameObject);
            }
        }
    }

    private void SpawnBuildTimer(Vector3 pos)
    {
        var timer = new GameObject("GasStationTimer");
        timer.transform.position = pos;

        var builder = timer.AddComponent<GasStationBuilder>();
        builder.buildTime = buildTime;
        builder.clockBorderSprite = clockBorderSprite;
        builder.clockInnerSprite = clockInnerSprite;
        builder.gasStationSprite = gasStationSprite;
    }
}
