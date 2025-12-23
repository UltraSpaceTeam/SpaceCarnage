using Mirror;
using UnityEngine;

public interface IDieable
{
    void Die(DamageContext source);
    bool IsDead { get; } 
    void TakeDamage(float damage, DamageContext source); 
}
