using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManagerScript : MonoBehaviour
{
    [SerializeField] private WeaponScript[] weapons;
    [SerializeField] private int activeWeapon;
    [SerializeField] private bool[] hasWeapon;
    [SerializeField] private int[] ammo;
    [SerializeField] private int[] lootAmmo;
    [SerializeField] private int[] startAmmo;
    [SerializeField] private int[] maxAmmo;
    [SerializeField] private CharacterStatus character;
    [SerializeField] private float gunOrbitRadius = 0.4f;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Reset()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            if (i == 0)
            {
                ammo[i] = 1;
                hasWeapon[i] = true;
            } else if (i == 1)
            {
                ammo[i] = startAmmo[i];
                hasWeapon[i] = true;
            }
            else
            {
                ammo[i] = startAmmo[i];
                hasWeapon[i] = false;
            }
        }
        activeWeapon = 1;
        SetWeaponsActive();
    }
    
    public void UpdateAim(float aimAngle)
    {
        // Direction from player to mouse
        float angleRadians = aimAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0f);

        // Set weapon position in orbit
        transform.localPosition = direction * gunOrbitRadius;
        transform.rotation = Quaternion.Euler(0, 0, aimAngle);
    }
    
    public void TriggerShoot(bool isFiring)
    {
        int ammoSpanded = weapons[activeWeapon].TriggerShoot(transform.localPosition.normalized, isFiring);
        ammo[activeWeapon] -= ammoSpanded;
        if (ammo[activeWeapon] == 0)
        {
            Switch(1);
        }

        character.UpdateAmmo(GetCurrentAmmo(), activeWeapon);
    }

    public void Switch(int direction)
    {
        weapons[activeWeapon].TriggerShoot(transform.localPosition.normalized, false);
        for (int i = 0; i <= weapons.Length; i++)
        {
            int c_activeWeapon = (activeWeapon + direction * (i+1) + weapons.Length) % weapons.Length;
            if (hasWeapon[c_activeWeapon] && ammo[c_activeWeapon] > 0)
            {
                activeWeapon = c_activeWeapon;
                break;
            }
            
        }

        SetWeaponsActive();
        weapons[activeWeapon].UpdateLastFireTime();
    }

    private void SetWeaponsActive()
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].gameObject.SetActive(i == activeWeapon);
        }
    }

    public int GetActiveWeapon()
    {
        return activeWeapon;
    }

    public void LootAmmo(int weaponIdx)
    {
        ammo[weaponIdx] += lootAmmo[weaponIdx];
        ammo[weaponIdx] = Math.Clamp(ammo[weaponIdx], 0, maxAmmo[weaponIdx]);
    }

    public void LootWeapon(int weaponIdx)
    {
        if (hasWeapon[weaponIdx])
        {
            ammo[weaponIdx] += lootAmmo[weaponIdx] / 10;
            ammo[weaponIdx] = Math.Clamp(ammo[weaponIdx], 0, maxAmmo[weaponIdx]);
        }
        else
        {
            hasWeapon[weaponIdx] = true;
            ammo[weaponIdx] = startAmmo[weaponIdx];
            activeWeapon = weaponIdx;
            SetWeaponsActive();
        }
    }

    public int GetCurrentAmmo()
    {
        return ammo[activeWeapon];
    }

    public bool[] GetHasWeapons() => hasWeapon;
    public int[] GetAmmos() => ammo;
    
    public int[] GetMaxAmmos() => maxAmmo;
}
