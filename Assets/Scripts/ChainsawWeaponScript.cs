using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainsawWeaponScript : WeaponScript
{
    // Start is called before the first frame update
    private BoxCollider2D hitBox;
    private bool isFiring;
    private Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();
    private ParticleSystem firingParticles;
    
    
    void Start()
    {
        hitBox = GetComponent<BoxCollider2D>();
        firingParticles = firingEffect.GetComponent<ParticleSystem>();
        isFiring = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isFiring)
        {
            if (!firingParticles.isPlaying)
            {
                firingParticles.Play();
            }
            hitBox.enabled = true;
        }
        else
        {
            if (firingParticles.isPlaying)
            {
                firingParticles.Stop();
                firingParticles.Clear();
            }
            hitBox.enabled = false;
        }
    }

    private void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("Character"))
        {
            CharacterStatus other = col.GetComponent<CharacterStatus>();
            if (other.TeamId != character.TeamId)
            {
                if (!lastHitTimes.ContainsKey(other.gameObject))
                {
                    lastHitTimes[other.gameObject] = 0;
                }

                if (lastHitTimes[other.gameObject] + fireRate < Time.time)
                {
                    other.TakeDamage(projDamage);
                    lastHitTimes[other.gameObject] = Time.time;
                }
            }
            Instantiate(hitEffect, col.transform.position, Quaternion.identity);
        }
    }
    
    public override int TriggerShoot(Vector3 direction, bool isFiring)
    {
        this.isFiring = isFiring;
        return 0;
    }
}
