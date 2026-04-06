using UnityEngine;

/// <summary>
/// 주유소: 일정 시간 동안 범위 내 플레이어를 초당 healPerSecond만큼 회복시킨다.
/// duration이 지나면 자동 파괴.
/// </summary>
public class GasStation : MonoBehaviour
{
    public float healPerSecond = 8f;
    public float duration = 15f;
    public float healRadius = 3f;

    private Transform player;
    private PlayerStats playerStats;
    private float elapsed;

    // 힐 범위 표시용
    private SpriteRenderer rangeIndicator;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        playerStats = PlayerStats.Instance;

        // 힐 범위 원 표시
        var rangeObj = new GameObject("HealRange");
        rangeObj.transform.SetParent(transform);
        rangeObj.transform.localPosition = Vector3.zero;
        rangeIndicator = rangeObj.AddComponent<SpriteRenderer>();
        rangeIndicator.sprite = CreateCircleSprite();
        rangeIndicator.color = new Color(0.2f, 0.9f, 0.3f, 0.15f);
        rangeIndicator.sortingOrder = 0;
        float diameter = healRadius * 2f;
        rangeObj.transform.localScale = new Vector3(diameter, diameter, 1f);
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        elapsed += Time.deltaTime;

        // 남은 시간에 따라 알파 깜빡임 (마지막 3초)
        float remaining = duration - elapsed;
        if (remaining <= 3f && rangeIndicator != null)
        {
            float blink = Mathf.PingPong(Time.time * 4f, 1f);
            rangeIndicator.color = new Color(0.2f, 0.9f, 0.3f, 0.05f + 0.15f * blink);
        }

        // 범위 내 플레이어 회복
        if (player != null && playerStats != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= healRadius)
            {
                playerStats.Heal(healPerSecond * Time.deltaTime);
            }
        }

        if (elapsed >= duration)
            Destroy(gameObject);
    }

    private Sprite CreateCircleSprite()
    {
        int size = 64;
        var tex = new Texture2D(size, size);
        float center = size / 2f;
        float radius = size / 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
