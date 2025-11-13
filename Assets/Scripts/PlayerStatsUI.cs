using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image ammoImage;
    [SerializeField] private ItemSpritesScriptableObject itemSprites;

    public void UpdateHp(float hp, float armor)
    {
        hpText.text = Mathf.RoundToInt(hp).ToString();
        armorText.text = Mathf.RoundToInt(armor).ToString();
    }

    public void UpdateAmmo(float ammo, int activeWeaponIdx)
    {
        ammoText.text = Mathf.RoundToInt(ammo).ToString();
        if (activeWeaponIdx > 0)
        {
            ammoImage.sprite = itemSprites.GetItemSprite(activeWeaponIdx + 4);
        }
        else
        {
            ammoImage.sprite = itemSprites.GetItemSprite(8);
        }
    }
}
