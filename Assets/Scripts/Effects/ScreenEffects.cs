using UnityEngine;
using System.Collections;

/// <summary>
/// 히트스톱(타격 시 짧은 프레임 정지) + 저체력 화면 경고 효과
/// GameBootstrap 또는 카메라에 붙여서 사용
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }

    private Coroutine hitStopCoroutine;

    // 저체력 경고
    private SpriteRenderer dangerVignette;
    private float dangerPulseTimer;
    private bool isDangerActive;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CreateDangerVignette();

        // 체력 변경 이벤트 구독
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged += OnHealthChanged;
    }

    private void OnDestroy()
    {
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnHealthChanged -= OnHealthChanged;
    }

    /// <summary>히트스톱: duration초 동안 게임 정지 (타격감 강화)</summary>
    public void HitStop(float duration)
    {
        if (hitStopCoroutine != null)
            StopCoroutine(hitStopCoroutine);
        hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        // LevelUp 등 다른 시스템이 timeScale을 바꿨을 수 있으므로 체크
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            Time.timeScale = 1f;
        hitStopCoroutine = null;
    }

    // ── 저체력 경고 (빨간 화면 깜빡임) ──

    private void OnHealthChanged(float current, float max)
    {
        float ratio = current / max;
        isDangerActive = ratio > 0f && ratio <= 0.3f;
        if (!isDangerActive && dangerVignette != null)
            dangerVignette.color = Color.clear;
    }

    private void Update()
    {
        if (!isDangerActive || dangerVignette == null) return;

        dangerPulseTimer += Time.deltaTime * 3f; // 펄스 속도
        float alpha = Mathf.Abs(Mathf.Sin(dangerPulseTimer)) * 0.25f; // 최대 25% 불투명
        dangerVignette.color = new Color(1f, 0f, 0f, alpha);

        // 카메라 따라가기
        var cam = Camera.main;
        if (cam != null)
            dangerVignette.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
    }

    private void CreateDangerVignette()
    {
        var go = new GameObject("DangerVignette");
        go.transform.SetParent(transform);
        dangerVignette = go.AddComponent<SpriteRenderer>();
        dangerVignette.sortingOrder = 999;

        // 큰 사각형 스프라이트 생성
        int size = 4;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        dangerVignette.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f);
        dangerVignette.transform.localScale = Vector3.one * 50f; // 화면 전체 덮기
        dangerVignette.color = Color.clear;
    }
}
