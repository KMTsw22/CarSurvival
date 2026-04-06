using UnityEngine;

/// <summary>
/// 3x3 타일을 플레이어 기준 그리드 스냅으로 배치하여 무한 배경 구현.
/// </summary>
public class InfiniteBackground : MonoBehaviour
{
    private Transform player;
    private const int gridSize = 9;
    private GameObject[,] tiles = new GameObject[gridSize, gridSize];
    private float tileSize;

    public void Init()
    {
        var mapSprite = Resources.Load<Sprite>("Sprites/Maps/map_1");
        Sprite fallbackSprite = null;

        if (mapSprite != null)
        {
            tileSize = mapSprite.bounds.size.x;
        }
        else
        {
            tileSize = 6f;
            var tex = new Texture2D(4, 4);
            for (int px = 0; px < 4; px++)
                for (int py = 0; py < 4; py++)
                    tex.SetPixel(px, py, new Color(0.15f, 0.15f, 0.2f));
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            fallbackSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        }

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                var tile = new GameObject($"BG_{x}_{y}");
                tile.transform.SetParent(transform);

                var sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = mapSprite != null ? mapSprite : fallbackSprite;
                sr.sortingOrder = -10;

                if (mapSprite == null)
                    tile.transform.localScale = new Vector3(tileSize, tileSize, 1);

                tiles[x, y] = tile;
            }
        }

        UpdateTilePositions(Vector3.zero);
    }

    private void Update()
    {
        if (player == null)
        {
            var ps = PlayerStats.Instance;
            if (ps != null) player = ps.transform;
            return;
        }

        UpdateTilePositions(player.position);
    }

    private void UpdateTilePositions(Vector3 center)
    {
        // 플레이어 위치를 타일 크기로 스냅 → 그리드 중심
        float snapX = Mathf.Round(center.x / tileSize) * tileSize;
        float snapY = Mathf.Round(center.y / tileSize) * tileSize;

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                int half = gridSize / 2;
                float posX = snapX + (x - half) * tileSize;
                float posY = snapY + (y - half) * tileSize;
                tiles[x, y].transform.position = new Vector3(posX, posY, 0);
            }
        }
    }
}
