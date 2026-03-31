using UnityEngine;

/// <summary>
/// 몬스터에 붙이면 걸어다니는 느낌의 통통 바운스.
/// Y 오프셋만 가감하여 위치를 덮어쓰지 않음.
/// </summary>
public class BounceEffect : MonoBehaviour
{
    public float bounceSpeed = 8f;
    public float bounceHeight = 0.08f;

    private float timer;
    private float currentBounceOffset;
    private Vector3 lastPos;

    private void Start()
    {
        lastPos = transform.position;
    }

    private void LateUpdate()
    {
        // 이전 바운스 오프셋 제거
        var pos = transform.position;
        pos.y -= currentBounceOffset;

        bool isMoving = (pos - lastPos).sqrMagnitude > 0.0001f;
        lastPos = pos;

        if (isMoving)
        {
            timer += Time.deltaTime * bounceSpeed;
            currentBounceOffset = Mathf.Abs(Mathf.Sin(timer)) * bounceHeight;
        }
        else
        {
            timer = 0f;
            currentBounceOffset = 0f;
        }

        // 새 바운스 오프셋 적용
        pos.y += currentBounceOffset;
        transform.position = pos;
    }
}
