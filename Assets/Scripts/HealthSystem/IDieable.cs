using Mirror;
using UnityEngine;

public interface IDieable
{
    void Die(); 
    bool IsDead { get; } 
    void TakeDamage(float damage); 
}
