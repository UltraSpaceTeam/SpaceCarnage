using UnityEngine;

public abstract class AbilityRuntime
{
    protected Rigidbody rb;
    protected Player player;

    public virtual void Bind(Rigidbody ownerRb, Player ownerPlayer)
    {
        rb = ownerRb;
        player = ownerPlayer;
    }

    public abstract void Run();
    public virtual void ServerUpdate() { }
    public virtual float AbsorbDamage(float damage) => damage;
    public virtual float GetSpeedMultiplier() => 1f;
    public virtual float GetVisualStatus() => 0f;

    public virtual void OnEquipped() { }
    public virtual void OnUnequipped() { }

    public virtual void OnOwnerDamaged() { }
    public virtual void OnOwnerAttacked() { }
}
