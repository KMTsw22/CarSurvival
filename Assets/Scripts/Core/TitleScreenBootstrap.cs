using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀 화면 — UI Toolkit 기반
/// 화면 아무 곳이나 클릭하면 다음 씬으로 전환
/// </summary>
public class TitleScreenBootstrap : MonoBehaviour
{
    [Header("다음 씬 이름")]
    public string nextSceneName = "MainLobby";

    private bool isTransitioning = false;
    private UIDocument uiDocument;
    private VisualElement root;
    private Label tapText;
    private float blinkTimer;

    private void Start()
    {
        // UIDocument가 없으면 생성
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            ps.match = 0.5f;
            ps.sortingOrder = 100;
            uiDocument.panelSettings = ps;

            var uxml = Resources.Load<VisualTreeAsset>("Sprites/UI/OutGame/TitleScreen/TitleScreen");
            if (uxml != null) uiDocument.visualTreeAsset = uxml;
        }

        root = uiDocument.rootVisualElement;
        tapText = root.Q<Label>("tap-text");

        // 타이틀 이미지 로드
        var titleSprite = Resources.Load<Sprite>("Sprites/UI/OutGame/TitleScreen");
        var titleImage = root.Q("title-image");
        if (titleSprite != null && titleImage != null)
            titleImage.style.backgroundImage = new StyleBackground(titleSprite);

        // 클릭 감지
        root.RegisterCallback<ClickEvent>(evt => OnTap());
    }

    private void Update()
    {
        if (tapText == null) return;

        // 텍스트 깜빡임
        if (!isTransitioning)
        {
            blinkTimer += Time.deltaTime;
            float alpha = (Mathf.Sin(blinkTimer * 2.5f) + 1f) / 2f;
            alpha = Mathf.Lerp(0.3f, 1f, alpha);
            tapText.style.opacity = alpha;
        }

        // 페이드 아웃
        if (isTransitioning && root != null)
        {
            float current = root.resolvedStyle.opacity;
            float next = current - Time.deltaTime * 1.5f;
            root.style.opacity = next;
            if (next <= 0f)
                SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnTap()
    {
        if (isTransitioning) return;
        isTransitioning = true;
    }
}
