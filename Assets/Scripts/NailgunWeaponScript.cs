using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NailgunWeaponScript : WeaponScript
{
    [SerializeField] protected float projDistance;
    [SerializeField] protected float accuracy;
    [SerializeField] protected LayerMask hitLayers;
    
    public override int TriggerShoot(Vector3 direction, bool isFiring)
    {
        if (isFiring)
        {
            if (lastFireTime + fireRate < Time.time)
            {
                Instantiate(firingEffect, transform.position, Quaternion.identity);
                Vector3 noise = Random.insideUnitSphere * accuracy;
                noise.z = 0;
                Vector3 noisedDirection = (direction + noise).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, noisedDirection, projDistance, hitLayers);
                Debug.DrawRay(transform.position, noisedDirection, Color.red, fireRate*2);
                if (hit.collider != null)
                {
                    if (hit.collider.CompareTag("Character"))
                    {
                        CharacterStatus hitCharacter = hit.collider.GetComponent<CharacterStatus>();
                        if (hitCharacter.TeamId != character.TeamId)
                        {
                            bool lastHit = hitCharacter.TakeDamage(projDamage);
                            character.MakeDamage(projDamage, lastHit);
                        }
                    }
                    Instantiate(hitEffect, hit.point, Quaternion.identity);
                }
                lastFireTime = Time.time;
                return 1;
            }
        }

        return 0;
    }
}
