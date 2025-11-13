using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketWeaponScript : WeaponScript
{
    [SerializeField] private float explodeRadius;
    
    public override int TriggerShoot(Vector3 direction, bool isFiring)
    {
        if (isFiring)
        {
            if (lastFireTime + fireRate < Time.time)
            {
                Instantiate(firingEffect, transform.position, Quaternion.identity);
                GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
                bullet.GetComponent<RocketProjectileScript>().Init(direction, projDamage, projSpeed, projLifeTime, 
                    explodeRadius, character.TeamId, hitEffect, character.TeamColor(), character);
                lastFireTime = Time.time;
                return 1;
            }
        }

        return 0;
    }
}
