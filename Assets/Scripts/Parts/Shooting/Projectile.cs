using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : NetworkBehaviour
{
    [SyncVar]
    private float _syncSpeed;

    private float _damage;
    private GameObject _hitVFX;
    private uint _ownerId;

    public void Initialize(float damage, float speed, float lifetime, GameObject hitPrefab, uint ownerId)
    {
        _damage = damage;
        _hitVFX = hitPrefab;
        _ownerId = ownerId;
        _syncSpeed = speed;

        Invoke(nameof(DestroySelf), lifetime);
    }

    public override void OnStartServer()
    {
        Launch();
    }

    public override void OnStartClient()
    {
        Launch();
    }

    private void Launch()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = transform.forward * _syncSpeed;
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null)
            if (other.attachedRigidbody.TryGetComponent<NetworkIdentity>(out var hitNetIdentity))
            {
                if (hitNetIdentity.netId == _ownerId) return;
            }
        Debug.Log("XDD" + other.gameObject.name); //затычка

        DestroySelf();
    }

    [ClientRpc]
    private void RpcSpawnExplosion(Vector3 pos, Quaternion rot)
    {
    }

    [Server]
    private void DestroySelf()
    {
        NetworkServer.Destroy(gameObject);
    }
}