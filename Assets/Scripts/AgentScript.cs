using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class AgentScript : Agent
{
    private PhysicalObject physicalObject;
    private CharacterStatus character;
    private WeaponManagerScript weapons;
    private Vector2 moveInput;
    private bool jumpPressed = false;
    private float aimAngle;
    private bool isFiring = false;
    
    // heuristic
    
    
    private void Awake()
    {
        physicalObject = GetComponent<PhysicalObject>();
        character = GetComponent<CharacterStatus>();
        weapons = character.GetWeapon();
        GetComponent<BehaviorParameters>().TeamId = character.TeamId;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        character.OnTakeDamage += (dmg) => AddReward(-0.1f * dmg, "TakeDamage");
        character.OnDeath += OnDeath;
        character.OnMakeDamage += (dmg, lasthit) => OnMakeDamage(dmg, lasthit);
        character.OnLoot += (points) => OnLoot(points * 0.01f);
    }

    void AddReward(float reward, string source)
    {
        Debug.Log($"Reward {source}: {reward}");
        AddReward(reward);
    }

    void OnDeath()
    {
        AddReward(-1f, "Death");
        // EndEpisode();
    }

    void OnMakeDamage(float damage, bool lastHit)
    {
        AddReward(0.4f * damage, "MakeDamage");
        if (lastHit)
        {
            AddReward(4f * damage, "KillEnemy");
        }
    }

    void OnLoot(float points)
    {
        AddReward(points, "Loot");
    }

    private void FixedUpdate()
    {
        MoveUpdate();
        FireUpdate();
    }
    
    private void FireUpdate()
    {
        weapons.UpdateAim(aimAngle);
        physicalObject.FlipSprite(aimAngle);
        weapons.TriggerShoot(isFiring);
    }

    private void MoveUpdate()
    {
        physicalObject.SetInput(moveInput, jumpPressed);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var contActs = actionsOut.ContinuousActions;
        contActs[0] = Random.Range(-1, 2);
        contActs[1] = Random.Range(-1, 2);
        contActs[2] = Random.Range(-1f, 1f);
        contActs[3] = Random.Range(-1f, 1f);
        contActs[4] = Random.Range(-1f, 1f);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        base.CollectObservations(sensor);
        // hp armor
        sensor.AddObservation(character.GetCurrentHp() / character.GetMaxHp());
        sensor.AddObservation(character.GetCurrentArmor() / character.GetMaxArmor());
        // weapons
        bool[] hasWeapons = weapons.GetHasWeapons();
        int[] ammos = weapons.GetAmmos();
        int[] maxAmmos = weapons.GetMaxAmmos();
        int activeWeapon = weapons.GetActiveWeapon();
        for (int i = 0; i < ammos.Length; i++)
        {
            float weaponStatus = 0f;
            if (hasWeapons[i])
            {
                weaponStatus = 0.5f;
                if (i == activeWeapon)
                {
                    weaponStatus = 1.0f;
                }
            }
            sensor.AddObservation(weaponStatus);
            sensor.AddObservation((float)ammos[i] / maxAmmos[i]);
        }
        // sensor.AddObservation(transform.position.x);
        // sensor.AddObservation(transform.position.y);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float x = actions.ContinuousActions[0];
        float y = actions.ContinuousActions[1];
        aimAngle = actions.ContinuousActions[2] * 180;
        moveInput = new Vector2(x, y);
        jumpPressed = y > 0;
        isFiring = actions.ContinuousActions[3] > 0.5;
        int switchW = (int)actions.ContinuousActions[4];
        if (Math.Abs(switchW) == 1)
        {
            weapons.Switch(switchW);
        }
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
    }
}
