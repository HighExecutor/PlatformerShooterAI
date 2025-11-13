using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class CharacterStatus : MonoBehaviour
{
    [SerializeField] private float maxHp;
    private float currentHp;
    [SerializeField] private float maxArmor;
    private float currentArmor;
    [SerializeField] private WeaponManagerScript weapons;
    [SerializeField] private PhysicalObject physicalObject;
    
    [SerializeField] private int teamId;
    private Color teamColor;
    [SerializeField] private Transform hpBar;
    private float hpBarScale;
    [SerializeField] private Transform armorBar;
    private float armorBarScale;
    [SerializeField] private PlayerStatsUI playerStatsUI;

    [SerializeField] private SharedDataScriptableObject sharedData;
    [SerializeField] private GameObject damageTextPrefab;
    private GameManagerScript gameManager;
    [SerializeField] private ParticleSystem damageEffect;
    
    public event Action<float> OnTakeDamage;
    public event Action OnDeath;
    public event Action<float, bool> OnMakeDamage;
    public event Action<float> OnLoot;
    
    public int TeamId
    {
        get => teamId;
        set => teamId = value;
    }

    // Start is called before the first frame update
    void Start()
    {
        currentHp = maxHp;
        currentArmor = 0;
        hpBarScale = hpBar.localScale.x;
        armorBarScale = hpBar.localScale.x;
        teamColor = sharedData.GetTeamColor(teamId);
        hpBar.GetComponent<SpriteRenderer>().color = teamColor;
        physicalObject.SetWrapColor(teamColor);
        UpdateHpStatus();
    }

    public void SetGameManager(GameManagerScript gameManager)
    {
        this.gameManager = gameManager;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool TakeDamage(float damage)
    {
        Debug.Log($"Taking damage: {damage}");
        OnTakeDamage?.Invoke(damage);
        float armorAbsorb = damage - currentArmor;
        currentArmor = Mathf.Clamp(currentArmor - damage, 0, maxArmor);
        
        currentHp -= armorAbsorb;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpStatus();
        
        // Damage text
        GameObject textObj = Instantiate(damageTextPrefab, transform.position, Quaternion.identity);
        textObj.GetComponent<TextMeshPro>().text = damage.ToString("F0");
        Destroy(textObj, 0.5f);
        damageEffect.Emit(Math.Max((int)damage / 5, 1));
        
        if (currentHp <= 0)
        {
            OnDeath?.Invoke();
            Death();
            return true;
        }

        return false;
    }

    public void Heal(float heal)
    {
        currentHp += heal;
        currentHp = Mathf.Clamp(currentHp, 0, maxHp);
        UpdateHpStatus();
        OnLoot?.Invoke(0.1f);
    }

    private void Death()
    {
        Transform spawnPoint = GameManagerScript.Instance.teamSpawnPoints[teamId];
        transform.position = spawnPoint.position;
        currentHp = maxHp;
        currentArmor = 0;
        weapons.Reset();
        physicalObject.Reset();
        UpdateHpStatus();
        gameManager.RegisterDeath(this);
    }

    public void MakeDamage(float damage, bool lastHit)
    {
        Debug.Log($"Make damage: {damage}");
        OnMakeDamage?.Invoke(damage, lastHit);
    }

    public WeaponManagerScript GetWeapon()
    {
        return weapons;
    }

    public void LootAmmo(int weaponIdx)
    {
        weapons.LootAmmo(weaponIdx);
        OnLoot?.Invoke(0.1f);
    }

    public void LootWeapon(int weaponIdx)
    {
        weapons.LootWeapon(weaponIdx);
        OnLoot?.Invoke(0.1f);
    }

    public void LootArmor(int armor)
    {
        currentArmor += armor;
        currentArmor = Mathf.Clamp(currentArmor, 0, maxArmor);
        UpdateHpStatus();
        OnLoot?.Invoke(0.1f);
    }

    public Color TeamColor()
    {
        return teamColor;
    }

    private void UpdateHpStatus()
    {
        hpBar.localScale = new Vector3(hpBarScale * currentHp / maxHp, hpBar.localScale.y, hpBar.localScale.z);
        armorBar.localScale = new Vector3(armorBarScale * currentArmor / maxArmor, armorBar.localScale.y, armorBar.localScale.z);
        if (playerStatsUI != null)
        {
            playerStatsUI.UpdateHp(currentHp, currentArmor);
        }
    }

    public void SetPlayerStatsUi(PlayerStatsUI playerStatsUI)
    {
        this.playerStatsUI = playerStatsUI;
    }

    public void UpdateAmmo(int ammo, int activeWeaponIdx)
    {
        if (playerStatsUI != null)
        {
            playerStatsUI.UpdateAmmo(ammo, activeWeaponIdx);
        }
    }
    
    public float GetCurrentHp() => currentHp;
    public float GetMaxHp() => maxHp;
    public float GetCurrentArmor() => currentArmor;
    public float GetMaxArmor() => maxArmor;
}
