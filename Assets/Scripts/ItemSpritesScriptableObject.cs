using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemSprites", menuName = "Game/ItemSprites")]
public class ItemSpritesScriptableObject : ScriptableObject
{
    [SerializeField] private Sprite[] itemSprites;

    public Sprite GetItemSprite(int itemId)
    {
        return itemSprites[itemId];
    }
}
