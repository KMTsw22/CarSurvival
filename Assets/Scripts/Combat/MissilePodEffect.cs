using UnityEngine;

/// <summary>
/// 미사일 포드: 가장 가까운 적들에게 유도 미사일 발사.
/// etc1=기본미사일수, etc2=레벨당미사일추가
/// </summary>
public class MissilePodEffect : MonoBehaviour
{
    public static void FireMissiles(Transform origin, int count, float dmg, float speed, GameObject bulletPrefab)
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) return;

        // 거리순 정렬
        System.Array.Sort(enemies, (a, b) =>
        {
            float da = Vector3.Distance(a.transform.position, origin.position);
            float db = Vector3.Distance(b.transform.position, origin.position);
            return da.CompareTo(db);
        });

        int actualCount = Mathf.Min(count, enemies.Length);
        for (int i = 0; i < actualCount; i++)
        {
            var target = enemies[i];
            if (target == null) continue;

            // 미사일 생성 — 약간의 각도 오프셋으로 퍼지며 발사
            float angleOffset = (i - count / 2f) * 15f;
            Vector2 baseDir = ((Vector2)target.transform.position - (Vector2)origin.position).normalized;
            float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
            float finalAngle = (baseAngle + angleOffset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(finalAngle), Mathf.Sin(finalAngle));

            float rot = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
            Vector3 spawnPos = origin.position + (Vector3)(dir * 0.5f);

            GameObject missile;
            if (bulletPrefab != null)
            {
                missile = Object.Instantiate(bulletPrefab, spawnPos, Quaternion.Euler(0, 0, rot));
                missile.SetActive(true);
                var sr = missile.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    var missileSprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_Missile_Pod");
                    if (missileSprite != null)
                    {
                        sr.sprite = missileSprite;
                        sr.color = Color.white;
                    }
                    else
                    {
                        sr.color = new Color(1f, 0.4f, 0.2f);
                    }
                    missile.transform.localScale = new Vector3(0.2f, 0.4f, 1f);
                }
            }
            else
            {
                missile = new GameObject($"Missile_{i}");
                missile.transform.position = spawnPos;
                missile.transform.rotation = Quaternion.Euler(0, 0, rot);
                missile.tag = "PlayerProjectile";

                var sr = missile.AddComponent<SpriteRenderer>();
                var missileSprite = Resources.Load<Sprite>("Sprites/Icons/SkillEffect/EFT_Missile_Pod");
                if (missileSprite != null)
                {
                    sr.sprite = missileSprite;
                    sr.color = Color.white;
                }
                else
                {
                    var tex = new Texture2D(4, 4);
                    for (int px = 0; px < 4; px++)
                        for (int py = 0; py < 4; py++)
                            tex.SetPixel(px, py, Color.white);
                    tex.Apply();
                    tex.filterMode = FilterMode.Point;
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                    sr.color = new Color(1f, 0.4f, 0.2f);
                }
                sr.sortingOrder = 5;
                missile.transform.localScale = new Vector3(0.2f, 0.4f, 1f);

                var col = missile.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(1f, 1f);

                missile.AddComponent<Bullet>();
            }

            var bullet = missile.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(dir, speed, dmg, 4f);
                bullet.SetHoming(target.transform);
            }
        }
    }
}
