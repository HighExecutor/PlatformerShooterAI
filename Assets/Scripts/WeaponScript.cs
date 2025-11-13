using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    [SerializeField] protected GameObject bulletPrefab;
    [SerializeField] protected float damageMultiplier = 1f;
    [SerializeField] protected float projDamage;
    [SerializeField] protected float projSpeed;
    [SerializeField] protected float projLifeTime;
    [SerializeField] protected CharacterStatus character;
    [SerializeField] protected float fireRate;
    [SerializeField] protected GameObject firingEffect;
    [SerializeField] protected GameObject hitEffect;
    protected float lastFireTime;

    private void Awake()
    {
        lastFireTime = Time.time;
    }

    public virtual int TriggerShoot(Vector3 direction, bool isFiring)
    {
        if (isFiring)
        {
            if (lastFireTime + fireRate < Time.time)
            {
                Instantiate(firingEffect, transform.position, Quaternion.identity);
                GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
                bullet.GetComponent<ProjectileScript>().Init(direction, projDamage, projSpeed, projLifeTime, character.TeamId, hitEffect, character.TeamColor(), character);
                lastFireTime = Time.time;
                return 1;
            }
        }

        return 0;
    }

    public void UpdateLastFireTime()
    {
        lastFireTime = Time.time;
    }
}
