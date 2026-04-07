using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    public Transform target;
    public float smoothSpeed = 5f;
    public Vector3 offset = new Vector3(0, 0, -10f);

    private Camera cam;

    // 아레나 카메라 클램핑
    private bool hasBounds;
    private Vector2 boundsCenter;
    private float boundsHalfW;
    private float boundsHalfH;

    // Screen Shake
    private float shakeDuration;
    private float shakeMagnitude;
    private float shakeDecay;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    /// <summary>화면 흔들림. intensity가 클수록 강하게, duration 동안 지속</summary>
    public void Shake(float intensity, float duration)
    {
        if (intensity > shakeMagnitude) // 더 강한 흔들림만 덮어씌움
        {
            shakeMagnitude = intensity;
            shakeDuration = duration;
            shakeDecay = intensity / duration;
        }
    }

    /// <summary>보스전 아레나 범위 설정 — 카메라가 이 범위 밖을 비추지 않음</summary>
    public void SetBounds(Vector2 center, float halfW, float halfH)
    {
        hasBounds = true;
        boundsCenter = center;
        boundsHalfW = halfW;
        boundsHalfH = halfH;
    }

    /// <summary>아레나 범위 해제</summary>
    public void ClearBounds()
    {
        hasBounds = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        // Screen Shake 적용
        if (shakeDuration > 0f)
        {
            Vector2 shakeOffset = Random.insideUnitCircle * shakeMagnitude;
            transform.position += (Vector3)shakeOffset;
            shakeMagnitude -= shakeDecay * Time.unscaledDeltaTime;
            shakeDuration -= Time.unscaledDeltaTime;
            if (shakeDuration <= 0f)
                shakeMagnitude = 0f;
        }

        // 아레나 범위 내로 카메라 클램핑
        if (hasBounds && cam != null)
        {
            float camH = cam.orthographicSize;
            float camW = camH * cam.aspect;

            Vector3 pos = transform.position;

            // 카메라 뷰가 아레나보다 크면 중심 고정
            if (camW >= boundsHalfW)
                pos.x = boundsCenter.x;
            else
                pos.x = Mathf.Clamp(pos.x, boundsCenter.x - boundsHalfW + camW, boundsCenter.x + boundsHalfW - camW);

            if (camH >= boundsHalfH)
                pos.y = boundsCenter.y;
            else
                pos.y = Mathf.Clamp(pos.y, boundsCenter.y - boundsHalfH + camH, boundsCenter.y + boundsHalfH - camH);

            transform.position = pos;
        }
    }
}
