using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnerConfig", menuName = "Game/Spawner Config")]
public class SpawnItemConfigScript : ScriptableObject
{
    public List<SpawnableEntry> spawnables;

    public int GetRandomSpawn()
    {
        float totalWeight = 0f;
        foreach (var entry in spawnables)
            totalWeight += entry.probability;

        float random = Random.Range(0, totalWeight);
        float current = 0f;

        foreach (var entry in spawnables)
        {
            current += entry.probability;
            if (random <= current)
                return entry.itemIdx;
        }

        return 0;
    }
}

[System.Serializable]
public class SpawnableEntry
{
    public int itemIdx;
    [Range(0f, 1f)] public float probability;
}
