using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 공구상자 드롭 아이템. 신호등 몬스터가 죽으면 드롭.
/// 빙글빙글 회전 + 빛나는 효과.
/// 플레이어가 픽업하면 보유 스킬 중 랜덤 하나 레벨업.
/// </summary>
public class ToolboxPickup : MonoBehaviour
{
    public float baseMagnetRange = 3f;
    public float magnetSpeed = 10f;
    public float collectRange = 1f;

    private Transform player;
    private PlayerStats playerStats;
    private bool isMagneted;
    private SpriteRenderer sr;
    private SpriteRenderer glowSr;
    private float glowTimer;

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        playerStats = PlayerStats.Instance;
        sr = GetComponent<SpriteRenderer>();

        // 빛나는 글로우 효과 (자식)
        var glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = Vector3.zero;
        glowObj.transform.localScale = Vector3.one * 1.8f;

        glowSr = glowObj.AddComponent<SpriteRenderer>();
        glowSr.sprite = sr.sprite;
        glowSr.color = new Color(1f, 0.9f, 0.5f, 0.2f);
        glowSr.sortingOrder = sr.sortingOrder - 1;

        // 소멸 없음 — 플레이어가 먹을 때까지 유지
    }

    private void Update()
    {
        if (player == null) return;

        // 게임이 일시정지 중이면 수집하지 않음 (레벨업 UI 등과 충돌 방지)
        if (Time.timeScale == 0f) return;

        // 빙글빙글 회전
        transform.Rotate(0, 0, 60f * Time.deltaTime);

        // 글로우 펄스
        glowTimer += Time.deltaTime;
        float glowAlpha = 0.15f + Mathf.PingPong(glowTimer * 0.8f, 0.15f);
        float glowScale = 1.6f + Mathf.PingPong(glowTimer * 0.5f, 0.4f);
        glowSr.color = new Color(1f, 0.9f, 0.5f, glowAlpha);
        glowSr.transform.localScale = Vector3.one * glowScale;

        // 자석 + 수집
        float dist = Vector2.Distance(transform.position, player.position);
        float magnetRange = baseMagnetRange * (1f + (playerStats != null ? playerStats.magnetBonusPercent : 0f) / 100f);

        if (dist < magnetRange)
            isMagneted = true;

        if (isMagneted)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                player.position,
                magnetSpeed * Time.deltaTime);

            if (dist < collectRange)
            {
                Debug.Log($"[ToolboxPickup] 수집! equippedParts={playerStats?.equippedParts?.Count}");
                LevelUpRandomSkill();
                Destroy(gameObject);
            }
        }
    }

    private void LevelUpRandomSkill()
    {
        if (playerStats == null) return;

        // 보유 스킬 중 레벨업 가능한 것 필터링
        var upgradeable = new List<OwnedPart>();
        foreach (var part in playerStats.equippedParts)
        {
            if (part.level < part.data.maxLevel)
                upgradeable.Add(part);
        }

        if (upgradeable.Count == 0)
        {
            Debug.Log("[ToolboxPickup] 레벨업 가능한 스킬 없음");
            return;
        }

        // 랜덤 선택 → 뽑기 연출 (레벨업은 연출 끝나고 적용)
        var chosen = upgradeable[Random.Range(0, upgradeable.Count)];
        SkillRoulette.Show(playerStats.equippedParts, chosen, playerStats);
    }
}
