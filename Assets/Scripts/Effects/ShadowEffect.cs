using UnityEngine;

/// <summary>
/// 캐릭터 발 아래에 동그란 그림자를 생성.
/// 접지감을 줘서 둥둥 뜨는 느낌 제거.
/// </summary>
public class ShadowEffect : MonoBehaviour
{
    public float shadowScale = 0.6f;
    public float shadowAlpha = 0.3f;
    public float yOffset = -0.3f;

    private void Start()
    {
        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0, yOffset, 0);

        var sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite = CreateOvalSprite();
        sr.color = new Color(0, 0, 0, shadowAlpha);
        sr.sortingOrder = -1;
        shadow.transform.localScale = new Vector3(shadowScale, shadowScale * 0.4f, 1f);
    }

    private Sprite CreateOvalSprite()
    {
        int size = 32;
        var tex = new Texture2D(size, size);
        float cx = size / 2f, cy = size / 2f;
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dx = (x - cx) / cx;
                float dy = (y - cy) / cy;
                float dist = dx * dx + dy * dy;
                float alpha = dist < 1f ? (1f - dist) : 0f;
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
