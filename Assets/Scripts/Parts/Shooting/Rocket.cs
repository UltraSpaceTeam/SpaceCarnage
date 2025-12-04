using UnityEngine;
using Mirror;

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

            _health.Die("Proximity Fuse");
            break;
        }
    }

    [Server]
    private void Detonate(string source)
    {
        if (_hasExploded) return;
        _hasExploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, explosionLayers);
        foreach (var hit in hits)
        {
            Rigidbody targetRb = hit.attachedRigidbody;
            if (targetRb != null && targetRb.gameObject != gameObject)
            {
                if (targetRb.TryGetComponent<IDieable>(out var targetHealth))
                {
                    targetHealth.TakeDamage(explosionDamage, "Rocket Explosion");
                }
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        if (source != "Hit")
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