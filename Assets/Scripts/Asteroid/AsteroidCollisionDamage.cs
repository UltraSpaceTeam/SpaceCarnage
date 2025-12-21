using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AsteroidCollisionDamage : NetworkBehaviour
{
    [SerializeField] private int damage = 100;
    
    [ServerCallback]
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Health>(out var health))
        {
            health.TakeDamage(damage, DamageContext.Environment("Meteor"));
        }
    }
}