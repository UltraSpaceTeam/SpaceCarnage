using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(NetworkIdentity))]
public class Projectile : NetworkBehaviour
{
    [SyncVar] private float _syncSpeed;

    public float damage;
    [SerializeField] private GameObject _hitVFX;
    public GameObject HitVFX { get { return _hitVFX; } }
    public uint OwnerId { get; private set; }
    private IDieable dieable;
    private Rigidbody _rb;
    private Collider _col;
    private bool _isImpacted = false;

    public void Initialize(float damage, float speed, float lifetime, GameObject hitPrefab, uint ownerId)
    {
        this.damage = damage;
        _hitVFX = hitPrefab;
        OwnerId = ownerId;
        _syncSpeed = speed;
        dieable = GetComponent<IDieable>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();

        Invoke(nameof(TimeOut), lifetime);
    }

    public override void OnStartServer() => Launch();
    public override void OnStartClient() => Launch();

    private void Launch()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearVelocity = transform.forward * _syncSpeed;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (_isImpacted || dieable.IsDead) return;

        if (other.attachedRigidbody != null)
        {
            if (other.attachedRigidbody.TryGetComponent<NetworkIdentity>(out var hitNetIdentity))
            {
                if (hitNetIdentity.netId == OwnerId) return;
            }
            if (other.attachedRigidbody.TryGetComponent<IDieable>(out var targetDieable))
            {
                targetDieable.TakeDamage(damage, "Projectile");
            }
        }

        Debug.Log("XDD Hit: " + other.gameObject.name);
        if (_hitVFX != null)
        {
            GameObject vfx = Instantiate(_hitVFX, transform.position, transform.rotation);
            NetworkServer.Spawn(vfx);
        }

        dieable.Die("Hit");
    }

    private void StopPhysics()
    {
        _isImpacted = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.isKinematic = true;
        if (_col != null) _col.enabled = false;
    }

    [Server]
    private void TimeOut()
    {
        if (!dieable.IsDead) dieable.Die("Timeout");
    }

    [ClientRpc]
    public void RpcSpawnExplosion(Vector3 pos, Quaternion rot)
    {
        if (_hitVFX != null) Instantiate(_hitVFX, pos, rot);
    }
}