using UnityEngine;

[CreateAssetMenu(fileName = "New Ability", menuName = "Ship/Abilities/Shield")]
public class ShieldAbility : AbstractAbility
{
    public ShieldAbility()
    {
        cooldown = 1f;
    }

    public override void RunAbility(Rigidbody shipRb)
    {
        Debug.Log("Shield is on");
    }
}