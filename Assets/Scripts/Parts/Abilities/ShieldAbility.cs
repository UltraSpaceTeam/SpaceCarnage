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
    //public GameObject shieldPrefab;


    private float currentShieldHealth;
    private float regenTimer;
    private bool isActive;
    private Rigidbody shipRb;
    private GameObject owner;
    private GameObject shield;

    private Player player;

    public override void RunAbility(Rigidbody rb)
    {
        shipRb = rb;
        owner = rb.gameObject;
        player = owner.GetComponent<Player>();

        // Toggle активации
        isActive = !isActive;

        if (isActive)
        {
            if (currentShieldHealth <= 0) currentShieldHealth = maxShieldHealth; // полный заряд при первом включении
            Debug.Log("Shield is on!");
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
        if (shield == null)
        {
            owner = rb.gameObject;
        }

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

        // Авто-выключение если щит пробит
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

        if (incomingDamage >= currentShieldHealth)
        {
            float leftover = incomingDamage - currentShieldHealth;
            currentShieldHealth = 0f;
            isActive = false;
            player?.RpcShowShield(false, 0f);
            regenTimer = regenerationDelay;
            Debug.Log("Shield broken! Remaining damage: " + leftover);
            return leftover; // остаток идёт в корпус
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
    }

    public override void OnUnequipped()
    {
        isActive = false;
    }

    // Для UI (опционально, если добавишь индикатор)
    public float GetShieldPercentage() => currentShieldHealth / maxShieldHealth;
    public bool IsShieldActive => isActive;
}