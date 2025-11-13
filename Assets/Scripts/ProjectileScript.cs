using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    protected float damage;
    protected float speed;
    protected float lifeTime;
    [SerializeField]
    private int teamId;
    protected CircleCollider2D collider;
    protected Vector2 direction;
    protected Rigidbody2D rb;
    protected GameObject hitEffect;
    protected CharacterStatus sourceCharacter;
    
    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        rb.velocity = direction * speed;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        lifeTime -= Time.fixedDeltaTime;
        if (lifeTime <= 0)
        {
            DestroyProjectile();
        }
        // rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }
    
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Character"))
        {
            CharacterStatus character = col.GetComponent<CharacterStatus>();
            if (character.TeamId != teamId)
            {
                bool lastHit = character.TakeDamage(damage);
                sourceCharacter.MakeDamage(damage, lastHit);
            }
            DestroyProjectile();
        } else if (col.CompareTag("Floor"))
        {
            DestroyProjectile();
        }
    }

    public void Init(Vector2 direction, float damage, float speed, 
        float lifeTime, int teamId, 
        GameObject hitEffect, Color teamColor, CharacterStatus character)
    {
        this.damage = damage;
        this.speed = speed;
        this.lifeTime = lifeTime;
        this.direction = direction;
        this.teamId = teamId;
        this.hitEffect = hitEffect;
        this.sourceCharacter = character;
        SetTeamColor(teamColor);
    }

    public virtual void DestroyProjectile()
    {
        Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    private void SetTeamColor(Color teamColor)
    {
        GetComponent<SpriteRenderer>().color = teamColor;
    }

    public int GetTeamId()
    {
        return teamId;
    }
}
