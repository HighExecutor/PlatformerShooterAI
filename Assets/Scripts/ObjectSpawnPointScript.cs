using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectSpawnPointScript : MonoBehaviour
{
    [SerializeField] private SpawnItemConfigScript spawnConfig;
    [SerializeField] private float spawnTime;
    private float lastLootedTime;
    private bool spawned = false;
    [SerializeField] private ItemSpritesScriptableObject itemSprites;
    [SerializeField] private GameObject itemPrefab;

    private void Awake()
    {
        lastLootedTime = Time.time;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!spawned && Time.time - lastLootedTime > spawnTime)
        {
            int itemIdx = spawnConfig.GetRandomSpawn();
            if (itemPrefab != null)
            {
                GameObject item = Instantiate(itemPrefab, transform.position, Quaternion.identity, transform);
                item.GetComponent<ItemScript>().SetSpawner(this);
                item.GetComponent<ItemScript>().SetItem(itemIdx, itemSprites.GetItemSprite(itemIdx));
                spawned = true;
            }
            else
            {
                Debug.Log("No item selected to spawn");
            }
        }
    }

    public void Loot()
    {
        spawned = false;
        lastLootedTime = Time.time;
    }
}
