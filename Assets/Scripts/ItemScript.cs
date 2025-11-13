using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemScript : MonoBehaviour
{
    private int itemIdx;
    private BoxCollider2D collider;
    private ObjectSpawnPointScript objectSpawnPoint;
    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<BoxCollider2D>();
        transform.localScale = new Vector3(3, 3, 3);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Character"))
        {
            CharacterStatus character = collision.GetComponent<CharacterStatus>();
            if (itemIdx == 0)
            {
                character.Heal(50);
            }

            if (itemIdx == 1)
            {
                character.LootArmor(50);
            }

            if (itemIdx > 1 && itemIdx < 5)
            {
                character.LootWeapon(itemIdx - 1);
            }
            if (itemIdx > 4)
            {
                character.LootAmmo(itemIdx - 4);
            }
            
            objectSpawnPoint.Loot();
            Destroy(this.gameObject);
        }
    }

    public void SetSpawner(ObjectSpawnPointScript platform)
    {
        objectSpawnPoint = platform;
    }

    public void SetItem(int idx, Sprite sprite)
    {
        itemIdx = idx;
        GetComponent<SpriteRenderer>().sprite = sprite;
    }
}
