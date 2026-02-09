using UnityEngine;
using Mirror;

[CreateAssetMenu(fileName = "InvisibilityAbility", menuName = "Ship/Abilities/Invisibility")]
public class InvisAbility : AbstractAbility
{
    [Header("Invisibility Settings")]
    public float activationDelay = 1.5f;
    public bool breakOnAttack = true;
    public bool breakOnDamage = true;
    public override AbilityRuntime CreateRuntime() => new InvisRuntime(this);


    private class InvisRuntime : AbilityRuntime
    {
        private readonly InvisAbility cfg;

        private bool isActive;
        private bool isFullyInvisible;
        private float delayTimer;

        public InvisRuntime(InvisAbility cfg) => this.cfg = cfg;

        private InvisManager Invis => rb != null ? rb.GetComponent<InvisManager>() : null;

        public override void Run()
        {
            if (Invis == null)
            {
                Debug.LogWarning("InvisManager not found!");
                return;
            }

            if (isActive) Deactivate();
            else Activate();
        }

        private void Activate()
        {
            isActive = true;
            isFullyInvisible = false;
            delayTimer = cfg.activationDelay;
        }

        private void Deactivate()
        {
            isActive = false;
            isFullyInvisible = false;
            delayTimer = 0f;

            Invis?.SetVisible(true);
        }

        public override void ServerUpdate()
        {
            if (!isActive) return;
            if (Invis == null) return;

            if (delayTimer > 0f)
            {
                delayTimer -= Time.fixedDeltaTime;
                if (delayTimer <= 0f)
                {
                    isFullyInvisible = true;
                    Invis.SetVisible(false);
                }
            }
        }

        public override void OnUnequipped()
        {
            if (isActive || isFullyInvisible)
                Deactivate();
        }

        public override void OnEquipped()
        {
            isActive = false;
            isFullyInvisible = false;
            delayTimer = 0f;
            Invis?.SetVisible(true);
        }

        public override void OnOwnerDamaged()
        {
            if (cfg.breakOnDamage && isActive) Deactivate();
        }

        public override void OnOwnerAttacked()
        {
            if (cfg.breakOnAttack && isActive) Deactivate();
        }

        public override float GetVisualStatus()
        {
            if (!isActive) return 0f;
            if (cfg.activationDelay <= 0f) return 1f;
            return 1f - Mathf.Clamp01(delayTimer / cfg.activationDelay);
        }
    }
}