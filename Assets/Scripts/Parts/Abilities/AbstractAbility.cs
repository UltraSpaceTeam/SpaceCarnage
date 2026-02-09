// AbstractAbility.cs
using UnityEngine;

public abstract class AbstractAbility : ScriptableObject
{
    [Header("Config")]
    public float cooldown;

    public abstract AbilityRuntime CreateRuntime();
}