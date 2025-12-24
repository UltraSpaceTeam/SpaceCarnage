// AbstractAbility.cs
using UnityEngine;

public abstract class AbstractAbility : ScriptableObject
{
    public float cooldown;

    public abstract void RunAbility(Rigidbody shipRb);

    // Для щита
    public virtual void ServerUpdate(Rigidbody shipRb) { }
    public virtual float AbsorbDamage(float damage) => damage;
    public virtual float GetSpeedMultiplier() => 1f;
    public virtual void OnEquipped() { }
    public virtual void OnUnequipped() { }
}