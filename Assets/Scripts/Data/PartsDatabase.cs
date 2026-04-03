using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "CarSurvivor/PartsDatabase")]
public class PartsDatabase : ScriptableObject
{
    public List<PartsData> allParts = new List<PartsData>();

    public List<PartsData> GetRandomParts(int count, List<PartsData> exclude = null)
    {
        var stats = PlayerStats.Instance;
        List<PartsData> available = new List<PartsData>();

        bool weaponFull = stats != null && stats.IsWeaponSlotFull();
        bool spellFull = stats != null && stats.IsSpellBookSlotFull();

        foreach (var part in allParts)
        {
            if (exclude != null && exclude.Contains(part)) continue;

            bool isWeapon = part.category == ItemCategory.MainWeapon || part.category == ItemCategory.SubWeapon;
            bool isSpell = part.category == ItemCategory.SpellBook;

            // 이미 보유한 파츠는 만렙이면 제외
            if (stats != null)
            {
                var owned = stats.equippedParts.Find(p => p.data == part);
                if (owned != null && owned.level >= part.maxLevel) continue;

                // 미보유 + 슬롯 꽉 찬 카테고리 → 제외
                if (owned == null && isWeapon && weaponFull) continue;
                if (owned == null && isSpell && spellFull) continue;
            }

            available.Add(part);
        }

        List<PartsData> result = new List<PartsData>();
        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            result.Add(available[index]);
            available.RemoveAt(index);
        }

        return result;
    }
}
