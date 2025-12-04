using Mirror;
using UnityEngine;

public interface IDieable
{
    void Die(string source = "unknown"); //пока строкой
    bool IsDead { get; } 
    void TakeDamage(float damage, string source = "unknown"); 
}
