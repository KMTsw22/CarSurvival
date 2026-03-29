using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 타이틀 화면 - 전체 화면 타이틀 이미지 + "Tap to Start" 텍스트
/// 화면 아무 곳이나 터치하면 다음 씬으로 전환
/// </summary>
[ExecuteInEditMode]
public class TitleScreenBootstrap : MonoBehaviour
{
    [Header("다음 씬 이름")]
    public string nextSceneName = "MainLobby";

    private bool isTransitioning = false;
    private CanvasGroup fadeGroup;
    private TextMeshProUGUI tapText;
    private float blinkTimer;

    private void Awake()
    {
        GenerateUI();
    }

    public void GenerateUI()
    {
        var existing = GameObject.Find("TitleCanvas");
        if (existing != null)
            DestroyImmediate(existing);

        // Canvas
        var canvasObj = new GameObject("TitleCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // 페이드용 CanvasGroup
        fadeGroup = canvasObj.AddComponent<CanvasGroup>();

        // EventSystem
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 배경 (검정)
        var bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgRT = bgObj.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.sizeDelta = Vector2.zero;
        bgObj.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f);

        // 타이틀 이미지
        var titleSprite = Resources.Load<Sprite>("Sprites/UI/TitleScreen");
        if (titleSprite != null)
        {
            var imgObj = new GameObject("TitleImage");
            imgObj.transform.SetParent(canvasObj.transform, false);
            var imgRT = imgObj.AddComponent<RectTransform>();
            // 화면 가로폭에 맞추고, 비율 유지, 중앙보다 살짝 위에 배치
            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.sizeDelta = Vector2.zero;
            imgRT.anchoredPosition = Vector2.zero;
            var img = imgObj.AddComponent<Image>();
            img.sprite = titleSprite;
            img.preserveAspect = false;
            img.raycastTarget = false;
        }

        // "Tap to Start" 텍스트
        var tapObj = new GameObject("TapText");
        tapObj.transform.SetParent(canvasObj.transform, false);
        var tapRT = tapObj.AddComponent<RectTransform>();
        tapRT.anchorMin = new Vector2(0.5f, 0.2f);
        tapRT.anchorMax = new Vector2(0.5f, 0.2f);
        tapRT.sizeDelta = new Vector2(600, 80);
        tapRT.anchoredPosition = Vector2.zero;

        tapText = tapObj.AddComponent<TextMeshProUGUI>();
        tapText.text = "TAP TO START";
        tapText.fontSize = 42;
        tapText.alignment = TextAlignmentOptions.Center;
        tapText.color = Color.white;
        tapText.fontStyle = FontStyles.Bold;
        tapText.raycastTarget = false;

        // 전체 화면 투명 버튼 (터치 감지용)
        var touchObj = new GameObject("TouchArea");
        touchObj.transform.SetParent(canvasObj.transform, false);
        var touchRT = touchObj.AddComponent<RectTransform>();
        touchRT.anchorMin = Vector2.zero;
        touchRT.anchorMax = Vector2.one;
        touchRT.sizeDelta = Vector2.zero;
        var touchImg = touchObj.AddComponent<Image>();
        touchImg.color = Color.clear;
        var touchBtn = touchObj.AddComponent<Button>();
        touchBtn.transition = Selectable.Transition.None;
        touchBtn.onClick.AddListener(OnTap);
    }

    private void Update()
    {
        // 텍스트 깜빡임
        if (tapText != null && !isTransitioning)
        {
            blinkTimer += Time.deltaTime;
            float alpha = (Mathf.Sin(blinkTimer * 2.5f) + 1f) / 2f;
            alpha = Mathf.Lerp(0.3f, 1f, alpha);
            tapText.color = new Color(1f, 1f, 1f, alpha);
        }

        // 페이드 아웃 진행
        if (isTransitioning)
        {
            fadeGroup.alpha -= Time.deltaTime * 1.5f;
            if (fadeGroup.alpha <= 0f)
                SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnTap()
    {
        if (isTransitioning) return;
        isTransitioning = true;
    }
}
