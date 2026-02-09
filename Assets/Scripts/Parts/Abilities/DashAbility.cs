using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ship/Abilities/Dash")]
public class DashAbility : AbstractAbility
{
    public float dashVelocityBoost = 100f;

    public override AbilityRuntime CreateRuntime() => new DashRuntime(this);

    public DashAbility()
    {
        cooldown = 10f;
    }
    private class DashRuntime : AbilityRuntime
    {
        private readonly DashAbility cfg;
        public DashRuntime(DashAbility cfg) => this.cfg = cfg;

        public override void Run()
        {
            rb.AddRelativeForce(Vector3.forward * cfg.dashVelocityBoost, ForceMode.Impulse);
        }
    }
}