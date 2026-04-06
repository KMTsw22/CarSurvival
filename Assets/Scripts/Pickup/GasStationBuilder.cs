using UnityEngine;

/// <summary>
/// 기름통 획득 후 바닥에 시계를 표시하고, 시간이 지나면 주유소를 생성한다.
/// SpriteRenderer로 테두리를 표시하고, 시계 침 역할의 좁은 파이를 회전시켜 진행도를 보여준다.
/// </summary>
public class GasStationBuilder : MonoBehaviour
{
    public float buildTime = 10f;
    public Sprite clockBorderSprite;
    public Sprite clockInnerSprite;
    public Sprite gasStationSprite;

    private SpriteRenderer borderRenderer;
    private SpriteRenderer handRenderer;
    private float elapsed;

    private const float clockScale = 0.35f;

    private void Start()
    {
        // 테두리 링 (고정)
        var borderObj = new GameObject("ClockBorder");
        borderObj.transform.SetParent(transform);
        borderObj.transform.localPosition = Vector3.zero;
        borderRenderer = borderObj.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = clockBorderSprite;
        borderRenderer.sortingOrder = 5;
        borderObj.transform.localScale = Vector3.one * clockScale;

        // 채움 원 (시계 채워지는 효과 — 스케일로 표현)
        var fillObj = new GameObject("ClockFill");
        fillObj.transform.SetParent(transform);
        fillObj.transform.localPosition = Vector3.zero;
        handRenderer = fillObj.AddComponent<SpriteRenderer>();
        handRenderer.sprite = clockInnerSprite;
        handRenderer.sortingOrder = 4;
        handRenderer.color = new Color(1f, 1f, 0.3f, 0.7f);
        fillObj.transform.localScale = Vector3.zero;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / buildTime);

        // 안쪽 원이 점점 커지면서 채워지는 효과 (테두리 안쪽까지만)
        if (handRenderer != null)
        {
            float s = clockScale * 0.75f * progress;
            handRenderer.transform.localScale = new Vector3(s, s, 1f);
            handRenderer.transform.localRotation = Quaternion.Euler(0, 0, -360f * progress);
        }

        // 테두리 펄스 효과
        if (borderRenderer != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.03f;
            borderRenderer.transform.localScale = Vector3.one * clockScale * pulse;
        }

        if (progress >= 1f)
        {
            SpawnGasStation();
            Destroy(gameObject);
        }
    }

    private void SpawnGasStation()
    {
        var station = new GameObject("GasStation");
        station.transform.position = transform.position;

        var sr = station.AddComponent<SpriteRenderer>();
        sr.sprite = gasStationSprite;
        sr.sortingOrder = 1;
        station.transform.localScale = Vector3.one * 0.7f;

        var col = station.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 2f;

        var gs = station.AddComponent<GasStation>();
        gs.healPerSecond = 8f;
        gs.duration = 15f;
        gs.healRadius = 6f;
    }
}
