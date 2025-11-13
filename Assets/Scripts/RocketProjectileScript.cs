using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketProjectileScript : ProjectileScript
{
    private float explodeRadius;
    
    public void Init(Vector2 direction, float damage, float speed, 
        float lifeTime, float explodeRadius, int teamId, 
        GameObject hitEffect, Color teamColor, CharacterStatus character)
    {
        base.Init(direction, damage, speed, lifeTime, teamId, hitEffect, teamColor, character);
        this.explodeRadius = explodeRadius;
    }

    public override void DestroyProjectile()
    {
        LayerMask playerMask = LayerMask.GetMask("Character");
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explodeRadius, playerMask);

        foreach (var hit in hits)
        {
            CharacterStatus damageable = hit.GetComponent<CharacterStatus>();
            if (damageable != null)
            {
                float hitDistance = (damageable.transform.position - transform.position).magnitude;
                if (hitDistance <= explodeRadius)
                {
                    float explodeDamage = damage * (1 - hitDistance / explodeRadius);
                    bool lastHit = damageable.TakeDamage(explodeDamage);
                    sourceCharacter.MakeDamage(explodeDamage, lastHit);
                }
            }
        }
        Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
