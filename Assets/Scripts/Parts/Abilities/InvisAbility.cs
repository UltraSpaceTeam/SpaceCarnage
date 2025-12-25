using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "InvisibilityAbility", menuName = "Ship/Abilities/Invisibility")]
public class InvisAbility : AbstractAbility
{
    [Header("Invisibility Settings")]
    public float activationDelay = 1.5f;
    public bool breakOnAttack = true;
    public bool breakOnDamage = true;

    // Внутреннее состояние
    private bool isActive = false;
    private bool isFullyInvisible = false;
    private float delayTimer = 0f;

    private GameObject owner;
    private InvisManager invisManager;

    public override void RunAbility(Rigidbody shipRb)
    {
        owner = shipRb.gameObject;
        invisManager = owner.GetComponent<InvisManager>();

        if (invisManager == null)
        {
            Debug.LogWarning("InvisManager is not found! Invisibility will not be working.");
            return;
        }

        if (isActive)
        {
            Deactivate();
        }
        else
        {
            Activate();
        }
    }

    private void Activate()
    {
        isActive = true;
        isFullyInvisible = false;
        delayTimer = activationDelay;

        Debug.Log($"Invisibility activation... Delay: {activationDelay} sec.");
    }

    private void Deactivate()
    {
        isActive = false;
        isFullyInvisible = false;
        delayTimer = 0f;

        invisManager?.SetVisible(true);

        Debug.Log("Invisibility deactivated.");
    }

    public void BreakInvisibility()
    {
        if (isActive)
        {
            Debug.Log("Invisibility deactivated (attack or damage)!");
            Deactivate();
        }
    }

    public override void ServerUpdate(Rigidbody shipRb)
    {
        if (!isActive || invisManager == null) return;

        if (delayTimer > 0f)
        {
            delayTimer -= Time.fixedDeltaTime;

            if (delayTimer <= 0f)
            {
                isFullyInvisible = true;
                invisManager.SetVisible(false);

                Debug.Log("Ship is completely invisible!");
            }
        }
    }

    public override void OnEquipped()
    {
        isActive = false;
        isFullyInvisible = false;
        delayTimer = 0f;
    }

    public override void OnUnequipped()
    {
        Deactivate();
    }

    // Для UI или других систем
    public bool IsActive => isActive;
    public bool IsFullyInvisible => isFullyInvisible;
    public float ActivationProgress => isActive ? 1f - (delayTimer / activationDelay) : 0f;
}