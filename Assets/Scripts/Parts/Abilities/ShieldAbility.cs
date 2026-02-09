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

    public override AbilityRuntime CreateRuntime() => new ShieldRuntime(this);


    private class ShieldRuntime : AbilityRuntime
    {
        private readonly ShieldAbility cfg;

        private float currentShieldHealth;
        private float regenTimer;
        private bool isActive;

        public ShieldRuntime(ShieldAbility cfg) => this.cfg = cfg;

        public override void OnEquipped()
        {
            currentShieldHealth = cfg.maxShieldHealth;
            regenTimer = 0f;
            isActive = false;
            player?.RpcShowShield(false, 0f);
        }

        public override void OnUnequipped()
        {
            isActive = false;
            player?.RpcShowShield(false, 0f);
        }

        public override void Run()
        {
            isActive = !isActive;
            if (isActive)
            {
                regenTimer = 0f;
                player?.RpcShowShield(true, currentShieldHealth / Mathf.Max(0.0001f, cfg.maxShieldHealth));
            }
            else
            {
                regenTimer = cfg.regenerationDelay;
                player?.RpcShowShield(false, 0f);
            }
        }

        public override void ServerUpdate()
        {
            if (isActive)
            {
                player?.RpcShowShield(true, currentShieldHealth / Mathf.Max(0.0001f, cfg.maxShieldHealth));

                if (currentShieldHealth <= 0f)
                {
                    isActive = false;
                    regenTimer = cfg.regenerationDelay;
                    player?.RpcShowShield(false, 0f);
                }
                return;
            }

            player?.RpcShowShield(false, 0f);

            if (currentShieldHealth < cfg.maxShieldHealth)
            {
                if (regenTimer > 0f) regenTimer -= Time.fixedDeltaTime;
                else
                {
                    currentShieldHealth += cfg.regenerationRate * Time.fixedDeltaTime;
                    currentShieldHealth = Mathf.Clamp(currentShieldHealth, 0, cfg.maxShieldHealth);
                }
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
                regenTimer = cfg.regenerationDelay;
                player?.RpcShowShield(false, 0f);
                return leftover;
            }

            currentShieldHealth -= incomingDamage;
            return 0f;
        }

        public override float GetSpeedMultiplier()
            => isActive ? (1f - cfg.speedReductionPercent) : 1f;

        public override float GetVisualStatus()
            => cfg.maxShieldHealth <= 0 ? 0f : currentShieldHealth / cfg.maxShieldHealth;
    }
}