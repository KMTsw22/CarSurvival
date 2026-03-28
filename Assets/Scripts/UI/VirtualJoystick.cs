using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("References")]
    public RectTransform background;
    public RectTransform handle;

    [Header("Settings")]
    public float handleRange = 50f;

    private Vector2 inputVector = Vector2.zero;
    private CarController carController;
    private Canvas canvas;

    private void Start()
    {
        carController = FindAnyObjectByType<CarController>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, eventData.position, eventData.pressEventCamera, out pos);

        pos.x /= background.sizeDelta.x;
        pos.y /= background.sizeDelta.y;

        inputVector = new Vector2(pos.x * 2, pos.y * 2);
        inputVector = inputVector.magnitude > 1f ? inputVector.normalized : inputVector;

        handle.anchoredPosition = inputVector * handleRange;

        if (carController != null)
        {
            carController.SetMoveInput(inputVector);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        if (carController != null)
        {
            carController.SetMoveInput(Vector2.zero);
        }
    }
}
