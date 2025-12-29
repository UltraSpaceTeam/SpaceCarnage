// ShieldAbility.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ShieldAbility", menuName = "Ship/Abilities/Shield")]
public class ShieldAbility : AbstractAbility
{
    [Header("Shield Settings")]
    public float maxShieldHealth = 100f;
    public float regenerationDelay = 5f;
    public float regenerationRate = 20f;
    public float speedReductionPercent = 0.3f;


    private float currentShieldHealth;
    private float regenTimer;
    private bool isActive;
    private GameObject owner;

    private Player player;

    public override void RunAbility(Rigidbody rb)
    {
        owner = rb.gameObject;

        // Toggle activation
        isActive = !isActive;

        player = owner.GetComponent<Player>();

        if (isActive)
        {
            Debug.Log($"Shield turned ON with {currentShieldHealth}/{maxShieldHealth} health");
            regenTimer = 0f;
            player?.RpcShowShield(true, currentShieldHealth / maxShieldHealth);
        }
        else
        {
            Debug.Log("Shield is off.");
            regenTimer = regenerationDelay;
            player?.RpcShowShield(false, 0f);
        }
    }

    public override void ServerUpdate(Rigidbody rb)
    {
        if (owner == null) owner = rb.gameObject;
        Player player = owner.GetComponent<Player>();

        if (isActive)
        {
            player?.RpcShowShield(true, currentShieldHealth / maxShieldHealth);
        }
        else if (currentShieldHealth < maxShieldHealth)
        {
            player?.RpcShowShield(false, 0f);
            if (regenTimer > 0)
            {
                regenTimer -= Time.fixedDeltaTime;
            }
            else
            {
                currentShieldHealth += regenerationRate * Time.fixedDeltaTime;
                currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, maxShieldHealth);
            }
        } else
        {
            player?.RpcShowShield(false, 0f);
        }

        // Automatically off when shield is broken
        if (isActive && currentShieldHealth <= 0)
        {
            player?.RpcShowShield(false, 0f);
            isActive = false;
            regenTimer = regenerationDelay;
            Debug.Log("Shield broken!");
        }
    }

    public override float AbsorbDamage(float incomingDamage)
    {
        if (!isActive) return incomingDamage;

        Player player = owner?.GetComponent<Player>();

        if (incomingDamage >= currentShieldHealth)
        {
            float leftover = incomingDamage - currentShieldHealth;
            currentShieldHealth = 0f;
            isActive = false;
            player?.RpcShowShield(false, 0f);
            regenTimer = regenerationDelay;
            Debug.Log("Shield broken! Remaining damage: " + leftover);
            return leftover;
        }
        else
        {
            currentShieldHealth -= incomingDamage;
            Debug.Log($"Shield absorbed {incomingDamage}. Shield has: {currentShieldHealth} health");
            return 0f;
        }
    }

    public override float GetSpeedMultiplier()
    {
        return isActive ? (1f - speedReductionPercent) : 1f;
    }

    public override void OnEquipped()
    {
        currentShieldHealth = maxShieldHealth;
        isActive = false;
        regenTimer = 0f;
        Debug.Log("Shield equipped - full health, inactive");
    }

    public override void OnUnequipped()
    {
        isActive = false;
        isActive = false;
        if (owner != null)
        {
            Player player = owner.GetComponent<Player>();
            player?.RpcShowShield(false, 0f);
        }
    }

    public override float GetVisualStatus()
    {
        if (maxShieldHealth <= 0) return 0f;
        return currentShieldHealth / maxShieldHealth;
    }

    public float ShieldPercentage => currentShieldHealth / maxShieldHealth;
    public bool IsShieldActive => isActive;
    public float CurrentShieldHealth => currentShieldHealth;
    public float MaxShieldHealth => maxShieldHealth;
}