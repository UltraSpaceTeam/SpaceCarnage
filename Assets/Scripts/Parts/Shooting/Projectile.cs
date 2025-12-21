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
    public string ownerName;
    public string weaponName;
    private IDieable dieable;
    private Rigidbody _rb;
    private Collider _col;
    private bool _isImpacted = false;

    public void Initialize(WeaponData data, uint ownerId, string ownerName)
    {
        damage = data.damage;
        _hitVFX = data.hitVFX;
        OwnerId = ownerId;
        this.ownerName = ownerName;
        _syncSpeed = data.projectileSpeed;
        dieable = GetComponent<IDieable>();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        weaponName = data.name;

        float lifetime = data.range / Mathf.Max(data.projectileSpeed, 1f);
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
                targetDieable.TakeDamage(damage, DamageContext.Weapon(OwnerId, ownerName, weaponName));
            }
        }

        Debug.Log("XDD Hit: " + other.gameObject.name);
        if (_hitVFX != null)
        {
            GameObject vfx = Instantiate(_hitVFX, transform.position, transform.rotation);
            NetworkServer.Spawn(vfx);
        }

        dieable.Die(DamageContext.Suicide("hit"));
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
        if (!dieable.IsDead) dieable.Die(DamageContext.Suicide("timeout"));
    }

    [ClientRpc]
    public void RpcSpawnExplosion(Vector3 pos, Quaternion rot)
    {
        if (_hitVFX != null) Instantiate(_hitVFX, pos, rot);
    }
}