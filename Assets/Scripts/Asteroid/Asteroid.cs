using Mirror;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Asteroid : NetworkBehaviour
{
    private Health _health;
    [SerializeField] private GameObject HitVFX;

    private void Start()
    {
        _health = GetComponent<Health>();
        _health.OnDeath += OnDie;
    }

    private void OnDie(DamageContext ctx)
    {
        GameObject vfx = Instantiate(HitVFX, transform.position, transform.rotation);
        NetworkServer.Spawn(vfx);
    }

}
