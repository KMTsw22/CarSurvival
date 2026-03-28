using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "CarSurvivor/PartsDatabase")]
public class PartsDatabase : ScriptableObject
{
    public List<PartsData> allParts = new List<PartsData>();

    public List<PartsData> GetRandomParts(int count, List<PartsData> exclude = null)
    {
        List<PartsData> available = new List<PartsData>(allParts);

        if (exclude != null)
        {
            available.RemoveAll(p => exclude.Contains(p));
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
