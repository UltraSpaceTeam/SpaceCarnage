using UnityEngine;

public abstract class AbstractAbility : ScriptableObject
{
    public float cooldown;
    public abstract void RunAbility();
}
