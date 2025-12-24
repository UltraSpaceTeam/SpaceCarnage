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
    public GameObject shieldPrefab;


    private float currentShieldHealth;
    private float regenTimer;
    private bool isActive;
    private Rigidbody shipRb;
    private GameObject owner;
    private GameObject shield;

    public override void RunAbility(Rigidbody rb)
    {
        shipRb = rb;
        

        // Toggle активации
        isActive = !isActive;

        if (isActive)
        {
            if (currentShieldHealth <= 0) currentShieldHealth = maxShieldHealth; // полный зар€д при первом включении
            Debug.Log("ўит включЄн!");
            regenTimer = 0f;
        }
        else
        {
            Debug.Log("ўит выключен.");
            regenTimer = regenerationDelay;
        }
    }

    public override void ServerUpdate(Rigidbody rb)
    {
        if (shield == null)
        {
            owner = rb.gameObject;
            shield = Instantiate(shieldPrefab, owner.transform);
            shield.transform.localPosition = Vector3.zero;
            shield.GetComponent<Renderer>().enabled = false;
        }

        if (isActive)
        {
            shield.GetComponent<Renderer>().enabled = true;
        }
        else if (currentShieldHealth < maxShieldHealth)
        {
            shield.GetComponent<Renderer>().enabled = false;
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
            shield.GetComponent<Renderer>().enabled = false;
        }

        // јвто-выключение если щит пробит
        if (isActive && currentShieldHealth <= 0)
        {
            shield.GetComponent<Renderer>().enabled = false;
            isActive = false;
            regenTimer = regenerationDelay;
            Debug.Log("ўит разрушен и выключен!");
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
            regenTimer = regenerationDelay;
            Debug.Log("ўит сломан! ќстаток урона: " + leftover);
            return leftover; // остаток идЄт в корпус
        }
        else
        {
            currentShieldHealth -= incomingDamage;
            Debug.Log($"ўит поглотил {incomingDamage}. ќсталось: {currentShieldHealth}");
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

    // ƒл€ UI (опционально, если добавишь индикатор)
    public float GetShieldPercentage() => currentShieldHealth / maxShieldHealth;
    public bool IsShieldActive => isActive;
}