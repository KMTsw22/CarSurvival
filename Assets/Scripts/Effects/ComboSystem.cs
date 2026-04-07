using UnityEngine;

/// <summary>
/// 연속 킬 콤보 시스템. 적 처치 시 콤보 카운트 증가, 일정 시간 내 추가 킬 없으면 리셋.
/// 화면 중앙 상단에 콤보 텍스트 표시.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }

    private int comboCount;
    private float comboTimer;
    private const float comboTimeout = 2.5f; // 2.5초 안에 다음 킬이 없으면 리셋

    // 콤보 텍스트 표시용
    private GUIStyle comboStyle;
    private float displayAlpha = 0f;
    private float displayScale = 1f;
    private float punchTimer; // 숫자가 커지는 펀치 효과

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>적 처치 시 호출</summary>
    public void RegisterKill()
    {
        comboCount++;
        comboTimer = comboTimeout;
        displayAlpha = 1f;

        // 펀치 효과: 콤보 올라갈 때 크기가 확 커졌다 줄어듦
        punchTimer = 0.2f;
        displayScale = 1.5f;

        // 높은 콤보 시 추가 이펙트
        if (comboCount >= 10 && CameraFollow.Instance != null)
            CameraFollow.Instance.Shake(0.3f, 0.1f);
    }

    private void Update()
    {
        if (comboCount <= 0) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            // 콤보 종료 — 페이드아웃
            displayAlpha -= Time.deltaTime * 3f;
            if (displayAlpha <= 0f)
            {
                comboCount = 0;
                displayAlpha = 0f;
            }
        }

        // 펀치 스케일 복귀
        if (punchTimer > 0f)
        {
            punchTimer -= Time.deltaTime;
            displayScale = Mathf.Lerp(1f, 1.5f, punchTimer / 0.2f);
        }
        else
        {
            displayScale = 1f;
        }
    }

    private void OnGUI()
    {
        if (comboCount < 2 || displayAlpha <= 0f) return;

        if (comboStyle == null)
        {
            comboStyle = new GUIStyle(GUI.skin.label);
            comboStyle.alignment = TextAnchor.MiddleCenter;
            comboStyle.fontStyle = FontStyle.Bold;
        }

        // 콤보 수에 따라 색상 변화
        Color comboColor;
        if (comboCount >= 30)
            comboColor = new Color(1f, 0.1f, 0.1f); // 빨강
        else if (comboCount >= 20)
            comboColor = new Color(1f, 0.3f, 0f);   // 주황
        else if (comboCount >= 10)
            comboColor = new Color(1f, 0.8f, 0f);   // 금색
        else if (comboCount >= 5)
            comboColor = new Color(1f, 1f, 0f);     // 노랑
        else
            comboColor = Color.white;

        comboColor.a = displayAlpha;

        // 폰트 크기: 콤보 높을수록 커짐
        int baseFontSize = Mathf.Min(40 + comboCount * 2, 80);
        int fontSize = Mathf.RoundToInt(baseFontSize * displayScale);
        comboStyle.fontSize = fontSize;

        // 외곽선 (검정 테두리)
        Color outlineColor = new Color(0, 0, 0, displayAlpha * 0.8f);
        comboStyle.normal.textColor = outlineColor;

        float x = Screen.width / 2f;
        float y = Screen.height * 0.18f;
        Rect rect = new Rect(x - 200, y - 40, 400, 80);

        string text = $"{comboCount} COMBO!";
        // 높은 콤보 강조
        if (comboCount >= 20) text = $"{comboCount} COMBO!!!";
        else if (comboCount >= 10) text = $"{comboCount} COMBO!!";

        // 외곽선 그리기 (4방향)
        Rect outlineRect = rect;
        outlineRect.x -= 2; GUI.Label(outlineRect, text, comboStyle);
        outlineRect.x += 4; GUI.Label(outlineRect, text, comboStyle);
        outlineRect.x -= 2; outlineRect.y -= 2; GUI.Label(outlineRect, text, comboStyle);
        outlineRect.y += 4; GUI.Label(outlineRect, text, comboStyle);

        // 본문
        comboStyle.normal.textColor = comboColor;
        GUI.Label(rect, text, comboStyle);
    }
}
