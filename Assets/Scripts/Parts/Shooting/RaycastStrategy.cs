using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName = "Ship/Shooting/Raycast Strategy")]
public class RaycastStrategy : WeaponStrategy
{
    public override void Fire(ShipShooting shooter, Vector3 pos, Quaternion rot)
    {
        WeaponData stats = shooter.CurrentWeaponData;

        float startOffset = 2.0f;
        Vector3 physicsStartPos = pos + (rot* Vector3.forward * startOffset);

        Vector3 endPoint = physicsStartPos + (rot * Vector3.forward * stats.range);

        if (Physics.Raycast(physicsStartPos, rot * Vector3.forward, out RaycastHit hit, stats.range))
        {
            if (hit.collider.gameObject != shooter.gameObject)
            {
                endPoint = hit.point;

                Debug.Log("Raycast hit: " + hit.collider.gameObject.name);
                if (hit.collider.attachedRigidbody != null)
                {
                    if (hit.collider.attachedRigidbody.TryGetComponent<IDieable>(out var dieable))
                    {
                        dieable.TakeDamage(stats.damage);
                    }
                }

                shooter.RpcSpawnHitEffect(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        shooter.RpcSpawnBeam(pos, endPoint);
    }
}