using UnityEngine;
using UnityEngine.UIElements;
using System;

public class MainLobbyUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Button startBtn;
    private Button settingBtn;
    private Button paintBtn;
    private Button upgradeBtn;

    private float bounceTimer;
    private const float bounceCycle = 1.5f;
    private bool isHovering;
    private bool isPressed;

    public event Action OnStartGameClicked;
    public event Action OnSettingClicked;
    public event Action OnPaintClicked;
    public event Action OnUpgradeClicked;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        startBtn = root.Q<Button>("start__btn");
        settingBtn = root.Q<Button>("setting-btn");
        paintBtn = root.Q<Button>("paint-btn");
        upgradeBtn = root.Q<Button>("upgrade-btn");

        // 자식 VisualElement가 이벤트를 가로채지 않도록 설정
        var startBg = startBtn?.Q(className: "start-btn-bg");
        if (startBg != null) startBg.pickingMode = PickingMode.Ignore;

        settingBtn?.RegisterCallback<ClickEvent>(evt => OnSettingClicked?.Invoke());
        paintBtn?.RegisterCallback<ClickEvent>(evt => OnPaintClicked?.Invoke());
        upgradeBtn?.RegisterCallback<ClickEvent>(evt => OnUpgradeClicked?.Invoke());

        startBtn?.RegisterCallback<MouseEnterEvent>(evt => isHovering = true);
        startBtn?.RegisterCallback<MouseLeaveEvent>(evt => { isHovering = false; isPressed = false; });
        startBtn?.RegisterCallback<PointerDownEvent>(evt => isPressed = true);
        startBtn?.RegisterCallback<PointerUpEvent>(evt => isPressed = false);
        startBtn?.RegisterCallback<ClickEvent>(evt => OnStartGameClicked?.Invoke());
    }

    void Update()
    {
        if (startBtn == null) return;

        if (isPressed)
        {
            startBtn.style.scale = new StyleScale(new Scale(new Vector3(0.92f, 0.92f, 1f)));
        }
        else if (isHovering)
        {
            startBtn.style.scale = new StyleScale(new Scale(new Vector3(1.08f, 1.08f, 1f)));
        }
        else
        {
            bounceTimer += Time.deltaTime;
            float t = (Mathf.Sin(bounceTimer / bounceCycle * Mathf.PI * 2f) + 1f) * 0.5f;
            float s = Mathf.Lerp(1f, 1.02f, t);
            startBtn.style.scale = new StyleScale(new Scale(new Vector3(s, s, 1f)));
        }
    }
}
