using UnityEngine;
using Mirror;
using System.Collections.Generic;

[RequireComponent(typeof(Health))]
[RequireComponent(typeof(Projectile))]
public class Rocket : NetworkBehaviour
{
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 1500f;

    [Header("Layers")]
    [SerializeField] private LayerMask proximityLayers;
    [SerializeField] private LayerMask explosionLayers;

    private Health _health;
    private Projectile _projectile;
    private bool _hasExploded = false;
    private float explosionDamage;

    private void Awake()
    {
        _health = GetComponent<Health>();
        _projectile = GetComponent<Projectile>();
        explosionDamage = _projectile.damage;
    }

    public override void OnStartServer()
    {
        _health.OnDeath += Detonate;
    }

    public override void OnStopServer()
    {
        _health.OnDeath -= Detonate;
    }

    private void FixedUpdate()
    {
        if (!isServer || _hasExploded || _health.IsDead) return;

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, proximityLayers);

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            if (col.attachedRigidbody != null &&
                col.attachedRigidbody.TryGetComponent<NetworkIdentity>(out var netId))
            {
                if (netId.netId == _projectile.OwnerId) continue;
            }

            _health.Die(DamageContext.Suicide("Proximity fuse"));
            break;
        }
    }

    [Server]
    private void Detonate(DamageContext source)
    {
        if (_hasExploded) return;
        _hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers);

        float baseDamage = _projectile.damage;
        var damaged = new HashSet<Health>();

        foreach (var hit in hits)
        {

            if (hit == null) continue;
            if (hit.gameObject == gameObject) continue;

            Health h = hit.attachedRigidbody.GetComponent<Health>();
            if (h == null) continue;
            if (!damaged.Add(h)) continue;
            var ctx = DamageContext.Weapon(_projectile.OwnerId, _projectile.ownerName, _projectile.weaponName);
            h.TakeDamage(baseDamage, ctx);

            Rigidbody rb = h.GetComponent<Rigidbody>();
            if (rb != null)
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
        }

        if (source.AttackerName != "hit")
        {
            if (_projectile.HitVFX != null)
            {
                GameObject vfx = Instantiate(_projectile.HitVFX, transform.position, transform.rotation);
                NetworkServer.Spawn(vfx);
                
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}