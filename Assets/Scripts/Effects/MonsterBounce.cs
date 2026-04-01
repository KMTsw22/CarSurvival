using UnityEngine;

/// <summary>
/// 몬스터가 이동할 때 통통 튀는 바운스 효과.
/// 스프라이트를 자식 오브젝트로 분리하여 본체 이동에 영향을 주지 않음.
/// </summary>
public class MonsterBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceSpeed = 12f;
    public float bounceHeight = 0.25f;
    public float squashAmount = 0.15f;

    private Transform spriteChild;
    private Vector3 baseScale;
    private float bounceTimer;
    private Vector3 lastPosition;

    private void Start()
    {
        baseScale = transform.localScale;
        lastPosition = transform.position;

        // SpriteRenderer를 자식 오브젝트로 분리
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var childObj = new GameObject("SpriteVisual");
            childObj.transform.SetParent(transform, false);
            childObj.transform.localPosition = Vector3.zero;
            childObj.transform.localScale = Vector3.one;

            var childSR = childObj.AddComponent<SpriteRenderer>();
            childSR.sprite = sr.sprite;
            childSR.color = sr.color;
            childSR.sortingOrder = sr.sortingOrder;
            childSR.flipX = sr.flipX;
            childSR.material = sr.material;

            // 원본 SpriteRenderer 비활성화
            sr.enabled = false;

            spriteChild = childObj.transform;

            // EnemyAI의 flipX를 자식에 동기화하기 위한 참조 저장
            var sync = gameObject.AddComponent<SpriteFlipSync>();
            sync.source = sr;
            sync.target = childSR;
        }
    }

    private void Update()
    {
        if (spriteChild == null) return;

        if (Time.deltaTime <= 0f) return;
        float speed = (transform.position - lastPosition).magnitude / Time.deltaTime;
        lastPosition = transform.position;

        if (speed < 0.1f)
        {
            // 정지 시 부드럽게 복귀
            spriteChild.localPosition = Vector3.Lerp(spriteChild.localPosition, Vector3.zero, Time.deltaTime * 8f);
            spriteChild.localScale = Vector3.Lerp(spriteChild.localScale, Vector3.one, Time.deltaTime * 8f);
            return;
        }

        bounceTimer += Time.deltaTime * (bounceSpeed + speed * 0.5f);

        float bounce01 = Mathf.Abs(Mathf.Sin(bounceTimer * Mathf.PI));

        // 스쿼시 & 스트레치 (자식 로컬 스케일)
        float stretchY = 1f + squashAmount * bounce01;
        float squashX = 1f - squashAmount * bounce01 * 0.5f;
        spriteChild.localScale = new Vector3(squashX, stretchY, 1f);

        // Y 오프셋 (자식만 위로 통통)
        float offsetY = bounce01 * bounceHeight;
        spriteChild.localPosition = new Vector3(0, offsetY, 0);
    }

    public void RefreshBaseScale()
    {
        baseScale = transform.localScale;
    }
}

/// <summary>
/// EnemyAI가 원본 SR의 flipX를 바꿀 때 자식 SR에 동기화.
/// </summary>
public class SpriteFlipSync : MonoBehaviour
{
    public SpriteRenderer source;
    public SpriteRenderer target;

    private void LateUpdate()
    {
        if (source != null && target != null)
            target.flipX = source.flipX;
    }
}
