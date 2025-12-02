using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ship/Abilities/Dash")]
public class DashAbility : AbstractAbility
{
    public float dashVelocityBoost = 100f;

    public DashAbility()
    {
        cooldown = 10f;
    }

    public override void RunAbility(Rigidbody shipRb)
    {
        shipRb.AddRelativeForce(Vector3.forward * dashVelocityBoost, ForceMode.Impulse);
    }
}